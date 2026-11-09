SET NOEXEC OFF;
USE [master];
GO
SET IMPLICIT_TRANSACTIONS OFF;

-- PHẦN 0. XÓA DATABASE CŨ VÀ TẠO LẠI
-- SINGLE_USER và DROP nằm trong cùng batch để giảm nguy cơ kết nối khác chen vào.
IF @@TRANCOUNT <> 0
BEGIN
    RAISERROR(N'Hãy kết thúc transaction đang mở trước khi reset database.', 16, 1);
    SET NOEXEC ON;
END;
GO
IF DB_ID(N'PRN212_SBHMS') IS NOT NULL
BEGIN
    EXEC(N'ALTER DATABASE [PRN212_SBHMS] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
           DROP DATABASE [PRN212_SBHMS];');
END;
GO
-- CREATE DATABASE chạy riêng, không nằm trong transaction tạo bảng và dữ liệu.
EXEC(N'CREATE DATABASE [PRN212_SBHMS]');
GO
USE [PRN212_SBHMS];
GO
-- Prevent later batches from creating tables in master if CREATE/USE failed.
IF DB_NAME() <> N'PRN212_SBHMS'
BEGIN
    RAISERROR(N'Chưa chuyển được sang database đích. Dừng script.', 16, 1);
    SET NOEXEC ON;
END;
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

    -- =============================================================
    -- PHẦN 1. CREATE TABLE, PRIMARY KEY, FOREIGN KEY, CHECK, INDEX
    -- =============================================================
    CREATE TABLE dbo.USERS (
        UserID INT IDENTITY(1,1) NOT NULL,
        Username VARCHAR(50) NOT NULL,
        Password VARCHAR(255) NOT NULL,
        Role NVARCHAR(50) NOT NULL,

        CONSTRAINT PK_USERS PRIMARY KEY (UserID),
        CONSTRAINT UQ_USERS_Username UNIQUE (Username),
        CONSTRAINT CK_USERS_Role CHECK (Role IN (N'Admin', N'Manager', N'Staff'))
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

    CREATE TABLE dbo.NATIONALITY (
        NationalityID CHAR(3) NOT NULL,
        NationalityName NVARCHAR(50) NOT NULL,

        CONSTRAINT PK_NATIONALITY PRIMARY KEY (NationalityID),
        CONSTRAINT UQ_NATIONALITY_Name UNIQUE (NationalityName)
    );

    CREATE TABLE dbo.CUSTOMER (
        CustomerID CHAR(6) NOT NULL,
        FullName NVARCHAR(100) NOT NULL,
        IdentityNumber VARCHAR(20) NULL,
        Phone VARCHAR(15) NOT NULL,
        Email VARCHAR(100) NULL,
        Address NVARCHAR(200) NULL,
        NationalityID CHAR(3) NULL,

        CONSTRAINT PK_CUSTOMER PRIMARY KEY (CustomerID),
        CONSTRAINT FK_CUSTOMER_NATIONALITY FOREIGN KEY (NationalityID)
            REFERENCES dbo.NATIONALITY(NationalityID)
    );

    CREATE TABLE dbo.ROOM_TYPE (
        RoomTypeID CHAR(4) NOT NULL,
        TypeName NVARCHAR(50) NOT NULL,
        Capacity INT NOT NULL,
        Price DECIMAL(12,2) NOT NULL,
        RoomTypeImage NVARCHAR(255) NULL,
        Description NVARCHAR(500) NULL,

        CONSTRAINT PK_ROOM_TYPE PRIMARY KEY (RoomTypeID),
        CONSTRAINT CK_ROOM_TYPE_ID CHECK (RoomTypeID LIKE 'RT[0-9][0-9]' AND RoomTypeID <> 'RT00'),
        CONSTRAINT UQ_ROOM_TYPE_TypeName UNIQUE (TypeName),
        CONSTRAINT CK_ROOM_TYPE_Name CHECK (LEN(LTRIM(RTRIM(TypeName))) > 0),
        CONSTRAINT CK_ROOM_TYPE_Capacity CHECK (Capacity > 0),
        CONSTRAINT CK_ROOM_TYPE_Price CHECK (Price > 0)
    );

    CREATE TABLE dbo.ROOM (
        RoomID CHAR(3) NOT NULL,
        RoomNumber NVARCHAR(20) NOT NULL,
        RoomImage NVARCHAR(100) NULL,
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
        IsAvailable BIT NOT NULL CONSTRAINT DF_SERVICE_IsAvailable DEFAULT (1),
        StockQuantity INT NULL,

        CONSTRAINT PK_SERVICE PRIMARY KEY (ServiceID),
        CONSTRAINT CK_SERVICE_StockQuantity CHECK (StockQuantity IS NULL OR StockQuantity >= 0)
    );

    CREATE TABLE dbo.BOOKING (
        BookingID CHAR(6) NOT NULL,
        CustomerID CHAR(6) NOT NULL,
        EmployeeID CHAR(6) NOT NULL,
        BookingDate DATETIME NOT NULL CONSTRAINT DF_BOOKING_Date DEFAULT GETDATE(),
        CheckInDate DATE NOT NULL,
        CheckOutDate DATE NOT NULL,
        BookingStatus VARCHAR(30) NOT NULL CONSTRAINT DF_BOOKING_Status DEFAULT 'PENDING',
        CancellationReason NVARCHAR(200) NULL,
        CheckInDeadlineOverride DATETIME2(0) NULL,
        -- Legacy override is retained for compatibility; check-in uses the arrival date only.
        CheckInDeadline AS DATEADD(SECOND, -1,
            DATEADD(DAY, 1, CONVERT(DATETIME2(0), CheckInDate))
        ) PERSISTED,
        IsDeleted BIT NOT NULL CONSTRAINT DF_BOOKING_IsDeleted DEFAULT (0),
        DeletedAt DATETIME2(0) NULL,
        DeletedByEmployee CHAR(6) NULL,

        CONSTRAINT PK_BOOKING PRIMARY KEY (BookingID),
        CONSTRAINT CK_BOOKING_ID CHECK (
            BookingID COLLATE Latin1_General_100_BIN2 LIKE 'BK[0-9][0-9][0-9][0-9]'
            AND BookingID <> 'BK0000'
        ),
        CONSTRAINT FK_BOOKING_CUSTOMER FOREIGN KEY (CustomerID) REFERENCES dbo.CUSTOMER(CustomerID),
        CONSTRAINT FK_BOOKING_EMPLOYEE FOREIGN KEY (EmployeeID) REFERENCES dbo.EMPLOYEE(EmployeeID),
        CONSTRAINT FK_BOOKING_DeletedByEmployee FOREIGN KEY (DeletedByEmployee) REFERENCES dbo.EMPLOYEE(EmployeeID),
        CONSTRAINT CK_BOOKING_Status CHECK (BookingStatus IN (
            'PENDING', 'CONFIRMED', 'CHECKED_IN', 'CHECKED_OUT', 'CANCELLED'
        )),
        CONSTRAINT CK_BOOKING_Dates CHECK (CheckOutDate > CheckInDate),
        CONSTRAINT CK_BOOKING_Deadline CHECK (
            CheckInDeadlineOverride IS NULL OR (
                CheckInDeadlineOverride >= CONVERT(DATETIME2(0), CheckInDate)
                AND CheckInDeadlineOverride < DATEADD(DAY, 1, CONVERT(DATETIME2(0), CheckInDate))
            )
        ),
        CONSTRAINT CK_BOOKING_DeletionAudit CHECK (
            (IsDeleted = 0 AND DeletedAt IS NULL AND DeletedByEmployee IS NULL)
            OR (IsDeleted = 1 AND DeletedAt IS NOT NULL
                AND DeletedByEmployee IS NOT NULL AND BookingStatus = 'CANCELLED')
        )
    );

    CREATE TABLE dbo.BOOKING_DETAIL (
        BookingDetailID INT IDENTITY(1,1) NOT NULL,
        BookingID CHAR(6) NOT NULL,
        RoomTypeID CHAR(4) NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(12,2) NOT NULL,

        CONSTRAINT PK_BOOKING_DETAIL PRIMARY KEY (BookingDetailID),
        CONSTRAINT FK_BOOKING_DETAIL_BOOKING FOREIGN KEY (BookingID) REFERENCES dbo.BOOKING(BookingID),
        CONSTRAINT FK_BOOKING_DETAIL_ROOMTYPE FOREIGN KEY (RoomTypeID) REFERENCES dbo.ROOM_TYPE(RoomTypeID),
        CONSTRAINT UQ_BOOKING_DETAIL_Booking_RoomType UNIQUE (BookingID, RoomTypeID),
        CONSTRAINT CK_BOOKING_DETAIL_Quantity CHECK (Quantity > 0),
        CONSTRAINT CK_BOOKING_DETAIL_UnitPrice CHECK (UnitPrice >= 0)
    );

    CREATE TABLE dbo.ROOM_ASSIGNMENT (
        AssignmentID INT IDENTITY(1,1) NOT NULL,
        BookingDetailID INT NOT NULL,
        RoomID CHAR(3) NOT NULL,
        EmployeeID CHAR(6) NULL,
        AssignedAt DATETIME NOT NULL CONSTRAINT DF_ASSIGNMENT_Date DEFAULT GETDATE(),

        CONSTRAINT PK_ROOM_ASSIGNMENT PRIMARY KEY (AssignmentID),
        CONSTRAINT FK_ASSIGNMENT_DETAIL FOREIGN KEY (BookingDetailID) REFERENCES dbo.BOOKING_DETAIL(BookingDetailID),
        CONSTRAINT FK_ASSIGNMENT_ROOM FOREIGN KEY (RoomID) REFERENCES dbo.ROOM(RoomID),
        CONSTRAINT FK_ROOM_ASSIGNMENT_EMPLOYEE FOREIGN KEY (EmployeeID) REFERENCES dbo.EMPLOYEE(EmployeeID)
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

    CREATE TABLE dbo.PAYMENTMETHOD (
        MethodID CHAR(4) NOT NULL,
        MethodName NVARCHAR(30) NOT NULL,

        CONSTRAINT PK_PAYMENTMETHOD PRIMARY KEY (MethodID)
    );

    CREATE TABLE dbo.PAYMENT (
        PaymentID CHAR(6) NOT NULL,
        BookingID CHAR(6) NOT NULL,
        PaymentTime DATETIME NOT NULL CONSTRAINT DF_PAYMENT_Time DEFAULT GETDATE(),
        Amount DECIMAL(12,2) NOT NULL,
        MethodID CHAR(4) NOT NULL,
        CollectedByEmployee CHAR(6) NULL,
        Note NVARCHAR(200) NULL,

        CONSTRAINT PK_PAYMENT PRIMARY KEY (PaymentID),
        CONSTRAINT FK_PAYMENT_BOOKING FOREIGN KEY (BookingID) REFERENCES dbo.BOOKING(BookingID),
        CONSTRAINT FK_PAYMENT_METHOD FOREIGN KEY (MethodID) REFERENCES dbo.PAYMENTMETHOD(MethodID),
        CONSTRAINT FK_PAYMENT_EMP FOREIGN KEY (CollectedByEmployee) REFERENCES dbo.EMPLOYEE(EmployeeID)
    );

    CREATE TABLE dbo.INVOICE (
        InvoiceID CHAR(6) NOT NULL,
        InvoiceDate DATETIME NOT NULL CONSTRAINT DF_INVOICE_Date DEFAULT GETDATE(),
        TotalAmount DECIMAL(12,2) NOT NULL,
        EmployeeID CHAR(6) NULL,
        BookingID CHAR(6) NOT NULL,

        CONSTRAINT PK_INVOICE PRIMARY KEY (InvoiceID),
        CONSTRAINT FK_INVOICE_EMPLOYEE FOREIGN KEY (EmployeeID) REFERENCES dbo.EMPLOYEE(EmployeeID),
        CONSTRAINT FK_INVOICE_BOOKING FOREIGN KEY (BookingID) REFERENCES dbo.BOOKING(BookingID)
    );

    CREATE TABLE dbo.COMPLAINT (
        ComplaintID INT IDENTITY(1,1) NOT NULL,
        Title NVARCHAR(200) NOT NULL CONSTRAINT DF_COMPLAINT_Title DEFAULT N'Phản hồi khách hàng',
        Content NVARCHAR(MAX) NOT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_COMPLAINT_CreatedAt DEFAULT GETDATE(),
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_COMPLAINT_Status DEFAULT N'Pending',
        CustomerID CHAR(6) NULL,
        ReplyMessage NVARCHAR(MAX) NULL,
        EmployeeID CHAR(6) NULL,
        ResolvedByEmployee CHAR(6) NULL,

        CONSTRAINT PK_COMPLAINT PRIMARY KEY (ComplaintID),
        CONSTRAINT FK_COMPLAINT_CUSTOMER FOREIGN KEY (CustomerID) REFERENCES dbo.CUSTOMER(CustomerID),
        CONSTRAINT FK_COMPLAINT_EMPLOYEE FOREIGN KEY (EmployeeID) REFERENCES dbo.EMPLOYEE(EmployeeID),
        CONSTRAINT FK_COMPLAINT_RESOLVED_EMPLOYEE FOREIGN KEY (ResolvedByEmployee) REFERENCES dbo.EMPLOYEE(EmployeeID),
        CONSTRAINT CK_COMPLAINT_Status CHECK (Status IN (N'Pending', N'Resolved'))
    );

    CREATE UNIQUE INDEX UX_CUSTOMER_Identity_NotNull
        ON dbo.CUSTOMER(IdentityNumber)
        WHERE IdentityNumber IS NOT NULL;

    CREATE INDEX IX_BOOKING_Active_Status_Dates
        ON dbo.BOOKING(IsDeleted, BookingStatus, CheckInDate, CheckOutDate)
        INCLUDE (CustomerID, EmployeeID);

    CREATE INDEX IX_BOOKING_CustomerID ON dbo.BOOKING(CustomerID);
    CREATE INDEX IX_BOOKING_DETAIL_RoomTypeID ON dbo.BOOKING_DETAIL(RoomTypeID);
    CREATE INDEX IX_ROOM_RoomType_Status ON dbo.ROOM(RoomTypeID, Status);

    CREATE INDEX IX_CUSTOMER_NationalityID ON dbo.CUSTOMER(NationalityID);
    CREATE INDEX IX_SERVICE_REQUEST_Booking_Status ON dbo.SERVICE_REQUEST(BookingID, RequestStatus);

    -- =============================================================
    -- PHẦN 2. INSERT INTO - DỮ LIỆU MẪU
    -- =============================================================
    DECLARE @Today DATE = CONVERT(DATE, GETDATE());
    DECLARE @DayShift INT = DATEDIFF(DAY, CONVERT(DATE, '20260930', 112), @Today);
    DECLARE @ReferenceDate DATE = @Today;

    INSERT INTO dbo.NATIONALITY (NationalityID, NationalityName)
    VALUES
        (N'N01', N'Vietnam'),
        (N'N02', N'Japan'),
        (N'N03', N'China'),
        (N'N04', N'South Korea'),
        (N'N05', N'Thailand'),
        (N'N06', N'Singapore'),
        (N'N07', N'Malaysia'),
        (N'N08', N'Indonesia'),
        (N'N09', N'Philippines'),
        (N'N10', N'India'),
        (N'N11', N'United States'),
        (N'N12', N'Canada'),
        (N'N13', N'United Kingdom'),
        (N'N14', N'France'),
        (N'N15', N'Germany'),
        (N'N16', N'Italy'),
        (N'N17', N'Spain'),
        (N'N18', N'Australia'),
        (N'N19', N'New Zealand'),
        (N'N20', N'Poland');

    INSERT INTO dbo.USERS (Username, Password, Role)
    VALUES
        (N'admin', N'8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92', N'Admin'),
        (N'manager', N'96CAE35CE8A9B0244178BF28E4966C2CE1B8385723A96A6B838858CDD6CA0A1E', N'Manager'),
        (N'staff', N'0A0667865BC17F9D624BCF11088057BBAB46336E7DAE65F3D5366F4F7A18333E', N'Staff');

    INSERT INTO dbo.EMPLOYEE (EmployeeID, FullName, Position, Phone, Email, UserID)
    VALUES
        (N'EMP001', N'Nguyễn Thành Đạt', N'Admin', N'0901000013', N'nv13.hbms@gmail.com', 1),
        (N'EMP002', N'Trần Quốc Bảo', N'Manager', N'0901000003', N'nv03.hbms@gmail.com', 2),
        (N'EMP003', N'Nguyễn Minh Anh', N'Staff', N'0901000002', N'nv02.hbms@gmail.com', 3);

    -- Synthetic identity/passport numbers for sample data only.
    INSERT INTO dbo.CUSTOMER (CustomerID, FullName, IdentityNumber, Phone, Email, Address, NationalityID)
    VALUES
        (N'CUS001', N'Phạm Minh Tuấn', N'079099000001', N'0903456789', N'tuan.pham@gmail.com', N'Quận 1, TP.HCM', N'N01'),
        (N'CUS002', N'Nguyễn Tuyết Mai', N'079099000002', N'0912888999', N'maituyet92@yahoo.com', N'Quận Cầu Giấy, Hà Nội', N'N01'),
        (N'CUS003', N'Lê Hoàng Nam', N'079099000003', N'0987111222', N'namlh@gmail.com', N'Quận 7, TP.HCM', N'N01'),
        (N'CUS004', N'Trần Thu Hà', N'079099000004', N'0356123456', N'hatran@fpt.com.vn', N'Quận Tây Hồ, Hà Nội', N'N01'),
        (N'CUS005', N'Robert Harrison', N'PTEST000005', N'0775123456', N'robert.h@outlook.com', N'London, United Kingdom', N'N13'),
        (N'CUS006', N'Chen Wei', N'PTEST000006', N'0933444555', N'chenwei88@qq.com', N'Beijing, China', N'N03'),
        (N'CUS007', N'Hans Müller', N'PTEST000007', N'0888777666', N'hans.m@gmail.de', N'Berlin, Germany', N'N15'),
        (N'CUS008', N'Kim Ji-won', N'PTEST000008', N'0944555666', N'jiwon.kim@naver.com', N'Seoul, South Korea', N'N04'),
        (N'CUS009', N'Jean Dupont', N'PTEST000009', N'0766112233', N'jean.dupont@france.fr', N'Paris, France', N'N14'),
        (N'CUS010', N'Đặng Phương Thảo', N'079099000010', N'0399123456', N'thaophuong@gmail.com', N'Quận Hải Châu, Đà Nẵng', N'N01'),
        (N'CUS011', N'Vũ Đình Tùng', N'079099000011', N'0988123123', N'tung.vu@gmail.com', N'Quận 3, TP.HCM', N'N01'),
        (N'CUS012', N'Lê Thị Thu Hương', N'079099000012', N'0977234234', N'huongle99@yahoo.com', N'Quận Đống Đa, Hà Nội', N'N01'),
        (N'CUS013', N'Nguyễn Quang Hải', N'079099000013', N'0912345345', N'hai.nq@fpt.edu.vn', N'Ninh Kiều, Cần Thơ', N'N01'),
        (N'CUS014', N'Phan Thanh Bình', N'079099000014', N'0933456456', N'binhpt@gmail.com', N'Quận 1, TP.HCM', N'N01'),
        (N'CUS015', N'Đỗ Mỹ Linh', N'079099000015', N'0944567567', N'mylinh.do@gmail.com', N'Quận Hoàn Kiếm, Hà Nội', N'N01'),
        (N'CUS016', N'Bùi Tiến Dũng', N'079099000016', N'0966678678', N'dung.bui@viettel.com.vn', N'Thạch Thất, Hà Nội', N'N01'),
        (N'CUS017', N'Trần Phương Anh', N'079099000017', N'0888789789', N'phuonganh.t@yahoo.com', N'Quận Sơn Trà, Đà Nẵng', N'N01'),
        (N'CUS018', N'Lý Nhã Kỳ', N'079099000018', N'0901890890', N'nhaky.ly@gmail.com', N'Quận 7, TP.HCM', N'N01'),
        (N'CUS019', N'Hoàng Lệ Thu', N'079099000019', N'0922901901', N'thuhl@outlook.com', N'Biên Hòa, Đồng Nai', N'N01'),
        (N'CUS020', N'Ngô Thanh Vân', N'079099000020', N'0933012012', N'van.ngo@gmail.com', N'Quận Phú Nhuận, TP.HCM', N'N01'),
        (N'CUS021', N'Võ Hoàng Yến', N'079099000021', N'0944123123', N'hoangyen.vo@gmail.com', N'Quận 4, TP.HCM', N'N01'),
        (N'CUS022', N'Đinh Ngọc Diệp', N'079099000022', N'0955234234', N'ngocdiep.dinh@yahoo.com', N'Quận Bình Thạnh, TP.HCM', N'N01'),
        (N'CUS023', N'Lương Xuân Trường', N'079099000023', N'0966345345', N'truong.lx@hagl.com.vn', N'Pleiku, Gia Lai', N'N01'),
        (N'CUS024', N'Cao Thái Sơn', N'079099000024', N'0977456456', N'soncao@gmail.com', N'Quận 10, TP.HCM', N'N01'),
        (N'CUS025', N'Hồ Ngọc Hà', N'079099000025', N'0988567567', N'hongocha@gmail.com', N'Quận 2, TP.HCM', N'N01'),
        (N'CUS026', N'Mai Phương Thúy', N'079099000026', N'0999678678', N'thuymp@gmail.com', N'Quận Cầu Giấy, Hà Nội', N'N01'),
        (N'CUS027', N'Dương Trương Thiên Lý', N'079099000027', N'0901789789', N'thienly.dt@gmail.com', N'Đồng Tháp', N'N01'),
        (N'CUS028', N'Tăng Thanh Hà', N'079099000028', N'0912890890', N'hathanh.tang@gmail.com', N'Quận 2, TP.HCM', N'N01'),
        (N'CUS029', N'Phạm Hương', N'079099000029', N'0923901901', N'huong.pham@gmail.com', N'Hải Phòng', N'N01'),
        (N'CUS030', N'Nguyễn Thúc Thùy Tiên', N'079099000030', N'0934012012', N'thuytien.nt@gmail.com', N'Quận Gò Vấp, TP.HCM', N'N01'),
        (N'CUS031', N'Lê Quốc Bảo', N'079099000031', N'0945123123', N'baolq@gmail.com', N'Quận Tân Bình, TP.HCM', N'N01'),
        (N'CUS032', N'Trần Kim Chi', N'079099000032', N'0956234234', N'kimchi.tran@yahoo.com', N'Quận Thanh Xuân, Hà Nội', N'N01'),
        (N'CUS033', N'Phan Đình Phùng', N'079099000033', N'0967345345', N'phungpd@gmail.com', N'Vinh, Nghệ An', N'N01'),
        (N'CUS034', N'Vũ Cát Tường', N'079099000034', N'0978456456', N'cattuong.vu@gmail.com', N'Long Xuyên, An Giang', N'N01'),
        (N'CUS035', N'Đỗ Trọng Hiếu', N'079099000035', N'0989567567', N'hieudo@gmail.com', N'Quận Hai Bà Trưng, Hà Nội', N'N01'),
        (N'CUS036', N'Bùi Anh Tuấn', N'079099000036', N'0990678678', N'anhtuan.bui@gmail.com', N'Quận 1, TP.HCM', N'N01'),
        (N'CUS037', N'Lê Minh Sơn', N'079099000037', N'0902789789', N'sonlm@gmail.com', N'Bắc Ninh', N'N01'),
        (N'CUS038', N'Hoàng Thùy Linh', N'079099000038', N'0913890890', N'thuylinh.hoang@gmail.com', N'Quận Ba Đình, Hà Nội', N'N01'),
        (N'CUS039', N'Nguyễn Trần Trung Quân', N'079099000039', N'0924901901', N'trungquan.nt@gmail.com', N'Quận Đống Đa, Hà Nội', N'N01'),
        (N'CUS040', N'Phạm Tiến Dũng', N'079099000040', N'0935012012', N'dungpt@gmail.com', N'Nha Trang, Khánh Hòa', N'N01'),
        (N'CUS041', N'Emily Johnson', N'PTEST000041', N'+12025550141', N'emily.j@yahoo.com', N'Los Angeles, USA', N'N11'),
        (N'CUS042', N'Michael Brown', N'PTEST000042', N'+13125550142', N'michael.b@outlook.com', N'Chicago, USA', N'N11'),
        (N'CUS043', N'Li Na', N'PTEST000043', N'+8613812340043', N'lina.beijing@qq.com', N'Shanghai, China', N'N03'),
        (N'CUS044', N'Wang Lei', N'PTEST000044', N'+8613912340044', N'wanglei88@wechat.com', N'Guangzhou, China', N'N03'),
        (N'CUS045', N'Park Min-young', N'PTEST000045', N'+821012340045', N'minyoung.park@naver.com', N'Busan, South Korea', N'N04'),
        (N'CUS046', N'Lee Jong-suk', N'PTEST000046', N'+821012340046', N'jongsuk.lee@daum.net', N'Incheon, South Korea', N'N04'),
        (N'CUS047', N'Sato Kenji', N'PTEST000047', N'+819012340047', N'kenji.sato@yahoo.co.jp', N'Tokyo, Japan', N'N02'),
        (N'CUS048', N'Tanaka Yuki', N'PTEST000048', N'+819012340048', N'yuki.tanaka@gmail.com', N'Osaka, Japan', N'N02'),
        (N'CUS049', N'Anna Kowalski', N'PTEST000049', N'+48500123449', N'anna.k@gmail.com', N'Warsaw, Poland', N'N20'),
        (N'CUS050', N'David Silva', N'PTEST000050', N'+34600123450', N'david.silva@hotmail.com', N'Madrid, Spain', N'N17');

    INSERT INTO dbo.ROOM_TYPE (RoomTypeID, TypeName, Capacity, Price, RoomTypeImage, Description)
    VALUES
        (N'RT01', N'Phòng đơn', 2, 500000, N'room-type-single.jpg', N'1 giường đôi, tối đa 2 khách'),
        (N'RT02', N'Phòng đôi', 4, 800000, N'room-type-double.jpg', N'2 giường đôi, tối đa 4 khách'),
        (N'RT03', N'Phòng Luxury đơn', 2, 1200000, N'room-type-luxury-single.jpg', N'Phòng cao cấp 1 giường đôi, tối đa 2 khách'),
        (N'RT04', N'Phòng Luxury đôi', 4, 1600000, N'room-type-luxury-double.jpg', N'Phòng cao cấp 2 giường đôi, tối đa 4 khách');

    INSERT INTO dbo.ROOM (RoomID, RoomNumber, RoomImage, Status, RoomTypeID)
    VALUES
        (N'R01', N'101', N'room01.png', N'ACTIVE', N'RT01'),
        (N'R02', N'102', N'room02.png', N'ACTIVE', N'RT01'),
        (N'R03', N'103', N'room03.png', N'MAINTENANCE', N'RT01'),
        (N'R04', N'201', N'room04.png', N'ACTIVE', N'RT02'),
        (N'R05', N'202', N'room05.png', N'ACTIVE', N'RT02'),
        (N'R06', N'203', N'room06.png', N'ACTIVE', N'RT02'),
        (N'R07', N'204', N'room07.png', N'ACTIVE', N'RT02'),
        (N'R08', N'301', N'room08.png', N'ACTIVE', N'RT03'),
        (N'R09', N'302', N'room09.png', N'ACTIVE', N'RT03'),
        (N'R10', N'401', N'room10.png', N'ACTIVE', N'RT04');

    INSERT INTO dbo.SERVICE (ServiceID, ServiceName, UnitPrice)
    VALUES
        (N'S01', N'Giặt ủi', 75000),
        (N'S02', N'Suối nước nóng', 200000),
        (N'S03', N'Thuê xe', 150000),
        (N'S04', N'Spa', 450000),
        (N'S05', N'Ăn uống tại phòng', 0),
        (N'S06', N'Dịch vụ trông trẻ', 150000),
        (N'S07', N'Đưa đón sân bay', 350000),
        (N'S08', N'Dọn dẹp phòng', 0),
        (N'S09', N'Ăn tối buffer', 350000),
        (N'S10', N'Giữ hành lý', 50000);

    INSERT INTO dbo.PAYMENTMETHOD (MethodID, MethodName)
    VALUES
        (N'PT01', N'VietQR'),
        (N'PT02', N'Ví nội bộ'),
        (N'PT03', N'Tiền mặt');

    INSERT INTO dbo.BOOKING (BookingID, CustomerID, EmployeeID, BookingDate, CheckInDate, CheckOutDate, BookingStatus, CancellationReason)
    VALUES
        (N'BK0001', N'CUS001', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-01 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-05', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-07', 120)), N'CHECKED_OUT', NULL),
        (N'BK0002', N'CUS002', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-03 10:15:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-08', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-10', 120)), N'CHECKED_OUT', NULL),
        (N'BK0003', N'CUS003', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-10 08:30:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-18', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-20', 120)), N'CONFIRMED', NULL),
        (N'BK0004', N'CUS004', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-11 14:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-20', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-22', 120)), N'CONFIRMED', NULL),
        (N'BK0005', N'CUS005', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-12 16:10:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-21', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-23', 120)), N'CONFIRMED', NULL),
        (N'BK0006', N'CUS006', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-13 11:45:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-24', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-26', 120)), N'CONFIRMED', NULL),
        (N'BK0007', N'CUS007', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-14 09:20:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-25', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-27', 120)), N'CANCELLED', N'Khách thay đổi lịch trình và hủy đặt phòng.'),
        (N'BK0008', N'CUS008', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-15 13:30:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-28', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-01', 120)), N'CHECKED_IN', NULL),
        (N'BK0009', N'CUS009', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-16 15:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-02', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-04', 120)), N'CONFIRMED', NULL),
        (N'BK0010', N'CUS010', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-17 06:30:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-05', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-07', 120)), N'CONFIRMED', NULL),
        (N'BK0011', N'CUS011', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-07-27 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-03', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-05', 120)), N'CHECKED_OUT', NULL),
        (N'BK0012', N'CUS012', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-07-28 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-04', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-07', 120)), N'CHECKED_OUT', NULL),
        (N'BK0013', N'CUS013', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-02 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-09', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-11', 120)), N'CHECKED_OUT', NULL),
        (N'BK0014', N'CUS014', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-05 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-12', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-14', 120)), N'CHECKED_OUT', NULL),
        (N'BK0015', N'CUS015', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-10 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-17', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-20', 120)), N'CHECKED_OUT', NULL),
        (N'BK0016', N'CUS016', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-11 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-18', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-21', 120)), N'CHECKED_OUT', NULL),
        (N'BK0017', N'CUS017', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-16 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-23', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-25', 120)), N'CHECKED_OUT', NULL),
        (N'BK0018', N'CUS018', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-19 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-26', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-29', 120)), N'CHECKED_OUT', NULL),
        (N'BK0019', N'CUS019', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-23 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-30', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-01', 120)), N'CHECKED_OUT', NULL),
        (N'BK0020', N'CUS020', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-24 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-31', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-03', 120)), N'CHECKED_OUT', NULL),
        (N'BK0021', N'CUS021', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-28 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-04', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-06', 120)), N'CHECKED_OUT', NULL),
        (N'BK0022', N'CUS022', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-30 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-06', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-08', 120)), N'CHECKED_OUT', NULL),
        (N'BK0023', N'CUS023', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-02 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-09', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-11', 120)), N'CHECKED_OUT', NULL),
        (N'BK0024', N'CUS024', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-04 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-11', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-13', 120)), N'CHECKED_OUT', NULL),
        (N'BK0025', N'CUS025', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-06 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-13', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-16', 120)), N'CHECKED_OUT', NULL),
        (N'BK0026', N'CUS026', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-08 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-15', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-17', 120)), N'CHECKED_OUT', NULL),
        (N'BK0027', N'CUS027', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-10 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-17', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-19', 120)), N'CHECKED_OUT', NULL),
        (N'BK0028', N'CUS028', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-12 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-19', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-21', 120)), N'CHECKED_OUT', NULL),
        (N'BK0029', N'CUS029', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-13 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-20', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-22', 120)), N'CHECKED_OUT', NULL),
        (N'BK0030', N'CUS030', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-14 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-21', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-23', 120)), N'CHECKED_OUT', NULL),
        (N'BK0031', N'CUS031', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-23 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-24', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-26', 120)), N'CONFIRMED', NULL),
        (N'BK0032', N'CUS032', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-20 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-26', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-28', 120)), N'CONFIRMED', NULL),
        (N'BK0033', N'CUS007', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-21 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-27', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-29', 120)), N'CONFIRMED', NULL),
        (N'BK0034', N'CUS034', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-22 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-29', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-01', 120)), N'CHECKED_IN', NULL),
        (N'BK0035', N'CUS035', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-23 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-02', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-04', 120)), N'CONFIRMED', NULL),
        (N'BK0036', N'CUS036', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-20 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-05', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-08', 120)), N'CONFIRMED', NULL),
        (N'BK0037', N'CUS037', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-21 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-08', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-10', 120)), N'CONFIRMED', NULL),
        (N'BK0038', N'CUS038', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-22 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-11', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-13', 120)), N'CANCELLED', N'Khách thay đổi lịch trình và hủy đặt phòng.'),
        (N'BK0039', N'CUS038', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-23 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-14', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-16', 120)), N'CONFIRMED', NULL),
        (N'BK0040', N'CUS040', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-20 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-18', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-21', 120)), N'CONFIRMED', NULL);

    SET IDENTITY_INSERT dbo.BOOKING_DETAIL ON;
    INSERT INTO dbo.BOOKING_DETAIL (BookingDetailID, BookingID, RoomTypeID, Quantity, UnitPrice)
    VALUES
        (1, N'BK0001', N'RT01', 1, 500000),
        (2, N'BK0002', N'RT02', 1, 800000),
        (3, N'BK0003', N'RT03', 1, 1200000),
        (4, N'BK0004', N'RT01', 1, 500000),
        (5, N'BK0004', N'RT02', 1, 800000),
        (6, N'BK0005', N'RT01', 1, 500000),
        (7, N'BK0006', N'RT02', 2, 800000),
        (8, N'BK0007', N'RT04', 1, 1600000),
        (9, N'BK0008', N'RT02', 1, 800000),
        (10, N'BK0009', N'RT01', 1, 500000),
        (11, N'BK0010', N'RT03', 1, 1200000),
        (12, N'BK0011', N'RT01', 1, 500000),
        (13, N'BK0012', N'RT02', 1, 800000),
        (14, N'BK0013', N'RT03', 1, 1200000),
        (15, N'BK0014', N'RT04', 1, 1600000),
        (16, N'BK0015', N'RT01', 1, 500000),
        (17, N'BK0016', N'RT02', 1, 800000),
        (18, N'BK0017', N'RT03', 1, 1200000),
        (19, N'BK0018', N'RT04', 1, 1600000),
        (20, N'BK0019', N'RT01', 1, 500000),
        (21, N'BK0020', N'RT02', 1, 800000),
        (22, N'BK0021', N'RT03', 1, 1200000),
        (23, N'BK0022', N'RT04', 1, 1600000),
        (24, N'BK0023', N'RT01', 1, 500000),
        (25, N'BK0024', N'RT02', 1, 800000),
        (26, N'BK0025', N'RT03', 1, 1200000),
        (27, N'BK0026', N'RT04', 1, 1600000),
        (28, N'BK0027', N'RT01', 1, 500000),
        (29, N'BK0028', N'RT02', 1, 800000),
        (30, N'BK0029', N'RT03', 1, 1200000),
        (31, N'BK0030', N'RT04', 1, 1600000),
        (32, N'BK0031', N'RT01', 1, 500000),
        (33, N'BK0032', N'RT02', 1, 800000),
        (34, N'BK0033', N'RT03', 1, 1200000),
        (35, N'BK0034', N'RT04', 1, 1600000),
        (36, N'BK0035', N'RT02', 1, 800000),
        (37, N'BK0036', N'RT01', 1, 500000),
        (38, N'BK0037', N'RT03', 1, 1200000),
        (39, N'BK0038', N'RT04', 1, 1600000),
        (40, N'BK0039', N'RT01', 1, 500000),
        (41, N'BK0040', N'RT02', 1, 800000);

    SET IDENTITY_INSERT dbo.BOOKING_DETAIL OFF;

    INSERT INTO dbo.ROOM_ASSIGNMENT (BookingDetailID, RoomID, EmployeeID, AssignedAt)
    VALUES
        (1, N'R01', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-04 15:00:00', 120))),
        (2, N'R04', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-07 16:00:00', 120))),
        (3, N'R08', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-17 09:00:00', 120))),
        (9, N'R05', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-27 10:00:00', 120))),
        (12, N'R01', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-02 15:00:00', 120))),
        (13, N'R04', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-03 15:00:00', 120))),
        (14, N'R08', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-08 15:00:00', 120))),
        (15, N'R10', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-11 15:00:00', 120))),
        (16, N'R02', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-16 15:00:00', 120))),
        (17, N'R05', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-17 15:00:00', 120))),
        (18, N'R09', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-22 15:00:00', 120))),
        (19, N'R10', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-25 15:00:00', 120))),
        (20, N'R01', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-29 15:00:00', 120))),
        (21, N'R04', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-30 15:00:00', 120))),
        (22, N'R08', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-03 15:00:00', 120))),
        (23, N'R10', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-05 15:00:00', 120))),
        (24, N'R02', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-08 15:00:00', 120))),
        (25, N'R05', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-10 15:00:00', 120))),
        (26, N'R09', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-12 15:00:00', 120))),
        (27, N'R10', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-14 15:00:00', 120))),
        (28, N'R01', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-16 15:00:00', 120))),
        (29, N'R04', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-18 15:00:00', 120))),
        (30, N'R08', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-19 15:00:00', 120))),
        (31, N'R10', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-20 15:00:00', 120))),
        (33, N'R06', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-25 15:00:00', 120))),
        (35, N'R10', N'EMP003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-28 15:00:00', 120)));

    INSERT INTO dbo.BOOKING_SERVICE (BookingID, ServiceID, Quantity, UnitPrice, OrderedByEmployee)
    VALUES
        (N'BK0001', N'S01', 1, 75000, N'EMP003'),
        (N'BK0002', N'S02', 2, 250000, N'EMP003'),
        (N'BK0003', N'S05', 1, 100000, N'EMP003'),
        (N'BK0004', N'S07', 1, 350000, N'EMP003'),
        (N'BK0006', N'S06', 1, 150000, N'EMP003'),
        (N'BK0008', N'S02', 2, 250000, N'EMP003'),
        (N'BK0009', N'S10', 1, 50000, N'EMP003'),
        (N'BK0011', N'S01', 1, 75000, N'EMP003'),
        (N'BK0013', N'S04', 1, 450000, N'EMP003'),
        (N'BK0015', N'S03', 1, 150000, N'EMP003'),
        (N'BK0017', N'S02', 2, 200000, N'EMP003'),
        (N'BK0019', N'S10', 1, 50000, N'EMP003'),
        (N'BK0021', N'S07', 1, 350000, N'EMP003'),
        (N'BK0023', N'S09', 2, 350000, N'EMP003'),
        (N'BK0025', N'S04', 1, 450000, N'EMP003'),
        (N'BK0027', N'S01', 2, 75000, N'EMP003'),
        (N'BK0029', N'S03', 1, 150000, N'EMP003'),
        (N'BK0031', N'S07', 1, 350000, N'EMP003'),
        (N'BK0032', N'S02', 1, 200000, N'EMP003'),
        (N'BK0034', N'S09', 1, 350000, N'EMP003'),
        (N'BK0035', N'S06', 1, 150000, N'EMP003'),
        (N'BK0039', N'S10', 1, 50000, N'EMP003'),
        (N'BK0040', N'S04', 1, 450000, N'EMP003');

    INSERT INTO dbo.PAYMENT (PaymentID, BookingID, PaymentTime, Amount, MethodID, Note, CollectedByEmployee)
    VALUES
        (N'PM0001', N'BK0001', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-01 09:05:00', 120)), 1075000, N'PT03', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0002', N'BK0002', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-03 10:20:00', 120)), 2100000, N'PT03', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0003', N'BK0003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-10 08:35:00', 120)), 750000, N'PT01', N'Thanh toán đặt phòng.', N'EMP003'),
        (N'PM0004', N'BK0004', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-11 14:05:00', 120)), 885000, N'PT01', N'Thanh toán đặt phòng.', N'EMP003'),
        (N'PM0006', N'BK0006', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-13 11:50:00', 120)), 3350000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0008', N'BK0008', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-15 13:35:00', 120)), 2900000, N'PT03', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0009', N'BK0009', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-16 15:05:00', 120)), 1050000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0011', N'BK0011', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-07-27 09:05:00', 120)), 1075000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0012', N'BK0012', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-07-28 09:05:00', 120)), 2400000, N'PT03', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0013', N'BK0013', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-02 09:05:00', 120)), 2850000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0014', N'BK0014', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-05 09:05:00', 120)), 3200000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0015', N'BK0015', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-10 09:05:00', 120)), 1650000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0016', N'BK0016', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-11 09:05:00', 120)), 2400000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0017', N'BK0017', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-16 09:05:00', 120)), 2800000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0018', N'BK0018', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-19 09:05:00', 120)), 4800000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0019', N'BK0019', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-23 09:05:00', 120)), 1050000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0020', N'BK0020', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-24 09:05:00', 120)), 2400000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0021', N'BK0021', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-28 09:05:00', 120)), 2750000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0022', N'BK0022', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-30 09:05:00', 120)), 3200000, N'PT03', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0023', N'BK0023', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-02 09:05:00', 120)), 1700000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0024', N'BK0024', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-04 09:05:00', 120)), 1600000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0025', N'BK0025', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-06 09:05:00', 120)), 4050000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0026', N'BK0026', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-08 09:05:00', 120)), 3200000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0027', N'BK0027', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-10 09:05:00', 120)), 1150000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0028', N'BK0028', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-12 09:05:00', 120)), 1600000, N'PT03', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0029', N'BK0029', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-13 09:05:00', 120)), 2550000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0030', N'BK0030', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-14 09:05:00', 120)), 3200000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0031', N'BK0031', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-23 09:05:00', 120)), 405000, N'PT03', N'Thanh toán đặt phòng.', N'EMP003'),
        (N'PM0032', N'BK0032', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-20 09:05:00', 120)), 1800000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0033', N'BK0033', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-21 09:05:00', 120)), 2400000, N'PT02', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0034', N'BK0034', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-22 09:05:00', 120)), 3550000, N'PT03', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0035', N'BK0035', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-23 09:05:00', 120)), 1750000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0039', N'BK0039', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-23 09:05:00', 120)), 1050000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0040', N'BK0040', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-20 09:05:00', 120)), 2850000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.', N'EMP003'),
        (N'PM0041', N'BK0004', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-12 10:00:00', 120)), 2065000, N'PT01', N'Thanh toán phần còn lại của đặt phòng.', N'EMP003');

    INSERT INTO dbo.INVOICE (InvoiceID, BookingID, InvoiceDate, TotalAmount, EmployeeID)
    VALUES
        (N'HD0007', N'BK0001', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-07 11:00:00', 120)), 1075000, N'EMP003'),
        (N'HD0002', N'BK0002', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-10 11:00:00', 120)), 2100000, N'EMP003'),
        (N'HD0011', N'BK0011', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-05 11:00:00', 120)), 1075000, N'EMP003'),
        (N'HD0012', N'BK0012', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-07 11:00:00', 120)), 2400000, N'EMP003'),
        (N'HD0013', N'BK0013', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-11 11:00:00', 120)), 2850000, N'EMP003'),
        (N'HD0014', N'BK0014', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-14 11:00:00', 120)), 3200000, N'EMP003'),
        (N'HD0015', N'BK0015', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-20 11:00:00', 120)), 1650000, N'EMP003'),
        (N'HD0016', N'BK0016', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-21 11:00:00', 120)), 2400000, N'EMP003'),
        (N'HD0017', N'BK0017', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-25 11:00:00', 120)), 2800000, N'EMP003'),
        (N'HD0018', N'BK0018', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-29 11:00:00', 120)), 4800000, N'EMP003'),
        (N'HD0019', N'BK0019', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-01 11:00:00', 120)), 1050000, N'EMP003'),
        (N'HD0020', N'BK0020', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-03 11:00:00', 120)), 2400000, N'EMP003'),
        (N'HD0021', N'BK0021', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-06 11:00:00', 120)), 2750000, N'EMP003'),
        (N'HD0022', N'BK0022', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-08 11:00:00', 120)), 3200000, N'EMP003'),
        (N'HD0023', N'BK0023', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-11 11:00:00', 120)), 1700000, N'EMP003'),
        (N'HD0024', N'BK0024', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-13 11:00:00', 120)), 1600000, N'EMP003'),
        (N'HD0025', N'BK0025', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-16 11:00:00', 120)), 4050000, N'EMP003'),
        (N'HD0026', N'BK0026', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-17 11:00:00', 120)), 3200000, N'EMP003'),
        (N'HD0027', N'BK0027', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-19 11:00:00', 120)), 1150000, N'EMP003'),
        (N'HD0028', N'BK0028', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-21 11:00:00', 120)), 1600000, N'EMP003'),
        (N'HD0029', N'BK0029', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-22 11:00:00', 120)), 2550000, N'EMP003'),
        (N'HD0030', N'BK0030', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-23 11:00:00', 120)), 3200000, N'EMP003');

    INSERT INTO dbo.COMPLAINT (CustomerID, Title, Content, Status, ReplyMessage, CreatedAt, EmployeeID, ResolvedByEmployee)
    VALUES
        (N'CUS003', N'Yêu cầu hỗ trợ đặt phòng', N'Tôi muốn kiểm tra lại thông tin phòng đã được sắp xếp cho booking của mình.', N'Pending', NULL, DATEADD(HOUR, 10, DATEADD(DAY, -11, CONVERT(DATETIME, @ReferenceDate))), NULL, NULL),
        (N'CUS008', N'Bổ sung khăn tắm', N'Gia đình tôi cần thêm hai khăn tắm cho phòng 202.', N'Pending', N'Lễ tân đã chuyển yêu cầu đến bộ phận buồng phòng.', DATEADD(HOUR, 10, DATEADD(DAY, 0, CONVERT(DATETIME, @ReferenceDate))), N'EMP003', NULL),
        (N'CUS034', N'Điều chỉnh nhiệt độ điều hòa', N'Điều hòa trong phòng 401 chưa đủ mát vào buổi chiều.', N'Resolved', N'Kỹ thuật đã vệ sinh bộ lọc và kiểm tra nhiệt độ; khách xác nhận phòng đã mát.', DATEADD(HOUR, 10, DATEADD(DAY, 0, CONVERT(DATETIME, @ReferenceDate))), N'EMP003', N'EMP002'),
        (N'CUS001', N'Kiểm tra phí giặt ủi', N'Tôi muốn nhận bảng kê chi tiết số lượng quần áo đã giặt.', N'Resolved', N'Đã gửi bảng kê và giải thích đơn giá giặt ủi cho khách.', DATEADD(HOUR, 10, DATEADD(DAY, -22, CONVERT(DATETIME, @ReferenceDate))), N'EMP003', N'EMP002'),
        (N'CUS040', N'Xác nhận giờ sử dụng spa', N'Tôi muốn đặt lịch spa lúc 16 giờ trong ngày nhận phòng.', N'Pending', NULL, DATEADD(HOUR, 10, DATEADD(DAY, 0, CONVERT(DATETIME, @ReferenceDate))), NULL, NULL),
        (N'CUS009', N'Hỗ trợ gửi hành lý', N'Tôi dự kiến đến sớm và muốn gửi hành lý trước giờ nhận phòng.', N'Pending', N'Lễ tân đã xác nhận có thể giữ hành lý trước giờ nhận phòng.', DATEADD(HOUR, 10, DATEADD(DAY, 0, CONVERT(DATETIME, @ReferenceDate))), N'EMP003', NULL);

    -- Kiểm tra lịch phân phòng, số phòng còn lại và tổng tiền của dữ liệu mẫu.
    IF EXISTS (SELECT 1 FROM dbo.ROOM_ASSIGNMENT AS a
        JOIN dbo.BOOKING_DETAIL AS d ON d.BookingDetailID = a.BookingDetailID
        JOIN dbo.ROOM AS r ON r.RoomID = a.RoomID
        WHERE r.RoomTypeID <> d.RoomTypeID OR r.Status <> 'ACTIVE')
        THROW 52113, N'Phòng được phân không hoạt động hoặc không đúng loại phòng.', 1;
    IF EXISTS (SELECT 1 FROM dbo.ROOM_ASSIGNMENT AS a
        JOIN dbo.BOOKING_DETAIL AS d ON d.BookingDetailID = a.BookingDetailID
        JOIN dbo.BOOKING AS b ON b.BookingID = d.BookingID
        JOIN dbo.ROOM_ASSIGNMENT AS a2 ON a2.RoomID = a.RoomID AND a2.AssignmentID <> a.AssignmentID
        JOIN dbo.BOOKING_DETAIL AS d2 ON d2.BookingDetailID = a2.BookingDetailID
        JOIN dbo.BOOKING AS b2 ON b2.BookingID = d2.BookingID
        WHERE b.IsDeleted = 0 AND b2.IsDeleted = 0
          AND b.BookingStatus <> 'CANCELLED' AND b2.BookingStatus <> 'CANCELLED'
          AND b.CheckInDate < CASE WHEN b2.BookingStatus = 'CHECKED_IN' AND b2.CheckOutDate <= @Today THEN CONVERT(DATE, '9999-12-31') ELSE b2.CheckOutDate END
          AND b2.CheckInDate < CASE WHEN b.BookingStatus = 'CHECKED_IN' AND b.CheckOutDate <= @Today THEN CONVERT(DATE, '9999-12-31') ELSE b.CheckOutDate END)
        THROW 52114, N'Lịch phân phòng bị trùng với lịch đã tồn tại. Không ghi dữ liệu.', 1;

    DECLARE @Demand TABLE (RoomTypeID CHAR(4), StartDate DATE, EndDate DATE, Quantity INT);
    INSERT INTO @Demand
    SELECT d.RoomTypeID,
        CASE WHEN b.BookingStatus = 'CHECKED_IN' OR b.CheckInDate < @Today THEN @Today ELSE b.CheckInDate END,
        CASE WHEN b.BookingStatus = 'CHECKED_IN' AND b.CheckOutDate <= @Today THEN CONVERT(DATE, '9999-12-31') ELSE b.CheckOutDate END,
        d.Quantity
    FROM dbo.BOOKING_DETAIL AS d JOIN dbo.BOOKING AS b ON b.BookingID = d.BookingID
    WHERE b.IsDeleted = 0 AND b.BookingStatus IN ('PENDING', 'CONFIRMED', 'CHECKED_IN')
      AND (b.CheckOutDate > @Today OR b.BookingStatus = 'CHECKED_IN');
    IF EXISTS (SELECT 1 FROM @Demand AS d
        CROSS APPLY (SELECT SUM(x.Quantity) AS Reserved FROM @Demand AS x
                     WHERE x.RoomTypeID = d.RoomTypeID AND x.StartDate <= d.StartDate AND x.EndDate > d.StartDate) AS used
        CROSS APPLY (SELECT COUNT(*) AS ActiveRooms FROM dbo.ROOM AS r
                     WHERE r.RoomTypeID = d.RoomTypeID AND r.Status = 'ACTIVE') AS stock
        WHERE used.Reserved > stock.ActiveRooms)
        THROW 52117, N'Số phòng đặt vượt số phòng đang hoạt động trong cùng khoảng ngày.', 1;

    DECLARE @Totals TABLE (BookingID CHAR(6) PRIMARY KEY, TotalAmount DECIMAL(12,2));
    INSERT INTO @Totals
    SELECT b.BookingID,
        ISNULL((SELECT SUM(CONVERT(DECIMAL(18,2), d.Quantity) * d.UnitPrice)
                FROM dbo.BOOKING_DETAIL AS d WHERE d.BookingID = b.BookingID), 0)
          * DATEDIFF(DAY, b.CheckInDate, b.CheckOutDate)
        + ISNULL((SELECT SUM(CONVERT(DECIMAL(18,2), s.Quantity) * s.UnitPrice)
                  FROM dbo.BOOKING_SERVICE AS s WHERE s.BookingID = b.BookingID), 0)
    FROM dbo.BOOKING AS b;
    IF EXISTS (SELECT 1 FROM @Totals AS t
        CROSS APPLY (SELECT ISNULL(SUM(p.Amount), 0) AS Paid FROM dbo.PAYMENT AS p WHERE p.BookingID = t.BookingID) AS p
        WHERE p.Paid > t.TotalAmount)
        THROW 52115, N'Tổng thanh toán vượt chi phí phòng và dịch vụ.', 1;
    IF EXISTS (SELECT 1 FROM dbo.INVOICE AS i JOIN @Totals AS t ON t.BookingID = i.BookingID
        WHERE i.TotalAmount <> t.TotalAmount)
        THROW 52116, N'Tổng hóa đơn không khớp giá lịch sử của phòng và dịch vụ.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    SET NOEXEC ON;
    THROW;
END CATCH;
GO

-- =============================================================
-- PHẦN 3. VIEW, PROCEDURE VÀ TRIGGER CHO ỨNG DỤNG WPF
-- =============================================================
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
       rt.TypeName, rt.Capacity, rt.Price, rt.Description,
       r.RoomImage, rt.RoomTypeImage
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
            WHERE e.EmployeeID = @EmployeeID AND u.Role = N'Staff'
        )
            THROW 51001, N'Chỉ Staff được xóa booking quá hạn check-in.', 1;

        DECLARE @Status VARCHAR(30), @CheckInDate DATE, @IsDeleted BIT;

        SELECT @Status = BookingStatus, @CheckInDate = CheckInDate,
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

        IF CONVERT(DATE, @Now) <= @CheckInDate
            THROW 51005, N'Booking chưa quá hạn check-in.', 1;

        UPDATE dbo.BOOKING
        SET BookingStatus = 'CANCELLED',
            CancellationReason = N'Quá hạn check-in (no-show).',
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
            WHERE e.EmployeeID = @EmployeeID AND u.Role = N'Manager'
        )
            THROW 51011, N'Chỉ Manager được xóa loại phòng.', 1;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.ROOM_TYPE WITH (XLOCK, HOLDLOCK)
            WHERE RoomTypeID = @RoomTypeID
        )
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

-- =============================================================
-- PHẦN 4. KIỂM TRA DỮ LIỆU SAU KHI TẠO
-- =============================================================
SELECT N'NATIONALITY' AS TableName, COUNT(*) AS TotalRows FROM dbo.NATIONALITY
UNION ALL SELECT N'CUSTOMER', COUNT(*) FROM dbo.CUSTOMER
UNION ALL SELECT N'ROOM_TYPE', COUNT(*) FROM dbo.ROOM_TYPE
UNION ALL SELECT N'ROOM', COUNT(*) FROM dbo.ROOM
UNION ALL SELECT N'BOOKING', COUNT(*) FROM dbo.BOOKING;

SELECT NationalityID, NationalityName FROM dbo.NATIONALITY ORDER BY NationalityID;
SELECT RoomTypeID, TypeName, Capacity, Price FROM dbo.ROOM_TYPE ORDER BY RoomTypeID;
SELECT BookingID, CustomerID, CheckInDate, CheckOutDate, BookingStatus
FROM dbo.BOOKING ORDER BY BookingID;

SELECT c.CustomerID, c.FullName, c.IdentityNumber, c.NationalityID, n.NationalityName
FROM dbo.CUSTOMER AS c LEFT JOIN dbo.NATIONALITY AS n ON n.NationalityID = c.NationalityID
ORDER BY c.CustomerID;
GO
SET NOEXEC OFF;
GO
