using Npgsql;
using Testcontainers.PostgreSql;

namespace AppTemplate.Infrastructure.Auth.IntegrationTests.Fixtures;

public sealed class DatabaseServer(string database) : IAsyncDisposable
{
    internal const string Image =
        "postgres:19.0-alpine3.24@sha256:8b0ff4cdf221f0c53d7fcda681d3b461be725c492cd881c5ec9c0ad25023f188";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(Image)
        .WithDatabase(database)
        .WithUsername("apptemplate_tests")
        .WithPassword("apptemplate_tests")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task StartAsync(CancellationToken cancellationToken) => _container.StartAsync(cancellationToken);

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    internal static string Table(string schema, string table) => $"{schema}.\"{table}\"";

    internal static Exception TransientFault() =>
        new NpgsqlException("Injected: the connection was lost.", new TimeoutException());
}
