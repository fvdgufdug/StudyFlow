using TaskStatus = StudyFlow.Domain.Models.TaskStatus;
using StudyFlow.Domain.Functional;
using StudyFlow.Domain.Models;
using Xunit;

namespace StudyFlow.Domain.Tests;

public class TaskRulesTests
{
    private static readonly Guid Course = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 11, 7);

    [Fact]
    public void Create_RejectsEmptyTitle() =>
        Assert.False(TaskRules.Create(Guid.NewGuid(), Course, " ", Today, 3, 60).IsSuccess);

    [Fact]
    public void Create_RejectsInvalidImportance() =>
        Assert.False(TaskRules.Create(Guid.NewGuid(), Course, "Task", Today, 6, 60).IsSuccess);

    [Fact]
    public void Rank_PrioritizesEarlierDueDate_WhenOtherValuesMatch()
    {
        var early = new StudyTask(Guid.NewGuid(), Course, "Early", Today, 3, 60);
        var late = early with { Id = Guid.NewGuid(), Title = "Late", DueDate = Today.AddDays(5) };
        Assert.Equal(early.Id, TaskRules.Rank(new[] { late, early }, Today).First().Id);
    }

    [Fact]
    public void Complete_DoesNotMutateOriginal()
    {
        var task = new StudyTask(Guid.NewGuid(), Course, "Test", Today, 3, 60);
        var completed = TaskRules.Complete(task);
        Assert.Equal(TaskStatus.ToDo, task.Status);
        Assert.Equal(TaskStatus.Done, completed.Status);
    }

    [Fact]
    public void CompletionPercent_HandlesEmptyList() =>
        Assert.Equal(0, TaskRules.CompletionPercent(Array.Empty<StudyTask>()));
}
