using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Services;
using StudyFlow.Domain.Models;
using StudyFlow.Infrastructure;
using Xunit;
using TaskStatus = StudyFlow.Domain.Models.TaskStatus;

namespace StudyFlow.Domain.Tests;

public sealed partial class PersistenceTests : IAsyncLifetime
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"studyflow-{Guid.NewGuid()}.db");
    private Factory factory = null!;
    private SqlitePlannerRepository repository = null!;
    private static Course Course() => new(Guid.NewGuid(), "FP", "#123abc");
    private static StudyTask Task(Course course) => new(Guid.NewGuid(), course.Id, "Final", new(2026, 11, 20), 4, 90);
    public async System.Threading.Tasks.Task InitializeAsync()
    {
        factory = new(new DbContextOptionsBuilder<PlannerDbContext>().UseSqlite($"Data Source={path};Foreign Keys=True;Pooling=False").Options);
        repository = new(factory);
        await using var db = factory.CreateDbContext();
        await db.Database.MigrateAsync();
    }
    public System.Threading.Tasks.Task DisposeAsync()
    {
        File.Delete(path); File.Delete(path + "-wal"); File.Delete(path + "-shm");
        return System.Threading.Tasks.Task.CompletedTask;
    }
    [Fact]
    public async System.Threading.Tasks.Task CrudPersistsAcrossRepositoryInstances()
    {
        var course = Course(); Assert.True((await repository.SaveCourseAsync(course, true)).IsSuccess);
        var task = Task(course); Assert.True((await repository.SaveTaskAsync(task, true)).IsSuccess);
        var reopenedRepository = new SqlitePlannerRepository(factory);
        Assert.Equal(task, Assert.Single((await reopenedRepository.LoadAsync()).Tasks));
        var edited = await reopenedRepository.SaveTaskAsync(task with { Title = "Edited", Status = TaskStatus.Done }, false);
        Assert.True(edited.IsSuccess); Assert.Equal(1, edited.Value!.Revision);
        var reopened = await repository.SaveTaskAsync(edited.Value with { Status = TaskStatus.ToDo }, false);
        Assert.True(reopened.IsSuccess);
        Assert.True((await repository.DeleteTaskAsync(reopened.Value!)).IsSuccess);
        var renamed = await repository.SaveCourseAsync(course with { Name = "Functional Programming" }, false);
        Assert.True(renamed.IsSuccess); Assert.Equal("Functional Programming", Assert.Single((await repository.LoadAsync()).Courses).Name);
        Assert.True((await repository.DeleteCourseAsync(renamed.Value!)).IsSuccess);
        Assert.Empty((await repository.LoadAsync()).Courses);
    }
    [Fact]
    public async System.Threading.Tasks.Task CourseWithTasksCannotBeDeletedButCanBeArchived()
    {
        var course = Course(); await repository.SaveCourseAsync(course, true);
        var task = Task(course); await repository.SaveTaskAsync(task, true);
        Assert.False((await repository.DeleteCourseAsync(course)).IsSuccess);
        Assert.True((await repository.SaveCourseAsync(course with { Archived = true }, false)).IsSuccess);
        Assert.False((await repository.SaveTaskAsync(Task(course), true)).IsSuccess);
        Assert.True((await repository.SaveTaskAsync(task with { Status = TaskStatus.Done }, false)).IsSuccess);
        Assert.Single((await repository.LoadAsync()).Tasks);
    }
    [Fact]
    public async System.Threading.Tasks.Task StaleUpdatesAndDeletesAreRejected()
    {
        var course = Course(); await repository.SaveCourseAsync(course, true);
        var task = Task(course); await repository.SaveTaskAsync(task, true);
        Assert.True((await repository.SaveTaskAsync(task with { Title = "New" }, false)).IsSuccess);
        Assert.False((await repository.SaveTaskAsync(task with { Title = "Stale" }, false)).IsSuccess);
        Assert.False((await repository.DeleteTaskAsync(task)).IsSuccess);
        Assert.Equal("New", Assert.Single((await repository.LoadAsync()).Tasks).Title);
        await repository.SaveCourseAsync(course with { Name = "New" }, false);
        Assert.False((await repository.SaveCourseAsync(course, false)).IsSuccess);
    }
    [Fact]
    public async System.Threading.Tasks.Task MissingCourseAndInvalidDataAreRejectedWithoutWrites()
    {
        var course = Course();
        Assert.False((await repository.SaveTaskAsync(Task(course), true)).IsSuccess);
        Assert.False((await repository.SaveCourseAsync(course with { Name = "" }, true)).IsSuccess);
        await repository.SaveCourseAsync(course, true);
        Assert.False((await repository.SaveTaskAsync(Task(course) with { Importance = 9 }, true)).IsSuccess);
        Assert.False((await repository.SaveTaskAsync(Task(course) with { Status = (TaskStatus)99 }, true)).IsSuccess);
        Assert.Empty((await repository.LoadAsync()).Tasks);
    }
    [Fact]
    public async System.Threading.Tasks.Task PlannerStateSynchronizesAfterSuccessfulWritesAndRetainsValidationError()
    {
        var state = new PlannerState(repository);
        await state.LoadAsync(); Assert.Empty(state.Courses);
        await state.SaveCourseAsync(null, "FP", "#123abc", false);
        Assert.Null(state.Error); Assert.Single(state.Courses);
        await state.SaveTaskAsync(null, state.Courses[0].Id, "", new(2026, 11, 9), 3, 60, TaskStatus.ToDo);
        Assert.NotNull(state.Error); Assert.Empty(state.Tasks); Assert.False(state.IsBusy);
        await state.SaveTaskAsync(null, state.Courses[0].Id, "Task", new(2026, 11, 9), 3, 60, TaskStatus.ToDo);
        Assert.Null(state.Error); Assert.Single(state.Tasks);
    }
    [Fact]
    public async System.Threading.Tasks.Task MigrationCanBeAppliedRepeatedlyAndModelMatchesSnapshot()
    {
        await using var db = factory.CreateDbContext();
        await db.Database.MigrateAsync();
        Assert.Equal(2, (await db.Database.GetAppliedMigrationsAsync()).Count());
        Assert.False(db.Database.HasPendingModelChanges());
    }
    [Fact]
    public async System.Threading.Tasks.Task ApplicationCommandsCoverEditStatusDeleteAndValidation()
    {
        var state = new PlannerState(repository);
        await state.SaveCourseAsync(null, "", "#123abc", false);
        Assert.NotNull(state.Error);
        await state.SaveCourseAsync(null, "Course", "#123abc", false);
        var course = Assert.Single(state.Courses);
        await state.SaveTaskAsync(null, course.Id, "Task", new(2026, 11, 20), 3, 60, (TaskStatus)99);
        Assert.NotNull(state.Error);
        await state.SaveTaskAsync(null, course.Id, "Task", new(2026, 11, 20), 3, 60, TaskStatus.ToDo);
        await state.SetStatusAsync(Assert.Single(state.Tasks), TaskStatus.Done);
        Assert.Equal(TaskStatus.Done, Assert.Single(state.Tasks).Status);
        await state.DeleteCourseAsync(course);
        Assert.NotNull(state.Error);
        await state.DeleteTaskAsync(Assert.Single(state.Tasks));
        Assert.Empty(state.Tasks);
        await state.DeleteCourseAsync(course);
        Assert.Empty(state.Courses);
    }
    private sealed class Factory(DbContextOptions<PlannerDbContext> options) : IDbContextFactory<PlannerDbContext>
    {
        public PlannerDbContext CreateDbContext() => new(options);
    }
}
