using FiapCloudGames.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Payments.Application.IntegrationEvents;
using Payments.Domain.Entities;
using Payments.Domain.Interfaces;

namespace Payments.Application.Commands.ProcessPayment;

public class ProcessPaymentHandler(
    IPaymentRepository repository,
    IPublishEndpoint publisher,
    IEventPublisher eventPublisher,
    ILogger<ProcessPaymentHandler> logger)
    : IRequestHandler<ProcessPaymentCommand>
{
    public async Task Handle(ProcessPaymentCommand cmd, CancellationToken ct)
    {
        var payment = Payment.Initiate(
            cmd.OrderId, cmd.UserId, cmd.UserEmail,
            cmd.GameId, cmd.GameName, cmd.Price);

        payment.Process();

        await repository.SaveAsync(payment, ct);

        logger.LogInformation(
            "Payment {PaymentId} processed with status {Status}",
            payment.Id, payment.Status.Value);

        var evt = new PaymentProcessedEvent(
            cmd.OrderId,
            cmd.UserId,
            cmd.UserEmail,
            cmd.GameId,
            cmd.GameName,
            cmd.Price,
            payment.Status.Value);

        await publisher.Publish(evt, ct);

        await eventPublisher.PublishPaymentProcessedAsync(evt, ct);
    }
}
