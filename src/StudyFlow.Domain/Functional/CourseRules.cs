using StudyFlow.Domain.Models;

namespace StudyFlow.Domain.Functional;

public static class CourseRules
{
    public static Result<Course> Create(Guid id, string name, string color)
    {
        if (id == Guid.Empty) return Result<Course>.Failure("Course ID is required.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80)
            return Result<Course>.Failure("Course name must contain 1–80 characters.");
        if (color is null || color.Length != 7 || color[0] != '#' || !color.Skip(1).All(Uri.IsHexDigit))
            return Result<Course>.Failure("Choose a valid six-digit hex color.");
        return Result<Course>.Success(new(id, name.Trim(), color));
    }
}
