
using Api;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tests.Infrastructure;

public sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public DataContext Context { get; }

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection(
            "Data Source=:memory:"
        );

        _connection.Open();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new DataContext(options);

        Context.Database.EnsureCreated();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
