using Npgsql;
using Testcontainers.PostgreSql;

namespace AppTemplate.Infrastructure.Auth.IntegrationTests.Fixtures;

public sealed class DatabaseServer(string database) : IAsyncDisposable
{
    internal const string Image =
        "postgres:18.7-alpine3.24@sha256:b6972e1115a4ff8eba5d7f7d19184926d1e16d7d1136ae1e1a7ebf286da0f015";

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
