# Event Ticketing API

A REST API for buying event tickets, built with **.NET 10**, **EF Core**, and **SQL Server** (in Docker).
It demonstrates clean layering, real-world seat-reservation concurrency, and an auditable seat-status history.

## What it does

- Create **events**; their full **seat map** is generated automatically from row/column dimensions.
- Browse seats and availability (read-only — looking never locks a seat).
- **Hold** a seat (click-to-hold / click-to-release toggle) with an automatic expiry.
- **Purchase** held seats — creates an `Order` and marks seats sold, in one transaction.
- Every seat status change is written to a **history** table for reporting.

## Architecture

Layered, with each layer in its own folder/namespace inside a single deployable project:

```
EventTicketing.Api/
├─ Controllers/    → thin HTTP layer (EventTicketing.Api.Controllers)
├─ BusinessLogic/  → services, rules, mapping, exceptions (EventTicketing.BusinessLogic)
├─ DataAccess/     → DbContext, configs, repositories, unit of work, migrations (EventTicketing.DataAccess)
└─ Models/         → entities, enums, DTOs (EventTicketing.Models)
```

Dependency flow is one-way: **Controller → BusinessLogic → DataAccess → Models**.

> **Why one project instead of four?** This machine enforces a Windows Application Control (WDAC)
> policy that blocks loading locally-built, unsigned class-library DLLs at runtime. Keeping all
> layers in the single entry assembly avoids that block while preserving the layering by
> folder/namespace. See [Environment notes](#environment-notes).

## Prerequisites

- Docker Desktop
- .NET 10 SDK (for running the API on the host)

## Getting started

```bash
# 1. Start SQL Server (host port 1434 -> container 1433)
docker compose up -d

# 2. Run the API (Development): applies migrations + seeds sample data on startup
dotnet run --project src/EventTicketing.Api --launch-profile http
```

Then open **Swagger UI** at <http://localhost:5111/swagger>.

The connection string lives in `src/EventTicketing.Api/appsettings.json`
(`Server=localhost,1434;...`). Hold timings are configurable under the `Hold` section
(Development uses a 1-minute hold + 10s sweep so expiry is easy to demo).

## Tests

```bash
bash scripts/test.sh
```

Tests run **inside the .NET SDK container** because the host WDAC policy blocks the signed test
host from loading our unsigned test assemblies. They cover:

- **Unit (xUnit + Moq + FluentAssertions):** reservation toggle/hold/release, conflict handling,
  expired-hold reuse, DB-concurrency → 409 translation, seat-map generation, unique-email rule.
- **Integration (SQLite in-memory):** EF mapping, the unique-email index, and the full
  hold → purchase flow (seat sold, order persisted, history written with the order id).

## Database migrations

Already generated (`DataAccess/Migrations/InitialCreate`) and applied automatically on startup.
To add a migration, run the EF tool **in the SDK container** (host EF tool is WDAC-blocked):

```bash
bash scripts/ef.sh migrations add <Name> -o DataAccess/Migrations
```

## API overview

| Area | Endpoint | Notes |
|------|----------|-------|
| Events | `GET/POST /api/events`, `GET/PUT/DELETE /api/events/{id}` | POST auto-generates seats |
| Seats | `GET /api/events/{id}/seats`, `GET /api/events/{id}/seats/available` | read-only |
| Customers | `GET/POST /api/customers`, `GET/PUT/DELETE /api/customers/{id}`, `GET /api/customers/{id}/orders` | unique email |
| Reservations | `POST /api/reservations/seats/{seatId}/toggle?customerId=` | click-to-hold/release |
| | `POST /api/reservations/hold`, `DELETE /api/reservations/seats/{seatId}?customerId=` | batch hold / release |
| Orders | `POST /api/orders`, `GET /api/orders/{id}` | purchase = confirm held seats |
| Reports | `GET /api/reports/seats/{seatId}/history`, `GET /api/reports/events/{eventId}/sales` | from history table |

## How seat holds work

`Available → InProgress (held) → Occupied (sold)`.

- **A hold expires** at `Seat.HoldExpiresAtUtc`. Expiry is enforced two ways:
  - *Lazily*, on every access — a seat counts as available if it's `Available` **or** its hold has
    lapsed. So correctness holds even if the sweeper is behind.
  - *Actively*, by `HoldExpiryBackgroundService`, which releases lapsed holds and logs a
    `HoldExpired` history row.
- **Reads never mutate** — viewing a seat does not hold it.
- **Double-booking is impossible**: `Seat.RowVersion` is a SQL Server `rowversion` optimistic-
  concurrency token, so two simultaneous grabs of the same seat → one wins, the other gets `409`.

## Data model

`Event 1—* Seat`, `Customer 1—* Order 1—* OrderItem`, `OrderItem` unique per `Seat`
(a seat sells once), and an append-only `SeatStatusHistory` (`Reserved/Released/Purchased/HoldExpired`).

## Environment notes

- **WDAC:** the host blocks loading locally-built unsigned DLLs. The API runs on the host because
  its single entry assembly plus signed NuGet dependencies are allowed; tests and EF tooling run in
  the SDK container (see scripts).
- SQL Server is mapped to host port **1434** to avoid clashing with any local SQL on 1433.
- The SQLite test dependency surfaces a NuGet vulnerability warning (`SQLitePCLRaw.lib.e_sqlite3`);
  it is test-only and does not ship in the API.
