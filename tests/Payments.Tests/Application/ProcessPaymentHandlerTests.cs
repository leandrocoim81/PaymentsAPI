using FiapCloudGames.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Payments.Application.Commands.ProcessPayment;
using Payments.Application.IntegrationEvents;
using Payments.Domain.Entities;
using Payments.Domain.Interfaces;
using Payments.Domain.ValueObjects;

namespace Payments.Tests.Application;

public class ProcessPaymentHandlerTests
{
    private static ProcessPaymentCommand ValidCmd => new(
        OrderId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        UserEmail: "player@example.com",
        GameId: Guid.NewGuid(),
        GameName: "Hades",
        Price: 59.90m);

    [Fact]
    public async Task Valid_Handle_SavesPaymentAndPublishesBothEvents()
    {
        var repo          = new Mock<IPaymentRepository>();
        var rabbitPublisher = new Mock<IPublishEndpoint>();
        var sqsPublisher    = new Mock<IEventPublisher>();
        var cmd = ValidCmd;

        var sut = new ProcessPaymentHandler(
            repo.Object,
            rabbitPublisher.Object,
            sqsPublisher.Object,
            NullLogger<ProcessPaymentHandler>.Instance);

        await sut.Handle(cmd, CancellationToken.None);

        repo.Verify(r => r.SaveAsync(
            It.Is<Payment>(p =>
                p.UserId == cmd.UserId &&
                p.GameId == cmd.GameId &&
                p.Price == cmd.Price &&
                (p.Status == PaymentStatus.Approved || p.Status == PaymentStatus.Rejected)),
            It.IsAny<CancellationToken>()), Times.Once);

        rabbitPublisher.Verify(p => p.Publish(
            It.Is<PaymentProcessedEvent>(e =>
                e.UserId == cmd.UserId &&
                e.GameId == cmd.GameId &&
                e.Price == cmd.Price &&
                (e.Status == "Approved" || e.Status == "Rejected")),
            It.IsAny<CancellationToken>()), Times.Once);

        sqsPublisher.Verify(p => p.PublishPaymentProcessedAsync(
            It.Is<PaymentProcessedEvent>(e =>
                e.UserId == cmd.UserId &&
                e.GameId == cmd.GameId &&
                e.Price == cmd.Price &&
                (e.Status == "Approved" || e.Status == "Rejected")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Valid_Handle_PaymentStatusMatchesBothPublishedEvents()
    {
        Payment? capturedPayment      = null;
        string?  rabbitStatus         = null;
        string?  sqsStatus            = null;

        var repo = new Mock<IPaymentRepository>();
        repo.Setup(r => r.SaveAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((p, _) => capturedPayment = p);

        var rabbitPublisher = new Mock<IPublishEndpoint>();
        rabbitPublisher
            .Setup(p => p.Publish(It.IsAny<PaymentProcessedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PaymentProcessedEvent, CancellationToken>((e, _) => rabbitStatus = e.Status);

        var sqsPublisher = new Mock<IEventPublisher>();
        sqsPublisher
            .Setup(p => p.PublishPaymentProcessedAsync(It.IsAny<PaymentProcessedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PaymentProcessedEvent, CancellationToken>((e, _) => sqsStatus = e.Status);

        var sut = new ProcessPaymentHandler(
            repo.Object,
            rabbitPublisher.Object,
            sqsPublisher.Object,
            NullLogger<ProcessPaymentHandler>.Instance);

        await sut.Handle(ValidCmd, CancellationToken.None);

        Assert.NotNull(capturedPayment);
        Assert.Equal(capturedPayment!.Status.Value, rabbitStatus);
        Assert.Equal(capturedPayment!.Status.Value, sqsStatus);
    }
}
