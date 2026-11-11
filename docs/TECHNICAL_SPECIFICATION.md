# StudyFlow — Technical Specification

**Course:** Introduction to Functional Programming (FP 3222). **Team:** two students, names pending. **Updated:** 9 November 2026. **Status:** implemented local planner with verified .NET/SQLite/Chromium flows; release and defense evidence still incomplete.

## Source requirements and design choices

The supplied project specification records these instructor requirements: individual or maximum two-person team; both members code; a 15–20 minute oral defense with either member questioned on every module; 20 criteria worth 5% each; presentation covering team/title/aim/goals/plan, technology comparison, architecture, relevance, methodology and mockups. It also records syllabus permission/disclosure requirements for AI contributions. Original instructor handouts are not present in this repository, so this mapping relies on the supplied specification and is not an independent verification of those handouts.

.NET 10, Blazor Interactive Server, EF Core SQLite and CI are team design choices. The intended result is a strong final project, not a guaranteed grade.

## Product and implemented acceptance

| ID | Requirement | Current implementation / evidence |
|---|---|---|
| FR-01 | Courses | Add, edit name/color, archive/restore, delete empty course; CRUD and referential safety tested |
| FR-02 | Task creation | Title, course, date, importance 1–5, 15–1440 estimated minutes; domain/server/browser validation |
| FR-03 | Task maintenance | Edit all fields, complete/reopen, confirm delete; persisted and tested |
| FR-04 | Search/filter | Title search, course/status/latest-date filters, priority/date/title sort, 12-card pages |
| FR-05 | Priority | Explicit date and weights, deterministic ties, component breakdown; unit tests and UI |
| FR-06 | Dashboard | Counts, completion, overdue, ranked tasks, next seven days |
| FR-07 | Schedule | Persistent windows and confirmed sessions; overlap/capacity/deadline validation, stale-snapshot protection, explicit plan replacement |
| FR-08 | Persistence | SQLite migrations, per-operation context, transactions, concurrency revisions; repository and process restart checks |
| FR-09 | Data portability | Versioned JSON export/import, prevalidation, conflict-safe merge and transaction rollback; legacy exports supported |
| FR-10 | Responsive UI | Five routes at 360/768/1280 px tested in Chromium/macOS; other platforms pending |

## Functional core

Domain contains sealed immutable Course and StudyTask records, pure validation, Result Map/Bind, priority scoring, analytics and scheduling. Application generates IDs and coordinates storage. Web supplies today's server-local date. Domain does not access time, random IDs, EF or UI.

Priority: `(overdue ? 75 : 60/(days+1)) × urgencyWeight + importance × 12 × importanceWeight + min(hours,12) × 1.5 × workloadWeight`. Weights are finite [0,3] with positive sum. Done tasks score zero. Ties use date, case-insensitive ordinal title and ID. This heuristic favors urgency and importance while considering estimated workload; it has not been scientifically validated.

Scheduling uses ranked work and explicit windows. Completed tasks are excluded, overdue tasks remain unallocated, and tasks are split when capacity is insufficient. Allocations never modify tasks. In-progress tasks retain their full estimated effort because actual tracked minutes are out of scope.

## Architecture and data

Web → Application → Domain. Infrastructure implements Application's IPlannerRepository. Program is the composition root. SQLite uses Courses and Tasks with foreign keys, indexes and check constraints. Revision protects updates/deletes from stale clients. See [architecture and ER diagram](ARCHITECTURE.md).

Course: Id, Name, ColorHex, Archived, Revision. StudyTask: Id, CourseId, Title, DueDate, Importance, EstimatedMinutes, Status, Revision. StudyWindow: Id, Start, Minutes, Revision. StudySession: Id, WindowId, TaskId, Start, Minutes. Windows/sessions persist in SQLite; only priority weights remain temporary UI state. Created/updated/completed timestamps and description are not part of this implementation.

## Non-functional targets and measured results

- Domain/Application line coverage target ≥80%: measured 92.85% / 98.78%; see QA report for branches and scope.
- Ranking 10,000 tasks target <250ms: measured median 2.97ms, p95 4.08ms in Release on macOS ARM64; excludes I/O and UI.
- Responsive widths 360/768/1280: automated no-overflow checks on five routes plus screenshots.
- Restart persistence: tested with file-backed repositories and a stopped/restarted application process.
- Accessibility: labels, keyboard focus, skip link, live feedback, readable chart data, confirmation controls; full assistive-technology audit pending.
- Portability: macOS Chromium measured. CI matrix for Linux/Windows/macOS authored but not run remotely.

## Limits and next release

Single-user local deployment; all sessions share one DB. No authentication or authorization boundary. Interactive Server requires an active connection. Server timezone determines dates. UI paging is not DB paging. No historical completion chart, persistent priority preferences, dark mode or multi-user sync. No remote deployment verified. Current tests exercise domain, application, SQLite integration and Chromium flows; not every browser or interaction is covered.

## Team work and submission

Student A proposed ownership: domain, priority/scheduling, application/repository tests, benchmarks. Student B: reusable components, CRUD forms, analytics/calendar UI, browser tests and docs. Both must review and explain the whole architecture. Actual contribution history must be recorded from their work; this workspace initially has no Git repository, so commits/PR reviews are not available as evidence.

Remaining submission items: instructor permission/disclosure, actual student contributions and reviews, additional platform checks, final slide deck with names, rehearsed demo, optional published demo/backup recording, LMS submission. See [rubric mapping](RUBRIC_MAPPING.md), [defense guide](DEFENSE_GUIDE_RU.md), and [release backlog](SPRINT_2_BACKLOG.md).
