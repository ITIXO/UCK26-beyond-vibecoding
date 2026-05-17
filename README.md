# UCK26 — Beyond Vibecoding

Demo repo for UCK26 conference session. Minimal full-stack scaffold used live on stage to show LLM-assisted workflow from prompt to working app.

> **Demo only.** No prod hardening — JWT signing key in `appsettings.json`, admin user hardcoded, CORS wide open in dev. Do not deploy as-is.

## What's in the box

- **Backend:** .NET 10 Minimal API, EF Core + SQLite, JWT bearer auth (username/password)
- **Frontend:** React 19 + Vite + TypeScript, Tailwind v4, `@itixo/component-library`
- **Auth:** username + password → JWT; `admin` role required for user management
- **Storage:** single SQLite file (`uck26.db`), seeded on first run with one admin user
- **Password protection:** ASP.NET Core Data Protection (`IDataProtectionProvider`) with keys persisted to `./dataprotection-keys` — hardcoded, no config
- **Tests:** TUnit for backend unit + integration (`UCK26.Api.Tests`), TUnit + Playwright for UI (`UCK26.Ui.Tests`)

## Repo layout

```
src/
├── UCK26.Api/                  # .NET 10 Minimal API backend
│   ├── Auth/                   # JWT + DataProtection password protector
│   ├── Endpoints/              # Minimal API route groups (auth, users)
│   ├── Persistence/            # AppDbContext, User entity, seed
│   ├── Program.cs
│   ├── appsettings.json
│   └── UCK26.Api.csproj
├── UCK26.Api.Tests/            # TUnit unit + integration tests
├── UCK26.Ui.Tests/             # TUnit + Playwright UI tests
└── uck26-frontend/             # React + Vite SPA
    ├── src/
    │   ├── app/                # Providers + router
    │   ├── features/auth/      # AuthProvider, RequireAuth, login form
    │   ├── features/users/     # User list page
    │   ├── pages/              # LoginPage, UsersPage
    │   ├── shared/lib/api/     # Fetch client + endpoint contracts
    │   └── main.tsx
    ├── index.html
    ├── package.json
    └── vite.config.ts
UCK26.slnx                      # Solution file
global.json                     # SDK pin
```

## Quick start

Prereqs: .NET 10 SDK, Node 20+, npm.

```bash
# 1. Backend
cd src/UCK26.Api
dotnet run
# API listens on http://localhost:5080
# Scalar API docs: http://localhost:5080/scalar
# On first run, SQLite file `uck26.db` is created next to the binary and seeded.

# 2. Frontend (in a second terminal)
cd src/uck26-frontend
npm install
npm run dev
# SPA on http://localhost:3000

# 3. Backend tests
dotnet run --project src/UCK26.Api.Tests

# 4. UI tests (require both API + SPA running)
dotnet run --project src/UCK26.Ui.Tests
```

Default admin creds:

| Username | Password    |
|----------|-------------|
| `admin`  | `Demo!2026` |

## What demo shows

1. Login page — POST `{ userName, password }` to `/api/auth/login`, store JWT in memory + `localStorage`.
2. After login, SPA fetch `/api/auth/me` to surface user + role.
3. Users page (`/users`) — visible only to `admin` role; list users from `/api/users`, admin can create/rename/delete.

## API

| Method | Path             | Auth          | Notes                                  |
|--------|------------------|---------------|----------------------------------------|
| POST   | `/api/auth/login`| Anonymous     | Body: `{ userName, password }` → JWT   |
| GET    | `/api/auth/me`   | Any logged-in | Return current user info               |
| GET    | `/api/users`     | Admin         | List users                             |
| POST   | `/api/users`     | Admin         | Create user                            |
| PUT    | `/api/users/{id}`| Admin         | Rename / change role / reset password  |
| DELETE | `/api/users/{id}`| Admin         | Delete user                            |

## Configuration

`src/UCK26.Api/appsettings.json` (override via env vars):

```json
{
  "ConnectionStrings": { "Default": "Data Source=uck26.db" },
  "Jwt": {
    "Issuer": "uck26",
    "Audience": "uck26-spa",
    "SigningKey": "dev-only-signing-key-change-me-please-32+chars",
    "ExpiresMinutes": 60
  },
  "Seed": {
    "AdminUserName": "admin",
    "AdminPassword": "Demo!2026"
  }
}
```

Frontend read `VITE_API_URL` from `.env.local` (default `http://localhost:5080`).

## License

See [LICENSE](LICENSE).
