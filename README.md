# Clinic Appointment and Billing System

A maintainable full-stack clinic system built for the Senior Full Stack Developer technical assessment. It includes JWT authentication with refresh-token rotation, role-based authorization, patient management, a weekly appointment calendar with double-booking prevention, itemized billing, payments, dashboard metrics, audit logs, and soft deletion.

## Technology

- **Backend:** ASP.NET Core 8 Web API, Entity Framework Core 8
- **Database:** SQL Server 2022
- **Authentication:** JWT bearer tokens and rotating refresh tokens
- **Frontend:** React 19 and Vite
- **Documentation:** Swagger/OpenAPI
- **Testing:** xUnit and EF Core InMemory
- **Local infrastructure:** Docker Compose

## Architecture

The backend follows a layered architecture:

```text
ClinicApp.Domain          Entities, enums, and repository contracts
ClinicApp.Application     DTOs, validation, use cases, authentication services
ClinicApp.Infrastructure  EF Core context, SQL Server mappings, repositories, migrations
ClinicApp.Api             Controllers, authorization, middleware, Swagger, composition root
ClinicApp.Api.Tests       Application unit tests
```

Dependency direction is `API -> Infrastructure -> Application -> Domain`; Application does not depend on Infrastructure. API responses use DTOs rather than exposing EF Core entities.

## Features

### Authentication and authorization

- Access and refresh tokens
- Atomic refresh-token rotation and revocation
- Roles: `Admin`, `Doctor`, and `Receptionist`
- Admin and Receptionist can manage patients, appointments, and billing
- Doctor has read access to patients, appointments, and dashboard data

### Patients

- Create, edit, search, retrieve, and soft-delete patients
- Medical history and insurance information
- Pagination and sortable API results

### Appointments

- Book, cancel, and reschedule
- Responsive seven-day calendar
- Date, doctor, and status filtering
- Serializable transactions and overlap checks prevent doctor double booking

### Billing

- Multiple invoice items
- Configurable VAT and discount percentages
- Partial and full payments
- Outstanding balance and payment status
- Transactional invoice and payment operations

### Platform quality

- RFC 7807 Problem Details responses
- Request and business-rule validation
- Structured ASP.NET Core logging
- SQL constraints and indexes
- Soft-delete query filters
- Audit columns and automatic audit-log records
- Swagger bearer-token support

### Bonus features

- Dark mode toggle
- PDF invoice download
- Patient document upload
- Appointment email reminders (background service)
- Dashboard charts (appointments and revenue)
- Docker Compose local infrastructure
- GitHub Actions CI pipeline

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/) with Docker Compose

## Local setup

### 1. Start SQL Server

```bash
docker compose up -d sqlserver
```

The local default is:

```text
Server: localhost,1433
Database: ClinicApp
User: sa
Password: ClinicApp!2026
```

To use another password, set `MSSQL_SA_PASSWORD` and override `ConnectionStrings__DefaultConnection` for the API. Never use the development password or JWT key in production.

### 2. Restore, build, and test the backend

```bash
dotnet tool restore
dotnet restore ClinicAssessment.sln
dotnet build ClinicAssessment.sln --no-restore
dotnet test ClinicAssessment.sln --no-build
```

### 3. Run the API

```bash
dotnet run --project "[BackEnd]/ClinicApp.Api/ClinicApp.Api.csproj"
```

The API starts at `http://localhost:5002`. On startup it applies EF Core migrations and seeds development users, one doctor, and one patient.

Swagger UI:

```text
http://localhost:5002/swagger
```

### 4. Run the React frontend

In another terminal:

```bash
cd ClinicApp.Frontend
npm ci
npm run dev
```

Open:

```text
http://localhost:5173
```

The Vite development server proxies `/api` to the API at port `5002`.

## Development users

|| Role | Username | Password |
|| --- | --- | --- |
|| Admin | `admin` | `admin` |
|| Doctor | `doctor` | `doctor` |
|| Receptionist | `receptionist` | `receptionist` |

These accounts are for local assessment use only.

## Configuration

ASP.NET Core environment variables override the dev defaults in `appsettings.json` using double underscores. See `.env.example` for the complete list of variables you can set.

```bash
export ConnectionStrings__DefaultConnection='Server=...'
export Jwt__Key='a-random-secret-with-at-least-32-bytes'
export Jwt__Issuer='ClinicApp'
export Jwt__Audience='ClinicAppUsers'
```

Email reminders use the `Smtp` section. When `Smtp:Host` is empty the background service logs a message and does not send mail.

```bash
export Smtp__Host='smtp.example.com'
export Smtp__Port='587'
export Smtp__Username='...'
export Smtp__Password='...'
export Smtp__From='clinic@example.com'
export Smtp__EnableSsl='true'
```

Allowed frontend origins are configured under `Cors:AllowedOrigins`.

## Database

- EF Core migration: `ClinicApp.Infrastructure/Data/Migrations`
- Idempotent SQL Server creation script: `sql/schema.sql`

Create a new migration:

```bash
dotnet tool run dotnet-ef migrations add MigrationName \
  --project "[BackEnd]/ClinicApp.Infrastructure/ClinicApp.Infrastructure.csproj" \
  --startup-project "[BackEnd]/ClinicApp.Api/ClinicApp.Api.csproj" \
  --output-dir Data/Migrations
```

Regenerate the SQL deliverable:

```bash
dotnet tool run dotnet-ef migrations script --idempotent \
  --project "[BackEnd]/ClinicApp.Infrastructure/ClinicApp.Infrastructure.csproj" \
  --startup-project "[BackEnd]/ClinicApp.Api/ClinicApp.Api.csproj" \
  --output sql/schema.sql
```

## API overview

|| Method | Route | Purpose |
|| --- | --- | --- |
|| POST | `/api/auth/login` | Sign in |
|| POST | `/api/auth/refresh` | Rotate refresh token |
|| POST | `/api/auth/logout` | Revoke refresh token |
|| GET/POST | `/api/patients` | Search/list or create patients |
|| GET/PUT/DELETE | `/api/patients/{id}` | Retrieve, edit, or soft-delete a patient |
|| GET | `/api/doctors` | List active doctors |
|| GET/POST | `/api/appointments` | Filter/list or book appointments |
|| POST | `/api/appointments/{id}/cancel` | Cancel an appointment |
|| POST | `/api/appointments/{id}/reschedule` | Reschedule an appointment |
|| GET/POST | `/api/invoices` | Filter/list or create invoices |
|| POST | `/api/invoices/{id}/payments` | Record a payment |
|| GET/POST | `/api/patients/{id}/documents` | List or upload patient documents |
|| GET | `/api/dashboard` | Retrieve dashboard metrics |
|| GET | `/api/dashboard/charts` | Retrieve dashboard chart data |

Swagger contains the complete request and response schemas.

## Validation and assumptions

- Appointment and dashboard timestamps are treated as UTC.
- The calendar displays the browser’s local time.
- VAT and discount are percentages stored on each invoice.
- The frontend displays money in USD; the domain stores currency-neutral decimal amounts.
- Revenue is the sum of recorded payments, not issued invoice totals.
- An invoice requires at least one positive-value item.
- Payments cannot exceed the outstanding invoice balance.
- Doctor roster administration is outside the requested assessment scope; one active doctor is seeded.
- Charts are optional. Implemented bonus features: dark mode, PDF invoice download, patient document upload, appointment email reminders, dashboard charts, Docker Compose, and CI.

## Quality commands

```bash
dotnet format ClinicAssessment.sln --verify-no-changes
dotnet test ClinicAssessment.sln
cd ClinicApp.Frontend
npm run lint
npm run build
```
