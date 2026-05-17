# CLAUDE.md

Behavioral guidelines for AI agents in this repo. **Conference demo project** — small, intentionally simple, live on stage. Optimise clarity over cleverness.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:

- State assumptions explicit. Uncertain → ask.
- Multiple interpretations exist → present them, don't pick silently.
- Simpler approach exists → say so. Push back when warranted.
- Unclear → stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves problem. Nothing speculative.**

Repo is **scaffold for a talk**, not a product. Reach for simplest thing that works:

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" demo does not need.
- No defensive error handling for impossible scenarios.
- 200 lines when 50 works → rewrite.

Senior engineer would call it overcomplicated → simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things not broken.
- Match existing style, even if you'd do it differently.
- Remove imports/variables YOUR changes orphaned; leave pre-existing dead code alone unless asked.

Test: every changed line traces direct to user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

Multi-step tasks → state brief plan before starting.

---

## 5. Documenting sessions

- Problem handled in Claude Cowork + user asks for documents → always create new folder per session. Folder named kebab-case, shortly describes session problem, placed in `documents` folder. Example: `jwt-login-flow`.
- Developer handover docs → `*.handover.md`. Example: `jwt-login-handover.md`.
- UX/UI design docs → interactive html `*.handover.html`. Example: `jwt-login-handover.html`.
- Architecture decision record → `*.adr.md`. Example: `jwt-login-adr.md`.
- Final step: always use /compress skill to reduce created documents. Delete `*.original.md`. Example `CLAUDE.original.md`.

## 6. GitHub issues

- Vague issue → ask for clarification before starting.
- New issue → assess size/complexity, assign correct type. Types: Feature, Task, Bug.
- New issue has sub issues → always assign type Feature.
- Feature must have sub issues of at least type Task.
- Feature must have sub issue to update docs (README.md, AGENTS.md, CLAUDE.md).
- Feature must have sub issue to add tests (unit, integration, UI) if applicable.
- Feature must have implementation plan (what to do next).
- Design Feature sub issues for maximum parallelism.
- Ensure issue and sub issues correctly linked.
- Never create new labels.
- Sub issue blocks another → assign via correct relationship.

## Project-Specific Guidelines

### Repository

- **GitHub**: [ITIXO/UCK26-beyond-vibecoding](https://github.com/ITIXO/UCK26-beyond-vibecoding)
- Commit often. No one big commit at end.

### Project Overview

**UCK26 — Beyond Vibecoding** — small full-stack demo app shown live at UCK26 talk. Two screens — login page + admin-only user list. Local JWT auth (no Azure, no SSO).

### Tech Stack

#### Backend

- **.NET 10** / **C#** — Backend framework
- **ASP.NET Core Minimal API** — HTTP layer (route groups, not controllers, not FastEndpoints)
- **Entity Framework Core 10** with **SQLite** — single-file persistence
- **JWT Bearer** auth (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- **BCrypt.Net-Next** — password hashing
- **Swashbuckle** — Swagger UI at `/swagger`

#### Frontend

- **React 19** + **Vite** + **TypeScript** (`strict: true`)
- **React Router 7** — client-side routing
- **@tanstack/react-query** — server state
- **Tailwind CSS v4**
- **@itixo/component-library** — internal component library
- **i18next** *not* used in demo; copy inline English.

### Solution Structure

```
src/
├── UCK26.Api/                          # Backend (.NET 10 Minimal API)
│   ├── Auth/
│   │   ├── JwtTokenService.cs          # Issues JWTs
│   │   ├── PasswordHasher.cs           # BCrypt wrapper
│   │   └── AuthOptions.cs              # Bound to "Jwt" section
│   ├── Endpoints/
│   │   ├── AuthEndpoints.cs            # /api/auth/* route group
│   │   └── UserEndpoints.cs            # /api/users/* route group (Admin only)
│   ├── Persistence/
│   │   ├── AppDbContext.cs             # EF Core DbContext
│   │   ├── User.cs                     # Single entity
│   │   └── DbSeeder.cs                 # Hardcodes admin on first run
│   ├── Program.cs                      # Composition root
│   ├── appsettings.json
│   └── UCK26.Api.csproj
│
└── uck26-frontend/                     # React 19 SPA
    ├── src/
    │   ├── app/
    │   │   ├── providers/AppProviders.tsx
    │   │   └── router/AppRouter.tsx
    │   ├── features/
    │   │   ├── auth/
    │   │   │   ├── AuthProvider.tsx    # JWT + useAuth() hook
    │   │   │   ├── RequireAuth.tsx
    │   │   │   └── LoginForm.tsx
    │   │   └── users/
    │   │       ├── UserList.tsx
    │   │       └── users.queries.ts
    │   ├── pages/
    │   │   ├── LoginPage.tsx
    │   │   └── UsersPage.tsx
    │   ├── shared/lib/api/
    │   │   ├── api.ts                  # fetch wrapper + token injection
    │   │   ├── auth.api.ts
    │   │   └── users.api.ts
    │   ├── app.css                     # Tailwind entry
    │   └── main.tsx
    ├── index.html
    ├── package.json
    ├── tsconfig.json
    └── vite.config.ts
```

### Commands

```bash
# Backend
cd src/UCK26.Api
dotnet run                    # http://localhost:5080
dotnet ef migrations add Name # if you change the model
dotnet ef database update

# Frontend
cd src/uck26-frontend
npm install
npm run dev                   # http://localhost:3000
npm run build
npm run lint
```

### Authentication & Authorization

- Backend issue JWTs signed HS256 using `Jwt:SigningKey`. Claims: `sub` (user id), `name` (username), `role`.
- Frontend POST `{ userName, password }` to `/api/auth/login`, store returned `accessToken` in `localStorage` under `uck26.token`, attach as `Authorization: Bearer <token>` on every API call.
- **Roles:** `Admin`, `User`. Only `Admin` hit `/api/users/*`.
- Admin account **hardcoded via seed** (`Seed:AdminUserName`, `Seed:AdminPassword` in `appsettings.json`). On first startup `DbSeeder` insert admin if absent.

### Database

- One entity: `User { Id, UserName, PasswordHash, Role, CreatedAt }`.
- SQLite file (`uck26.db`) next to API binary. Delete file to reset.
- Migrations live in `src/UCK26.Api/Persistence/Migrations/` if/when needed; for one-entity demo, rely on `db.Database.EnsureCreated()` instead.

### Code Conventions

#### C# (.NET Backend)

- `async`/`await` with `*Async` suffix; always pass `CancellationToken` where framework provides one.
- Nullable reference types enabled.
- File-scoped namespaces.
- Prefer primary constructors for DI.
- Prefer `record` for request/response DTOs.
- No comment narration — descriptive names instead.
- Endpoints live in `Endpoints/` as `IEndpointRouteBuilder` extension methods (`MapAuth()`, `MapUsers()`). Keep route group set-up flat — no abstractions until demo grows.

#### TypeScript / React

- `strict: true`.
- Functional components + hooks only.
- Explicit types for function params + return types.
- Tailwind utility classes — no inline `style={}` unless dynamic.
- Components from `@itixo/component-library` first; build bespoke only when library lacks one.
- API contracts colocated in `shared/lib/api/<feature>.api.ts`.
- No comment narration.

#### API Design

- Routes versioned by prefix `/api/`. No `/api/v1/` for demo — keep short.
- Standard HTTP codes: 200/201/204/400/401/403/404.
- Authorization with `.RequireAuthorization("Admin")` policy on users group.

### Common Tasks

#### Add a Minimal API endpoint

1. Open relevant `*Endpoints.cs` in `src/UCK26.Api/Endpoints/`.
2. Add `group.MapGet/Post/...("path", handler)` line.
3. Attach `.RequireAuthorization("Admin")` if admin-only.
4. Define request/response as records in same file (keep close to handler).

#### Add a frontend page

1. Create `src/features/<feature>/...` (component + queries).
2. Add `pages/<Name>Page.tsx` that composes feature.
3. Register route in `src/app/router/AppRouter.tsx`. Wrap in `<RequireAuth role="Admin" />` for admin pages.

#### Database changes

1. Edit `src/UCK26.Api/Persistence/User.cs` (or add new entity).
2. Update `AppDbContext` if needed.
3. Demo: delete `uck26.db`, let `EnsureCreated()` re-make it. Migrations: `dotnet ef migrations add <Name>` then `dotnet ef database update`.

### Out of scope for demo

- Azure / Entra ID / MSAL — explicitly removed.
- Refresh tokens, password reset emails, MFA — out of scope.
- Multi-tenancy, role hierarchy beyond `Admin` / `User`.
- Docker, nginx, CI — out of scope (`Dockerfile`/`nginx.conf` at repo root are vestiges of earlier README-only stub, not wired up).
