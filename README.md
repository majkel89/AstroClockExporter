# AstroClockExporter

Prometheus exporter exposing **sun and moon position** metrics (azimuth, altitude, sunrise/sunset,
civil/nautical/astronomical, dawn/dusk, moonrise/moonset, lunar illumination) for named geographic locations.

Built in C# .NET 10 with NativeAOT — ~16 MB native binary, ~32 MB RAM. The astronomical math is delegated to
[AASharp](https://github.com/jsauve/AASharp), a C# port of Naughter's [AA+](http://www.naughter.com/aa.html)
(Meeus's *Astronomical Algorithms*).

## Usage

Locations are defined in `config.yml`:

```yaml
locations:
  Warsaw:
    latitude: 52.2297
    longitude: 21.0122
    elevation: 110
```

Then scrape with the location name as a query param:

```
GET /metrics?location=warsaw
```

Location names are matched case-insensitively, so `warsaw`, `Warsaw`, and `WARSAW` all resolve to the same entry.
The match uses ordinal case folding, which covers ASCII A–Z only — non-ASCII letters (e.g. `Łódź` vs `łódź`) are
**not** folded and must match exactly.

## Endpoints

| Path | Description |
|------|-------------|
| `GET /metrics?location=<name>` | Prometheus exposition for the named location. 400 if `location` missing, 404 if name unknown. |
| `GET /healthy` | Returns the literal string `healthy` for container healthchecks. |
| `GET /` | Returns the literal string `AstroClockExporter`. |

## Metrics

All event timestamps are the **next** occurrence within 24h of the scrape, expressed in Unix seconds (UTC).
`NaN` if the event does not occur in that window (e.g. polar day/night, latitudes too high for astronomical
darkness in midsummer).

| Metric | Type | Description |
|--------|------|-------------|
| `astro_sun_altitude_degrees` | gauge | Current sun altitude above the horizon |
| `astro_sun_azimuth_degrees` | gauge | Current sun azimuth (clockwise from north) |
| `astro_moon_altitude_degrees` | gauge | Current moon altitude above the horizon |
| `astro_moon_azimuth_degrees` | gauge | Current moon azimuth (clockwise from north) |
| `astro_moon_illumination_fraction` | gauge | Fraction of moon disk illuminated (0..1) |
| `astro_moon_phase_angle_degrees` | gauge | Moon phase angle (0 = full, 180 = new) |
| `astro_moon_distance_km` | gauge | Earth-Moon centre-to-centre distance (km) |
| `astro_sun_event_time_seconds{event="sunrise\|sunset\|solar_noon\|civil_dawn\|civil_dusk\|nautical_dawn\|nautical_dusk\|astronomical_dawn\|astronomical_dusk"}` | gauge | Next sun event time (Unix seconds, UTC) |
| `astro_moon_event_time_seconds{event="moonrise\|moonset\|moon_transit"}` | gauge | Next moon event time (Unix seconds, UTC) |
| `astro_calculation_seconds` | gauge | Wall-clock time spent computing the scrape |

## Precision

Output values are rounded to reflect the actual accuracy of the underlying Meeus algorithms:

| Value type | Output precision | Algorithm accuracy |
|---|---|---|
| Angles (altitude, azimuth, phase angle) | 2 decimal places (0.01°) | Truncated VSOP87: ~36 arcseconds ≈ 0.01° |
| Illumination fraction | 4 decimal places (0.0001) | Propagated from position error (~0.00009 max) |
| Moon distance | Integer kilometres | 60-term series: ±10 km |
| Event timestamps | Integer Unix seconds | Rise/set step resolution ~10 minutes |
| `astro_calculation_seconds` | 4 decimal places | Real wall-clock measurement |

Digits beyond these bounds would reflect floating-point rounding noise, not physical accuracy.

## Build & run

Pull the published image from Docker Hub:

```sh
docker pull majkel89/astro-clock-exporter:latest
docker run --rm -p 8080:8080 -v $PWD/AstroClockExporter.Api/config.yml:/app/config.yml:ro \
  majkel89/astro-clock-exporter:latest
curl 'http://localhost:8080/metrics?location=warsaw'
```

For reproducible deployments, pin to a specific version (e.g. `:v0.1.1`) or to an immutable digest — see the
[tags on Docker Hub](https://hub.docker.com/r/majkel89/astro-clock-exporter/tags) and each GitHub Release's notes
for the published `sha256:...` digest.

Or build locally:

```sh
docker build -t astro-clock-exporter -f AstroClockExporter.Api/Dockerfile .
docker run --rm -p 8080:8080 -v $PWD/AstroClockExporter.Api/config.yml:/app/config.yml:ro astro-clock-exporter
```

Or via compose:

```sh
docker compose up --build
```

## Prometheus scrape config

Because `location` is a query parameter, each location is a separate scrape target. List the locations as targets
and use `relabel_configs` to rewrite each into the `location` query param:

```yaml
scrape_configs:
  - job_name: astro_clock
    metrics_path: /metrics
    static_configs:
      - targets:
          - warsaw
          - reykjavik
          - tromso
    relabel_configs:
      - source_labels: [__address__]
        target_label: __param_location
      - source_labels: [__param_location]
        target_label: location
      - target_label: __address__
        replacement: astro-clock-exporter:8080
```

Each target produces a series with a `location="<name>"` label, all scraped from the single exporter instance.

For a one-off scrape with a single hardcoded location, the simpler form is:

```yaml
scrape_configs:
  - job_name: astro_clock_warsaw
    metrics_path: /metrics
    params:
      location: [warsaw]
    static_configs:
      - targets: [astro-clock-exporter:8080]
```

## Configuration env vars

| Variable | Default | Purpose |
|----------|---------|---------|
| `CONFIG_FILE` | `config.yml` | Path to the locations config file |
| `ASPNETCORE_URLS` | `http://0.0.0.0:8080/` | Kestrel listen URL |
| `Logging__LogLevel__AstroClockExporter.Core.Prometheus.PrometheusMetricsExporter` | `Information` | Per-scrape log verbosity |

Environment variables of the form `${VAR}` or `${VAR:-default}` inside `config.yml` are substituted at startup.
