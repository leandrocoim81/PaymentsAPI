using Payments.Domain.Events;
using Payments.Domain.Exceptions;
using Payments.Domain.ValueObjects;

namespace Payments.Domain.Entities;

public class Payment
{
    private readonly List<IDomainEvent> _uncommittedEvents = new();

    public Guid          Id            { get; private set; }
    public Guid          UserId        { get; private set; }
    public string        UserEmail     { get; private set; } = string.Empty;
    public Guid          GameId        { get; private set; }
    public string        GameName      { get; private set; } = string.Empty;
    public decimal       Price         { get; private set; }
    public PaymentStatus Status        { get; private set; } = null!;
    public DateTime?     ProcessedAt   { get; private set; }
    public int           Version       { get; private set; }

    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    private Payment() { }

    public static Payment LoadFromHistory(IEnumerable<IDomainEvent> events)
    {
        var payment = new Payment();
        foreach (var @event in events)
            payment.Apply(@event);
        return payment;
    }

    public static Payment Initiate(Guid orderId, Guid userId, string userEmail,
        Guid gameId, string gameName, decimal price)
    {
        var payment = new Payment();
        payment.RaiseEvent(new PaymentInitiatedEvent(
            orderId, userId, userEmail, gameId, gameName, price, DateTime.UtcNow));
        return payment;
    }

    public void Process()
    {
        if (Status != PaymentStatus.Initiated)
            throw new DomainException($"Cannot process a payment in status '{Status.Value}'.");

        var approved = Random.Shared.NextDouble() > 0.1; // 90% approval rate
        if (approved)
            RaiseEvent(new PaymentApprovedEvent(Id, DateTime.UtcNow));
        else
            RaiseEvent(new PaymentRejectedEvent(Id, "Insufficient funds", DateTime.UtcNow));
    }

    public bool IsApproved => Status == PaymentStatus.Approved;

    private void RaiseEvent(IDomainEvent @event)
    {
        Apply(@event);
        _uncommittedEvents.Add(@event);
    }

    private void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case PaymentInitiatedEvent e:
                Id        = e.PaymentId;
                UserId    = e.UserId;
                UserEmail = e.UserEmail;
                GameId    = e.GameId;
                GameName  = e.GameName;
                Price     = e.Price;
                Status    = PaymentStatus.Initiated;
                break;
            case PaymentApprovedEvent e:
                Status      = PaymentStatus.Approved;
                ProcessedAt = e.OccurredAt;
                break;
            case PaymentRejectedEvent e:
                Status      = PaymentStatus.Rejected;
                ProcessedAt = e.OccurredAt;
                break;
        }
        Version++;
    }
}
