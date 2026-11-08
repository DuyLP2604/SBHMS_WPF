USE [PRN212_SBHMS];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
-- Apply to the existing database before launching the updated application.
-- Supports both the main database and the teammate's original SERVICE_REQUEST script.
-- Keeps all existing booking, service charge, payment, invoice and request records.
BEGIN TRY
    IF DB_NAME() <> N'PRN212_SBHMS'
        THROW 52200, N'Run this migration in PRN212_SBHMS.', 1;
    IF OBJECT_ID(N'dbo.BOOKING', N'U') IS NULL
       OR OBJECT_ID(N'dbo.SERVICE', N'U') IS NULL
       OR OBJECT_ID(N'dbo.EMPLOYEE', N'U') IS NULL
       OR OBJECT_ID(N'dbo.BOOKING_SERVICE', N'U') IS NULL
        THROW 52201, N'The existing SBHMS tables are required.', 1;

    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.SERVICE', N'IsAvailable') IS NULL
        ALTER TABLE dbo.SERVICE ADD IsAvailable BIT NOT NULL
            CONSTRAINT DF_SERVICE_IsAvailable DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.SERVICE', N'StockQuantity') IS NULL
        ALTER TABLE dbo.SERVICE ADD StockQuantity INT NULL;

    IF OBJECT_ID(N'dbo.SERVICE_REQUEST', N'U') IS NULL
    BEGIN
    CREATE TABLE dbo.SERVICE_REQUEST (
        RequestID INT IDENTITY(1,1) NOT NULL,
        BookingID CHAR(6) NOT NULL,
        ServiceID CHAR(4) NOT NULL,
        RequestDescription NVARCHAR(500) NOT NULL,
        RequestStatus VARCHAR(20) NOT NULL CONSTRAINT DF_SERVICE_REQUEST_Status DEFAULT 'Pending',
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_SERVICE_REQUEST_CreatedAt DEFAULT SYSDATETIME(),
        ResolvedAt DATETIME2 NULL,
        HandledByEmployeeID CHAR(6) NULL,
        Quantity INT NOT NULL CONSTRAINT DF_SERVICE_REQUEST_Quantity DEFAULT 1,
        UnitPrice DECIMAL(12,2) NOT NULL,
        BilledAt DATETIME2 NULL,
        CONSTRAINT PK_SERVICE_REQUEST PRIMARY KEY (RequestID),
        CONSTRAINT FK_ServiceRequest_Booking FOREIGN KEY (BookingID) REFERENCES dbo.BOOKING(BookingID),
        CONSTRAINT FK_ServiceRequest_Service FOREIGN KEY (ServiceID) REFERENCES dbo.SERVICE(ServiceID),
        CONSTRAINT FK_ServiceRequest_Employee FOREIGN KEY (HandledByEmployeeID) REFERENCES dbo.EMPLOYEE(EmployeeID),
        CONSTRAINT CK_ServiceRequest_Status CHECK (RequestStatus IN ('Pending','InProgress','Resolved','Rejected')),
        CONSTRAINT CK_SERVICE_REQUEST_Quantity CHECK (Quantity > 0),
        CONSTRAINT CK_SERVICE_REQUEST_UnitPrice CHECK (UnitPrice >= 0),
        CONSTRAINT CK_SERVICE_REQUEST_Billed CHECK (BilledAt IS NULL OR
            (RequestStatus = 'Resolved' AND ResolvedAt IS NOT NULL AND HandledByEmployeeID IS NOT NULL))
    );
    END;

    IF COL_LENGTH(N'dbo.SERVICE_REQUEST', N'Quantity') IS NULL
        ALTER TABLE dbo.SERVICE_REQUEST ADD Quantity INT NOT NULL
            CONSTRAINT DF_SERVICE_REQUEST_Quantity DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.SERVICE_REQUEST', N'UnitPrice') IS NULL
        ALTER TABLE dbo.SERVICE_REQUEST ADD UnitPrice DECIMAL(12,2) NULL;
    IF COL_LENGTH(N'dbo.SERVICE_REQUEST', N'BilledAt') IS NULL
        ALTER TABLE dbo.SERVICE_REQUEST ADD BilledAt DATETIME2 NULL;

    -- Compile after adding columns. Preserve known booked prices, otherwise use the catalog price.
    -- This fills only missing snapshots and never changes recorded booking service charges.
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SERVICE_REQUEST')
               AND name = N'UnitPrice' AND is_nullable = 1)
    EXEC(N'UPDATE r SET UnitPrice = COALESCE(bs.UnitPrice, s.UnitPrice)
        FROM dbo.SERVICE_REQUEST r
        JOIN dbo.SERVICE s ON s.ServiceID = r.ServiceID
        LEFT JOIN dbo.BOOKING_SERVICE bs ON bs.BookingID = r.BookingID AND bs.ServiceID = r.ServiceID
        WHERE r.UnitPrice IS NULL;
        ALTER TABLE dbo.SERVICE_REQUEST ALTER COLUMN UnitPrice DECIMAL(12,2) NOT NULL;');

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                   WHERE parent_object_id = OBJECT_ID(N'dbo.SERVICE') AND name = N'CK_SERVICE_StockQuantity')
        EXEC(N'ALTER TABLE dbo.SERVICE WITH CHECK ADD CONSTRAINT CK_SERVICE_StockQuantity
            CHECK (StockQuantity IS NULL OR StockQuantity >= 0);');
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                   WHERE parent_object_id = OBJECT_ID(N'dbo.SERVICE_REQUEST') AND name = N'CK_SERVICE_REQUEST_Quantity')
        EXEC(N'ALTER TABLE dbo.SERVICE_REQUEST WITH CHECK ADD CONSTRAINT CK_SERVICE_REQUEST_Quantity CHECK (Quantity > 0);');
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                   WHERE parent_object_id = OBJECT_ID(N'dbo.SERVICE_REQUEST') AND name = N'CK_SERVICE_REQUEST_UnitPrice')
        EXEC(N'ALTER TABLE dbo.SERVICE_REQUEST WITH CHECK ADD CONSTRAINT CK_SERVICE_REQUEST_UnitPrice CHECK (UnitPrice >= 0);');
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                   WHERE parent_object_id = OBJECT_ID(N'dbo.SERVICE_REQUEST') AND name = N'CK_SERVICE_REQUEST_Billed')
        EXEC(N'ALTER TABLE dbo.SERVICE_REQUEST WITH CHECK ADD CONSTRAINT CK_SERVICE_REQUEST_Billed
            CHECK (BilledAt IS NULL OR (RequestStatus = ''Resolved'' AND ResolvedAt IS NOT NULL AND HandledByEmployeeID IS NOT NULL));');
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE object_id = OBJECT_ID(N'dbo.SERVICE_REQUEST') AND name = N'IX_SERVICE_REQUEST_Booking_Status')
        EXEC(N'CREATE INDEX IX_SERVICE_REQUEST_Booking_Status ON dbo.SERVICE_REQUEST(BookingID, RequestStatus);');

    -- Existing closed requests keep their status. They are not charged again by this migration.
    -- BilledAt is assigned only when the updated application completes an open request.
    COMMIT TRANSACTION;
    PRINT N'Service request and availability migration completed.';
    -- Review old resolved requests against recorded charges; do not create a second charge.
    EXEC(N'SELECT r.RequestID, r.BookingID, r.ServiceID, r.RequestStatus,
        r.Quantity, r.UnitPrice, bs.Quantity AS RecordedBookingServiceQuantity
        FROM dbo.SERVICE_REQUEST r
        LEFT JOIN dbo.BOOKING_SERVICE bs ON bs.BookingID = r.BookingID AND bs.ServiceID = r.ServiceID
        WHERE r.RequestStatus = ''Resolved'' AND r.BilledAt IS NULL;');
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
