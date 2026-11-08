namespace SHBMS.Service.Implementation;
internal record RoomReservation(DateOnly Arrival, DateOnly Departure, int Quantity, bool CheckedIn);
internal static class RoomInventoryRules
{
    // A checkout date is exclusive: checkout and arrival on the same day can share a room.
    // Overdue checked-in guests continue occupying inventory until checkout is recorded.
    internal static int PeakDemand(IEnumerable<RoomReservation> bookings, DateOnly today)
    {
        var events = new SortedDictionary<DateOnly, int>();
        foreach (var b in bookings)
        {
            var start = b.Arrival < today ? today : b.Arrival;
            if (b.CheckedIn && start > today) start = today;
            var end = b.CheckedIn && b.Departure <= today ? DateOnly.MaxValue : b.Departure;
            if (end <= start) continue;
            events[start] = events.GetValueOrDefault(start) + b.Quantity;
            events[end] = events.GetValueOrDefault(end) - b.Quantity;
        }
        int current = 0, peak = 0;
        foreach (int change in events.Values) { current += change; peak = Math.Max(peak, current); }
        return peak;
    }
}
