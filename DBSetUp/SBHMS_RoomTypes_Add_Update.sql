-- Run this script on the existing PRN212_SBHMS database before using Add.
-- It updates the room type ID rule and keeps all existing data.
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

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.ROOM_TYPE', N'U') IS NULL
        THROW 50000, 'ROOM_TYPE does not exist in the selected database.', 1;

    IF EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.ROOM_TYPE')
          AND name = N'CK_ROOM_TYPE_ID'
    )
        ALTER TABLE dbo.ROOM_TYPE DROP CONSTRAINT CK_ROOM_TYPE_ID;

    -- Keep the current CHAR(4) keys and allow RT05, RT06, ... RT99.
    ALTER TABLE dbo.ROOM_TYPE WITH CHECK
        ADD CONSTRAINT CK_ROOM_TYPE_ID
        CHECK (RoomTypeID LIKE 'RT[0-9][0-9]' AND RoomTypeID <> 'RT00');

    COMMIT TRANSACTION;
    PRINT N'Room type IDs RT01 to RT99 are now allowed. Existing data is preserved.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
