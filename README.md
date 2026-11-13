# StudyFlow — Student Planner
Name: Yerkebulan Korganbek, Miras Kanatov


Group: IT-2501

A .NET 10 / Blazor planner for a two-student Introduction to Functional Programming final project. Existing starter extended with SQLite persistence, complete course/task CRUD, dashboard, calendar, analytics and an explainable priority engine.

## Run

Install .NET 10 SDK. From the repository root:

```sh
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet run --project src/StudyFlow.Web --urls http://127.0.0.1:5080
```

Open <http://127.0.0.1:5080>. Start with **Courses → Add course**, then create assignments. No preloaded personal data is required. SQLite migrations run automatically at startup. The default database is `src/StudyFlow.Web/App_Data/studyflow.db`; it survives process restarts. Set `StudyFlow__DatabasePath` to choose another file.

This is a single-user local planner: all browser sessions share the same database. It has no accounts or per-user isolation. Keep local binding for this version. Server-local dates define “today”; the interface uses English formatting. Interactive Server requires a connection. Priority weights are temporary session state; courses, assignments, study windows and confirmed sessions are persistent.

## Features

- Courses: create, rename, recolor, archive/restore, delete empty courses.
- Assignments: create, edit, change course/status/date/effort, complete, reopen, confirm deletion.
- Search, course/status/due-date filters, stable sorting and 12-item page rendering.
- Dashboard: totals, completion, overdue work, ranked recommendations and upcoming deadlines.
- Smart Priority Engine: adjustable urgency/importance/workload weights with visible breakdowns.
- Calendar: monthly deadlines, persistent non-overlapping study windows, capacity-limited proposals and explicitly saved study sessions.
- Analytics: remaining effort, per-course completion, seven-day deadline workload and validated JSON backup export/import.
- Optimistic concurrency, relational constraints, transactional operations and validation/error feedback.

## Verification

```sh
dotnet test --collect:"XPlat Code Coverage" --results-directory artifacts/coverage
dotnet run -c Release --project tools/StudyFlow.Benchmarks
cd tests/e2e
npm ci
npx playwright install chromium
npm test
```

Browser tests start a separate server on port 5081 with an isolated database. See [QA evidence](docs/QA_REPORT.md) for measured results and remaining checks. `.github/workflows/ci.yml` defines a three-OS .NET matrix and Linux Chromium checks; remote execution is not yet verified.

## Architecture and documents

- Domain: immutable records, pure validation, priority, analytics, scheduling and `Result<T>` composition.
- Application: repository contract, asynchronous use cases and scoped planner state.
- Infrastructure: EF Core SQLite, migrations, constraints and concurrency handling.
- Web: five pages and reusable components; composition root wires infrastructure to interfaces.
- Tests: domain, application, file-backed SQLite integration and Playwright browser scenarios.

Read [specification](docs/TECHNICAL_SPECIFICATION.md), [architecture](docs/ARCHITECTURE.md), [user guide](docs/USER_GUIDE.md), [rubric mapping](docs/RUBRIC_MAPPING.md), and [Russian defense guide](docs/DEFENSE_GUIDE_RU.md).

## Remaining release work

Persistent priority preferences, historical completion trends, authentication, dark mode and database-level pagination are not implemented. Cross-OS/browser checks, a deployed demo and actual two-person code reviews still need evidence. Instructor approval/disclosure for AI assistance remains a submission requirement stated in the supplied specification; this repository does not establish approval or guarantee a grade.
