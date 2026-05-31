using FiapCloudGames.Contracts.Events;

namespace Payments.Application.IntegrationEvents;

public interface IEventPublisher
{
    Task PublishPaymentProcessedAsync(PaymentProcessedEvent evt, CancellationToken ct = default);
}
