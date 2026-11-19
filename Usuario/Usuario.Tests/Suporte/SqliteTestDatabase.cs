using AppTemplate;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Usuario.Tests.Suporte;

public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new DataContext(options);
        Context.Database.EnsureCreated();
    }

    public DataContext Context { get; }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
