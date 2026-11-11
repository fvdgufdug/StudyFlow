using System.Text;
using System.Text.Json.Nodes;
using StudyFlow.Application.Contracts;
using StudyFlow.Application.Services;
using StudyFlow.Domain.Functional;
using StudyFlow.Domain.Models;
using Xunit;

namespace StudyFlow.Domain.Tests;

public class BackupTests
{
    public static PlannerSnapshot Example()
    {
        var course = new Course(Guid.NewGuid(), "FP", "#123abc");
        var task = new StudyTask(Guid.NewGuid(), course.Id, "Final", new(2026, 12, 1), 3, 60);
        var window = new StudyWindow(Guid.NewGuid(), new(2026, 11, 20, 10, 0, 0), 60);
        var session = new StudySession(Guid.NewGuid(), window.Id, task.Id, window.Start, 60);
        return new([course], [task], [window], [session]);
    }
    [Fact]
    public void ExportRoundTripsAllCollectionsAndSupportsLegacyExports()
    {
        var snapshot = Example();
        var json = Encoding.UTF8.GetString(BackupCodec.Export(snapshot));
        var parsed = BackupCodec.Parse(json);
        Assert.True(parsed.IsSuccess, parsed.Error);
        Assert.Equal(snapshot.Courses, parsed.Value!.Courses);
        Assert.Equal(snapshot.Tasks, parsed.Value.Tasks);
        Assert.Equal(snapshot.Windows, parsed.Value.Windows);
        Assert.Equal(snapshot.Sessions, parsed.Value.Sessions);
        var legacy = JsonNode.Parse(json)!.AsObject();
        legacy.Remove("SchemaVersion"); legacy.Remove("Windows"); legacy.Remove("Sessions");
        Assert.Empty(BackupCodec.Parse(legacy.ToJsonString()).Value!.Sessions);
    }
    [Theory]
    [InlineData("")] [InlineData("{")] [InlineData("null")] [InlineData("[]")]
    [InlineData("{\"Courses\":[],\"Tasks\":null}")]
    [InlineData("{\"Courses\":[],\"Tasks\":[],\"Unknown\":1}")]
    [InlineData("{\"Courses\":[],\"Tasks\":[],\"SchemaVersion\":2}")]
    [InlineData("{\"Courses\":[],\"Courses\":[],\"Tasks\":[]}")]
    [InlineData("{\"Tasks\":[]}")]
    public void InvalidDocumentsAreRejected(string json) => Assert.False(BackupCodec.Parse(json).IsSuccess);
    [Fact]
    public void OversizedAndNullRecordsAreRejected()
    {
        Assert.False(BackupCodec.Parse(new string(' ', BackupCodec.MaxBytes + 1)).IsSuccess);
        Assert.False(BackupCodec.Parse("{\"Courses\":[null],\"Tasks\":[]}").IsSuccess);
    }
    [Theory]
    [InlineData("Status")] [InlineData("Revision")] [InlineData("DueDate")] [InlineData("Importance")]
    public void MissingTaskFieldsAreRejected(string property)
    {
        var document = JsonNode.Parse(BackupCodec.Export(Example()))!;
        document["Tasks"]![0]!.AsObject().Remove(property);
        Assert.False(BackupCodec.Parse(document.ToJsonString()).IsSuccess);
    }
    [Fact]
    public void DuplicateIdsOrBrokenLinksAreRejected()
    {
        var snapshot = Example();
        Assert.False(BackupCodec.Validate(snapshot with { Courses = [snapshot.Courses[0], snapshot.Courses[0]] }).IsSuccess);
        Assert.False(BackupCodec.Validate(snapshot with { Tasks = [snapshot.Tasks[0] with { CourseId = Guid.NewGuid() }] }).IsSuccess);
        Assert.False(BackupCodec.Validate(snapshot with { Sessions = [snapshot.Sessions[0] with { TaskId = Guid.NewGuid() }] }).IsSuccess);
        Assert.False(BackupCodec.Validate(snapshot with { Tasks = [snapshot.Tasks[0] with { Status = (StudyFlow.Domain.Models.TaskStatus)999 }] }).IsSuccess);
    }
    [Fact]
    public void OverlapCapacityAndTimezoneAreValidated()
    {
        var snapshot = Example(); var window = snapshot.Windows[0]; var session = snapshot.Sessions[0];
        Assert.False(StoredScheduleRules.Validate([window, window with { Id = Guid.NewGuid() }], [], []).IsSuccess);
        Assert.False(StoredScheduleRules.Validate([window], [session with { Minutes = 61 }], snapshot.Tasks).IsSuccess);
        Assert.False(StoredScheduleRules.Validate([window with { Start = DateTime.SpecifyKind(window.Start, DateTimeKind.Utc) }], [], []).IsSuccess);
        Assert.False(StoredScheduleRules.Validate([window with { Start = DateTime.MaxValue }], [], []).IsSuccess);
        Assert.True(StoredScheduleRules.Validate([window, window with { Id = Guid.NewGuid(), Start = window.Start.AddMinutes(60) }], [], []).IsSuccess);
    }
    [Fact]
    public void SavedHistoryMayBePastButNewPlansMustRespectStatusAndDeadline()
    {
        var snapshot = Example();
        Assert.True(BackupCodec.Validate(snapshot).IsSuccess);
        Assert.False(StoredScheduleRules.ValidateNewPlan(snapshot.Windows, snapshot.Sessions, snapshot.Tasks, new(2026, 12, 1)).IsSuccess);
        Assert.False(StoredScheduleRules.ValidateNewPlan(snapshot.Windows, snapshot.Sessions,
            [snapshot.Tasks[0] with { Status = StudyFlow.Domain.Models.TaskStatus.Done }], new(2026, 11, 1)).IsSuccess);
        Assert.False(StoredScheduleRules.ValidateNewPlan(snapshot.Windows, snapshot.Sessions,
            [snapshot.Tasks[0] with { DueDate = new(2026, 11, 19) }], new(2026, 11, 1)).IsSuccess);
        Assert.False(StoredScheduleRules.ValidateNewPlan(snapshot.Windows, snapshot.Sessions,
            [snapshot.Tasks[0] with { EstimatedMinutes = 15 }], new(2026, 11, 1)).IsSuccess);
    }
}
