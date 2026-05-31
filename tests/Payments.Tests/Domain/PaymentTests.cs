using Payments.Domain.Entities;
using Payments.Domain.Events;
using Payments.Domain.Exceptions;
using Payments.Domain.ValueObjects;

namespace Payments.Tests.Domain;

public class PaymentTests
{
    private static (Guid orderId, Guid userId, Guid gameId) Ids() =>
        (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Initiate_SetsStatusInitiatedAndRaisesPaymentInitiatedEvent()
    {
        var (orderId, userId, gameId) = Ids();

        var payment = Payment.Initiate(orderId, userId, "user@example.com", gameId, "Hades", 59.90m);

        Assert.Equal(PaymentStatus.Initiated, payment.Status);
        Assert.Equal(userId, payment.UserId);
        Assert.Equal("user@example.com", payment.UserEmail);
        Assert.Equal(gameId, payment.GameId);
        Assert.Equal("Hades", payment.GameName);
        Assert.Equal(59.90m, payment.Price);
        Assert.Single(payment.UncommittedEvents);
        Assert.IsType<PaymentInitiatedEvent>(payment.UncommittedEvents[0]);
    }

    [Fact]
    public void Initiate_PaymentIdMatchesEventPaymentId()
    {
        var (orderId, userId, gameId) = Ids();
        var payment = Payment.Initiate(orderId, userId, "user@example.com", gameId, "Hades", 10m);

        var evt = Assert.IsType<PaymentInitiatedEvent>(payment.UncommittedEvents[0]);
        Assert.Equal(payment.Id, evt.PaymentId);
    }

    [Fact]
    public void Process_WhenNotInitiated_ThrowsDomainException()
    {
        var (orderId, userId, gameId) = Ids();
        var payment = Payment.Initiate(orderId, userId, "user@example.com", gameId, "Hades", 10m);
        payment.Process();

        Assert.Throws<DomainException>(() => payment.Process());
    }

    [Fact]
    public void Process_WhenInitiated_TransitionsToApprovedOrRejected()
    {
        var (orderId, userId, gameId) = Ids();
        var payment = Payment.Initiate(orderId, userId, "user@example.com", gameId, "Hades", 10m);

        payment.Process();

        Assert.True(payment.Status == PaymentStatus.Approved || payment.Status == PaymentStatus.Rejected);
        Assert.NotNull(payment.ProcessedAt);
        Assert.Equal(2, payment.UncommittedEvents.Count);
    }

    [Fact]
    public void Process_ApprovedStatus_IsApprovedReturnsTrue()
    {
        Payment? approvedPayment = null;
        for (int i = 0; i < 50 && approvedPayment == null; i++)
        {
            var (orderId, userId, gameId) = Ids();
            var p = Payment.Initiate(orderId, userId, "user@example.com", gameId, "Hades", 10m);
            p.Process();
            if (p.IsApproved) approvedPayment = p;
        }

        Assert.NotNull(approvedPayment);
        Assert.True(approvedPayment!.IsApproved);
        Assert.Equal(PaymentStatus.Approved, approvedPayment.Status);
    }

    [Fact]
    public void Process_RejectedStatus_IsApprovedReturnsFalse()
    {
        Payment? rejectedPayment = null;
        for (int i = 0; i < 50 && rejectedPayment == null; i++)
        {
            var (orderId, userId, gameId) = Ids();
            var p = Payment.Initiate(orderId, userId, "user@example.com", gameId, "Hades", 10m);
            p.Process();
            if (!p.IsApproved) rejectedPayment = p;
        }

        Assert.NotNull(rejectedPayment);
        Assert.False(rejectedPayment!.IsApproved);
        Assert.Equal(PaymentStatus.Rejected, rejectedPayment.Status);
    }

    [Fact]
    public void LoadFromHistory_WithInitiatedAndApproved_ReconstitutesCorrectly()
    {
        var paymentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        var events = new IDomainEvent[]
        {
            new PaymentInitiatedEvent(paymentId, userId, "user@example.com", gameId, "Hades", 59.90m, occurredAt),
            new PaymentApprovedEvent(paymentId, occurredAt.AddSeconds(1))
        };

        var payment = Payment.LoadFromHistory(events);

        Assert.Equal(paymentId, payment.Id);
        Assert.Equal(PaymentStatus.Approved, payment.Status);
        Assert.True(payment.IsApproved);
        Assert.Equal(2, payment.Version);
        Assert.Empty(payment.UncommittedEvents);
    }

    [Fact]
    public void LoadFromHistory_WithInitiatedAndRejected_ReconstitutesCorrectly()
    {
        var paymentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        var events = new IDomainEvent[]
        {
            new PaymentInitiatedEvent(paymentId, userId, "user@example.com", gameId, "Hades", 59.90m, occurredAt),
            new PaymentRejectedEvent(paymentId, "Insufficient funds", occurredAt.AddSeconds(1))
        };

        var payment = Payment.LoadFromHistory(events);

        Assert.Equal(PaymentStatus.Rejected, payment.Status);
        Assert.False(payment.IsApproved);
        Assert.Equal(2, payment.Version);
    }

    [Fact]
    public void Initiate_VersionIs1()
    {
        var (orderId, userId, gameId) = Ids();
        var payment = Payment.Initiate(orderId, userId, "user@example.com", gameId, "Hades", 10m);
        Assert.Equal(1, payment.Version);
    }

    [Fact]
    public void Process_VersionIs2()
    {
        var (orderId, userId, gameId) = Ids();
        var payment = Payment.Initiate(orderId, userId, "user@example.com", gameId, "Hades", 10m);
        payment.Process();
        Assert.Equal(2, payment.Version);
    }
}
