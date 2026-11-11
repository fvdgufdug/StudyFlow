using StudyFlow.Domain.Models;
using TaskStatus = StudyFlow.Domain.Models.TaskStatus;

namespace StudyFlow.Domain.Functional;

public sealed record PlannerSummary(int Total, int Completed, int Overdue, int CompletionPercent, int RemainingMinutes);
public sealed record WorkloadDay(DateOnly Date, int Minutes, int Count);
public sealed record CourseProgress(Course Course, int Total, int Completed, int RemainingMinutes);

public static class AnalyticsRules
{
    public static PlannerSummary Summarize(IEnumerable<StudyTask> tasks, DateOnly today)
    {
        var all = tasks.ToArray();
        var active = all.Where(t => t.Status != TaskStatus.Done).ToArray();
        return new(all.Length, all.Length - active.Length, active.Count(t => t.DueDate < today),
            TaskRules.CompletionPercent(all), active.Sum(t => t.EstimatedMinutes));
    }
    public static IReadOnlyList<WorkloadDay> Week(IEnumerable<StudyTask> tasks, DateOnly start)
    {
        var grouped = tasks.Where(t => t.Status != TaskStatus.Done).GroupBy(t => t.DueDate)
            .ToDictionary(g => g.Key, g => (Minutes: g.Sum(t => t.EstimatedMinutes), Count: g.Count()));
        return Enumerable.Range(0, 7).Select(i => start.AddDays(i)).Select(d =>
            new WorkloadDay(d, grouped.GetValueOrDefault(d).Minutes, grouped.GetValueOrDefault(d).Count)).ToArray();
    }
    public static IReadOnlyList<CourseProgress> ByCourse(IEnumerable<Course> courses, IEnumerable<StudyTask> tasks)
    {
        var grouped = tasks.ToLookup(t => t.CourseId);
        return courses.Select(c => new CourseProgress(c, grouped[c.Id].Count(),
            grouped[c.Id].Count(t => t.Status == TaskStatus.Done),
            grouped[c.Id].Where(t => t.Status != TaskStatus.Done).Sum(t => t.EstimatedMinutes))).ToArray();
    }
}
