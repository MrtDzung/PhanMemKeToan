using MediatR;

namespace PhanMemKeToan.Domain.Common;

public abstract class DomainEvent : INotification
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
