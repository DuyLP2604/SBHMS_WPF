
USE [master];
GO
IF DB_ID(N'PRN212_SBHMS') IS NOT NULL
BEGIN
    ALTER DATABASE [PRN212_SBHMS] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [PRN212_SBHMS];
END;
GO
CREATE DATABASE [PRN212_SBHMS];
GO
USE [PRN212_SBHMS];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
GO
BEGIN TRY
    BEGIN TRANSACTION;

CREATE TABLE dbo.USERS (
    UserID INT IDENTITY(1,1) NOT NULL,
    Username VARCHAR(50) NOT NULL,
    Password VARCHAR(255) NOT NULL,
    Role VARCHAR(50) NOT NULL,

    CONSTRAINT PK_USERS PRIMARY KEY (UserID),
    CONSTRAINT UQ_USERS_Username UNIQUE (Username),
    CONSTRAINT CK_USERS_Role CHECK (Role IN ('Admin', 'Manager', 'Staff'))
);

CREATE TABLE dbo.EMPLOYEE (
    EmployeeID CHAR(6) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Position NVARCHAR(50) NOT NULL,
    Phone VARCHAR(20) NOT NULL,
    Email VARCHAR(100) NULL,
    UserID INT NOT NULL,

    CONSTRAINT PK_EMPLOYEE PRIMARY KEY (EmployeeID),
    CONSTRAINT UQ_EMPLOYEE_User UNIQUE (UserID),
    CONSTRAINT FK_EMPLOYEE_USERS FOREIGN KEY (UserID) REFERENCES dbo.USERS(UserID)
);

CREATE TABLE dbo.CUSTOMER (
    CustomerID CHAR(6) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    IdentityNumber VARCHAR(20) NULL,
    Phone VARCHAR(15) NOT NULL,
    Email VARCHAR(100) NULL,
    Address NVARCHAR(200) NULL,
    Nationality NVARCHAR(50) NULL, 

    CONSTRAINT PK_CUSTOMER PRIMARY KEY (CustomerID),
    CONSTRAINT UQ_CUSTOMER_Identity UNIQUE (IdentityNumber)
);

CREATE TABLE dbo.ROOM_TYPE (
    RoomTypeID CHAR(4) NOT NULL,
    TypeName NVARCHAR(50) NOT NULL,
    Capacity INT NOT NULL,
    Price DECIMAL(12,2) NOT NULL,
    Description NVARCHAR(500) NULL,

    CONSTRAINT PK_ROOM_TYPE PRIMARY KEY (RoomTypeID)
);

CREATE TABLE dbo.ROOM (
    RoomID CHAR(3) NOT NULL,
    RoomNumber NVARCHAR(20) NOT NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_ROOM_Status DEFAULT 'ACTIVE',
    RoomTypeID CHAR(4) NOT NULL,

    CONSTRAINT PK_ROOM PRIMARY KEY (RoomID),
    CONSTRAINT UQ_ROOM_Number UNIQUE (RoomNumber),
    CONSTRAINT FK_ROOM_ROOM_TYPE FOREIGN KEY (RoomTypeID) REFERENCES dbo.ROOM_TYPE(RoomTypeID),
    CONSTRAINT CK_ROOM_Status CHECK (Status IN ('ACTIVE', 'MAINTENANCE', 'INACTIVE'))
);

CREATE TABLE dbo.SERVICE (
    ServiceID CHAR(4) NOT NULL,
    ServiceName NVARCHAR(60) NOT NULL,
    UnitPrice DECIMAL(12,2) NOT NULL,

    CONSTRAINT PK_SERVICE PRIMARY KEY (ServiceID)
);

CREATE TABLE dbo.BOOKING (
    BookingID CHAR(6) NOT NULL,
    CustomerID CHAR(6) NOT NULL,
    EmployeeID CHAR(6) NOT NULL,
    BookingDate DATETIME NOT NULL CONSTRAINT DF_BOOKING_Date DEFAULT GETDATE(),
    CheckInDate DATE NOT NULL,
    CheckOutDate DATE NOT NULL,
    BookingStatus VARCHAR(30) NOT NULL,
    CancellationReason NVARCHAR(200) NULL,

    CONSTRAINT PK_BOOKING PRIMARY KEY (BookingID),
    CONSTRAINT FK_BOOKING_CUSTOMER FOREIGN KEY (CustomerID) REFERENCES dbo.CUSTOMER(CustomerID),
    CONSTRAINT FK_BOOKING_EMPLOYEE FOREIGN KEY (EmployeeID) REFERENCES dbo.EMPLOYEE(EmployeeID),
    CONSTRAINT CK_BOOKING_Status CHECK (BookingStatus IN (
        'PENDING', 'CONFIRMED', 'CHECKED_IN', 'CHECKED_OUT', 'CANCELLED'
    ))
);

CREATE TABLE dbo.BOOKING_DETAIL (
    BookingDetailID INT IDENTITY(1,1) NOT NULL,
    BookingID CHAR(6) NOT NULL,
    RoomTypeID CHAR(4) NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(12,2) NOT NULL, 

    CONSTRAINT PK_BOOKING_DETAIL PRIMARY KEY (BookingDetailID),
    CONSTRAINT FK_BOOKING_DETAIL_BOOKING FOREIGN KEY (BookingID) REFERENCES dbo.BOOKING(BookingID),
    CONSTRAINT FK_BOOKING_DETAIL_ROOMTYPE FOREIGN KEY (RoomTypeID) REFERENCES dbo.ROOM_TYPE(RoomTypeID)
);

CREATE TABLE dbo.ROOM_ASSIGNMENT (
    AssignmentID INT IDENTITY(1,1) NOT NULL,
    BookingDetailID INT NOT NULL,
    RoomID CHAR(3) NOT NULL,
    AssignedByEmployee CHAR(6) NOT NULL, 
    AssignedAt DATETIME NOT NULL CONSTRAINT DF_ASSIGNMENT_Date DEFAULT GETDATE(),

    CONSTRAINT PK_ROOM_ASSIGNMENT PRIMARY KEY (AssignmentID),
    CONSTRAINT FK_ASSIGNMENT_DETAIL FOREIGN KEY (BookingDetailID) REFERENCES dbo.BOOKING_DETAIL(BookingDetailID),
    CONSTRAINT FK_ASSIGNMENT_ROOM FOREIGN KEY (RoomID) REFERENCES dbo.ROOM(RoomID),
    CONSTRAINT FK_ASSIGNMENT_EMP FOREIGN KEY (AssignedByEmployee) REFERENCES dbo.EMPLOYEE(EmployeeID)
);

CREATE TABLE dbo.BOOKING_SERVICE (
    BookingID CHAR(6) NOT NULL,
    ServiceID CHAR(4) NOT NULL,
    Quantity INT NOT NULL CONSTRAINT DF_B_SERVICE_Qty DEFAULT 1,
    UnitPrice DECIMAL(12,2) NOT NULL, 
    OrderedByEmployee CHAR(6) NOT NULL, 

    CONSTRAINT PK_BOOKING_SERVICE PRIMARY KEY (BookingID, ServiceID),
    CONSTRAINT FK_B_SERVICE_BOOKING FOREIGN KEY (BookingID) REFERENCES dbo.BOOKING(BookingID),
    CONSTRAINT FK_B_SERVICE_SERVICE FOREIGN KEY (ServiceID) REFERENCES dbo.SERVICE(ServiceID),
    CONSTRAINT FK_B_SERVICE_EMP FOREIGN KEY (OrderedByEmployee) REFERENCES dbo.EMPLOYEE(EmployeeID)
);

CREATE TABLE dbo.PAYMENTMETHOD (
    MethodID CHAR(4) NOT NULL,
    MethodName NVARCHAR(30) NOT NULL,
    CONSTRAINT PK_PAYMENTMETHOD PRIMARY KEY (MethodID)
);


CREATE TABLE dbo.PAYMENT (
    PaymentID CHAR(6) NOT NULL,
    BookingID CHAR(6) NOT NULL,
    Amount DECIMAL(12,2) NOT NULL,
    MethodID CHAR(4) NOT NULL,
    PaymentTime DATETIME NOT NULL CONSTRAINT DF_PAYMENT_Time DEFAULT GETDATE(),
    CollectedByEmployee CHAR(6) NOT NULL, 
    Note NVARCHAR(200) NULL, -- Ghi chú (VD: Thu cọc, Thu phần còn lại, Hoàn tiền)

    CONSTRAINT PK_PAYMENT PRIMARY KEY (PaymentID),
    CONSTRAINT FK_PAYMENT_BOOKING FOREIGN KEY (BookingID) REFERENCES dbo.BOOKING(BookingID),
    CONSTRAINT FK_PAYMENT_METHOD FOREIGN KEY (MethodID) REFERENCES dbo.PAYMENTMETHOD(MethodID),
    CONSTRAINT FK_PAYMENT_EMP FOREIGN KEY (CollectedByEmployee) REFERENCES dbo.EMPLOYEE(EmployeeID)
);

CREATE TABLE dbo.INVOICE (
    InvoiceID CHAR(6) NOT NULL,
    BookingID CHAR(6) NOT NULL,
    InvoiceDate DATETIME NOT NULL CONSTRAINT DF_INVOICE_Date DEFAULT GETDATE(),
    TotalAmount DECIMAL(12,2) NOT NULL,
    IssuedByEmployee CHAR(6) NOT NULL, -- Nhân viên xuất hóa đơn

    CONSTRAINT PK_INVOICE PRIMARY KEY (InvoiceID),
    CONSTRAINT FK_INVOICE_BOOKING FOREIGN KEY (BookingID) REFERENCES dbo.BOOKING(BookingID),
    CONSTRAINT FK_INVOICE_EMP FOREIGN KEY (IssuedByEmployee) REFERENCES dbo.EMPLOYEE(EmployeeID)
);

CREATE TABLE dbo.COMPLAINT (
    ComplaintID INT IDENTITY(1,1) NOT NULL,
    CustomerID CHAR(6) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_COMPLAINT_Status DEFAULT N'Chưa xử lý',
    RecordedByEmployee CHAR(6) NOT NULL, 
    ResolvedByEmployee CHAR(6) NULL,     
    ReplyMessage NVARCHAR(MAX) NULL,

    CONSTRAINT PK_COMPLAINT PRIMARY KEY (ComplaintID),
    CONSTRAINT FK_COMPLAINT_CUS FOREIGN KEY (CustomerID) REFERENCES dbo.CUSTOMER(CustomerID),
    CONSTRAINT FK_COMPLAINT_REC_EMP FOREIGN KEY (RecordedByEmployee) REFERENCES dbo.EMPLOYEE(EmployeeID)
);

IF COL_LENGTH(N'dbo.BOOKING', N'CheckInDeadlineOverride') IS NULL
    ALTER TABLE dbo.BOOKING ADD CheckInDeadlineOverride DATETIME2(0) NULL;
IF COL_LENGTH(N'dbo.BOOKING', N'CheckInDeadline') IS NULL
    EXEC(N'ALTER TABLE dbo.BOOKING ADD CheckInDeadline AS
        ISNULL(CheckInDeadlineOverride,
               DATEADD(MINUTE, 855, CONVERT(DATETIME2(0), CheckInDate))) PERSISTED;');
IF COL_LENGTH(N'dbo.BOOKING', N'IsDeleted') IS NULL
    ALTER TABLE dbo.BOOKING ADD IsDeleted BIT NOT NULL
        CONSTRAINT DF_BOOKING_IsDeleted DEFAULT (0) WITH VALUES;
IF COL_LENGTH(N'dbo.BOOKING', N'DeletedAt') IS NULL
    ALTER TABLE dbo.BOOKING ADD DeletedAt DATETIME2(0) NULL;
IF COL_LENGTH(N'dbo.BOOKING', N'DeletedByEmployee') IS NULL
    ALTER TABLE dbo.BOOKING ADD DeletedByEmployee CHAR(6) NULL;

IF OBJECT_ID(N'dbo.FK_BOOKING_DeletedByEmployee', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.BOOKING WITH CHECK ADD
        CONSTRAINT FK_BOOKING_DeletedByEmployee FOREIGN KEY (DeletedByEmployee)
        REFERENCES dbo.EMPLOYEE(EmployeeID);');
IF OBJECT_ID(N'dbo.CK_BOOKING_Dates', N'C') IS NULL
    ALTER TABLE dbo.BOOKING WITH CHECK ADD CONSTRAINT CK_BOOKING_Dates
        CHECK (CheckOutDate > CheckInDate);
IF OBJECT_ID(N'dbo.CK_BOOKING_Deadline', N'C') IS NULL
    EXEC(N'ALTER TABLE dbo.BOOKING WITH CHECK ADD CONSTRAINT CK_BOOKING_Deadline
        CHECK (CheckInDeadlineOverride IS NULL OR
            (CheckInDeadlineOverride >= CONVERT(DATETIME2(0), CheckInDate)
             AND CheckInDeadlineOverride <
                 DATEADD(DAY, 1, CONVERT(DATETIME2(0), CheckInDate))));');
IF OBJECT_ID(N'dbo.CK_BOOKING_DeletionAudit', N'C') IS NULL
    EXEC(N'ALTER TABLE dbo.BOOKING WITH CHECK ADD CONSTRAINT CK_BOOKING_DeletionAudit
        CHECK ((IsDeleted = 0 AND DeletedAt IS NULL AND DeletedByEmployee IS NULL)
            OR (IsDeleted = 1 AND DeletedAt IS NOT NULL
                AND DeletedByEmployee IS NOT NULL AND BookingStatus = ''CANCELLED''));');
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints
               WHERE parent_object_id = OBJECT_ID(N'dbo.BOOKING')
                 AND parent_column_id = COLUMNPROPERTY(
                     OBJECT_ID(N'dbo.BOOKING'), 'BookingStatus', 'ColumnId'))
    ALTER TABLE dbo.BOOKING ADD CONSTRAINT DF_BOOKING_Status
        DEFAULT ('PENDING') FOR BookingStatus;

IF OBJECT_ID(N'dbo.UQ_ROOM_TYPE_TypeName', N'UQ') IS NULL
    ALTER TABLE dbo.ROOM_TYPE ADD CONSTRAINT UQ_ROOM_TYPE_TypeName UNIQUE (TypeName);
IF OBJECT_ID(N'dbo.CK_ROOM_TYPE_Name', N'C') IS NULL
    ALTER TABLE dbo.ROOM_TYPE WITH CHECK ADD CONSTRAINT CK_ROOM_TYPE_Name
        CHECK (LEN(LTRIM(RTRIM(TypeName))) > 0);
IF OBJECT_ID(N'dbo.CK_ROOM_TYPE_Capacity', N'C') IS NULL
    ALTER TABLE dbo.ROOM_TYPE WITH CHECK ADD CONSTRAINT CK_ROOM_TYPE_Capacity
        CHECK (Capacity > 0);
IF OBJECT_ID(N'dbo.CK_ROOM_TYPE_Price', N'C') IS NULL
    ALTER TABLE dbo.ROOM_TYPE WITH CHECK ADD CONSTRAINT CK_ROOM_TYPE_Price
        CHECK (Price > 0);
IF OBJECT_ID(N'dbo.CK_BOOKING_DETAIL_Quantity', N'C') IS NULL
    ALTER TABLE dbo.BOOKING_DETAIL WITH CHECK ADD CONSTRAINT CK_BOOKING_DETAIL_Quantity
        CHECK (Quantity > 0);
IF OBJECT_ID(N'dbo.CK_BOOKING_DETAIL_UnitPrice', N'C') IS NULL
    ALTER TABLE dbo.BOOKING_DETAIL WITH CHECK ADD CONSTRAINT CK_BOOKING_DETAIL_UnitPrice
        CHECK (UnitPrice >= 0);
IF OBJECT_ID(N'dbo.UQ_BOOKING_DETAIL_Booking_RoomType', N'UQ') IS NULL
    ALTER TABLE dbo.BOOKING_DETAIL ADD CONSTRAINT UQ_BOOKING_DETAIL_Booking_RoomType
        UNIQUE (BookingID, RoomTypeID);

-- SQL Server UNIQUE on a nullable column permits only one NULL.
-- The filtered index permits many customers without identity documents.
IF OBJECT_ID(N'dbo.UQ_CUSTOMER_Identity', N'UQ') IS NOT NULL
    ALTER TABLE dbo.CUSTOMER DROP CONSTRAINT UQ_CUSTOMER_Identity;
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID(N'dbo.CUSTOMER')
                 AND name = N'UX_CUSTOMER_Identity_NotNull')
    CREATE UNIQUE INDEX UX_CUSTOMER_Identity_NotNull ON dbo.CUSTOMER(IdentityNumber)
        WHERE IdentityNumber IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID(N'dbo.BOOKING')
                 AND name = N'IX_BOOKING_Active_Status_Dates')
    EXEC(N'CREATE INDEX IX_BOOKING_Active_Status_Dates
        ON dbo.BOOKING(IsDeleted, BookingStatus, CheckInDate, CheckOutDate)
        INCLUDE (CustomerID, EmployeeID);');
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID(N'dbo.BOOKING')
                 AND name = N'IX_BOOKING_CustomerID')
    CREATE INDEX IX_BOOKING_CustomerID ON dbo.BOOKING(CustomerID);
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID(N'dbo.BOOKING_DETAIL')
                 AND name = N'IX_BOOKING_DETAIL_RoomTypeID')
    CREATE INDEX IX_BOOKING_DETAIL_RoomTypeID ON dbo.BOOKING_DETAIL(RoomTypeID);
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID(N'dbo.ROOM')
                 AND name = N'IX_ROOM_RoomType_Status')
    CREATE INDEX IX_ROOM_RoomType_Status ON dbo.ROOM(RoomTypeID, Status);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO 

CREATE OR ALTER VIEW dbo.vw_BookingList
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
       CAST(CASE WHEN b.BookingStatus IN ('PENDING', 'CONFIRMED')
                      AND SYSDATETIME() > b.CheckInDeadline
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
WHERE b.IsDeleted = 0;
GO

CREATE OR ALTER VIEW dbo.vw_BookingDetail
AS
SELECT bd.BookingDetailID, bd.BookingID, bd.RoomTypeID, rt.TypeName,
       rt.Capacity, bd.Quantity, bd.UnitPrice,
       DATEDIFF(DAY, b.CheckInDate, b.CheckOutDate) AS NumberOfNights,
       CONVERT(DECIMAL(28,2), bd.Quantity) * bd.UnitPrice
           * DATEDIFF(DAY, b.CheckInDate, b.CheckOutDate) AS LineTotal
FROM dbo.BOOKING_DETAIL AS bd
JOIN dbo.BOOKING AS b ON b.BookingID = bd.BookingID
JOIN dbo.ROOM_TYPE AS rt ON rt.RoomTypeID = bd.RoomTypeID
WHERE b.IsDeleted = 0;
GO

CREATE OR ALTER VIEW dbo.vw_RoomList
AS
SELECT r.RoomID, r.RoomNumber, r.Status, r.RoomTypeID,
       rt.TypeName, rt.Capacity, rt.Price, rt.Description
FROM dbo.ROOM AS r
JOIN dbo.ROOM_TYPE AS rt ON rt.RoomTypeID = r.RoomTypeID;
GO


CREATE OR ALTER PROCEDURE dbo.usp_DeleteExpiredBooking
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
            WHERE e.EmployeeID = @EmployeeID AND u.Role = 'Staff'
        )
            THROW 51001, N'Chỉ Staff được xóa booking quá hạn check-in.', 1;

        DECLARE @Status VARCHAR(30), @Deadline DATETIME2(0), @IsDeleted BIT;
        SELECT @Status = BookingStatus, @Deadline = CheckInDeadline,
               @IsDeleted = IsDeleted
        FROM dbo.BOOKING WITH (UPDLOCK, HOLDLOCK)
        WHERE BookingID = @BookingID;
        IF @@ROWCOUNT = 0
            THROW 51002, N'Booking không tồn tại.', 1;
        IF @IsDeleted = 1
            THROW 51003, N'Booking đã được xóa trước đó.', 1;
        IF @Status NOT IN ('PENDING', 'CONFIRMED')
            THROW 51004, N'Chỉ xóa booking đang chờ hoặc đã xác nhận, chưa check-in.', 1;

        DECLARE @Now DATETIME2(7) = SYSDATETIME();
        IF @Now <= @Deadline
            THROW 51005, N'Booking chưa quá hạn check-in.', 1;

        UPDATE dbo.BOOKING
        SET BookingStatus = 'CANCELLED',
            CancellationReason = N'Quá hạn check-in (no-show).',
            IsDeleted = 1, DeletedAt = @Now, DeletedByEmployee = @EmployeeID
        WHERE BookingID = @BookingID;
        COMMIT TRANSACTION;
        SELECT @BookingID AS BookingID, CAST(1 AS BIT) AS IsDeleted;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_DeleteRoomType
    @RoomTypeID CHAR(4),
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
            WHERE e.EmployeeID = @EmployeeID AND u.Role = 'Manager'
        )
            THROW 51011, N'Chỉ Manager được xóa loại phòng.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.ROOM_TYPE WITH (XLOCK, HOLDLOCK)
                       WHERE RoomTypeID = @RoomTypeID)
            THROW 51012, N'Loại phòng không tồn tại.', 1;
        IF EXISTS (SELECT 1 FROM dbo.ROOM WHERE RoomTypeID = @RoomTypeID)
            THROW 51013, N'Không thể xóa: loại phòng đang có phòng tham chiếu.', 1;
        IF EXISTS (SELECT 1 FROM dbo.BOOKING_DETAIL WHERE RoomTypeID = @RoomTypeID)
            THROW 51014, N'Không thể xóa: loại phòng đã có lịch sử đặt phòng.', 1;

        DELETE FROM dbo.ROOM_TYPE WHERE RoomTypeID = @RoomTypeID;
        COMMIT TRANSACTION;
        SELECT @RoomTypeID AS RoomTypeID, CAST(1 AS BIT) AS IsDeleted;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

GO

CREATE OR ALTER TRIGGER dbo.trg_BOOKING_BlockPhysicalDelete
ON dbo.BOOKING
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted)
        THROW 51021, N'Không xóa vật lý booking. Hãy dùng usp_DeleteExpiredBooking.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.trg_ROOM_TYPE_ProtectBookedCapacity
ON dbo.ROOM_TYPE
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(Capacity)
       AND EXISTS (
           SELECT 1 FROM inserted AS i
           JOIN deleted AS old ON old.RoomTypeID = i.RoomTypeID
           JOIN dbo.BOOKING_DETAIL AS bd ON bd.RoomTypeID = i.RoomTypeID
           JOIN dbo.BOOKING AS b ON b.BookingID = bd.BookingID
           WHERE i.Capacity < old.Capacity AND b.IsDeleted = 0
             AND b.BookingStatus IN ('PENDING', 'CONFIRMED', 'CHECKED_IN')
       )
        THROW 51022, N'Không giảm sức chứa loại phòng đang có booking chưa kết thúc.', 1;
END;
GO

