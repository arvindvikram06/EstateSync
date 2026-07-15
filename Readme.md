# EstateSync

A full-stack real estate rental platform — ASP.NET Core 10 backend, Angular 21 frontend.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) + npm
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- [Docker](https://www.docker.com/) (for Redis)

---

## Local Setup

### 1. Clone the repo

```bash
git clone <repo-url>
cd RealEstate
```

### 2. Start Redis (required for email verification & password reset)

```bash
docker compose up -d
```

This starts a Redis container on `localhost:6379`.

### 3. Configure the backend

Copy the template and fill in your values:

```bash
cp backend/EstateSync.Api/appsettings.json backend/EstateSync.Api/appsettings.Development.json
```

Edit `appsettings.Development.json` and replace the placeholders:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=realestate_db;Username=postgres;Password=YOUR_POSTGRES_PASSWORD",
    "Redis": "localhost:6379,abortConnect=false"
  },
  "Jwt": {
    "SecretKey": "any-random-string-min-32-characters-long!!"
  },
  "Email": {
    "Enabled": true,
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-gmail@gmail.com",
    "Password": "your-gmail-app-password",
    "FromName": "EstateSync",
    "FromAddress": "your-gmail@gmail.com"
  },
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_..."
  }
}
```

> `appsettings.Development.json` is gitignored — your secrets never get committed.

### 4. Restore .NET tools (avoids EF version mismatch warning)

```bash
dotnet tool restore
```

### 5. Run the backend

```bash
cd backend/EstateSync.Api
dotnet run
```

API runs at `http://localhost:5118`. EF Core migrations run automatically on startup.

### 6. Install frontend dependencies and run

```bash
cd frontend/estatesync-ui
npm install
npm start
```

Frontend runs at `http://localhost:4200`.

### 7. Seed test data (optional)

```bash
curl -X POST http://localhost:5118/api/v1/system/reset-and-seed
```

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@estatesync.com | Password123! |
| Owner | owner@estatesync.com | Password123! |
| Tenant | tenant@estatesync.com | Password123! |

---

## Project Structure

```
RealEstate/
├── backend/EstateSync.Api/     # ASP.NET Core 10 API
├── frontend/estatesync-ui/     # Angular 21 SPA
├── docs/onboarding/            # Architecture and coding standards
└── docker-compose.yml          # Local Redis
```

## Docs

- [Backend architecture](docs/onboarding/BACKEND.md)
- [Frontend architecture](docs/onboarding/FRONTEND.md)
- [DB schema & flows](docs/onboarding/DB_SCHEMA_AND_FLOWS.md)
- [Backend coding standards](docs/onboarding/CODING_STANDARDS_BACKEND.md)
- [Frontend coding standards](docs/onboarding/CODING_STANDARDS_FRONTEND.md)
- [Styling cheatsheet](docs/onboarding/STYLING_CHEATSHEET.md)
