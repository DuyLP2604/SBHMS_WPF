USE [PRN212_SBHMS];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO
-- Run once for an existing database. Safe to run again.
-- Keeps all bookings, payments and other data. Does not create payment history.
-- CheckInDeadlineOverride is retained as a legacy column but no longer used.
BEGIN TRY
    BEGIN TRANSACTION;

    ALTER TABLE dbo.BOOKING DROP COLUMN CheckInDeadline;
    EXEC(N'ALTER TABLE dbo.BOOKING ADD CheckInDeadline AS DATEADD(SECOND, -1,
        DATEADD(DAY, 1, CONVERT(DATETIME2(0), CheckInDate))) PERSISTED;');

    -- Dynamic SQL allows the view and procedure to be updated in the same transaction.
    EXEC(N'CREATE OR ALTER VIEW dbo.vw_BookingList
AS
SELECT b.BookingID, b.CustomerID, c.FullName AS CustomerName, c.Phone,
       b.EmployeeID, e.FullName AS CreatedByEmployeeName,
       b.BookingDate, b.CheckInDate, b.CheckOutDate, b.CheckInDeadlineOverride,
       b.CheckInDeadline, b.BookingStatus, b.CancellationReason,
       DATEDIFF(DAY, b.CheckInDate, b.CheckOutDate) AS NumberOfNights,
       ISNULL(d.TotalRooms, 0) AS TotalRooms,
       ISNULL(d.NightlyRoomSubtotal, 0)
           * DATEDIFF(DAY, b.CheckInDate, b.CheckOutDate) AS RoomSubtotal,
       ISNULL(s.ServiceSubtotal, 0) AS ServiceSubtotal,
       ISNULL(d.NightlyRoomSubtotal, 0)
           * DATEDIFF(DAY, b.CheckInDate, b.CheckOutDate)
           + ISNULL(s.ServiceSubtotal, 0) AS TotalAmount,
       ISNULL(p.PaidAmount, 0) AS PaidAmount,
       CAST(CASE WHEN b.BookingStatus IN (''PENDING'', ''CONFIRMED'')
                      AND CONVERT(DATE, SYSDATETIME()) > b.CheckInDate
                 THEN 1 ELSE 0 END AS BIT) AS IsOverdueCheckIn
FROM dbo.BOOKING AS b
JOIN dbo.CUSTOMER AS c ON c.CustomerID = b.CustomerID
JOIN dbo.EMPLOYEE AS e ON e.EmployeeID = b.EmployeeID
OUTER APPLY (
    SELECT SUM(bd.Quantity) AS TotalRooms,
           SUM(CONVERT(DECIMAL(28,2), bd.Quantity) * bd.UnitPrice) AS NightlyRoomSubtotal
    FROM dbo.BOOKING_DETAIL AS bd WHERE bd.BookingID = b.BookingID
) AS d
OUTER APPLY (
    SELECT SUM(CONVERT(DECIMAL(28,2), bs.Quantity) * bs.UnitPrice) AS ServiceSubtotal
    FROM dbo.BOOKING_SERVICE AS bs WHERE bs.BookingID = b.BookingID
) AS s
OUTER APPLY (
    SELECT SUM(CONVERT(DECIMAL(28,2), pay.Amount)) AS PaidAmount
    FROM dbo.PAYMENT AS pay WHERE pay.BookingID = b.BookingID
) AS p
WHERE b.IsDeleted = 0;');

    EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_DeleteExpiredBooking
    @BookingID CHAR(6),
    @EmployeeID CHAR(6)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.EMPLOYEE AS e WITH (HOLDLOCK)
            JOIN dbo.USERS AS u WITH (HOLDLOCK) ON u.UserID = e.UserID
            WHERE e.EmployeeID = @EmployeeID AND u.Role = N''Staff''
        )
            THROW 51001, N''Chỉ Staff được xóa booking quá hạn check-in.'', 1;

        DECLARE @Status VARCHAR(30), @CheckInDate DATE, @IsDeleted BIT;

        SELECT @Status = BookingStatus, @CheckInDate = CheckInDate,
               @IsDeleted = IsDeleted
        FROM dbo.BOOKING WITH (UPDLOCK, HOLDLOCK)
        WHERE BookingID = @BookingID;

        IF @@ROWCOUNT = 0
            THROW 51002, N''Booking không tồn tại.'', 1;
        IF @IsDeleted = 1
            THROW 51003, N''Booking đã được xóa trước đó.'', 1;
        IF @Status NOT IN (''PENDING'', ''CONFIRMED'')
            THROW 51004, N''Chỉ xóa booking đang chờ hoặc đã xác nhận, chưa check-in.'', 1;

        DECLARE @Now DATETIME2(7) = SYSDATETIME();

        IF CONVERT(DATE, @Now) <= @CheckInDate
            THROW 51005, N''Booking chưa quá hạn check-in.'', 1;

        UPDATE dbo.BOOKING
        SET BookingStatus = ''CANCELLED'',
            CancellationReason = N''Quá hạn check-in (no-show).'',
            IsDeleted = 1,
            DeletedAt = @Now,
            DeletedByEmployee = @EmployeeID
        WHERE BookingID = @BookingID;

        COMMIT TRANSACTION;
        SELECT @BookingID AS BookingID, CAST(1 AS BIT) AS IsDeleted;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;');

    COMMIT TRANSACTION;
    PRINT N'Updated: check-in is allowed all day on the arrival date.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
