using StudyFlow.Application.Contracts;
using StudyFlow.Domain.Functional;
using StudyFlow.Domain.Models;
using TaskStatus = StudyFlow.Domain.Models.TaskStatus;

namespace StudyFlow.Application.Services;

public sealed class PlannerState(IPlannerRepository repository)
{
    public IReadOnlyList<Course> Courses { get; private set; } = Array.Empty<Course>();
    public IReadOnlyList<StudyTask> Tasks { get; private set; } = Array.Empty<StudyTask>();
    public IReadOnlyList<StudyWindow> Windows { get; private set; } = Array.Empty<StudyWindow>();
    public IReadOnlyList<StudySession> Sessions { get; private set; } = Array.Empty<StudySession>();
    public PlannerSnapshot Snapshot => new(Courses, Tasks, Windows, Sessions);
    public bool IsBusy { get; private set; }
    public string? Error { get; private set; }
    public string? Notice { get; private set; }
    public PriorityWeights Weights { get; private set; } = new();
    public event Action? Changed;

    public Task LoadAsync() => Run(() => Task.FromResult<string?>(null), "Planner refreshed.");
    public void SetWeights(PriorityWeights weights)
    {
        Error = weights.IsValid ? null : "Weights must be between 0 and 3, with at least one above zero.";
        if (weights.IsValid) Weights = weights;
        Changed?.Invoke();
    }
    public Task SaveCourseAsync(Guid? id, string name, string color, bool archived, int revision = 0) => Run(async () =>
    {
        var valid = CourseRules.Create(id ?? Guid.NewGuid(), name, color);
        if (!valid.IsSuccess) return valid.Error;
        var result = await repository.SaveCourseAsync(valid.Value! with { Archived = archived, Revision = revision }, id is null);
        return result.Error;
    }, "Course saved.");

    public Task SaveTaskAsync(Guid? id, Guid courseId, string title, DateOnly due, int importance, int minutes, TaskStatus status, int revision = 0) => Run(async () =>
    {
        var valid = TaskRules.Create(id ?? Guid.NewGuid(), courseId, title, due, importance, minutes);
        if (!valid.IsSuccess) return valid.Error;
        if (!Enum.IsDefined(status)) return "Choose a valid status.";
        var result = await repository.SaveTaskAsync(valid.Value! with { Status = status, Revision = revision }, id is null);
        return result.Error;
    }, "Assignment saved.");

    public Task SetStatusAsync(StudyTask task, TaskStatus status) => SaveTaskAsync(task.Id, task.CourseId, task.Title,
        task.DueDate, task.Importance, task.EstimatedMinutes, status, task.Revision);
    public Task DeleteTaskAsync(StudyTask task) => Run(async () => (await repository.DeleteTaskAsync(task)).Error, "Assignment deleted.");
    public Task DeleteCourseAsync(Course course) => Run(async () => (await repository.DeleteCourseAsync(course)).Error, "Course deleted.");

    public Task ImportAsync(string json) => Run(async () =>
    {
        var parsed = BackupCodec.Parse(json);
        return parsed.IsSuccess ? (await repository.ImportAsync(parsed.Value!)).Error : parsed.Error;
    }, "Backup imported. Existing identical records were kept.");
    public Task AddWindowAsync(DateTime start, int minutes, DateOnly today) => Run(async () =>
        (await repository.AddWindowAsync(new(Guid.NewGuid(), start, minutes), today)).Error, "Study window saved.");
    public Task DeleteWindowAsync(StudyWindow window) => Run(async () =>
        (await repository.DeleteWindowAsync(window)).Error, "Study window and its sessions removed.");
    public Task SavePlanAsync(DateOnly today) => Run(async () =>
    {
        var activeWindows = Windows.Where(w => DateOnly.FromDateTime(w.Start) >= today).ToArray();
        var proposal = ScheduleRules.Propose(Tasks, activeWindows.Select(w => new StudySlot(w.Start, w.Minutes)), today, Weights);
        if (!proposal.IsSuccess) return proposal.Error;
        var sessions = proposal.Value!.Allocations.Select(a => new StudySession(Guid.NewGuid(),
            activeWindows.Single(w => a.Start >= w.Start && a.Start.AddMinutes(a.Minutes) <= w.Start.AddMinutes(w.Minutes)).Id,
            a.TaskId, a.Start, a.Minutes)).ToArray();
        return (await repository.SavePlanAsync(Snapshot, sessions, today)).Error;
    }, "Study plan saved.");

    private async Task Reload()
    {
        var snapshot = await repository.LoadAsync();
        Courses = snapshot.Courses;
        Tasks = snapshot.Tasks;
        Windows = snapshot.Windows;
        Sessions = snapshot.Sessions;
    }
    private async Task Run(Func<Task<string?>> operation, string success)
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = null;
        Notice = null;
        Changed?.Invoke();
        try
        {
            Error = await operation();
            await Reload();
            if (Error is null) Notice = success;
        }
        catch (Exception)
        {
            Error = "Storage is unavailable. Your changes may not have been saved. Refresh before retrying.";
        }
        finally
        {
            IsBusy = false;
            Changed?.Invoke();
        }
    }
}
