# 20-criterion rubric mapping

Each criterion is worth 5 points according to the supplied specification. These are implementation/evidence statuses, not awarded grades. Updated 9 November 2026.

| # | Criterion | Concrete evidence | Remaining evidence / limitation |
|---|---|---|---|
| 1 | Code Structure and Design | Four original layers retained; repository interface, composition root; ARCHITECTURE.md | Student cross-review |
| 2 | Component Reusability | TaskCard on Dashboard/Assignments; CoursePill on three views; StatCard; PlannerFeedback; PlannerPage | Explain parameters/callbacks in defense |
| 3 | Cross-Platform Compatibility | macOS ARM64 .NET and Chromium tests; three-OS CI configuration | Windows/Linux runs and Firefox/Safari not verified |
| 4 | State Management | Scoped PlannerState, busy guard, reload after writes, immutable snapshots, revision conflicts; application/DB tests | No automatic multi-tab push sync |
| 5 | User Interface Design | Consistent tokens, five screens, card/list/chart hierarchy, empty states | Independent usability review |
| 6 | User Experience | Labels, search/filter, explicit errors, confirmation, complete/reopen, refresh, skip link | Full keyboard/screen-reader audit |
| 7 | Responsiveness | Five routes × 360/768/1280 automated checks; screenshots | Physical-device and browser-zoom checks |
| 8 | Core Functionality | Persistent courses/tasks CRUD, Dashboard, Calendar, Analytics; E2E lifecycle, validated JSON import | Single-user local scope |
| 9 | Advanced Features | Adjustable explainable priority, persistent windows/confirmed plans, transactional JSON import/export, optimistic concurrency | Priority weights remain temporary |
| 10 | Error Handling/Resilience | Result, validation, transactions, constraints, conflict handling, storage failure tests; visible feedback | Operational logging/backup UX can improve |
| 11 | Efficiency | Repeatable 10k-task Release benchmark: 2.97ms median / 4.08ms p95 | End-to-end request latency not benchmarked |
| 12 | Scalability | Interface boundary, per-operation DbContext, FK/status/date indexes, UI paging | Full snapshots loaded; SQLite single-writer limits |
| 13 | Resource Utilization | Benchmark records ~1.92MB managed allocations/rank; contexts disposed, no circuit tracker | Whole-app CPU/RSS/load testing pending |
| 14 | Testing Coverage | 60 xUnit tests and 6 Chromium scenarios; Domain 92.85%, Application 98.78% line coverage | Broader combinations and platforms |
| 15 | Quality Assurance | QA_REPORT.md, recorded defect fixes, E2E/static asset checks | CI remote run and complete manual release audit |
| 16 | Documentation | README, current specification, user/dev guides, architecture/ER diagrams, QA and defense guide | Add actual deployment notes after deployment |
| 17 | Innovation | Explainable weighted priorities composed with capacity-limited scheduling | Heuristic is not an optimal/validated recommendation model |
| 18 | Creative FP/Blazor/C#/.NET | Records/with, pure explicit-input rules, LINQ/HOF, Aggregate, Result Map/Bind, reusable Razor | Both students demonstrate understanding |
| 19 | Professionalism | Locked browser dependencies, solution commands, CI workflow, honest evidence/limits | Initialize/use Git, real contributions and PR reviews |
| 20 | Presentation | Russian 15–20m defense script, code walkthrough questions, architecture, measurements, demo sequence | Final named slides, rehearsal, backup video and submission |

Priority before grading: student understanding and contribution evidence → cross-platform/manual checks → polished slides and rehearsed demo → optional extra features. No claim of 100/100 is justified without instructor evaluation.
