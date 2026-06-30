using EventTicketing.Models.Dtos;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.DataAccess.Repositories;

/// <summary>
/// Aggregate reports backed by raw SQL. We use raw SQL here (instead of LINQ) because
/// these queries lean on SQL strengths the rest of the codebase doesn't need: conditional
/// aggregation in a single pass, window functions for running totals, and DENSE_RANK.
/// All queries are parameterised; EF still owns the connection and pooling.
/// </summary>
public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _db;
    public ReportRepository(AppDbContext db) => _db = db;

    // Status enum int values (kept as literals in SQL for readability):
    //   EventStatus: Scheduled=0, OnSale=1, SoldOut=2, Cancelled=3, Completed=4
    //   SeatStatus:  Available=0, InProgress=1, Occupied=2
    //   OrderStatus: Pending=0, Paid=1, Cancelled=2

    private const string ActiveOverviewSql = @"
SELECT
    e.Id                                            AS EventId,
    e.Name                                          AS EventName,
    e.[Status]                                      AS [Status],
    e.RowCount * e.SeatsPerRow                      AS PlannedSeats,
    SUM(CASE WHEN s.[Status] = 2 THEN 1 ELSE 0 END) AS SoldSeats,
    SUM(CASE WHEN s.[Status] = 1
              AND s.HoldExpiresAtUtc >= @now THEN 1 ELSE 0 END) AS HeldSeats,
    (e.RowCount * e.SeatsPerRow)
        - SUM(CASE WHEN s.[Status] = 2 THEN 1 ELSE 0 END)
        - SUM(CASE WHEN s.[Status] = 1
                    AND s.HoldExpiresAtUtc >= @now THEN 1 ELSE 0 END) AS AvailableSeats,
    CAST(SUM(CASE WHEN s.[Status] = 2 THEN 1 ELSE 0 END) AS decimal(18,4))
        / NULLIF(e.RowCount * e.SeatsPerRow, 0)     AS SoldRatio,
    COALESCE(SUM(CASE WHEN s.[Status] = 2 THEN s.Price END), 0) AS Revenue
FROM Events e
LEFT JOIN Seats s ON s.EventId = e.Id
WHERE e.[Status] NOT IN (3, 4)        -- exclude Cancelled, Completed; KEEP SoldOut so mismatches show up
GROUP BY e.Id, e.Name, e.[Status], e.RowCount, e.SeatsPerRow
ORDER BY e.StartsAtUtc;";

    private const string DailySalesSql = @"
WITH DailySales AS (
    SELECT
        CAST(o.CreatedAtUtc AS date) AS SaleDate,
        COUNT(*)                      AS TicketsSold,
        SUM(oi.UnitPrice)             AS DailyRevenue
    FROM OrderItems oi
    INNER JOIN Orders o ON o.Id = oi.OrderId
    WHERE oi.EventId = @eventId
      AND o.[Status] = 1                                       -- Paid
      AND o.CreatedAtUtc < DATEADD(day, 1, CAST(@toDate AS date))
    GROUP BY CAST(o.CreatedAtUtc AS date)
)
SELECT
    SaleDate,
    TicketsSold,
    DailyRevenue,
    SUM(TicketsSold)  OVER (ORDER BY SaleDate
                            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS RunningTickets,
    SUM(DailyRevenue) OVER (ORDER BY SaleDate
                            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS RunningRevenue,
    DENSE_RANK()      OVER (ORDER BY DailyRevenue DESC)                       AS RevenueRank
FROM DailySales
ORDER BY SaleDate;";

    public async Task<IReadOnlyList<ActiveEventOverviewRow>> GetActiveEventsOverviewAsync(DateTime nowUtc)
    {
        var rows = await _db.Database
            .SqlQueryRaw<ActiveOverviewSqlRow>(ActiveOverviewSql, new SqlParameter("@now", nowUtc))
            .ToListAsync();

        return rows.Select(r => new ActiveEventOverviewRow(
            r.EventId, r.EventName, (Models.Enums.EventStatus)r.Status,
            r.PlannedSeats, r.SoldSeats, r.HeldSeats, r.AvailableSeats,
            r.SoldRatio ?? 0m, r.Revenue)).ToList();
    }

    public async Task<IReadOnlyList<DailySalesRow>> GetEventDailySalesAsync(int eventId, DateTime toDateUtc)
    {
        var rows = await _db.Database
            .SqlQueryRaw<DailySalesSqlRow>(DailySalesSql,
                new SqlParameter("@eventId", eventId),
                new SqlParameter("@toDate", toDateUtc))
            .ToListAsync();

        return rows.Select(r => new DailySalesRow(
            r.SaleDate, r.TicketsSold, r.DailyRevenue,
            r.RunningTickets, r.RunningRevenue, r.RevenueRank)).ToList();
    }

    // Flat row classes for SqlQueryRaw mapping. Kept private to this file — the public
    // shape (records in ReportDtos) is what callers see.
    private class ActiveOverviewSqlRow
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int Status { get; set; }
        public int PlannedSeats { get; set; }
        public int SoldSeats { get; set; }
        public int HeldSeats { get; set; }
        public int AvailableSeats { get; set; }
        public decimal? SoldRatio { get; set; } // NULL when PlannedSeats = 0 (NULLIF guard)
        public decimal Revenue { get; set; }
    }

    private class DailySalesSqlRow
    {
        public DateTime SaleDate { get; set; }
        public int TicketsSold { get; set; }
        public decimal DailyRevenue { get; set; }
        public int RunningTickets { get; set; }
        public decimal RunningRevenue { get; set; }
        public int RevenueRank { get; set; }
    }
}
