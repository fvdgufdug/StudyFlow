# StudyFlow

StudyFlow is a student planner built with C#, Blazor and SQLite for the Functional Programming final project.

Team: Yerkebulan Korganbek and Miras Kanatov. Group: IT-2501.

## Run the project

Install the .NET 10 SDK, then run these commands from this folder:

```sh
dotnet restore
dotnet build
dotnet test
dotnet run --project src/StudyFlow.Web --urls http://127.0.0.1:5080
```

Open http://127.0.0.1:5080. Add a course, then add assignments.

## What it does

- Courses: add, edit, archive and delete empty courses.
- Assignments: add, edit, complete, search and filter.
- Dashboard: see progress and assignments ordered by priority.
- Calendar: enter available time and save a proposed study plan.
- Analytics: see remaining work and export or import JSON data.

The priority score uses the deadline, importance and estimated duration. It is a calculation, not AI. Select **Why?** to see the calculation.

## How the code is organized

| Project | Purpose |
| --- | --- |
| Domain | Records, validation, priority calculations and scheduling |
| Application | Actions, shared page state and the storage interface |
| Infrastructure | SQLite storage using Entity Framework Core |
| Web | Blazor pages, components and CSS |
| tests | Unit, integration and browser tests |

The main FP examples are records and `with` expressions, pure functions with explicit inputs, LINQ transformations, and `Result` values for errors. Database writes and UI events are handled outside the domain rules.

## Storage and limits

The database is created automatically in `src/StudyFlow.Web/App_Data/studyflow.db`. Set `StudyFlow__DatabasePath` to use another location. Data remains after restarting the app.

This is a local, single-user application. All browser sessions share the database. There are no accounts or cloud synchronization. Priority settings last for the browser session. Dates use the server's local time. Keep the local address above for this version.

Known issues: the scheduler can use elapsed hours today; large exports can exceed the import limit. See `docs/SIMPLIFICATION_NOTES.md`.

## Tests and documentation

Run `dotnet test` for unit and integration tests. Browser tests are in `tests/e2e`; run `npm ci`, `npx playwright install chromium` and `npm test` there after building the application.

- [Architecture](docs/ARCHITECTURE.md)
- [User guide](docs/USER_GUIDE.md)
- [Technology comparison](docs/TECH_COMPARISON.md)
- [Rubric checklist](docs/RUBRIC_MAPPING.md)
- [Recorded test results](docs/QA_REPORT.md)
- [Backup format](docs/BACKUP_FORMAT.md)

Both students must contribute code and be ready to explain every part during the defense. Record actual contributions, not a proposed division of work.

## Syllabus requirements

The course syllabus, page 4, requires explicit instructor permission to use AI tools and treats uncredited or unauthorized AI-generated work as plagiarism. This package does not establish that permission has been granted. Credit permitted assistance accurately; simpler wording or styling does not change these requirements.

Keep a concise project report and the presentation: the syllabus assessment table (pages 9-10) evaluates report/presentation quality and detailed documentation. Explain the design, functional programming concepts, tests, limitations and actual contributions. Each student must be able to explain the code during the defense (page 4). See [syllabus corrections](docs/SYLLABUS_REQUIREMENTS.md).
