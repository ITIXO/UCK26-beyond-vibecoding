# UCK26 — Beyond Vibecoding

Demo repo for UCK26 conf session. Small full-stack app used live on stage to show LLM-assisted workflow prompt→working feature.

> **Demo only.** No prod hardening. JWT signing key lives in `appsettings.json`, seed admin fixed, dev CORS local only. Do not deploy as-is.

## What's in the box

- **Backend:** .NET 10 Minimal API, EF Core 10 + SQLite, JWT bearer auth.
- **Frontend:** React 19 + Vite + TypeScript, Tailwind v4, `@itixo/component-library`.
- **Auth:** username + password -> JWT; `Admin` role required for user management.
- **Work tracking:** monthly worksheet per user, work/holiday/doctor entries.
- **Storage:** SQLite file `uck26.db`; EF migrations run at API startup.
- **Password protection:** ASP.NET Core Data Protection, keys in `./dataprotection-keys`; seed admin uses special `seed:` password hash.
- **Tests:** TUnit backend unit/integration tests; TUnit + Playwright UI tests.

## Repo layout

```
src/
├── UCK26.Api/                  # .NET 10 Minimal API backend
│   ├── Auth/                   # JWT + DataProtection password protector
│   ├── Endpoints/              # Auth, users, worksheets route groups
│   ├── Persistence/            # DbContext, entities, EF migrations, seed data
│   ├── Program.cs
│   ├── appsettings.json
│   └── UCK26.Api.csproj
├── UCK26.Api.Tests/            # TUnit unit + integration tests
├── UCK26.Ui.Tests/             # TUnit + Playwright UI tests
└── uck26-frontend/             # React + Vite SPA
    ├── src/
    │   ├── app/                # Providers + router
    │   ├── features/auth/      # AuthProvider, RequireAuth, login form
    │   ├── features/users/     # Admin user CRUD
    │   ├── features/work/      # Monthly worksheet UI
    │   ├── pages/              # Home, Login, Users, Work pages
    │   ├── shared/lib/api/     # Fetch client + endpoint contracts
    │   ├── shared/ui/          # AppShell
    │   └── main.tsx
    ├── index.html
    ├── package.json
    └── vite.config.ts
UCK26.slnx                      # Solution file
global.json                     # .NET SDK pin
```

## Quick start

Prereqs: .NET 10 SDK, Node 20+, npm.

```bash
# 1. Backend
cd src/UCK26.Api
dotnet run
# API: http://localhost:5080
# Scalar docs: http://localhost:5080/scalar
# Startup runs EF migrations and creates `uck26.db` when missing.

# 2. Frontend (second terminal)
cd src/uck26-frontend
npm install
npm run dev
# SPA: http://localhost:3000

# 3. Backend tests
dotnet run --project src/UCK26.Api.Tests

# 4. UI tests (need API + SPA running)
dotnet run --project src/UCK26.Ui.Tests
```

Default admin creds:

| Username | Password    |
|----------|-------------|
| `admin`  | `Demo!2026` |

## What demo shows

1. Login page posts `{ userName, password }` to `/api/auth/login`, stores JWT in memory + `localStorage`.
2. Home page links to work tracking and admin user management.
3. Work page (`/work`) shows monthly worksheet, supports month nav and entry create/edit/delete.
4. Users page (`/users`) admin-only; admin can create/rename/delete users and change role/password.

## API

| Method | Path                         | Auth          | Notes                                 |
|--------|------------------------------|---------------|---------------------------------------|
| POST   | `/api/auth/login`            | Anonymous     | Body: `{ userName, password }` -> JWT |
| GET    | `/api/auth/me`               | Any logged-in | Return current user info              |
| GET    | `/api/users`                 | Admin         | List users                            |
| POST   | `/api/users`                 | Admin         | Create user                           |
| PUT    | `/api/users/{id}`            | Admin         | Rename / role / password              |
| DELETE | `/api/users/{id}`            | Admin         | Delete user                           |
| GET    | `/api/worksheets`            | Any logged-in | Query: `year`, `month`, optional admin-only `userId` |
| POST   | `/api/worksheets/entries`    | Any logged-in | Create work/holiday/doctor entry      |
| PUT    | `/api/worksheets/entries/{id}` | Owner/Admin  | Update entry time + description       |
| DELETE | `/api/worksheets/entries/{id}` | Owner/Admin  | Delete entry                          |

## Configuration

`src/UCK26.Api/appsettings.json`:

```json
{
  "ConnectionStrings": { "Default": "Data Source=uck26.db" },
  "Jwt": {
    "Issuer": "uck26",
    "Audience": "uck26-spa",
    "SigningKey": "dev-only-signing-key-change-me-please-32+chars-long",
    "ExpiresMinutes": 60
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:3000", "http://localhost:5173" ]
  }
}
```

Frontend reads `VITE_API_URL` from `.env.local`; `.env.example` points at `http://localhost:5080`.

## Reset data

Delete `src/UCK26.Api/uck26.db` or runtime `uck26.db` to reset SQLite. Delete `dataprotection-keys/` only with DB reset; existing protected passwords become unverifiable otherwise.

## License

See [LICENSE](LICENSE).
