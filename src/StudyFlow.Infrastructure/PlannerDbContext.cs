using Microsoft.EntityFrameworkCore;
using StudyFlow.Domain.Models;

namespace StudyFlow.Infrastructure;

public sealed class PlannerDbContext(DbContextOptions<PlannerDbContext> options) : DbContext(options)
{
    public DbSet<StudyWindow> Windows => Set<StudyWindow>();
    public DbSet<StudySession> Sessions => Set<StudySession>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<StudyTask> Tasks => Set<StudyTask>();
    protected override void OnModelCreating(ModelBuilder model) => PlannerModel.Configure(model);
}
