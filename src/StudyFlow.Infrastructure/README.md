# Infrastructure boundary

EF Core 10 SQLite implements Application's IPlannerRepository. Every operation uses a new factory-created DbContext. Explicit transactions protect snapshot consistency and writes. Foreign keys prohibit deleting a linked course; revisions reject stale writes/deletes. Domain validation and database check constraints reject invalid records.

Program applies versioned migrations on startup. The design-time factory supports `dotnet ef migrations add Name --project src/StudyFlow.Infrastructure`; the generated snapshot records the prior model for subsequent migrations. Runtime storage defaults to Web/App_Data/studyflow.db and can be configured with StudyFlow__DatabasePath. Tests use temporary file databases and migrations, not the EF in-memory provider.

PersistentSchedule adds Windows and Sessions, with cascading session deletion for removed windows/tasks. Import and plan replacement are transactionally atomic. The repository rejects conflicting backup IDs and stale plan snapshots. Failure-injection tests verify rollback after an SQLite write error. See docs/BACKUP_FORMAT.md for the merge policy and historical schedule semantics.
