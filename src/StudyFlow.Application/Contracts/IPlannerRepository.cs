using StudyFlow.Domain.Functional;
using StudyFlow.Domain.Models;

namespace StudyFlow.Application.Contracts;

public sealed record PlannerSnapshot(IReadOnlyList<Course> Courses, IReadOnlyList<StudyTask> Tasks,
    IReadOnlyList<StudyWindow> Windows, IReadOnlyList<StudySession> Sessions)
{
    public PlannerSnapshot(IReadOnlyList<Course> courses, IReadOnlyList<StudyTask> tasks)
        : this(courses, tasks, Array.Empty<StudyWindow>(), Array.Empty<StudySession>()) { }
}
public interface IPlannerRepository
{
    Task<PlannerSnapshot> LoadAsync();
    Task<Result<bool>> ImportAsync(PlannerSnapshot snapshot);
    Task<Result<StudyWindow>> AddWindowAsync(StudyWindow window, DateOnly today);
    Task<Result<bool>> DeleteWindowAsync(StudyWindow window);
    Task<Result<bool>> SavePlanAsync(PlannerSnapshot expected, IReadOnlyList<StudySession> sessions, DateOnly today);
    Task<Result<Course>> SaveCourseAsync(Course course, bool isNew);
    Task<Result<StudyTask>> SaveTaskAsync(StudyTask task, bool isNew);
    Task<Result<bool>> DeleteCourseAsync(Course course);
    Task<Result<bool>> DeleteTaskAsync(StudyTask task);
}
