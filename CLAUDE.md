# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

The solution file is `AstroClockExporter.slnx` (SLNX, not .sln).

```sh
dotnet restore AstroClockExporter.slnx
dotnet build AstroClockExporter.slnx -c Release
dotnet test AstroClockExporter.slnx -c Release
dotnet format --verify-no-changes              # what CI runs as the "Lint" step
dotnet list package --include-transitive --vulnerable
```

Run a single test:

```sh
dotnet test AstroClockExporter.slnx --filter "FullyQualifiedName~AstroCalculatorTests.Calculate_WarsawSummerSolsticeNoon"
```

Run the Api locally:

```sh
dotnet run --project AstroClockExporter.Api
# or build the AOT container — see README "Build & run"
```

CI gates (`.github/workflows/dotnet.yml`): build with `TreatWarningsAsErrors`, vulnerable-package scan, `dotnet test`
with cobertura coverage, **coverage must exceed 0.63**, and `dotnet format --verify-no-changes` must pass.

## Architecture

Three-project layout — Api (entry point), Core (all logic), Tests.

**Composition root** is `AstroClockExporter.Core.Extensions.AddCore()` (`AstroClockExporter.Core/Extensions.cs`).
All interfaces (`IConfigReader`, `IConfigProvider`, `IAstroCalculator`, `IMetricsExporter`, `TimeProvider`) are
registered as singletons there. `Program.cs` does only HTTP wiring; do not register Core services in `Program.cs`.

**Request path** (`/metrics?location=<name>`):
`Program.cs` → `IMetricsExporter.ExportMetrics(name)` → `IConfigProvider.GetLocation(name)` →
`IAstroCalculator.Calculate(location, utcNow)` → Prometheus text formatted by hand in `PrometheusMetricsExporter`
(no library — per-type rounding: 2 dp for angles, 4 dp for fractions, integer for timestamps/distance;
`long.MinValue` sentinel maps to `NaN`; explicit `+Inf`/`-Inf`). Location lookup is case-insensitive
(ASCII only — see README caveat).

**Config loading** happens once at startup in `Program.cs` (`await IConfigProvider.LoadAsync(...)`), not per-request.
`${VAR}` / `${VAR:-default}` substitution runs over the raw YAML text in `ConfigProvider.ReplaceEnvVariables` before
deserialization. `CONFIG_FILE` env var overrides the default `config.yml` path.

**Error handling** uses `Result<T>` from `AstroClockExporter.Core.Common` — methods that can fail return
`Result<T>.Success(value)` or `Result<T>.Fail(httpStatusCode, message)`. The endpoint maps `result.Err.Code`
directly to the HTTP status; preserve that convention when adding new failure modes.

## AOT and serialization

`AstroClockExporter.Api` sets `PublishAot=true`; `AstroClockExporter.Core` sets `IsAotCompatible=true`.
Avoid runtime reflection, dynamic code, and reflection-based JSON/YAML.

YAML deserialization uses YamlDotNet's **static source generator** via `Vecc.YamlDotNet.Analyzers.StaticGenerator`.
When adding a new DTO type used in `config.yml`, register it in
`AstroClockExporter.Core/Configuration/Serialization/YamlConfigStaticContext.cs` with a
`[YamlSerializable(typeof(NewType))]` attribute on the partial class — otherwise the deserializer will fail at
runtime under AOT.

## Package management

**Central Package Management** is enabled — `Directory.Packages.props` at the repo root holds every package version.
Bump versions there, never in `.csproj` files. `PackageReference` entries in csprojs must not carry a `Version`
attribute. Shared MSBuild properties (`TargetFramework=net10.0`, `Nullable`, `ImplicitUsings`,
`TreatWarningsAsErrors`) live in `Directory.Build.props`.

## Testing conventions

xUnit + NSubstitute + Verify (snapshot tests). Snapshots live next to tests as `*.verified.txt` and must be
committed. To accept changed output, replace `.received.txt` with `.verified.txt` (or use a Verify diff tool).

Note: `Verify.Xunit` exposes `VerifyTests.TempDirectory` which collides with the repo's own
`AstroClockExporter.Tests.Helpers.TempDirectory`. Test files using the local helper need a using-alias:
`using TempDirectory = AstroClockExporter.Tests.Helpers.TempDirectory;`.
See `AstroClockExporter.Tests/Configuration/ConfigProviderTests.cs` for the pattern.

`AstroClockExporter.Core` and `.Api` expose internals to `AstroClockExporter.Tests` via `InternalsVisibleTo`,
so tests can target internal types directly.

## PII

Do not include any PII (names, emails, addresses, personal coordinates, account identifiers, etc.) in code, config
samples, tests, snapshots, commit messages, or docs. Example locations in `config.yml` and tests must use public
landmarks or generic city names (e.g. `warsaw`, `reykjavik`) — never a personal home location.

## Markdown conventions

Wrap Markdown prose lines at 120 characters for readability. Exception: table rows must not be wrapped
(breaking a `|`-delimited row destroys the table).

## Coding conventions

Do not use magic numbers inline. Every non-obvious numeric literal must be extracted to a named `const` with a name
that conveys its physical or domain meaning (e.g. `SecondsPerDay`, `JdUnixEpoch`, `DegreesPerHour`). Pure structural
literals (`0`, `1`, `2`) used as indices or loop bounds are exempt.

## README

User-facing docs (metrics catalogue, Prometheus scrape config, Docker run, env vars) live in `README.md`.
Update it when adding metrics, endpoints, or env vars.

## Releasing

Releases are gated on creating a GitHub Release. The `.github/workflows/docker-publish.yml` workflow listens for
`release: published` and builds + pushes a Docker image to `docker.io/majkel89/astro-clock-exporter`.

To cut a new version `vX.Y.Z`:

1. Ensure everything to ship is merged to `main`.
2. Create the release (this creates the tag and fires the workflow):

   ```sh
   gh release create vX.Y.Z --target main --title "vX.Y.Z" --generate-notes
   ```

   Mark it as pre-release if it should not move the `latest` tag.

3. The workflow pushes these tags to Docker Hub: `vX.Y.Z`, `X.Y.Z`, `X.Y`, `latest` (stable releases only),
   `sha-<short>`. The README references `:latest` so docs stay current automatically — no commit-back from CI.
4. The workflow appends a "Docker image" section to the GitHub Release notes containing the immutable
   `sha256:...` digest and a copy-pasteable pinned-pull command.
5. Verify with `gh run watch` and a clean `docker pull majkel89/astro-clock-exporter:vX.Y.Z`.

If the publish fails after the release was created, fix the cause then re-run via the Actions tab using the
workflow's `workflow_dispatch` input (the existing release tag) — no need to delete and re-cut the release.

Required repo secrets: `DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN` (token scoped Read/Write/Delete on
`majkel89/astro-clock-exporter`).
