using EventTicketing.BusinessLogic;
using EventTicketing.BusinessLogic.Services;
using EventTicketing.DataAccess;
using EventTicketing.DataAccess.Repositories;
using EventTicketing.Tests.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EventTicketing.Tests.Integration;

/// <summary>
/// Spins up a real (relational) database using SQLite in-memory and wires the actual
/// repositories, unit of work and services — so these tests exercise EF mapping,
/// constraints and the transactional purchase flow end-to-end.
/// </summary>
public sealed class SqliteHarness : IDisposable
{
    private readonly SqliteConnection _connection;

    public AppDbContext Db { get; }
    public TestClock Clock { get; } = new(new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc));
    public HoldSettings HoldSettings { get; } = new() { HoldDuration = TimeSpan.FromMinutes(5) };

    public SqliteHarness()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open(); // keep the in-memory DB alive for the harness lifetime

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        Db = new AppDbContext(options);
        Db.Database.EnsureCreated();
    }

    // Fresh context against the same DB (so tests can verify persisted state, not the change tracker).
    public AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new AppDbContext(options);
    }

    public ReservationService Reservations(AppDbContext db) => new(
        new SeatRepository(db), new EventRepository(db), new CustomerRepository(db),
        new SeatStatusHistoryRepository(db), new UnitOfWork(db), Clock, Options.Create(HoldSettings));

    public OrderService Orders(AppDbContext db) => new(
        new SeatRepository(db), new CustomerRepository(db), new OrderRepository(db),
        new SeatStatusHistoryRepository(db), new UnitOfWork(db), Clock);

    public EventService Events(AppDbContext db) => new(
        new EventRepository(db), new SeatRepository(db), new UnitOfWork(db), Clock);

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
