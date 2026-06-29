using EventTicketing.Models.Entities;
using EventTicketing.Models.Enums;

namespace EventTicketing.Models;

/// <summary>Domain helper for seat position calculations.</summary>
public static class SeatLayout
{
    /// <summary>Generates all (rowLabel, seatNumber) position tuples for an event's grid.</summary>
    public static IEnumerable<(string RowLabel, int SeatNumber)> AllPositions(Event ev)
    {
        for (var row = 0; row < ev.RowCount; row++)
        {
            var label = ToRowLabel(row);
            for (var number = 1; number <= ev.SeatsPerRow; number++)
                yield return (label, number);
        }
    }

    /// <summary>0 → "A", 25 → "Z", 26 → "AA" (spreadsheet-style).</summary>
    public static string ToRowLabel(int index)
    {
        var label = string.Empty;
        index++;
        while (index > 0)
        {
            index--;
            label = (char)('A' + index % 26) + label;
            index /= 26;
        }
        return label;
    }

    /// <summary>Inverse of ToRowLabel: "A" → 0, "Z" → 25, "AA" → 26. Returns -1 for invalid input.</summary>
    public static int ToRowIndex(string label)
    {
        var result = 0;
        foreach (var c in label.ToUpperInvariant())
        {
            if (c < 'A' || c > 'Z') return -1;
            result = result * 26 + (c - 'A' + 1);
        }
        return result - 1;
    }
}
