using StudyFlow.Domain.Models;
using TaskStatus = StudyFlow.Domain.Models.TaskStatus;

namespace StudyFlow.Domain.Functional;

public static class StoredScheduleRules
{
    public static Result<bool> Validate(IEnumerable<StudyWindow> windows, IEnumerable<StudySession> sessions, IEnumerable<StudyTask> tasks)
    {
        var ordered = windows.OrderBy(w => w.Start).ToArray();
        if (ordered.Any(w => w.Id == Guid.Empty || w.Revision < 0 || !ValidRange(w.Start, w.Minutes, 15)))
            return Result<bool>.Failure("Study windows require an ID, local date/time and 15–1440 minutes.");
        if (ordered.Select(w => w.Id).Distinct().Count() != ordered.Length)
            return Result<bool>.Failure("Duplicate study window IDs.");
        if (ordered.Zip(ordered.Skip(1)).Any(p => p.First.Start.AddMinutes(p.First.Minutes) > p.Second.Start))
            return Result<bool>.Failure("Study windows cannot overlap.");
        var windowsById = ordered.ToDictionary(w => w.Id);
        var tasksById = tasks.ToDictionary(t => t.Id);
        var planned = sessions.OrderBy(s => s.Start).ToArray();
        if (planned.Select(s => s.Id).Distinct().Count() != planned.Length)
            return Result<bool>.Failure("Duplicate study session IDs.");
        foreach (var session in planned)
        {
            if (session.Id == Guid.Empty || !ValidRange(session.Start, session.Minutes, 1))
                return Result<bool>.Failure("Study sessions require an ID, local date/time and 1–1440 minutes.");
            if (!windowsById.TryGetValue(session.WindowId, out var window) || !tasksById.ContainsKey(session.TaskId))
                return Result<bool>.Failure("A study session references a missing window or assignment.");
            if (session.Start < window.Start || session.Start.AddMinutes(session.Minutes) > window.Start.AddMinutes(window.Minutes))
                return Result<bool>.Failure("A study session exceeds its window.");
        }
        if (planned.Zip(planned.Skip(1)).Any(p => p.First.Start.AddMinutes(p.First.Minutes) > p.Second.Start))
            return Result<bool>.Failure("Study sessions cannot overlap.");
        return Result<bool>.Success(true);
    }
    public static Result<bool> ValidateNewPlan(IEnumerable<StudyWindow> windows, IEnumerable<StudySession> sessions, IEnumerable<StudyTask> tasks, DateOnly today)
    {
        var allTasks = tasks.ToArray();
        var allSessions = sessions.ToArray();
        var validation = Validate(windows, allSessions, allTasks);
        if (!validation.IsSuccess) return validation;
        var byId = allTasks.ToDictionary(t => t.Id);
        if (allSessions.Any(s => DateOnly.FromDateTime(s.Start) < today || byId[s.TaskId].Status == TaskStatus.Done
            || DateOnly.FromDateTime(s.Start.AddMinutes(s.Minutes).AddTicks(-1)) > byId[s.TaskId].DueDate))
            return Result<bool>.Failure("Refresh the proposal: sessions must be current, unfinished and before deadlines.");
        if (allSessions.GroupBy(s => s.TaskId).Any(g => g.Sum(s => s.Minutes) > byId[g.Key].EstimatedMinutes))
            return Result<bool>.Failure("Planned time exceeds the assignment estimate.");
        return Result<bool>.Success(true);
    }
    private static bool ValidRange(DateTime start, int minutes, int minimum) =>
        start != default && start.Kind == DateTimeKind.Unspecified && minutes >= minimum && minutes <= 1440
        && start <= DateTime.MaxValue.AddMinutes(-minutes) && start.Ticks % TimeSpan.TicksPerMinute == 0;
}
