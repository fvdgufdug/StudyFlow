using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using StudyFlow.Application.Contracts;
using StudyFlow.Domain.Functional;
using StudyFlow.Domain.Models;

namespace StudyFlow.Application.Services;

public static class BackupCodec
{
    public const int MaxBytes = 2 * 1024 * 1024;
    private static readonly JsonSerializerOptions Options = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
        MaxDepth = 16
    };
    public static byte[] Export(PlannerSnapshot snapshot) => JsonSerializer.SerializeToUtf8Bytes(new BackupDocument
    {
        Courses = snapshot.Courses.ToArray(), Tasks = snapshot.Tasks.ToArray(),
        Windows = snapshot.Windows.ToArray(), Sessions = snapshot.Sessions.ToArray()
    }, Options);
    public static Result<PlannerSnapshot> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || Encoding.UTF8.GetByteCount(json) > MaxBytes)
            return Result<PlannerSnapshot>.Failure("Choose a nonempty JSON backup no larger than 2 MiB.");
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
            CheckProperties(document.RootElement);
            var backup = JsonSerializer.Deserialize<BackupDocument>(json, Options);
            if (backup is null || backup.SchemaVersion != 1)
                return Result<PlannerSnapshot>.Failure("Unsupported backup version. Expected SchemaVersion 1.");
            if (backup.Courses is null || backup.Tasks is null || backup.Windows is null || backup.Sessions is null)
                return Result<PlannerSnapshot>.Failure("Backup collections must be arrays, not null.");
            var snapshot = new PlannerSnapshot(backup.Courses, backup.Tasks, backup.Windows, backup.Sessions);
            return Validate(snapshot).Map(_ => snapshot);
        }
        catch (JsonException)
        {
            return Result<PlannerSnapshot>.Failure("Invalid JSON backup: check required fields, types, duplicate or unknown properties.");
        }
    }
    public static Result<bool> Validate(PlannerSnapshot snapshot)
    {
        if (snapshot.Courses is null || snapshot.Tasks is null || snapshot.Windows is null || snapshot.Sessions is null)
            return Result<bool>.Failure("Backup collections are required.");
        if ((long)snapshot.Courses.Count + snapshot.Tasks.Count + snapshot.Windows.Count + snapshot.Sessions.Count > 10000)
            return Result<bool>.Failure("A backup may contain at most 10,000 records.");
        if (snapshot.Courses.Any(c => c is null) || snapshot.Tasks.Any(t => t is null)
            || snapshot.Windows.Any(w => w is null) || snapshot.Sessions.Any(s => s is null))
            return Result<bool>.Failure("Backup arrays cannot contain null records.");
        if (snapshot.Courses.Select(c => c.Id).Distinct().Count() != snapshot.Courses.Count
            || snapshot.Tasks.Select(t => t.Id).Distinct().Count() != snapshot.Tasks.Count)
            return Result<bool>.Failure("Duplicate course or assignment IDs.");
        foreach (var course in snapshot.Courses)
        {
            var valid = CourseRules.Create(course.Id, course.Name, course.ColorHex);
            if (!valid.IsSuccess || course.Revision < 0) return Result<bool>.Failure(valid.Error ?? "Invalid course revision.");
        }
        var courses = snapshot.Courses.Select(c => c.Id).ToHashSet();
        foreach (var task in snapshot.Tasks)
        {
            var valid = TaskRules.Create(task.Id, task.CourseId, task.Title, task.DueDate, task.Importance, task.EstimatedMinutes);
            if (!valid.IsSuccess) return Result<bool>.Failure(valid.Error!);
            if (!Enum.IsDefined(task.Status) || task.Revision < 0 || !courses.Contains(task.CourseId))
                return Result<bool>.Failure("Invalid assignment status/revision or missing course reference.");
        }
        return StoredScheduleRules.Validate(snapshot.Windows, snapshot.Sessions, snapshot.Tasks);
    }
    private static void CheckProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new JsonException();
                CheckProperties(property.Value);
            }
            if (names.Contains("Id"))
            {
                var required = names.Contains("CourseId")
                    ? new[] { "Id", "CourseId", "Title", "DueDate", "Importance", "EstimatedMinutes", "Status", "Revision" }
                    : names.Contains("WindowId") ? new[] { "Id", "WindowId", "TaskId", "Start", "Minutes" }
                    : names.Contains("Start") ? new[] { "Id", "Start", "Minutes", "Revision" }
                    : new[] { "Id", "Name", "ColorHex", "Archived", "Revision" };
                if (required.Any(name => !names.Contains(name))) throw new JsonException();
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var child in element.EnumerateArray()) CheckProperties(child);
    }
    private sealed class BackupDocument
    {
        public int SchemaVersion { get; init; } = 1;
        [JsonRequired] public Course[] Courses { get; init; } = [];
        [JsonRequired] public StudyTask[] Tasks { get; init; } = [];
        public StudyWindow[] Windows { get; init; } = [];
        public StudySession[] Sessions { get; init; } = [];
    }
}
