namespace HabitApp.Domain.Entities;

public class HabitCompletion : BaseEntity
{
    public int HabitId { get; set; }
    public Habit? Habit { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public DateOnly CompletedDate { get; set; }
    public DateTime CompletedAt { get; set; }
}
