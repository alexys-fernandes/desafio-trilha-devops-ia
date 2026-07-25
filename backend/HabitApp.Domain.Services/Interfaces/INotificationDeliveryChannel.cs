using HabitApp.Domain.Services.Models;

namespace HabitApp.Domain.Services.Interfaces;

public interface INotificationDeliveryChannel
{
    string ChannelName { get; }
    Task QueueAsync(NotificationPayload payload, CancellationToken cancellationToken = default);
}
