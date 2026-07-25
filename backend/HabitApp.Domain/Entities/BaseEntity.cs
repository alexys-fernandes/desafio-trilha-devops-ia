namespace HabitApp.Domain.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
}
