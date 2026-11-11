using StudyFlow.Domain.Models;
using TaskStatus = StudyFlow.Domain.Models.TaskStatus;

namespace StudyFlow.Domain.Functional;

public sealed record PriorityWeights(double Urgency = 1, double Importance = 1, double Workload = 1)
{
    public bool IsValid => new[] { Urgency, Importance, Workload }.All(w => double.IsFinite(w) && w >= 0 && w <= 3)
        && Urgency + Importance + Workload > 0;
}

public sealed record PriorityBreakdown(double Urgency, double Importance, double Workload)
{
    public double Total => Math.Round(Urgency + Importance + Workload, 2);
}

public static class TaskRules
{
    public static Result<StudyTask> Create(Guid id, Guid courseId, string title, DateOnly dueDate, int importance, int estimatedMinutes)
    {
        if (id == Guid.Empty) return Result<StudyTask>.Failure("Task ID is required.");
        if (courseId == Guid.Empty) return Result<StudyTask>.Failure("Select a course.");
        if (string.IsNullOrWhiteSpace(title)) return Result<StudyTask>.Failure("Title is required.");
        if (title.Trim().Length > 120) return Result<StudyTask>.Failure("Title is too long (max 120).");
        if (dueDate == default) return Result<StudyTask>.Failure("Choose a valid due date.");
        if (importance is < 1 or > 5) return Result<StudyTask>.Failure("Importance must be 1–5.");
        if (estimatedMinutes is < 15 or > 1440) return Result<StudyTask>.Failure("Estimated time must be 15–1440 minutes.");
        return Result<StudyTask>.Success(new(id, courseId, title.Trim(), dueDate, importance, estimatedMinutes));
    }

    public static PriorityBreakdown Explain(StudyTask task, DateOnly today, PriorityWeights? weights = null)
    {
        var w = weights ?? new();
        if (!w.IsValid) throw new ArgumentOutOfRangeException(nameof(weights));
        if (task.Status == TaskStatus.Done) return new(0, 0, 0);
        var days = task.DueDate.DayNumber - today.DayNumber;
        return new((days < 0 ? 75 : 60.0 / (days + 1)) * w.Urgency,
            task.Importance * 12 * w.Importance,
            Math.Min(task.EstimatedMinutes / 60.0, 12) * 1.5 * w.Workload);
    }

    public static double Priority(StudyTask task, DateOnly today) => Explain(task, today).Total;
    public static IReadOnlyList<StudyTask> Rank(IEnumerable<StudyTask> tasks, DateOnly today, PriorityWeights? weights = null) =>
        tasks.Where(t => t.Status != TaskStatus.Done)
            .Select(t => (Task: t, Score: Explain(t, today, weights).Total))
            .OrderByDescending(t => t.Score).ThenBy(t => t.Task.DueDate)
            .ThenBy(t => t.Task.Title, StringComparer.OrdinalIgnoreCase).ThenBy(t => t.Task.Id)
            .Select(t => t.Task).ToArray();

    public static StudyTask Complete(StudyTask task) => task with { Status = TaskStatus.Done };
    public static int CompletionPercent(IEnumerable<StudyTask> tasks)
    {
        var counts = tasks.Aggregate((Total: 0, Done: 0), (a, t) => (a.Total + 1, a.Done + (t.Status == TaskStatus.Done ? 1 : 0)));
        return counts.Total == 0 ? 0 : (int)Math.Round(100.0 * counts.Done / counts.Total);
    }
}
