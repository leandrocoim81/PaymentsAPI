using Amazon;
using Amazon.SQS;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payments.Application.IntegrationEvents;
using Payments.Domain.Interfaces;
using Payments.Infrastructure.Messaging;
using Payments.Infrastructure.Messaging.Sqs;
using Payments.Infrastructure.Persistence;
using Payments.Infrastructure.Persistence.Repositories;

namespace Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextPool<PaymentsDbContext>(
            options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("Payments")),
            poolSize: 128);

        services.AddScoped<IPaymentRepository, PaymentRepository>();

        var region = configuration["AWS:Region"];

        if (!string.IsNullOrWhiteSpace(region))
        {
            services.AddSingleton<IAmazonSQS>(_ =>
                new AmazonSQSClient(
                    RegionEndpoint.GetBySystemName(region)));

            services.AddSingleton<ISqsPublisher, SqsPublisher>();
        }
        else
        {
            services.AddSingleton<ISqsPublisher, NoopSqsPublisher>();
        }

        services.AddScoped<IEventPublisher, SqsEventPublisher>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<OrderPlacedConsumer>(cfg =>
            {
                cfg.ConcurrentMessageLimit = 16;
            });

            x.UsingAmazonSqs((ctx, cfg) =>
            {
                // Credenciais via cadeia padrão da AWS (IAM): perfil local / node role (LabRole) no EKS.
                cfg.Host(configuration["AWS:Region"] ?? "us-east-1", h =>
                {
                    var scope = configuration["Messaging:Scope"];
                    if (!string.IsNullOrWhiteSpace(scope))
                        h.Scope(scope, true);
                });

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}