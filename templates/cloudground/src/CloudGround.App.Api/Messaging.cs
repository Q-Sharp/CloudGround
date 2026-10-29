using JasperFx;
using JasperFx.CodeGeneration;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace CloudGround.App.Api;

internal static class Messaging
{
    private const string BrokerConnectionName = "messaging";
    private const string DatabaseConnectionName = "database";
    private const string StorageSchema = "wolverine";

    private static readonly TimeSpan[] RetryCooldowns = [TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)];

    public static WebApplicationBuilder AddMessaging(this WebApplicationBuilder builder)
    {
        builder.Services.CritterStackDefaults(defaults => defaults.Production.GeneratedCodeMode = TypeLoadMode.Static);
        builder.UseWolverine(options =>
        {
            options.UseRabbitMqUsingNamedConnection(BrokerConnectionName)
                .AutoProvision()
                .UseConventionalRouting();
            options.PersistMessagesWithPostgresqlUsingNamedConnection(DatabaseConnectionName, StorageSchema);
            options.UseEntityFrameworkCoreTransactions();
            options.Policies.AutoApplyTransactions();
            options.Policies.DisableConventionalLocalRouting();
            options.Policies.UseDurableInboxOnAllListeners();
            options.Policies.UseDurableOutboxOnAllSendingEndpoints();
            options.Policies.OnException<Exception>()
                .RetryWithCooldown(RetryCooldowns)
                .Then.MoveToErrorQueue();
        });

        return builder;
    }
}
