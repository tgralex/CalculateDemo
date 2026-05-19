# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

CalculateDemo is a full-stack arithmetic expression evaluator. Users type math expressions (`12 + 14/2`, `(10 + 5) * 10 / (45 + 5)`) into an Angular SPA, which sends them to an ASP.NET Core 8 API. The server sanitizes the expression (rejecting anything beyond `\d.+*/\-()\s`) and evaluates it using the NCalc library. A standalone Python implementation mirrors the same logic.

## Project Structure

```
CalculateDemo.Server/         # ASP.NET Core 8 Web API
  Classes/Calculation.cs      # Core evaluation logic (NCalc, input sanitization)
  Controllers/CalculateController.cs  # PUT /api/Calculate endpoint
  Program.cs                  # DI wiring: CORS, rate limiting, NLog, Kestrel limits
  nlog.config                 # Structured log targets (all-log, own-log, HTTP-JSON)
  appsettings.json            # Rate limit: 1 req/s on PUT /api/Calculate (production)

CalculateDemo.ServerTests/    # MSTest project referencing Server project
  Controllers/CalculationTests.cs  # Unit tests directly against Calculation class

calculatedemo.client/         # Angular 17 SPA
  src/app/
    app.component.*           # Root component: expression input, history list, predefined dropdown
    classes/calculation.ts    # DTO mirroring the server response shape
    classes/calculation-ext.ts  # Extends Calculation with async state (callInProgress, cssClass)
    services/api.service.ts   # HTTP PUT to /api/Calculate + faking mode
  src/assets/base_path.txt    # Hardcoded to https://localhost:7299 for localhost dev

Python/                       # Standalone Python implementation (same sanitization logic)
  calculate.py                # Calculator class using eval() with identical regex sanitization
  test.py                     # unittest matching the C# test cases
```

## Commands

### Backend (.NET)

```bash
# Run the server (from repo root or CalculateDemo.Server/)
dotnet run --project CalculateDemo.Server/CalculateDemo.Server.csproj

# Run all backend tests
dotnet test

# Run a single test method
dotnet test --filter "FullyQualifiedName~SuccessCalculationTest"
```

### Frontend (Angular)

```bash
cd calculatedemo.client

npm install           # first time setup
npm start             # starts Angular dev server on https://localhost:4200
npm run build         # production build to dist/
ng test               # run Karma/Jasmine tests (opens browser, watch mode)
ng test --watch=false # single-run tests
```

### Python

```bash
cd Python
python -m unittest test.py
```

## Architecture Notes

### Development Mode: Dual-Server Setup
In development, two servers run concurrently. The .NET server (`https://localhost:7299`) auto-launches the Angular dev server (`https://localhost:4200`) via `SpaProxyLaunchCommand` in the `.csproj`. CORS is allowed from port 4200 only in the Development environment.

`ApiService` reads `src/assets/base_path.txt` (value: `https://localhost:7299`) when on localhost to build the full API URL. On non-localhost deployments, `basePath` is set to `""` so the Angular app calls the same origin.

### Request Lifecycle
1. Angular `AppComponent.calculate()` → creates a `CalculationExt` (fires the HTTP call in its constructor)
2. `CalculationExt` extends `Calculation` with `callInProgress`, `cssClass`, and async subscription lifecycle
3. `ApiService.calculate()` → `PUT /api/Calculate` with body `{ expression: string }`
4. `CalculateController` → `new Calculation(expression)` (on error, returns a default failed `Calculation`)
5. `Calculation` sanitizes via regex, evaluates via NCalc, rejects `Infinity`/`NaN`

### Input Sanitization Contract
Both the C# and Python implementations use the same regex: `[^\d.+*/\-()\s]`. Any character outside digits, `.`, `+`, `*`, `/`, `-`, `(`, `)`, and whitespace causes immediate rejection. NCalc functions (Pi, Ln, etc.) are intentionally blocked by this sanitization even though NCalc supports them.

### Rate Limiting
Production `appsettings.json` sets 1 request/second for `PUT:/api/Calculate`. Development overrides this via `appsettings.Development.json`. The limit is configurable via `IpRateLimitOptions:Limit` and `IpRateLimitOptions:PerPeriod` in config.

### Faking Mode
Clicking the `<h3>` title in the Angular app toggles a client-side fake mode (`ApiService.fakeIt()`). It returns a fixed result (20), adds artificial delay (1s), and fails every 3rd request — useful for testing UI states without a running server.

### Logging
NLog writes three log files to `Logs/`: all-messages, own-messages (excludes Microsoft.*), and HTTP requests as JSON. The internal NLog log goes to `c:\temp\` (Windows-oriented path).
