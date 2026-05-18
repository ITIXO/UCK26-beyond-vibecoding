# CLAUDE.md

Behavioral guidelines for AI agents in this repo. **Conference demo project** — small, intentionally simple, live on stage. Optimise clarity over cleverness.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:

- State assumptions explicit. Uncertain -> ask.
- Multiple interpretations exist -> present them, don't pick silently.
- Simpler approach exists -> say so. Push back when warranted.
- Unclear -> stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves problem. Nothing speculative.**

Repo is **scaffold for talk**, not product. Reach for simplest thing that works:

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" demo does not need.
- No defensive error handling for impossible scenarios.
- 200 lines when 50 works -> rewrite.

Senior engineer would call it overcomplicated -> simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things not broken.
- Match existing style, even if you'd do it differently.
- Remove imports/variables YOUR changes orphaned; leave pre-existing dead code alone unless asked.

Test: every changed line traces direct to user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

- "Add validation" -> "Write tests for invalid inputs, then make them pass"
- "Fix the bug" -> "Write test that reproduces it, then make it pass"
- "Refactor X" -> "Ensure tests pass before and after"

Multi-step tasks -> state brief plan before starting.

---

## 5. Documenting sessions

- Problem handled in Claude Cowork + user asks for documents -> always create new folder per session. Folder named kebab-case, shortly describes session problem, placed in `documents` folder. Example: `jwt-login-flow`.
- Developer handover docs -> `*.handover.md`. Example: `jwt-login-handover.md`.
- UX/UI design docs -> interactive HTML `*.handover.html`. Example: `jwt-login-handover.html`.
- Architecture decision record -> `*.adr.md`. Example: `jwt-login-adr.md`.
- Final step: always use /compress skill to reduce created documents. Delete `*.original.md`. Example `CLAUDE.original.md`.

## 6. GitHub issues

- Vague issue -> ask for clarification before starting.
- Never create new labels.
- New issue -> assess size/complexity/domain, assign correct type. Issue types: Feature, Task, Bug.
- New issue has sub issues -> always assign issue type Feature.
- Feature must have sub issues of issue type Task to cover implementation.
- Task must have in AC's to update docs (README.md,...).
- Task must have in AC's to update agent's instructions (CLAUDE.md,...).
- Task must have in AC's to add tests (unit, integration, UI) if applicable.
- Feature must have implementation plan (what to do next).
- Design Feature sub issues for maximum parallelism.
- Ensure issue and sub issues correctly linked.
- Sub issue blocks another -> assign via correct relationship.
- **Task issues must use the Task issue template** (`.github/ISSUE_TEMPLATE/task.yml`). Every Task must have a filled Description and Acceptance Criteria section. AC section must include all three mandatory checkboxes: test coverage, update README.md, update CLAUDE.md.
- **All three AC checkboxes must be specific** — replace each placeholder with concrete detail. Generic text is not acceptable. If a checkbox does not apply, state why explicitly.
  - Test coverage: which test file, which cases, what to set up, what to assert.
  - Update README.md: which section, what to add or change.
  - Update CLAUDE.md: which convention, structure, or file entry to add or change.
- **Feature issues must link all session documents** in the body: `*.adr.md`, `*.handover.md`, and `*.handover.html` from the `documents/` folder.

## Project-Specific Guidelines

### Repository

- **GitHub**: [ITIXO/UCK26-beyond-vibecoding](https://github.com/ITIXO/UCK26-beyond-vibecoding)
- Commit often. No one big commit at end.

### Project Overview

**UCK26 — Beyond Vibecoding** — small full-stack demo app shown live at UCK26 talk. Local JWT auth, admin user management, monthly work tracking. No Azure, SSO, CI, Docker, production deployment.

### Tech Stack

#### Backend

- **.NET 10** / **C#** — backend framework.
- **ASP.NET Core Minimal API** — HTTP layer. Route groups, not controllers, not FastEndpoints.
- **Entity Framework Core 10** with **SQLite** — single-file persistence, migrations applied via `db.Database.MigrateAsync()` at startup.
- **JWT Bearer** auth (`Microsoft.AspNetCore.Authentication.JwtBearer`).
- **ASP.NET Core Data Protection** — password protection (`IDataProtectionProvider`, keys persisted to `./dataprotection-keys`). Hardcoded purpose `UCK26.Api.Passwords.v1`. Not one-way hash — encrypt/decrypt + compare. Seed admin uses `seed:Demo!2026`.
- **Scalar.AspNetCore** — Scalar API docs at `/scalar` in development.

#### Tests

- **TUnit** — unit + integration runner (Microsoft.Testing.Platform).
- **Microsoft.AspNetCore.Mvc.Testing** — `WebApplicationFactory<Program>` for backend integration tests.
- **Microsoft.Playwright** — UI tests (Chromium); auth handled by hitting `/api/auth/login` directly + injecting JWT into `localStorage` via `addInitScript`.

#### Frontend

- **React 19** + **Vite** + **TypeScript** (`strict: true`).
- **React Router 7** — client-side routing.
- **@tanstack/react-query** — server state.
- **Tailwind CSS v4**.
- **@itixo/component-library** — internal component library.
- **lucide-react** — icons.
- **i18next** not used; copy inline English.

### Solution Structure

```
src/
├── UCK26.Api/                          # Backend (.NET 10 Minimal API)
│   ├── Auth/
│   │   ├── JwtTokenService.cs          # Issues JWTs
│   │   ├── PasswordHasher.cs           # DataProtection password protector + seed fallback
│   │   └── AuthOptions.cs              # Bound to "Jwt" section
│   ├── Endpoints/
│   │   ├── AuthEndpoints.cs            # /api/auth/* route group
│   │   ├── UserEndpoints.cs            # /api/users/* route group (Admin only)
│   │   └── WorksheetEndpoints.cs       # /api/worksheets/* route group
│   ├── Persistence/
│   │   ├── AppDbContext.cs             # EF Core DbContext + seed data
│   │   ├── User.cs                     # User, Worksheet, WorkEntry entities
│   │   ├── AuditLog.cs                 # WorkEntry audit log entity + changed-field record
│   │   ├── AuditInterceptor.cs         # EF interceptor writing WorkEntry audit logs
│   │   └── Migrations/                 # EF Core migrations
│   ├── Program.cs                      # Composition root
│   ├── appsettings.json
│   └── UCK26.Api.csproj
│
├── UCK26.Api.Tests/                    # Backend unit + integration tests (TUnit)
│   ├── Infrastructure/
│   │   ├── TestAuthHandler.cs          # Replaces JWT bearer in tests; principal driven by TestAuthState
│   │   └── TestWebApplicationFactory.cs# WebApplicationFactory<Program> with SQLite temp file + TestAuth
│   ├── Integration/
│   │   ├── AuthEndpointTests.cs
│   │   ├── UserEndpointTests.cs
│   │   └── WorksheetEndpointTests.cs
│   └── Unit/
│       └── PasswordHasherTests.cs
│
├── UCK26.Ui.Tests/                     # UI tests (TUnit + Playwright)
│   ├── BaseTests.cs                    # Browser bootstrap + admin auth helper
│   ├── AuthSetup.cs                    # POST /api/auth/login -> cached JWT
│   ├── PlaywrightSetup.cs              # `playwright install chromium` on first run
│   ├── TestConfig.cs                   # Frontend/API URLs + admin creds via env vars
│   ├── LoginPageTests.cs
│   ├── UsersPageTests.cs
│   └── WorkPageTests.cs
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
    │   │   ├── users/
    │   │   │   ├── UserList.tsx
    │   │   │   └── users.queries.ts
│   │   └── work/
│   │       ├── WorkSheet.tsx
│   │       ├── WorkEntryAuditSidebar.tsx
│   │       └── worksheets.queries.ts
    │   ├── pages/
    │   │   ├── HomePage.tsx
    │   │   ├── LoginPage.tsx
    │   │   ├── UsersPage.tsx
    │   │   └── WorkPage.tsx
    │   ├── shared/lib/api/
    │   │   ├── api.ts                  # fetch wrapper + token injection
    │   │   ├── auth.api.ts
    │   │   ├── auth.contracts.api.ts
    │   │   ├── users.api.ts
    │   │   ├── users.contracts.api.ts
    │   │   ├── worksheets.api.ts
    │   │   └── worksheets.contracts.api.ts
    │   ├── shared/ui/AppShell.tsx
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
dotnet ef migrations add Name # after model changes
dotnet ef database update

# Frontend
cd src/uck26-frontend
npm install
npm run dev                   # http://localhost:3000
npm run build
npm run lint                  # TypeScript check via tsc -b --noEmit

# Backend tests (unit + integration)
dotnet run --project src/UCK26.Api.Tests

# UI tests (require API + SPA running)
dotnet run --project src/UCK26.Ui.Tests
# Override targets via env vars:
#   UCK26_FRONTEND_URL=http://localhost:3000
#   UCK26_API_URL=http://localhost:5080
#   UCK26_ADMIN_USER=admin
#   UCK26_ADMIN_PASSWORD=Demo!2026
```

### Authentication & Authorization

- Backend issues JWTs signed HS256 using `Jwt:SigningKey`. Claims: `sub` (user id), `name` (username), `role`.
- Frontend POSTs `{ userName, password }` to `/api/auth/login`, stores `accessToken` in `localStorage` under `uck26.token`, attaches `Authorization: Bearer <token>` on API calls.
- **Roles:** `Admin`, `User`. Only `Admin` hits `/api/users/*`. `/api/worksheets/*` requires logged-in user; admin may pass `userId` to view/edit another user's worksheet.
- Admin account seeded by EF data in `AppDbContext`: `admin` / `Demo!2026`, password hash `seed:Demo!2026`.
- Passwords for created/updated users stored as Data Protection ciphertext. Keys persist to `./dataprotection-keys` (gitignored). Wipe keys -> protected passwords unverifiable; delete `uck26.db` to reseed.

### Database

- Entities: `User`, `Worksheet`, `WorkEntry`.
- `Worksheet` unique per `{ UserId, Year, Month }`, cascades delete to entries.
- `WorkEntry.Type`: `work`, `holiday`, `doctor`. `End` must be after `Start`.
- `AuditLog` records `WorkEntry` mutations without FK to `WorkEntry`; `AuditInterceptor` is registered as singleton and wired through `AddInterceptors`.
- SQLite file: `uck26.db` next to API working directory/binary depending run context. Delete file to reset.
- Migrations live in `src/UCK26.Api/Persistence/Migrations/`; startup applies with `MigrateAsync()`.

### Code Conventions

#### C# (.NET Backend)

- `async`/`await` with `*Async` suffix; pass `CancellationToken` where framework provides one.
- Nullable reference types enabled.
- File-scoped namespaces.
- Prefer primary constructors for DI.
- Prefer `record` for request/response DTOs.
- No comment narration — descriptive names instead.
- Endpoints live in `Endpoints/` as `IEndpointRouteBuilder` extension methods (`MapAuth()`, `MapUsers()`, `MapWorksheets()`). Keep route group setup flat.

#### TypeScript / React

- `strict: true`.
- Functional components + hooks only.
- Explicit types for function params + return types.
- Tailwind utility classes — no inline `style={}` unless dynamic.
- Components from `@itixo/component-library` first; build bespoke only when library lacks one.
- Icons from `lucide-react`.
- API contracts colocated in `shared/lib/api/<feature>.contracts.api.ts`; API calls in `<feature>.api.ts`.
- No comment narration.

#### API Design

- Routes prefixed `/api/`; no `/api/v1/` for demo.
- Standard HTTP codes: 200/201/204/400/401/403/404/409.
- Authorization via `.RequireAuthorization()` or `.RequireAuthorization("Admin")` on groups.
- Keep request/response records in same endpoint file until real duplication appears.

### Common Tasks

#### Add Minimal API endpoint

1. Open relevant `*Endpoints.cs` in `src/UCK26.Api/Endpoints/`.
2. Add `group.MapGet/Post/...("path", handler)`.
3. Attach `.RequireAuthorization("Admin")` if admin-only.
4. Define request/response records in same file.
5. Add focused integration test in `src/UCK26.Api.Tests/Integration/`.

#### Add frontend page

1. Create `src/uck26-frontend/src/features/<feature>/...` component + query hooks.
2. Create `src/uck26-frontend/src/pages/<Name>Page.tsx` composing feature and `AppShell`.
3. Register route in `src/uck26-frontend/src/app/router/AppRouter.tsx`.
4. Wrap with `<RequireAuth />` or `<RequireAuth role={Role.Admin} />`.
5. Add Playwright coverage when page has user-facing workflow.

#### Database changes

1. Edit `src/UCK26.Api/Persistence/User.cs` or add entity file.
2. Update `AppDbContext`.
3. Add migration: `dotnet ef migrations add <Name>`.
4. Run backend tests. Delete local `uck26.db` if old local schema blocks manual demo.

### Testing conventions

- TUnit for unit, integration, UI. `[Test]`; `await Assert.That(...).IsEqualTo(...)`.
- Backend integration tests use `TestWebApplicationFactory`: temp SQLite file per factory + `TestAuthHandler` principal injection. Dedicated login tests use real login path.
- UI tests hit `/api/auth/login` once, cache JWT, inject into `localStorage` through Playwright `addInitScript`. `LoginPageTests` still cover form path.
- UI elements located via `data-test-id` — never text/title/label/role. Add `data-test-id` to anything tested.
- Work history uses `work-history-{date}` buttons and `work-history-sidebar` for the sidebar root.
- Every new page needs at least one Playwright test.

### Out of scope for demo

- Azure / Entra ID / MSAL.
- Refresh tokens, password reset emails, MFA.
- Multi-tenancy, role hierarchy beyond `Admin` / `User`.
- Docker, nginx, CI, production deploy.
