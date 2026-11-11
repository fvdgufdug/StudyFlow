# QA evidence — 9 November 2026

## Environment and results

Local macOS 26.6.2 ARM64, .NET SDK 10.0.403 / runtime 10.0.14. Browser automation: Playwright 1.58.2, Chromium 145.0.7632.6. All figures below are local measurements, not remote CI or instructor grades.

- `dotnet restore`: succeeded. Initial restores used `-p:NuGetAudit=false` while resolving local sandbox/cache access; the final plain `dotnet restore` also succeeded.
- Debug and Release builds succeeded, zero warnings/errors.
- Debug and Release test runs: 60 passed, 0 failed/skipped.
- `dotnet publish src/StudyFlow.Web -c Release --no-restore -o artifacts/publish`: succeeded; local release artifact created, no public deployment performed.
- `npm test` from tests/e2e: 6 Chromium scenarios passed (9.4 seconds in recorded run).
- Five routes at each of 360, 768, 1280 CSS px: no horizontal page overflow. Screenshots saved under artifacts/screenshots; mobile analytics visually inspected.
- Browser CRUD: course create/rename/archive/restore/delete restriction, assignment create/edit/complete/reopen/delete, search/status filters, refresh persistence, JSON export.
- Calendar: add/remove availability and overlap rejection.
- Process restart: after stopping and restarting the 5080 app, “FP final presentation” retained its edited title and 120-minute estimate.

## Coverage

Command: `dotnet test --collect:"XPlat Code Coverage" --results-directory artifacts/import-coverage`.

| Assembly | Line coverage | Branch coverage |
|---|---:|---:|
| StudyFlow.Domain | 92.85% | 90.67% |
| StudyFlow.Application | 98.78% | 87.03% |
| StudyFlow.Infrastructure | 98.74% | 96.87% |

Coverage includes integration tests and generated migration code in Infrastructure; it does not measure Razor/browser coverage. XML lives in artifacts/import-coverage under the generated run directory. Line coverage is not a correctness guarantee.

## Performance

Command: `dotnet run -c Release --project tools/StudyFlow.Benchmarks`.

10,000 deterministic generated assignments, 5 warmups, 25 measured in-process ranking iterations. Median **2.9661ms**, p95 **4.0758ms**, maximum **5.4871ms**; mean allocated bytes **1,921,167** per ranking. Raw output: artifacts/benchmark.json. Includes ranking and score objects; excludes database, SignalR, rendering and network. This is a lightweight repeatable microbenchmark, not a load test or BenchmarkDotNet study. Whole-app CPU/RSS is not measured.

## Defects found and fixed

1. No root solution: plain restore/build/test could not find a project. Added StudyFlow.slnx containing existing projects and benchmark tool.
2. Ambiguous TaskStatus collided with System.Threading.Tasks.TaskStatus. Added explicit aliases.
3. Pure validation generated random IDs. Moved ID creation to Application and made domain creation deterministic.
4. Local production-mode run did not serve Blazor framework assets. Enabled static asset manifests and endpoint mapping; E2E checks asset responses.
5. PlannerFeedback could retain stale notice because an unchanged child component was not refreshed. Subscribed it to state changes and disposed the subscription.
6. Storage reload failure could coexist with a success notice. Assign success only after successful reload; regression test verifies failure state.
7. Prerendered forms appeared before event handlers connected. Disabled planner prerender to make controls available with interactivity.
8. Browser test initially typed before async edit-mode rendering completed. Wait for Edit heading before filling; full lifecycle rerun passed.

## Reproduction and limitations

Run README commands from a clean working copy. E2E uses port 5081 and a disposable database under tests/e2e/test-results, leaving default planner data alone. Test screenshots/reports and local data are ignored by Git. Unit/integration tests use temporary SQLite files; they validate migration idempotence/model snapshot, referential integrity, archived-course rules, revisions, failure recovery and busy-state exclusion.

Not verified: Windows/Linux runtime behavior, Firefox/Safari, actual touch devices, full keyboard/screen-reader/zoom audit, CI execution, public deployment, sustained concurrent load or historical analytics (not implemented). The workflow defines checks for other OSes, but a configuration is not evidence they passed. No Git repository was present initially; no commits, PR reviews or student contribution history have been fabricated.


## Import and persistent schedule update

- Migration PersistentSchedule is applied after InitialSchema. An integration test downgrades an isolated test DB to the original schema, inserts a course/task, upgrades and verifies that both survive.
- Versioned JSON export round-trips all four collections. Legacy exports without version/windows/sessions still load. Tests cover malformed/oversized/null input, missing fields, duplicate IDs/properties, unsupported versions, bad references, invalid enum values and invalid time ranges.
- Integration tests verify idempotent merge, ID conflicts, overlap against existing windows, stale plan rejection, persisted window/session reload and cascading session removal.
- SQLite trigger fault injection rejects a session insert after earlier work in the transaction. Both importing a backup and replacing an existing plan roll back completely; the saved data remains intact.
- Browser test selects malformed and valid files, checks the preview/confirmation, verifies conflict rejection without partial imports, saves a proposal, reloads, exports/reimports all schedule data and checks cleanup.
- The new file input initially caused 2px overflow at 360px. Constraining its width fixed the regression; all three responsive checks pass again.
- Coverage above was recalculated for the new implementation. Microbenchmark results remain the earlier ranking measurement; ranking logic was not changed by this update.
