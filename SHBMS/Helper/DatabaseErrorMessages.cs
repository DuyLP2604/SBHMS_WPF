using Microsoft.EntityFrameworkCore;
namespace SHBMS.Helper;
public static class DatabaseErrorMessages
{
    public static string Describe(Exception exception)
    {
        if (exception is ArgumentException or InvalidOperationException or UnauthorizedAccessException)
            return exception.Message;
        if (exception is DbUpdateException)
            return "Unable to save. Check duplicate IDs, required fields and linked records. "
                + exception.GetBaseException().Message;
        return "Operation failed. Check SQL Server and DefaultConnection in appsettings.json. "
            + exception.GetBaseException().Message;
    }
}
