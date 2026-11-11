using StudyFlow.Application.Contracts;
using StudyFlow.Application.Services;
using StudyFlow.Domain.Functional;
using StudyFlow.Domain.Models;
using Xunit;
using TaskStatus = StudyFlow.Domain.Models.TaskStatus;

namespace StudyFlow.Domain.Tests;

public class ApplicationTests
{
    [Fact]
    public async Task StorageFailureIsVisibleAndBusyStateIsReleased()
    {
        var state = new PlannerState(new BrokenRepository());
        var events = 0;
        state.Changed += () => events++;
        await state.LoadAsync();
        Assert.Contains("Storage is unavailable", state.Error);
        Assert.False(state.IsBusy);
        Assert.Null(state.Notice);
        Assert.Equal(2, events);
    }
    [Fact]
    public void InvalidWeightsDoNotReplaceValidPreferences()
    {
        var state = new PlannerState(new BrokenRepository());
        state.SetWeights(new(2, 1, 0));
        Assert.Equal(new PriorityWeights(2, 1, 0), state.Weights);
        state.SetWeights(new(0, 0, 0));
        Assert.NotNull(state.Error);
        Assert.Equal(new PriorityWeights(2, 1, 0), state.Weights);
    }
    [Fact]
    public async Task BusyStatePreventsOverlappingOperations()
    {
        var repository = new DelayedRepository();
        var state = new PlannerState(repository);
        var load = state.LoadAsync();
        Assert.True(state.IsBusy);
        await state.LoadAsync();
        Assert.Equal(1, repository.Loads);
        repository.Completion.SetResult(new(Array.Empty<Course>(), Array.Empty<StudyTask>()));
        await load;
        Assert.False(state.IsBusy);
    }
    private class BrokenRepository : IPlannerRepository
    {
        public Task<Result<bool>> ImportAsync(PlannerSnapshot snapshot) => throw new IOException();
        public Task<Result<StudyWindow>> AddWindowAsync(StudyWindow window, DateOnly today) => throw new IOException();
        public Task<Result<bool>> DeleteWindowAsync(StudyWindow window) => throw new IOException();
        public Task<Result<bool>> SavePlanAsync(PlannerSnapshot expected, IReadOnlyList<StudySession> sessions, DateOnly today) => throw new IOException();
        public virtual Task<PlannerSnapshot> LoadAsync() => throw new IOException("Unavailable");
        public Task<Result<Course>> SaveCourseAsync(Course course, bool isNew) => throw new IOException();
        public Task<Result<StudyTask>> SaveTaskAsync(StudyTask task, bool isNew) => throw new IOException();
        public Task<Result<bool>> DeleteCourseAsync(Course course) => throw new IOException();
        public Task<Result<bool>> DeleteTaskAsync(StudyTask task) => throw new IOException();
    }
    private sealed class DelayedRepository : BrokenRepository
    {
        public TaskCompletionSource<PlannerSnapshot> Completion { get; } = new();
        public int Loads { get; private set; }
        public override Task<PlannerSnapshot> LoadAsync() { Loads++; return Completion.Task; }
    }
}
