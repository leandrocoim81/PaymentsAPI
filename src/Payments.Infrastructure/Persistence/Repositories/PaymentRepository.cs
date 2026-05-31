using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Payments.Domain.Entities;
using Payments.Domain.Events;
using Payments.Domain.Interfaces;

namespace Payments.Infrastructure.Persistence.Repositories;

public class PaymentRepository(PaymentsDbContext dbContext) : IPaymentRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task SaveAsync(Payment payment, CancellationToken ct)
    {
        var baseVersion = payment.Version - payment.UncommittedEvents.Count;
        var records = payment.UncommittedEvents
            .Select((@event, i) => new PaymentEventRecord
            {
                Id          = Guid.NewGuid(),
                AggregateId = payment.Id,
                EventType   = @event.GetType().Name,
                EventData   = JsonSerializer.Serialize(@event, @event.GetType(), JsonOptions),
                OccurredAt  = DateTime.UtcNow,
                Version     = baseVersion + i + 1
            });

        dbContext.PaymentEvents.AddRange(records);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct)
    {
        var records = await dbContext.PaymentEvents
            .AsNoTracking()
            .Where(e => e.AggregateId == orderId)
            .OrderBy(e => e.Version)
            .ToListAsync(ct);

        if (records.Count == 0) return null;

        var events = records.Select(DeserializeEvent).OfType<IDomainEvent>();

        return Payment.LoadFromHistory(events);
    }

    private static IDomainEvent? DeserializeEvent(PaymentEventRecord record) =>
        record.EventType switch
        {
            nameof(PaymentInitiatedEvent) =>
                JsonSerializer.Deserialize<PaymentInitiatedEvent>(record.EventData, JsonOptions),
            nameof(PaymentApprovedEvent) =>
                JsonSerializer.Deserialize<PaymentApprovedEvent>(record.EventData, JsonOptions),
            nameof(PaymentRejectedEvent) =>
                JsonSerializer.Deserialize<PaymentRejectedEvent>(record.EventData, JsonOptions),
            _ => null
        };
}
