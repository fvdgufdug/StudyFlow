using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StudyFlow.Infrastructure;

public sealed class PlannerDbContextFactory : IDesignTimeDbContextFactory<PlannerDbContext>
{
    public PlannerDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<PlannerDbContext>()
        .UseSqlite("Data Source=studyflow-design.db").Options);
}
