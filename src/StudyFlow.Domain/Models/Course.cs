namespace StudyFlow.Domain.Models;

public sealed record Course(Guid Id, string Name, string ColorHex, bool Archived = false, int Revision = 0);
