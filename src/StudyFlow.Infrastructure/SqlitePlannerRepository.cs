using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Contracts;
using StudyFlow.Application.Services;
using StudyFlow.Domain.Functional;
using StudyFlow.Domain.Models;

namespace StudyFlow.Infrastructure;

public sealed class SqlitePlannerRepository(IDbContextFactory<PlannerDbContext> factory) : IPlannerRepository
{
    public async Task<PlannerSnapshot> LoadAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var courses = await db.Courses.AsNoTracking().OrderBy(c => c.Name).ToArrayAsync();
        var tasks = await db.Tasks.AsNoTracking().OrderBy(t => t.DueDate).ThenBy(t => t.Id).ToArrayAsync();
        var windows = await db.Windows.AsNoTracking().OrderBy(w => w.Start).ToArrayAsync();
        var sessions = await db.Sessions.AsNoTracking().OrderBy(s => s.Start).ToArrayAsync();
        await transaction.CommitAsync();
        return new(courses, tasks, windows, sessions);
    }
    public async Task<Result<Course>> SaveCourseAsync(Course course, bool isNew)
    {
        var validation = CourseRules.Create(course.Id, course.Name, course.ColorHex);
        if (!validation.IsSuccess) return Result<Course>.Failure(validation.Error!);
        return await Write(async db =>
        {
            var next = course with { Revision = isNew ? 0 : course.Revision + 1 };
            if (isNew) db.Courses.Add(next);
            else
            {
                db.Courses.Update(next);
                db.Entry(next).Property(c => c.Revision).OriginalValue = course.Revision;
            }
            await db.SaveChangesAsync();
            return Result<Course>.Success(next);
        });
    }
    public async Task<Result<StudyTask>> SaveTaskAsync(StudyTask task, bool isNew)
    {
        var valid = TaskRules.Create(task.Id, task.CourseId, task.Title, task.DueDate, task.Importance, task.EstimatedMinutes);
        if (!valid.IsSuccess) return Result<StudyTask>.Failure(valid.Error!);
        if (!Enum.IsDefined(task.Status)) return Result<StudyTask>.Failure("Choose a valid status.");
        return await Write(async db =>
        {
            var course = await db.Courses.AsNoTracking().SingleOrDefaultAsync(c => c.Id == task.CourseId);
            if (course is null) return Result<StudyTask>.Failure("The selected course no longer exists.");
            var existing = isNew ? null : await db.Tasks.AsNoTracking().SingleOrDefaultAsync(t => t.Id == task.Id);
            if (course.Archived && (isNew || existing?.CourseId != course.Id))
                return Result<StudyTask>.Failure("Restore the archived course before adding assignments.");
            var next = task with { Revision = isNew ? 0 : task.Revision + 1 };
            if (isNew) db.Tasks.Add(next);
            else
            {
                db.Tasks.Update(next);
                db.Entry(next).Property(t => t.Revision).OriginalValue = task.Revision;
            }
            await db.SaveChangesAsync();
            return Result<StudyTask>.Success(next);
        });
    }
    public Task<Result<bool>> DeleteCourseAsync(Course course) => Write(async db =>
    {
        if (await db.Tasks.AnyAsync(t => t.CourseId == course.Id))
            return Result<bool>.Failure("This course has assignments. Archive it or move/delete its assignments first.");
        db.Courses.Remove(course);
        await db.SaveChangesAsync();
        return Result<bool>.Success(true);
    });
    public Task<Result<bool>> DeleteTaskAsync(StudyTask task) => Write(async db =>
    {
        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
        return Result<bool>.Success(true);
    });
    public Task<Result<StudyWindow>> AddWindowAsync(StudyWindow window, DateOnly today) => Write(async db =>
    {
        if (DateOnly.FromDateTime(window.Start) < today)
            return Result<StudyWindow>.Failure("New study windows must be today or later.");
        var windows = await db.Windows.AsNoTracking().ToArrayAsync();
        var valid = StoredScheduleRules.Validate(windows.Append(window), [], []);
        if (!valid.IsSuccess) return Result<StudyWindow>.Failure(valid.Error!);
        db.Windows.Add(window with { Revision = 0 });
        await db.SaveChangesAsync();
        return Result<StudyWindow>.Success(window with { Revision = 0 });
    });
    public Task<Result<bool>> DeleteWindowAsync(StudyWindow window) => Write(async db =>
    {
        db.Windows.Remove(window);
        await db.SaveChangesAsync();
        return Result<bool>.Success(true);
    });
    public Task<Result<bool>> SavePlanAsync(PlannerSnapshot expected, IReadOnlyList<StudySession> sessions, DateOnly today) => Write(async db =>
    {
        var tasks = await db.Tasks.AsNoTracking().ToArrayAsync();
        var windows = await db.Windows.AsNoTracking().ToArrayAsync();
        var stored = await db.Sessions.AsNoTracking().ToArrayAsync();
        if (!tasks.OrderBy(t => t.Id).SequenceEqual(expected.Tasks.OrderBy(t => t.Id))
            || !windows.OrderBy(w => w.Id).SequenceEqual(expected.Windows.OrderBy(w => w.Id))
            || !stored.OrderBy(s => s.Id).SequenceEqual(expected.Sessions.OrderBy(s => s.Id)))
            return Result<bool>.Failure("The planner changed in another tab. Refresh before saving the proposal.");
        var valid = StoredScheduleRules.ValidateNewPlan(windows, sessions, tasks, today);
        if (!valid.IsSuccess) return valid;
        await db.Sessions.ExecuteDeleteAsync();
        db.Sessions.AddRange(sessions);
        await db.SaveChangesAsync();
        return Result<bool>.Success(true);
    });
    public async Task<Result<bool>> ImportAsync(PlannerSnapshot snapshot)
    {
        var validation = BackupCodec.Validate(snapshot);
        if (!validation.IsSuccess) return validation;
        return await Write(async db =>
        {
            var courses = await db.Courses.AsNoTracking().ToArrayAsync();
            var tasks = await db.Tasks.AsNoTracking().ToArrayAsync();
            var windows = await db.Windows.AsNoTracking().ToArrayAsync();
            var sessions = await db.Sessions.AsNoTracking().ToArrayAsync();
            var incomingCourses = snapshot.Courses.Select(c => c with { Revision = 0 }).ToArray();
            var incomingTasks = snapshot.Tasks.Select(t => t with { Revision = 0 }).ToArray();
            var incomingWindows = snapshot.Windows.Select(w => w with { Revision = 0 }).ToArray();
            if (Conflicts(courses.Select(c => c with { Revision = 0 }), incomingCourses, c => c.Id)
                || Conflicts(tasks.Select(t => t with { Revision = 0 }), incomingTasks, t => t.Id)
                || Conflicts(windows.Select(w => w with { Revision = 0 }), incomingWindows, w => w.Id)
                || Conflicts(sessions, snapshot.Sessions, s => s.Id))
                return Result<bool>.Failure("Import conflicts with an existing ID. No data was imported. Existing records are never overwritten.");
            var newCourses = incomingCourses.ExceptBy(courses.Select(x => x.Id), c => c.Id).ToArray();
            var newTasks = incomingTasks.ExceptBy(tasks.Select(x => x.Id), t => t.Id).ToArray();
            var newWindows = incomingWindows.ExceptBy(windows.Select(x => x.Id), w => w.Id).ToArray();
            var newSessions = snapshot.Sessions.ExceptBy(sessions.Select(x => x.Id), s => s.Id).ToArray();
            var combined = new PlannerSnapshot(courses.Concat(newCourses).ToArray(), tasks.Concat(newTasks).ToArray(),
                windows.Concat(newWindows).ToArray(), sessions.Concat(newSessions).ToArray());
            var valid = BackupCodec.Validate(combined);
            if (!valid.IsSuccess) return valid;
            db.Courses.AddRange(newCourses);
            db.Tasks.AddRange(newTasks);
            db.Windows.AddRange(newWindows);
            db.Sessions.AddRange(newSessions);
            await db.SaveChangesAsync();
            return Result<bool>.Success(true);
        });
    }
    private static bool Conflicts<T>(IEnumerable<T> existing, IEnumerable<T> incoming, Func<T, Guid> key)
    {
        var lookup = existing.ToDictionary(key);
        return incoming.Any(item => lookup.TryGetValue(key(item), out var old) && !EqualityComparer<T>.Default.Equals(old, item));
    }
    private async Task<Result<T>> Write<T>(Func<PlannerDbContext, Task<Result<T>>> action)
    {
        await using var db = await factory.CreateDbContextAsync();
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var result = await action(db);
            if (result.IsSuccess) await transaction.CommitAsync();
            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<T>.Failure("This record changed in another tab or was deleted. Refresh and edit the latest version.");
        }
        catch (DbUpdateException)
        {
            return Result<T>.Failure("The record could not be saved. Refresh and check its course before retrying.");
        }
    }
}
