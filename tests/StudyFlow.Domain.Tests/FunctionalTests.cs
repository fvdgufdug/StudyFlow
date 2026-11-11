using StudyFlow.Domain.Functional;
using StudyFlow.Domain.Models;
using Xunit;
using TaskStatus = StudyFlow.Domain.Models.TaskStatus;

namespace StudyFlow.Domain.Tests;

public class FunctionalTests
{
    private static readonly DateOnly Today = new(2026, 11, 9);
    private static StudyTask Task(int days = 0, int minutes = 60) => new(Guid.NewGuid(), Guid.NewGuid(), "Essay", Today.AddDays(days), 3, minutes);

    [Theory]
    [InlineData(0, 60)] [InlineData(6, 60)] [InlineData(3, 14)] [InlineData(3, 1441)]
    public void InvalidBoundsAreRejected(int importance, int minutes) =>
        Assert.False(TaskRules.Create(Guid.NewGuid(), Guid.NewGuid(), "Task", Today, importance, minutes).IsSuccess);

    [Fact]
    public void CreateIsDeterministicAndTrimsTitle()
    {
        var id = Guid.NewGuid(); var course = Guid.NewGuid();
        var first = TaskRules.Create(id, course, "  Essay  ", Today, 1, 15);
        Assert.Equal(first, TaskRules.Create(id, course, "  Essay  ", Today, 1, 15));
        Assert.Equal("Essay", first.Value!.Title);
    }
    [Fact]
    public void RejectsMissingIdsDateAndLongTitle()
    {
        Assert.False(TaskRules.Create(Guid.Empty, Guid.NewGuid(), "Essay", Today, 3, 60).IsSuccess);
        Assert.False(TaskRules.Create(Guid.NewGuid(), Guid.Empty, "Essay", Today, 3, 60).IsSuccess);
        Assert.False(TaskRules.Create(Guid.NewGuid(), Guid.NewGuid(), "Essay", default, 3, 60).IsSuccess);
        Assert.False(TaskRules.Create(Guid.NewGuid(), Guid.NewGuid(), new string('a', 121), Today, 3, 60).IsSuccess);
    }
    [Theory]
    [InlineData("", "#ffffff")] [InlineData("Course", "red")] [InlineData("Course", "#gggggg")]
    public void InvalidCourseIsRejected(string name, string color) => Assert.False(CourseRules.Create(Guid.NewGuid(), name, color).IsSuccess);
    [Fact]
    public void CourseTrimsAndAcceptsHex() => Assert.Equal("Math", CourseRules.Create(Guid.NewGuid(), " Math ", "#aBc123").Value!.Name);
    [Fact]
    public void OverduePriorityHasExpectedBreakdown()
    {
        var breakdown = TaskRules.Explain(Task(-1), Today);
        Assert.Equal(75, breakdown.Urgency); Assert.Equal(36, breakdown.Importance);
        Assert.Equal(1.5, breakdown.Workload); Assert.Equal(112.5, breakdown.Total);
    }
    [Fact]
    public void CompletedTasksHaveZeroScoreAndAreExcluded()
    {
        var done = TaskRules.Complete(Task());
        Assert.Equal(0, TaskRules.Priority(done, Today)); Assert.Empty(TaskRules.Rank(new[] { done }, Today));
    }
    [Fact]
    public void TieBreakIsIndependentOfInputOrder()
    {
        var a = Task(); var b = a with { Id = Guid.NewGuid() };
        Assert.Equal(TaskRules.Rank(new[] { a, b }, Today), TaskRules.Rank(new[] { b, a }, Today));
    }
    [Fact]
    public void WeightsAreValidatedAndApplied()
    {
        Assert.False(new PriorityWeights(0, 0, 0).IsValid);
        Assert.False(new PriorityWeights(double.NaN).IsValid);
        Assert.Throws<ArgumentOutOfRangeException>(() => TaskRules.Explain(Task(), Today, new(-1)));
        Assert.Equal(72, TaskRules.Explain(Task(), Today, new(0, 2, 0)).Total);
        Assert.Equal(18, TaskRules.Explain(Task(0, 1440), Today).Workload);
    }
    [Fact]
    public void ResultComposesAndShortCircuits()
    {
        Assert.Equal(8, Result<int>.Success(3).Map(x => x + 1).Bind(x => Result<int>.Success(x * 2)).Value);
        Assert.Equal("bad", Result<int>.Failure("bad").Map<int>(_ => throw new Exception()).Error);
    }
    [Fact]
    public void AnalyticsExcludeCompletedFromOverdueAndWorkload()
    {
        var tasks = new[] { Task(-1), TaskRules.Complete(Task(-1)), Task(1, 120) };
        var summary = AnalyticsRules.Summarize(tasks, Today);
        Assert.Equal(new PlannerSummary(3, 1, 1, 33, 180), summary);
        var week = AnalyticsRules.Week(tasks, Today);
        Assert.Equal(7, week.Count); Assert.Equal(120, week.Sum(d => d.Minutes));
        Assert.Equal(1, week[1].Count);
    }
    [Fact]
    public void ScheduleNeverOverbooksAndReportsRemainder()
    {
        var tasks = new[] { Task(2, 120), Task(3, 60) };
        var start = Today.ToDateTime(new TimeOnly(10, 0));
        var result = ScheduleRules.Propose(tasks, new[] { new StudySlot(start, 90), new StudySlot(start.AddHours(2), 60) }, Today).Value!;
        Assert.Equal(150, result.Allocations.Sum(a => a.Minutes)); Assert.Equal(30, result.UnallocatedMinutes);
        Assert.All(result.Allocations.Zip(result.Allocations.Skip(1)), pair => Assert.True(pair.First.Start.AddMinutes(pair.First.Minutes) <= pair.Second.Start));
        Assert.Equal(120, result.Allocations.Where(a => a.TaskId == tasks[0].Id).Sum(a => a.Minutes));
    }
    [Fact]
    public void ScheduleRejectsOverlapAndPastSlots()
    {
        var start = Today.ToDateTime(new TimeOnly(10, 0));
        Assert.False(ScheduleRules.Propose(new[] { Task() }, new[] { new StudySlot(start, 60), new StudySlot(start.AddMinutes(30), 60) }, Today).IsSuccess);
        Assert.False(ScheduleRules.Propose(new[] { Task() }, new[] { new StudySlot(start.AddDays(-1), 60) }, Today).IsSuccess);
    }
    [Fact]
    public void ScheduleDoesNotAllocateDoneOrPastDeadlineTasks()
    {
        var tasks = new[] { Task(-1), TaskRules.Complete(Task(1)) };
        var result = ScheduleRules.Propose(tasks, new[] { new StudySlot(Today.ToDateTime(TimeOnly.MinValue), 60) }, Today).Value!;
        Assert.Empty(result.Allocations); Assert.Equal(60, result.UnallocatedMinutes);
    }
}
