using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using server.Controllers;

public class CreateInitialTablesService : BackgroundService
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<CreateInitialTablesService> logger;

    public CreateInitialTablesService(
        IServiceScopeFactory serviceScopeFactory,
        IServiceProvider serviceProvider,
        ILogger<CreateInitialTablesService> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = this.serviceScopeFactory.CreateScope();

        using var connection = (NpgsqlConnection)scope.ServiceProvider.GetService(typeof(NpgsqlConnection));
        connection.Open();
        this.logger.LogInformation("Before creating tables for start");
        using var command = connection.CreateCommandText("CREATE TABLE IF NOT EXISTS values (number INT)");
        await command.ExecuteNonQueryAsync();
        this.logger.LogInformation("Tables created");
    }
}