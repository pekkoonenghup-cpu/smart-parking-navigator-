# Technical Requirements Document — Smart Parking Navigator

| Field        | Value                              |
|--------------|------------------------------------|
| Version      | 1.0.0                              |
| Status       | Draft                              |
| Author       | @copilot                           |
| Reviewed by  | —                                  |
| Created      | 2026-08-19                         |
| Last updated | 2026-08-19                         |
| Issue        | #2                                 |
| PRD          | [PRD.md](PRD.md) v1.0.0            |

---

## 1. Purpose

This document defines the technical design for the Smart Parking Navigator MVP.  It maps each PRD requirement to a concrete implementation approach within the existing .NET Aspire solution, specifies component responsibilities, data contracts, and error-handling rules, and provides a test strategy.

No application behaviour is implemented in this document.  All design decisions are constrained to the scaffolded projects listed in section 2.

---

## 2. Solution Structure

The solution is defined in `CarparkAvailability.slnx` and contains exactly four projects.  No new projects are added as part of this MVP.

```
CarparkAvailability.slnx
└── src/
    ├── CarparkAvailability.AppHost/        # Aspire orchestrator
    ├── CarparkAvailability.ApiApp/         # ASP.NET Core Minimal API backend
    ├── CarparkAvailability.WebApp/         # Blazor Server frontend
    └── CarparkAvailability.ServiceDefaults/# Shared Aspire service configuration
```

---

## 3. Component Responsibilities

### 3.1 AppHost (`CarparkAvailability.AppHost`)

| Responsibility | Detail |
|----------------|--------|
| Orchestration | Starts and stops ApiApp and WebApp as a single Aspire application |
| Secret injection | Reads `GoogleMaps:ApiKey` and `DataGovSg:ApiKey` from `appsettings.json` / user secrets; injects each as an environment variable into the correct project only |
| Service discovery | Provides the ApiApp base URL to WebApp via Aspire's `WithReference` mechanism |
| Credential boundary enforcement | `GoogleMaps__ApiKey` → WebApp only; `DataGovSg__ApiKey` → ApiApp only |

### 3.2 ApiApp (`CarparkAvailability.ApiApp`)

| Responsibility | Detail |
|----------------|--------|
| HDB CSV ingestion | Parse and validate `HDBCarparkInformation.csv` at startup into an in-memory dictionary keyed by `car_park_no` |
| SVY21 → WGS84 conversion | Convert each car park's `x_coord` / `y_coord` to WGS84 at parse time; store both representations |
| Live availability polling | Background service polls `https://api.data.gov.sg/v1/transport/carpark-availability` every 60 s; stores last-known-good response in memory |
| Joining | On each poll, join live records (`carpark_number`) with the static dictionary (`car_park_no`) |
| Distance calculation | For each `/api/carparks` request, compute Haversine distance between the destination coordinates and each car park's WGS84 position; return only those ≤ 500 m |
| API endpoints | Expose the endpoints defined in section 5 |
| Error handling | Follow the rules in section 7 |
| Credential protection | Use `DataGovSg__ApiKey` from environment only; never return it in any response |

### 3.3 WebApp (`CarparkAvailability.WebApp`)

| Responsibility | Detail |
|----------------|--------|
| Destination search UI | Razor component wrapping Google Maps Places Autocomplete, restricted to `SG` |
| Map display | Google Maps JavaScript SDK embedded via a `<script>` tag using `GoogleMaps__ApiKey` from environment; renders car park markers |
| Results list | Razor component that renders the list of nearby car parks returned by ApiApp |
| Freshness indicators | Client-side logic derives freshness state from `updateDatetime` and current browser time |
| Auto-refresh | Calls `/api/carparks` every 60 s using a Blazor timer; updates the displayed data without full page reload |
| Filter panel | Client-side filtering applied to the in-memory list returned by the last API call |
| Detail panel | Displays all fields from the API response for a selected car park |
| Error and stale banners | Renders appropriate UI for each state defined in PRD section 5.4 |
| Credential protection | Uses `GoogleMaps__ApiKey` from environment only for the Maps SDK script tag; never sends it to ApiApp or includes it in any server-to-server call |

### 3.4 ServiceDefaults (`CarparkAvailability.ServiceDefaults`)

| Responsibility | Detail |
|----------------|--------|
| OpenTelemetry | Configures tracing, metrics, and logging for ApiApp and WebApp via `AddServiceDefaults()` |
| Health checks | Registers `/healthz` (live) and `/alive` endpoints on both projects via `MapDefaultEndpoints()` |
| Resilience | Configures `AddStandardResilienceHandler()` on all outbound `HttpClient` instances registered through `AddServiceDefaults()` |

---

## 4. Data Pipeline

### 4.1 HDB CSV ingestion

**File:** `data/HDBCarparkInformation.csv`

**Columns and types:**

| Column | Type | Notes |
|--------|------|-------|
| `car_park_no` | `string` | Primary join key; trim whitespace |
| `address` | `string` | Display as-is |
| `x_coord` | `double` | SVY21 easting; convert to WGS84 longitude |
| `y_coord` | `double` | SVY21 northing; convert to WGS84 latitude |
| `car_park_type` | `string` | One of: `SURFACE CAR PARK`, `MULTI-STOREY CAR PARK`, `BASEMENT CAR PARK`, `COVERED CAR PARK`, `MECHANISED AND SURFACE CAR PARK`, `MECHANISED CAR PARK` |
| `type_of_parking_system` | `string` | `ELECTRONIC PARKING` or `COUPON PARKING` |
| `short_term_parking` | `string` | Reproduce verbatim |
| `free_parking` | `string` | Reproduce verbatim; do not interpret |
| `night_parking` | `string` | `YES` or `NO` |
| `car_park_decks` | `int` | Parse as integer; 0 = not applicable |
| `gantry_height` | `double` | Metres; 0.0 = not applicable |
| `car_park_basement` | `string` | `Y` or `N` |

**Validation rules:**
- Skip rows where `car_park_no` is blank or whitespace; log a warning.
- Skip rows where `x_coord` or `y_coord` cannot be parsed as `double`; log a warning with the row number.
- Duplicate `car_park_no` values: keep the first occurrence and log a warning.
- The CSV must contain at least one valid row; if zero rows are parsed, fail startup with a descriptive error.

**Load strategy:** Read synchronously during `IHostedService.StartAsync` before the application begins serving requests.  If the file is not found, fail startup.

### 4.2 SVY21 to WGS84 conversion

SVY21 is a Transverse Mercator projection with the following parameters (Onemap/SLA specification):

| Parameter | Value |
|-----------|-------|
| Semi-major axis (a) | 6378137.0 m |
| Flattening (1/f) | 298.257223563 |
| Origin latitude (φ₀) | 1.366666° N |
| Origin longitude (λ₀) | 103.833333° E |
| False northing (N₀) | 38744.572 m |
| False easting (E₀) | 28001.642 m |
| Scale factor (k₀) | 1.0 |

The conversion must be performed using the closed-form inverse Transverse Mercator formula.  A pure-C# implementation is required (no runtime HTTP calls or external services for coordinate conversion).  The converted coordinates are cached in the in-memory dictionary at startup.

**Accuracy target:** Results must match the Onemap SVY21 converter to within ±0.5 m.

### 4.3 Live availability polling

**Endpoint:** `GET https://api.data.gov.sg/v1/transport/carpark-availability`

**Request:**
- No `date_time` parameter (retrieves current availability).
- Optionally include `x-api-key` header using `DataGovSg__ApiKey` if set.

**Expected response shape** (from `data/CarparkAvailability.json`):

```json
{
  "api_info": { "status": "healthy" },
  "items": [
    {
      "timestamp": "2025-01-01T00:00:00+08:00",
      "carpark_data": [
        {
          "carpark_number": "ACB",
          "update_datetime": "2025-01-01T00:00:00",
          "carpark_info": [
            {
              "lot_type": "C",
              "lots_available": "100",
              "total_lots": "200"
            }
          ]
        }
      ]
    }
  ]
}
```

**Parsing rules:**
- `lots_available` and `total_lots` are strings; parse to `int` with `int.TryParse`; treat parse failure as 0 and log a warning.
- `update_datetime` is a local Singapore time string without timezone suffix; parse as `DateTime` and attach `+08:00` offset.
- The `items` array is expected to contain exactly one element in live responses; use `items[0]`.  If empty, treat as an API error.
- Unknown properties must be ignored (forward-compatible parsing).
- Missing required fields (`carpark_number`, `carpark_info`) cause the affected record to be skipped; log a warning.

**Polling behaviour:**
- A `BackgroundService` fires the first poll immediately on startup, then every 60 seconds.
- A successful response replaces the in-memory store atomically (swap reference under lock or `Interlocked`).
- A failed response (non-2xx status, timeout, or deserialization error) leaves the last-known-good store unchanged.
- The in-memory store records: the data array, the wall-clock time of the last successful poll (`lastSuccessfulPollUtc`), and the wall-clock time of the last attempted poll.

### 4.4 Joining

At query time (not at poll time), the API handler joins the static dictionary and the live store:

```
for each live carpark_data record:
    look up car_park_no in static dictionary
    if found: merge static fields + live lot counts
    if not found: include live record with static fields as null
    
for each static record not matched by any live record:
    include with static fields only; mark live data as unavailable
```

The result is filtered by distance before being returned.

### 4.5 Distance calculation

Use the **Haversine formula** with Earth radius = **6371000 m**.

```
Δlat = lat2 − lat1  (radians)
Δlon = lon2 − lon1  (radians)
a = sin²(Δlat/2) + cos(lat1)·cos(lat2)·sin²(Δlon/2)
c = 2·atan2(√a, √(1−a))
distance = R·c
```

Include a car park if `distance ≤ 500 m`.  Sort results by distance ascending.

---

## 5. API Boundaries

### 5.1 GET `/api/carparks`

Returns HDB car parks within 500 m of the given destination, joined with the latest live availability.

**Query parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `lat` | `double` | Yes | Destination WGS84 latitude |
| `lon` | `double` | Yes | Destination WGS84 longitude |

**Validation:**
- `lat` must be in `[1.1, 1.5]` (Singapore bounding box, degrees N).
- `lon` must be in `[103.6, 104.1]` (Singapore bounding box, degrees E).
- Return `400 Bad Request` with a problem-details body for out-of-range or missing values.

**Success response (`200 OK`):**

```json
{
  "lastSuccessfulPollUtc": "2025-01-01T00:00:00Z",
  "carparks": [
    {
      "carparkNo": "ACB",
      "address": "BLK 270/271 ALBERT CENTRE BASEMENT CAR PARK",
      "latitude": 1.3010,
      "longitude": 103.8520,
      "carparkType": "BASEMENT CAR PARK",
      "typeOfParkingSystem": "ELECTRONIC PARKING",
      "shortTermParking": "WHOLE DAY",
      "freeParking": "NO",
      "nightParking": true,
      "carparkDecks": 1,
      "gantryHeight": 1.8,
      "carparkBasement": true,
      "distanceMetres": 42.3,
      "liveData": {
        "updateDatetime": "2025-01-01T00:00:00+08:00",
        "lots": [
          {
            "lotType": "C",
            "lotsAvailable": 100,
            "totalLots": 200
          }
        ]
      }
    }
  ]
}
```

- `liveData` is `null` when no live record exists for the car park.
- `lastSuccessfulPollUtc` is `null` when no successful poll has occurred in the current process lifetime.

**Error responses:**

| Status | Condition |
|--------|-----------|
| `400 Bad Request` | Missing or out-of-range `lat`/`lon` |
| `500 Internal Server Error` | Unexpected exception (problem-details body) |

### 5.2 GET `/api/health` (via `MapDefaultEndpoints`)

Standard Aspire health endpoint.  Returns `200 OK` when the process is live.

---

## 6. Google Maps Integration

- The Maps JavaScript SDK is loaded in `WebApp` only, via a `<script>` tag that includes `GoogleMaps__ApiKey` read from the environment at server render time.
- The API key is rendered into the script URL on the server; it is never stored in a Blazor component state or returned as JSON.
- The Places Autocomplete widget is configured with `componentRestrictions: { country: 'SG' }`.
- When the user selects a place, the Blazor component extracts `geometry.location.lat()` and `geometry.location.lng()` and passes them to the result-loading logic.
- The map displays a destination pin and car park markers.  Marker click opens the detail panel.

---

## 7. Error Handling

### 7.1 Startup failures

| Condition | Behaviour |
|-----------|-----------|
| CSV file not found | Log error, throw `InvalidOperationException`, abort startup |
| CSV has zero valid rows | Log error, throw `InvalidOperationException`, abort startup |
| SVY21 conversion produces NaN or out-of-range WGS84 for a row | Log warning, skip the row, continue |

### 7.2 Runtime — ApiApp

| Condition | Behaviour |
|-----------|-----------|
| data.gov.sg returns non-2xx | Log warning with status code; retain last-known-good store |
| data.gov.sg request times out (15 s) | Log warning; retain last-known-good store |
| JSON deserialization error | Log warning with exception; retain last-known-good store |
| No successful poll since startup | `lastSuccessfulPollUtc` is `null` in the response |
| Individual carpark record parse failure | Skip record; log warning; continue with remaining records |

### 7.3 Runtime — WebApp

| Condition | Behaviour |
|-----------|-----------|
| ApiApp returns non-2xx | Display app-level error banner; do not clear existing results if any |
| ApiApp request times out | Same as non-2xx |
| `liveData` is `null` for a car park | Show "No live data" label; still display static fields |
| `lastSuccessfulPollUtc` is `null` | Show "Live data not yet available" banner |
| `lastSuccessfulPollUtc` > 5 min ago | Show stale-data banner with age of data |

---

## 8. Security

| Control | Detail |
|---------|--------|
| API keys in environment variables | Both keys are injected by AppHost via `WithEnvironment`; never committed to source |
| User secrets in development | Developers configure keys with `dotnet user-secrets` as documented in `docs/` |
| No key in API responses | ApiApp must not echo `DataGovSg__ApiKey` in any response body or header |
| No key in JS bundles | `GoogleMaps__ApiKey` is rendered server-side into the script URL only; it is not stored in client-side state |
| Problem details | ApiApp uses `AddProblemDetails()` and `UseExceptionHandler()`; internal exception messages are not exposed in production |
| HTTPS | AppHost enforces HTTPS for WebApp external endpoints; `UseHttpsRedirection` is configured in WebApp |

---

## 9. Accessibility

- All interactive controls (search input, filter checkboxes, map markers, list items) must have accessible labels (`aria-label` or associated `<label>`).
- Freshness indicators must convey state via text or `aria-label`, not colour alone.
- The detail panel must be keyboard-accessible (focusable, closeable with `Escape`).
- Colour contrast must meet WCAG 2.1 AA (4.5:1 for normal text, 3:1 for large text).

---

## 10. Test Strategy

Tests are written using xUnit and placed in a `tests/` folder at the solution root.  No test project exists yet; the implementation issue must create it.

### 10.1 Unit tests — ApiApp

| Test area | What to test |
|-----------|-------------|
| CSV parser | Happy path: all columns parsed correctly from a representative row |
| CSV parser | Skip row when `car_park_no` is blank |
| CSV parser | Skip row when `x_coord` is not a valid double |
| CSV parser | Duplicate `car_park_no` uses first occurrence |
| SVY21 converter | Known coordinate pair matches expected WGS84 to within ±0.000005° (≈0.5 m) |
| SVY21 converter | Origin point `(28001.642, 38744.572)` converts to `(1.366666°, 103.833333°)` |
| Haversine | Distance between two known Singapore points matches expected value to within 1 m |
| Haversine | Distance ≤ 500 m includes the car park; > 500 m excludes it |
| Availability parser | `lots_available` and `total_lots` parsed from string to int |
| Availability parser | Non-numeric `lots_available` treated as 0 |
| Join logic | Car park present in both sources produces merged record |
| Join logic | Car park in live source only returns null static fields |
| Join logic | Car park in static source only returns null live data |
| `/api/carparks` handler | `lat` out of Singapore bounding box returns 400 |
| `/api/carparks` handler | `lon` out of Singapore bounding box returns 400 |
| `/api/carparks` handler | Valid request returns only car parks within 500 m, sorted by distance |

### 10.2 Unit tests — WebApp

| Test area | What to test |
|-----------|-------------|
| Freshness logic | `update_datetime` ≤ 5 min ago → Fresh state |
| Freshness logic | `update_datetime` > 5 min and ≤ 60 min ago → Stale state |
| Freshness logic | `update_datetime` > 60 min ago → Very stale state |
| Freshness logic | `liveData` null → Unavailable state |
| Filter logic | "Available only" hides car parks with 0 available lots for selected type |
| Filter logic | Vehicle type filter applied to correct lot type |
| Filter logic | Clearing all filters restores full list |

### 10.3 Contract / integration tests — ApiApp

| Test area | What to test |
|-----------|-------------|
| data.gov.sg schema | `carpark-availability-sample.json` validates against the OpenAPI schema in `data/CarparkAvailability.json` |
| data.gov.sg schema | Unknown optional fields in the sample are accepted without error |
| data.gov.sg schema | Missing required field (`carpark_number`) causes the record to be skipped |

### 10.4 Out-of-scope tests

- End-to-end browser tests (Playwright or Selenium) are deferred to post-MVP.
- Load and performance tests are deferred.
- Data.gov.sg live API calls from tests are not permitted; use the sample JSON fixture.

---

## 11. Deferred and Out-of-Scope Technical Concerns

| Concern | Status |
|---------|--------|
| Relational or document database | Deferred; in-memory store is sufficient for MVP |
| Background alert workers | Deferred with favourites feature |
| Historical data collection | Deferred with occupancy forecasting |
| Cloud deployment / containers | Out of workshop scope |
| MCP (Model Context Protocol) | Out of scope for core application |
| Agentic AI | Introduced in step 05; not part of this TRD |
| Authentication / authorisation | Deferred with user accounts |

---

## 12. Open Questions

| # | Question | Owner | Status |
|---|----------|-------|--------|
| Q1 | Should the 500 m radius be configurable via an environment variable or remain a compile-time constant? | Engineering | Open |
| Q2 | Is `car_park_no` / `carpark_number` matching case-sensitive? Based on sample data both appear to be uppercase. | Engineering | Assumed uppercase; verify at implementation |
| Q3 | What is the correct SVY21 origin for the HDB dataset — SLA or an older definition? | Engineering | Open; use SLA `AddSVY21` parameters above |

---

## Appendix A — CSV Column Reference

See section 4.1 for the authoritative column list.  The first five rows of `HDBCarparkInformation.csv` are reproduced below for reference.

```
car_park_no,address,x_coord,y_coord,car_park_type,type_of_parking_system,short_term_parking,free_parking,night_parking,car_park_decks,gantry_height,car_park_basement
ACB,BLK 270/271 ALBERT CENTRE BASEMENT CAR PARK,30314.7936,31490.4942,BASEMENT CAR PARK,ELECTRONIC PARKING,WHOLE DAY,NO,YES,1,1.8,Y
ACM,BLK 98A ALJUNIED CRESCENT,33758.4143,33695.5198,MULTI-STOREY CAR PARK,ELECTRONIC PARKING,WHOLE DAY,SUN & PH FR 7AM-10.30PM,YES,5,2.1,N
AH1,BLK 101 JALAN DUSUN,29257.7203,34500.3599,SURFACE CAR PARK,ELECTRONIC PARKING,WHOLE DAY,SUN & PH FR 7AM-10.30PM,YES,0,0,N
AK19,BLOCK 253 ANG MO KIO STREET 21,28185.4359,39012.6664,SURFACE CAR PARK,COUPON PARKING,7AM-7PM,NO,NO,0,0,N
```

## Appendix B — Live Availability Response Reference

See `data/carpark-availability-sample.json` for a representative live API response.  The OpenAPI schema is in `data/CarparkAvailability.json`.
