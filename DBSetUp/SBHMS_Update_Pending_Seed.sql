-- Run this on the existing database; do not run the full database-reset script.
USE [PRN212_SBHMS];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'PRN212_SBHMS' OR OBJECT_ID(N'dbo.BOOKING', N'U') IS NULL
    THROW 51020, N'Select the existing PRN212_SBHMS database before running this update.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Only update the four original seed rows that are still PENDING.
    -- Keep bookings that have since been checked in, checked out or cancelled.
    UPDATE dbo.BOOKING
    SET BookingStatus = 'CONFIRMED'
    WHERE BookingID IN ('BK0005', 'BK0010', 'BK0036', 'BK0037')
      AND BookingStatus = 'PENDING'
      AND IsDeleted = 0;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT BookingID, BookingStatus, CheckInDate, CheckOutDate
FROM dbo.BOOKING
WHERE BookingID IN ('BK0005', 'BK0010', 'BK0036', 'BK0037')
ORDER BY BookingID;
GO
