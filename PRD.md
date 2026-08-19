# Product Requirements Document — Smart Parking Navigator

| Field        | Value                              |
|--------------|------------------------------------|
| Version      | 1.0.0                              |
| Status       | Draft                              |
| Author       | @copilot                           |
| Reviewed by  | —                                  |
| Created      | 2026-08-19                         |
| Last updated | 2026-08-19                         |
| Issue        | #2                                 |

---

## 1. Purpose

This document defines the MVP scope, user context, user journeys, acceptance criteria, and explicit exclusions for the **Smart Parking Navigator** — a web application that helps drivers in Singapore find HDB car parks with real-time lot availability near a chosen destination.

---

## 2. Background and Context

### 2.1 Problem statement

Drivers in Singapore often circle car parks looking for an empty lot, wasting time and fuel.  HDB publishes real-time lot availability through the data.gov.sg API, and static car park information through an open dataset, but neither source alone provides a useful navigation experience.

### 2.2 Singapore context

- All car parks in scope are **HDB-managed** and located within Singapore.
- Coordinates in the HDB dataset use the **SVY21** projection; the map and distance calculations require **WGS84** (latitude/longitude).
- The live availability API is polled at most **once per minute** per data.gov.sg guidance.
- All displayed times use **Singapore Standard Time (UTC+8)**.
- Lot types published by the API are `C` (cars), `H` (heavy vehicles), `S` (motorcycles with sidecar), and `Y` (motorcycles).

### 2.3 Data sources

| Source | Description | Update frequency |
|--------|-------------|-----------------|
| data.gov.sg `/transport/carpark-availability` | Live lot counts per car park | ~1 minute |
| HDB Carpark Information CSV (`HDBCarparkInformation.csv`) | Static attributes: address, type, systems, restrictions | Released periodically by HDB |

---

## 3. Target Users

### 3.1 Primary: Singapore driver (commuter or occasional)

- Knows their destination but not where to park nearby.
- May be on a mobile browser while a passenger, or on a desktop before departing.
- Values speed of finding a car park over exhaustive filtering.

### 3.2 Secondary: Singapore driver with vehicle constraints

- Drives a tall van, heavy vehicle, or motorcycle.
- Needs to filter out car parks that do not accommodate their vehicle.

---

## 4. User Journeys

### Journey 1 — Find a car park near a destination

1. User opens the app.
2. User types a destination address or place name into the search bar.
3. The map pans to the destination and displays HDB car parks within 500 m.
4. Each marker shows available lot count and an occupancy indicator.
5. User selects a car park to view its detail panel.
6. Detail panel shows address, lot breakdown, type, restrictions, and data freshness.
7. User decides to navigate to the chosen car park (out of scope for MVP — no turn-by-turn navigation is provided).

### Journey 2 — Filter results

1. From the results list or map, user opens the filter panel.
2. User toggles "Available lots only", selects a vehicle type (car / motorcycle), and optionally filters by night parking.
3. The map and list update immediately.
4. User clears filters to return to the default view.

### Journey 3 — Handle stale or unavailable data

1. The live API has not returned a successful response for more than 5 minutes.
2. The app continues to display the last successfully retrieved data.
3. A clearly visible banner states the age of the data and that it may be stale.
4. If no successful response has ever been received in the current session, an error state is displayed instead of an empty list.

---

## 5. MVP Feature Scope (P0)

### 5.1 Destination search

- Free-text search restricted to **Singapore** addresses and place names via Google Maps Places API.
- Autocomplete suggestions scoped to Singapore (`componentRestrictions: { country: 'SG' }`).
- On selection, map recentres on the destination and a marker is placed.

### 5.2 Nearby HDB car parks

- Display all HDB car parks whose WGS84 coordinates fall within **500 metres** (straight-line) of the searched destination.
- Distance is calculated on the backend from the destination coordinates and each car park's converted WGS84 coordinates.
- Car parks without a matching live record are still displayed with their static information; lot counts are shown as unavailable.

### 5.3 Live lot availability

- Display **total lots** and **available lots** per lot type (C, H, Y, S) for each car park.
- Display **occupancy percentage** (available ÷ total × 100, rounded to the nearest integer).
- Show the `update_datetime` from the live API response for each car park's data.
- Refresh live data automatically every **60 seconds**.

### 5.4 Freshness states

| State        | Condition                                      | UI indicator                          |
|--------------|------------------------------------------------|---------------------------------------|
| Fresh        | `update_datetime` ≤ 5 minutes ago             | No indicator                          |
| Stale        | `update_datetime` > 5 minutes and ≤ 60 minutes ago | Amber warning icon on the card   |
| Very stale   | `update_datetime` > 60 minutes ago            | Red warning icon on the card          |
| Unavailable  | Car park has no live record in current response | "No live data" label                 |
| API error    | Last successful poll > 5 minutes ago (session) | App-level banner with last-good time  |

### 5.5 Basic filters

| Filter              | Values                              | Default   |
|---------------------|-------------------------------------|-----------|
| Available lots only | On / Off                            | Off       |
| Vehicle type        | Any / Car (C) / Motorcycle (Y or S) / Heavy vehicle (H) | Any |
| Night parking       | Any / Night parking available        | Any       |
| Car park type       | Any / Surface / Multi-storey / Basement | Any    |

Filters are applied client-side to the current result set.  They do not trigger a new API call.

### 5.6 Car park detail panel

Fields displayed (all sourced from the static HDB CSV unless marked live):

| Field                  | Source |
|------------------------|--------|
| Car park number        | Static |
| Address                | Static |
| Car park type          | Static |
| Parking system         | Static |
| Short-term parking     | Static |
| Free parking           | Static — display raw value without interpretation |
| Night parking          | Static |
| Number of decks        | Static |
| Gantry height (m)      | Static (0 = not applicable) |
| Underground            | Static |
| Available lots (by type) | Live |
| Total lots (by type)   | Live |
| Data update time       | Live |

> **Note:** `free_parking` and `short_term_parking` values are reproduced verbatim from the CSV.  The application must not infer time windows or policy meanings beyond what the raw value states.

### 5.7 Loading and empty states

- **Loading:** skeleton cards shown while the first live response is pending.
- **No results:** "No HDB car parks found within 500 m of this location" message.
- **Search cleared:** map returns to default Singapore overview; no results shown.

---

## 6. Out of Scope for MVP

The following are **explicitly deferred** and must not be implemented in this issue or in the issues that implement this PRD:

| Feature | Reason deferred |
|---------|-----------------|
| User accounts and favourites | Requires persistent storage; deferred to post-MVP |
| Availability alerts / push notifications | Requires accounts and background workers |
| Occupancy forecasting | Requires historical data collection |
| Traffic and weather integration | Additional external APIs; out of workshop scope |
| Turn-by-turn navigation | Covered by native map apps |
| Database (relational or document) | Not needed for MVP; in-memory + CSV sufficient |
| Deployment / cloud infrastructure | Out of workshop scope |
| MCP (Model Context Protocol) | Out of scope; not part of core application |
| Agentic AI features | Introduced separately in step 05 capstone |
| Private / non-HDB car parks | Data not available from these sources |

---

## 7. Measurable Acceptance Criteria

The following acceptance criteria define "done" for the MVP.  Each must pass before the implementation is considered complete.

### AC-01 Destination search scoped to Singapore
- Given the user types a partial address in the search bar, autocomplete suggestions are returned only for Singapore locations.
- Given the user selects a result, the map pans to that location within 1 second on a standard desktop browser.

### AC-02 Nearby car parks displayed
- Given a destination is selected, all HDB car parks within 500 m (Haversine distance in WGS84) are shown on the map and in the list.
- Given a destination with no HDB car parks within 500 m, an appropriate empty-state message is displayed.

### AC-03 Live availability shown
- Given a successful API response, available and total lot counts per lot type are visible on each car park card.
- Given a refresh cycle (60 s), updated counts replace the previous values without a full page reload.

### AC-04 Freshness states communicated
- Given `update_datetime` is more than 5 minutes old, an amber or red indicator appears on the affected card.
- Given the live API has not responded successfully for more than 5 minutes, an app-level stale-data banner is visible.

### AC-05 Filters work correctly
- Given "Available lots only" is on, no card with zero available lots (for the selected vehicle type) is visible.
- Given a vehicle type filter is selected, only lot counts for that type contribute to the availability check.
- Given all filters are cleared, the full unfiltered result set is restored.

### AC-06 Car park detail correctness
- Given a car park is selected, the detail panel shows the correct static fields from the CSV and the correct live counts from the most recent API response.
- `free_parking` and `short_term_parking` values are displayed as the raw string from the CSV without modification.

### AC-07 Stale API banner
- Given no successful API poll has occurred in the current session, the app displays an error/loading state rather than empty results with no explanation.

### AC-08 No credential leakage
- The Google Maps API key is used only in the WebApp (frontend).
- The data.gov.sg API key is used only in the ApiApp (backend).
- Neither key appears in API responses, HTML source, or JavaScript bundles of the other project.

---

## 8. Non-Functional Requirements

| Category        | Requirement |
|-----------------|-------------|
| Performance     | First meaningful paint < 3 s on a 10 Mbps connection for the initial page load |
| Accessibility   | WCAG 2.1 AA — all interactive controls have accessible labels; colour is not the sole means of conveying freshness state |
| Internationalisation | English only for MVP |
| Browser support | Latest two stable releases of Chrome, Edge, Firefox, and Safari |
| Security        | API keys stored in environment variables / user secrets; never committed to source control |

---

## 9. Glossary

| Term | Definition |
|------|------------|
| SVY21 | Singapore's local projected coordinate system, used in the HDB CSV |
| WGS84 | World Geodetic System 1984; standard latitude/longitude used by Google Maps |
| Lot type | Vehicle category: C = car, H = heavy vehicle, S = motorcycle with sidecar, Y = motorcycle |
| Freshness | Derived state indicating how recently the live data for a car park was updated |
| Stale data | Live data whose `update_datetime` exceeds the defined freshness threshold |
