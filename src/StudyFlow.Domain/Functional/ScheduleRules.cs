using StudyFlow.Domain.Models;

namespace StudyFlow.Domain.Functional;

public sealed record StudySlot(DateTime Start, int Minutes);
public sealed record StudyAllocation(Guid TaskId, DateTime Start, int Minutes);
public sealed record ScheduleProposal(IReadOnlyList<StudyAllocation> Allocations, int UnallocatedMinutes);

public static class ScheduleRules
{
    public static Result<ScheduleProposal> Propose(IEnumerable<StudyTask> tasks, IEnumerable<StudySlot> slots, DateOnly today, PriorityWeights? weights = null)
    {
        var windows = slots.OrderBy(s => s.Start).ToArray();
        if (windows.Any(s => s.Minutes is < 15 or > 1440 || DateOnly.FromDateTime(s.Start) < today || s.Start > DateTime.MaxValue.AddMinutes(-s.Minutes)))
            return Result<ScheduleProposal>.Failure("Slots must be today or later and contain 15–1440 minutes.");
        if (windows.Zip(windows.Skip(1)).Any(pair => pair.First.Start.AddMinutes(pair.First.Minutes) > pair.Second.Start))
            return Result<ScheduleProposal>.Failure("Study slots cannot overlap.");
        var ranked = TaskRules.Rank(tasks, today, weights);
        var remaining = ranked.ToDictionary(t => t.Id, t => t.EstimatedMinutes);
        var allocations = new List<StudyAllocation>();
        foreach (var slot in windows)
        {
            var cursor = slot.Start;
            var available = slot.Minutes;
            foreach (var task in ranked.Where(t => t.DueDate >= DateOnly.FromDateTime(slot.Start)))
            {
                var deadlineCapacity = (int)Math.Min(available, (task.DueDate.ToDateTime(TimeOnly.MaxValue) - cursor).TotalMinutes + 1);
                var minutes = Math.Min(remaining[task.Id], deadlineCapacity);
                if (minutes <= 0) continue;
                allocations.Add(new(task.Id, cursor, minutes));
                remaining[task.Id] -= minutes;
                available -= minutes;
                cursor = cursor.AddMinutes(minutes);
            }
        }
        return Result<ScheduleProposal>.Success(new(allocations.ToArray(), remaining.Values.Sum()));
    }
}
