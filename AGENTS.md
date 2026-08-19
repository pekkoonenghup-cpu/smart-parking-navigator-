# Repository Instructions for Coding Agents

## Project overview

This repository is a prepared **Smart Parking Navigator** workshop starter. It is
an **Aspire-based .NET 10** solution with:

- **ApiApp**: ASP.NET Core backend starter in `src/CarparkAvailability.ApiApp/`
- **WebApp**: Blazor Web App frontend starter in `src/CarparkAvailability.WebApp/`
- **AppHost**: .NET Aspire orchestrator in `src/CarparkAvailability.AppHost/`
- **ServiceDefaults**: shared health, telemetry, and service-discovery setup in
  `src/CarparkAvailability.ServiceDefaults/`

Keep the current project boundaries. Do not invent extra services, databases, or
deployment architecture that are not already present in the repository.

## What exists today

- `ApiApp` is an ASP.NET Core minimal API starter with OpenAPI enabled in
  development.
- `WebApp` is a Blazor Web App using Razor components with interactive server
  components.
- `AppHost` wires `ApiApp` and `WebApp` together and passes secrets through
  environment variables.
- `ServiceDefaults` provides OpenTelemetry, health endpoints, service discovery,
  and HTTP resilience defaults.
- `data/` contains the prepared HDB dataset, sample availability payloads, and
  the published API contract/request examples.
- `docs/` contains the workshop guides, including setup, this `AGENTS.md` step,
  PRD/TRD generation, implementation, canvas, and AI-agent instructions.

This is still a starter. Do not claim that parking-search behavior, production
API contracts, or test coverage already exist unless you add and validate them.

## Stack

- .NET SDK pinned by `global.json` to **10.0.100**
- ASP.NET Core on **.NET 10**
- Blazor Web App frontend
- .NET Aspire AppHost (`Aspire.AppHost.Sdk` **13.4.6**)
- Shared OpenTelemetry/service-discovery defaults in `ServiceDefaults`

## Verified commands

Run commands from the repository root:

```bash
dotnet restore CarparkAvailability.slnx
dotnet build CarparkAvailability.slnx --no-restore --configuration Release
dotnet test CarparkAvailability.slnx --no-build --configuration Release
dotnet run --project src/CarparkAvailability.AppHost/CarparkAvailability.AppHost.csproj --no-build --configuration Release
```

Notes:

- The current starter has no test projects yet, so `dotnet test` is only a
  placeholder validation command until tests are added.
- The workshop docs also use `aspire run`, but that requires the Aspire CLI and
  supported runtime from `docs/00-setup.md`.

## Security and guardrails

- **Never commit credentials** or paste them into prompts, issues, source files,
  commits, or logs.
- `GoogleMaps:ApiKey` belongs in AppHost user secrets and is passed only to
  **WebApp** as `GoogleMaps__ApiKey`.
- `DataGovSg:ApiKey` belongs in AppHost user secrets and is passed only to
  **ApiApp** as `DataGovSg__ApiKey`.
- **Do not call data.gov.sg directly from the browser.** Browser-facing code
  must go through `WebApp` -> `ApiApp`; only `ApiApp` may call data.gov.sg.
- Follow `SECURITY.md` for vulnerability handling. Do not open public issues for
  security reports.

## Tests and documentation

- Keep changes minimal and add tests for behavior changes when test projects are
  introduced.
- Do not invent nonexistent test infrastructure or commands.
- When `PRD.md` and `TRD.md` are added, implementation, tests, and
  documentation must follow them.
- Update relevant docs when setup, behavior, or agent instructions change.

## Commit and pull request guidance

- Keep changes focused.
- Prefer Conventional Commits such as `docs:`, `feat:`, `fix:`, `test:`,
  `refactor:`, and `chore:`.
- Link the related issue in the pull request.
- Record the validation commands you ran.
- Confirm that no credentials, local data, or other sensitive information were
  committed.
