# JSON backup format and import behavior

Export from Analytics downloads a UTF-8 JSON object:

```json
{
  "SchemaVersion": 1,
  "Courses": [],
  "Tasks": [],
  "Windows": [],
  "Sessions": []
}
```

Property names are case-sensitive. Courses and Tasks are required arrays; Windows and Sessions default to empty for compatibility with earlier exports. A missing SchemaVersion is treated as legacy version 1; explicitly unsupported versions are rejected. Unknown/duplicate properties, null collections/records, malformed JSON, incorrect types and missing record fields are rejected. Revision is a nonnegative integer. Files are limited to 2 MiB, depth 16, and at most 10,000 records in the resulting combined dataset.

| Collection | Record fields |
|---|---|
| Courses | Id (GUID), Name (1–80 nonblank chars), ColorHex (#RRGGBB), Archived (boolean), Revision |
| Tasks | Id, CourseId, Title (1–120 nonblank chars), DueDate (YYYY-MM-DD), Importance (1–5), EstimatedMinutes (15–1440), Status (0 ToDo / 1 InProgress / 2 Done), Revision |
| Windows | Id, Start (local YYYY-MM-DDTHH:mm:ss with no offset), Minutes (15–1440), Revision |
| Sessions | Id, WindowId, TaskId, Start, Minutes (1–1440) |

IDs must be nonempty and unique within their collection. Every task's course and session's task/window must exist in the backup itself. Start values must have whole-minute precision, no timezone suffix/offset and a valid nonoverflowing end. Windows cannot overlap. Sessions cannot overlap and must fit entirely within their referenced windows. Adjacent intervals are allowed.

Backups may include archived courses, completed assignments and past sessions. Saved sessions are snapshots and can outlive task deadline/estimate/status edits; importing history does not apply current-date or active-task checks. Saving a new proposal applies those additional checks and never schedules completed work, work after deadlines, or more than the task estimate.

## Atomic merge

1. Read and validate the entire document. UI shows record counts without writing anything.
2. After the user clicks Import validated backup, validate again at the application/storage boundary.
3. Within one SQLite transaction, load current data and compare IDs. Same content is skipped; different content under the same ID aborts the whole import. Revision differences alone are ignored.
4. Validate the combined schedule, including overlaps with existing windows/sessions and the total record limit.
5. Insert new records together and commit. New records start with Revision 0; existing revisions stay unchanged. Any database failure rolls back every insertion.

This operation never deletes or overwrites existing records. To restore an older backup whose IDs now conflict, use a separate empty database (`StudyFlow__DatabasePath`) rather than erasing current work. Reimporting an unchanged export is idempotent. Import preview counts describe the file, not the number of new insertions.

## Persistent schedule lifecycle

Windows save automatically. Save current proposal persists a snapshot of the current suggested sessions. Existing sessions require explicit replacement confirmation; saving replaces all sessions, including past ones. Concurrent changes to tasks, windows or saved sessions cause rejection and require refresh. Removing a window or assignment cascades its sessions. Editing/completing an assignment leaves its saved sessions in place; regenerate the plan explicitly if desired. Priority weights are not included in backups; the accepted session allocation is stored directly.
