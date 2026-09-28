# Repository guidance

## Project structure

This is a .NET 10 service for programming olympiad results. The solution is
`Texnokaktus.ProgOlymp.ResultService.slnx`.

- `src/Texnokaktus.ProgOlymp.ResultService`: ASP.NET Core host, HTTP endpoints,
  gRPC services, application logic, API models, and ranking extensions.
- `src/Texnokaktus.ProgOlymp.ResultService.Domain`: domain models for results,
  scores, participants, rankings, and disqualifications.
- `src/Texnokaktus.ProgOlymp.ResultService.DataAccess`: EF Core entities,
  `AppDbContext`, registration, and migrations. The application uses SQL Server.
- `src/Texnokaktus.ProgOlymp.ResultService.Infrastructure`: participant service
  client abstraction, implementation, and dependency injection registration.
- `modules/Common.Contracts`: separate Git submodule containing shared contracts.
  Keep changes here deliberate; do not casually update the submodule revision or
  edit generated contract output.
- `tests/Texnokaktus.ProgOlymp.ResultService.Tests`: unit tests for ranking.
- `tests/Texnokaktus.ProgOlymp.ResultService.IntegrationTests`: gRPC integration
  tests and reusable data builders.

## Setup and validation

Use the .NET 10 SDK. `global.json` selects Microsoft.Testing.Platform but does
not pin an SDK version. Run commands from the repository root:

```powershell
git submodule update --init --recursive
dotnet restore
dotnet build --no-restore
dotnet test --no-build --verbosity normal
```

Initialize the submodule when it is missing. Restore requires access to the
GitHub Packages NuGet feed at
`https://nuget.pkg.github.com/Texnokaktus-ProgOlymp/index.json` for the private
Texnokaktus packages. Use existing local credentials; never commit tokens.
The workflows in `.github/workflows` document CI setup and coverage arguments.

For a focused test run, use Microsoft.Testing.Platform project selection:

```powershell
dotnet test --project tests/Texnokaktus.ProgOlymp.ResultService.Tests/Texnokaktus.ProgOlymp.ResultService.Tests.csproj
dotnet test --project tests/Texnokaktus.ProgOlymp.ResultService.IntegrationTests/Texnokaktus.ProgOlymp.ResultService.IntegrationTests.csproj
```

Build and run relevant tests for code changes. Use `--no-build` only after a
successful build of the current changes. Report any validation blocked by
package access or environment issues.

## Implementation conventions

- Match nearby C# style: file-scoped namespaces, four-space indentation,
  PascalCase members/types, camelCase locals/parameters, and modern C# syntax.
- Nullable reference types and implicit usings are enabled. Warnings are errors;
  fix causes instead of broadly suppressing diagnostics.
- Package versions belong in root `Directory.Packages.props`; project files use
  versionless `PackageReference` entries. The contracts submodule has its own
  package configuration.
- Keep persistence entities, domain models, and HTTP models in their respective
  layers. Follow existing service abstractions and DI registration patterns.
- Keep EF Core migrations and the model snapshot consistent with schema changes.
- Preserve HTTP/gRPC contract behavior and established RPC error handling when
  changing service logic.

## Testing conventions

Tests use NUnit and `Assert.That`; substitutes use NSubstitute. Follow existing
test names describing the scenario and expected outcome.

Integration tests use `CustomWebApplicationFactory` in the `Testing` environment,
an in-memory SQLite database, and a substituted participant gRPC client. The
host skips platform initialization in this environment. Reuse the existing
setup and data builders; these tests do not require a live SQL Server or
participant service.

Keep changes scoped to the task. Do not modify personal IDE settings or commit
build output, test results, credentials, or unrelated local files.
