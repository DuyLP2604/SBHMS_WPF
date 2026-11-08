namespace SHBMS.Service.Implementation;

/// <summary>
/// Describe a stay date range and room quantity for inventory calculations.
/// </summary>
internal record RoomReservation(DateOnly Arrival, DateOnly Departure, int Quantity, bool CheckedIn);

internal static class RoomInventoryRules
{
    // A checkout date is exclusive: checkout and arrival on the same day can share a room.
    // Overdue checked-in guests continue occupying inventory until checkout is recorded.

    /// <summary>
    /// Find the highest number of rooms needed at the same time.
    /// </summary>
    internal static int PeakDemand(IEnumerable<RoomReservation> bookings, DateOnly today)
    {
        // Add demand on arrival and remove it on departure; the departure day is excluded.
        var events = new SortedDictionary<DateOnly, int>();
        foreach (var b in bookings)
        {
            var start = b.Arrival < today ? today : b.Arrival;
            if (b.CheckedIn && start > today)
                start = today;
            // An overdue in-house guest keeps occupying stock until checkout is recorded.
            var end = b.CheckedIn && b.Departure <= today ? DateOnly.MaxValue : b.Departure;
            if (end <= start)
                continue;
            events[start] = events.GetValueOrDefault(start) + b.Quantity;
            events[end] = events.GetValueOrDefault(end) - b.Quantity;
        }
        // Process dates in order and keep the largest running total.
        int current = 0, peak = 0;
        foreach (int change in events.Values)
        {
            current += change;
            peak = Math.Max(peak, current);
        }
        return peak;
    }
}
