using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StudyFlow.Application.Contracts;
using StudyFlow.Application.Services;
using StudyFlow.Domain.Models;
using StudyFlow.Infrastructure;
using Xunit;

namespace StudyFlow.Domain.Tests;

public sealed partial class PersistenceTests
{
    [Fact]
    public async System.Threading.Tasks.Task ImportRoundTripIsAtomicAndIdempotent()
    {
        var input = BackupTests.Example();
        Assert.True((await repository.ImportAsync(input)).IsSuccess);
        Assert.True((await repository.ImportAsync(input)).IsSuccess);
        var stored = await new SqlitePlannerRepository(factory).LoadAsync();
        Assert.Equal(input.Courses, stored.Courses); Assert.Equal(input.Tasks, stored.Tasks);
        Assert.Equal(input.Windows, stored.Windows); Assert.Equal(input.Sessions, stored.Sessions);
        var extra = new Course(Guid.NewGuid(), "New", "#ffffff");
        var conflict = input with { Courses = [input.Courses[0] with { Name = "Overwrite" }, extra] };
        Assert.False((await repository.ImportAsync(conflict)).IsSuccess);
        Assert.Single((await repository.LoadAsync()).Courses);
        var invalid = input with { Courses = [extra], Tasks = [input.Tasks[0]] };
        Assert.False((await repository.ImportAsync(invalid)).IsSuccess);
        Assert.Equal(input.Courses[0], Assert.Single((await repository.LoadAsync()).Courses));
    }
    [Fact]
    public async System.Threading.Tasks.Task ImportRejectsOverlapsWithExistingScheduleWithoutPartialWrites()
    {
        var input = BackupTests.Example(); await repository.ImportAsync(input);
        var extra = BackupTests.Example();
        Assert.False((await repository.ImportAsync(extra)).IsSuccess);
        var stored = await repository.LoadAsync();
        Assert.Single(stored.Courses); Assert.Single(stored.Tasks); Assert.Single(stored.Windows); Assert.Single(stored.Sessions);
    }
    [Fact]
    public async System.Threading.Tasks.Task WindowsAndConfirmedProposalPersistAcrossStateInstances()
    {
        var state = new PlannerState(repository); var today = new DateOnly(2026, 11, 1);
        var data = BackupTests.Example();
        await state.ImportAsync(Encoding.UTF8.GetString(BackupCodec.Export(data with { Windows = [], Sessions = [] })));
        await state.AddWindowAsync(data.Windows[0].Start, 60, today);
        Assert.Null(state.Error);
        await state.SavePlanAsync(today); Assert.Null(state.Error); Assert.Single(state.Sessions);
        var another = new PlannerState(new SqlitePlannerRepository(factory)); await another.LoadAsync();
        Assert.Equal(state.Windows, another.Windows); Assert.Equal(state.Sessions, another.Sessions);
        await another.DeleteWindowAsync(another.Windows[0]); Assert.Null(another.Error);
        Assert.Empty(another.Windows); Assert.Empty(another.Sessions); Assert.Single(another.Tasks);
    }
    [Fact]
    public async System.Threading.Tasks.Task WindowValidationAndStalePlanDoNotReplaceSavedData()
    {
        var input = BackupTests.Example(); await repository.ImportAsync(input);
        var today = new DateOnly(2026, 11, 1);
        Assert.False((await repository.AddWindowAsync(input.Windows[0] with { Id = Guid.NewGuid() }, today)).IsSuccess);
        Assert.False((await repository.AddWindowAsync(new(Guid.NewGuid(), new(2026, 10, 1), 60), today)).IsSuccess);
        await repository.SaveTaskAsync(input.Tasks[0] with { Title = "Changed" }, false);
        Assert.False((await repository.SavePlanAsync(input, [], today)).IsSuccess);
        Assert.Single((await repository.LoadAsync()).Sessions);
        var current = await repository.LoadAsync();
        Assert.False((await repository.SavePlanAsync(current, [input.Sessions[0] with { Minutes = 90 }], today)).IsSuccess);
        Assert.Single((await repository.LoadAsync()).Sessions);
        Assert.True((await repository.SavePlanAsync(current, [], today)).IsSuccess);
        Assert.Empty((await repository.LoadAsync()).Sessions);
    }
    [Fact]
    public async System.Threading.Tasks.Task DeletingTaskCascadesSessionsButKeepsWindow()
    {
        var input = BackupTests.Example(); await repository.ImportAsync(input);
        Assert.True((await repository.DeleteTaskAsync(input.Tasks[0])).IsSuccess);
        var stored = await repository.LoadAsync();
        Assert.Empty(stored.Sessions); Assert.Single(stored.Windows);
    }
    [Fact]
    public async System.Threading.Tasks.Task MigrationUpgradesOriginalSchemaWithoutLosingAssignments()
    {
        await using var db = factory.CreateDbContext();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261109113534_InitialSchema");
        var course = Course(); var task = Task(course);
        db.Courses.Add(course); db.Tasks.Add(task); await db.SaveChangesAsync();
        await migrator.MigrateAsync();
        var stored = await repository.LoadAsync();
        Assert.Equal(course, Assert.Single(stored.Courses)); Assert.Equal(task, Assert.Single(stored.Tasks));
        Assert.Empty(stored.Windows); Assert.Empty(stored.Sessions);
    }
    [Fact]
    public async System.Threading.Tasks.Task InvalidJsonThroughApplicationLeavesDataUntouched()
    {
        var state = new PlannerState(repository);
        await state.ImportAsync(Encoding.UTF8.GetString(BackupCodec.Export(BackupTests.Example())));
        Assert.Null(state.Error);
        await state.ImportAsync("{invalid");
        Assert.NotNull(state.Error); Assert.Single(state.Courses); Assert.Single(state.Sessions); Assert.False(state.IsBusy);
    }
    [Fact]
    public async System.Threading.Tasks.Task DatabaseFailureRollsBackImportAndPlanReplacement()
    {
        var original = BackupTests.Example();
        await repository.ImportAsync(original);
        await using var db = factory.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectSession BEFORE INSERT ON Sessions BEGIN SELECT RAISE(ABORT, 'simulated disk constraint'); END;");
        var incoming = BackupTests.Example();
        var shiftedWindow = incoming.Windows[0] with { Start = incoming.Windows[0].Start.AddDays(1) };
        incoming = incoming with { Windows = [shiftedWindow], Sessions = [incoming.Sessions[0] with { Start = shiftedWindow.Start }] };
        Assert.False((await repository.ImportAsync(incoming)).IsSuccess);
        var after = await repository.LoadAsync();
        Assert.Single(after.Courses); Assert.Single(after.Tasks); Assert.Single(after.Windows); Assert.Equal(original.Sessions, after.Sessions);
        Assert.False((await repository.SavePlanAsync(after, [original.Sessions[0] with { Id = Guid.NewGuid() }], new(2026, 11, 1))).IsSuccess);
        Assert.Equal(original.Sessions, (await repository.LoadAsync()).Sessions);
    }
}
