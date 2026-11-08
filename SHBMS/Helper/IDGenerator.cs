using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace SHBMS.Helper;

public static class IDGenerator
{
    /// <summary>
    /// Pure logic: finds the largest number among existing IDs and returns the next ID.
    /// </summary>
    public static string Next(IEnumerable<string?> existingIds, string prefix, int digits)
    {
        int max = 0;

        foreach (var raw in existingIds)
        {
            // CHAR(n) columns are padded with spaces, so always trim.
            var id = raw?.Trim();
            if (string.IsNullOrEmpty(id) ||
                !id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (int.TryParse(id.AsSpan(prefix.Length), NumberStyles.None,
                    CultureInfo.InvariantCulture, out int number) && number > max)
            {
                max = number;
            }
        }

        string numberPart = (max + 1).ToString(CultureInfo.InvariantCulture);
        if (numberPart.Length > digits)
            throw new InvalidOperationException(
                $"No more IDs available for prefix '{prefix}' with {digits} digits.");

        return prefix + numberPart.PadLeft(digits, '0');
    }

    public static async Task<string> NextAsync(IQueryable<string> idQuery, string prefix, int digits)
    {
        var ids = await idQuery
            .Where(id => id.StartsWith(prefix))
            .ToListAsync();

        return Next(ids, prefix, digits);
    }
}