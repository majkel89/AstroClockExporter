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

CI gates (`.github/workflows/dotnet.yml`): build with `TreatWarningsAsErrors`, vulnerable-package scan, `dotnet test` with cobertura coverage, **coverage must exceed 0.63**, and `dotnet format --verify-no-changes` must pass.

## Architecture

Three-project layout — Api (entry point), Core (all logic), Tests.

**Composition root** is `AstroClockExporter.Core.Extensions.AddCore()` (`AstroClockExporter.Core/Extensions.cs`). All interfaces (`IConfigReader`, `IConfigProvider`, `IAstroCalculator`, `IMetricsExporter`, `TimeProvider`) are registered as singletons there. `Program.cs` does only HTTP wiring; do not register Core services in `Program.cs`.

**Request path** (`/metrics?location=<name>`):
`Program.cs` → `IMetricsExporter.ExportMetrics(name)` → `IConfigProvider.GetLocation(name)` → `IAstroCalculator.Calculate(location, utcNow)` → Prometheus text formatted by hand in `PrometheusMetricsExporter` (no library — invariant-culture `"R"` formatting, explicit `NaN`/`+Inf`/`-Inf`). Location lookup is case-insensitive (ASCII only — see README caveat).

**Config loading** happens once at startup in `Program.cs` (`await IConfigProvider.LoadAsync(...)`), not per-request. `${VAR}` / `${VAR:-default}` substitution runs over the raw YAML text in `ConfigProvider.ReplaceEnvVariables` before deserialization. `CONFIG_FILE` env var overrides the default `config.yml` path.

**Error handling** uses `Result<T>` from `AstroClockExporter.Core.Common` — methods that can fail return `Result<T>.Success(value)` or `Result<T>.Fail(httpStatusCode, message)`. The endpoint maps `result.Err.Code` directly to the HTTP status; preserve that convention when adding new failure modes.

## AOT and serialization

`AstroClockExporter.Api` sets `PublishAot=true`; `AstroClockExporter.Core` sets `IsAotCompatible=true`. Avoid runtime reflection, dynamic code, and reflection-based JSON/YAML.

YAML deserialization uses YamlDotNet's **static source generator** via `Vecc.YamlDotNet.Analyzers.StaticGenerator`. When adding a new DTO type used in `config.yml`, register it in `AstroClockExporter.Core/Configuration/Serialization/YamlConfigStaticContext.cs` with a `[YamlSerializable(typeof(NewType))]` attribute on the partial class — otherwise the deserializer will fail at runtime under AOT.

## Package management

**Central Package Management** is enabled — `Directory.Packages.props` at the repo root holds every package version. Bump versions there, never in `.csproj` files. `PackageReference` entries in csprojs must not carry a `Version` attribute. Shared MSBuild properties (`TargetFramework=net10.0`, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`) live in `Directory.Build.props`.

## Testing conventions

xUnit + NSubstitute + Verify (snapshot tests). Snapshots live next to tests as `*.verified.txt` and must be committed. To accept changed output, replace `.received.txt` with `.verified.txt` (or use a Verify diff tool).

Note: `Verify.Xunit` exposes `VerifyTests.TempDirectory` which collides with the repo's own `AstroClockExporter.Tests.Helpers.TempDirectory`. Test files using the local helper need a using-alias: `using TempDirectory = AstroClockExporter.Tests.Helpers.TempDirectory;`. See `AstroClockExporter.Tests/Configuration/ConfigProviderTests.cs` for the pattern.

`AstroClockExporter.Core` and `.Api` expose internals to `AstroClockExporter.Tests` via `InternalsVisibleTo`, so tests can target internal types directly.

## PII

Do not include any PII (names, emails, addresses, personal coordinates, account identifiers, etc.) in code, config samples, tests, snapshots, commit messages, or docs. Example locations in `config.yml` and tests must use public landmarks or generic city names (e.g. `warsaw`, `reykjavik`) — never a personal home location.

## README

User-facing docs (metrics catalogue, Prometheus scrape config, Docker run, env vars) live in `README.md`. Update it when adding metrics, endpoints, or env vars.
