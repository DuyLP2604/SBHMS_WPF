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

IF @@TRANCOUNT <> 0
    THROW 52100, N'Hãy chạy file trong cửa sổ query không có transaction đang mở.', 1;

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @LockResult INT;
    EXEC @LockResult = sys.sp_getapplock
        @Resource = N'SBHMS_HotelData', @LockMode = N'Exclusive',
        @LockOwner = N'Transaction', @LockTimeout = 10000;
    IF @LockResult < 0 THROW 52101, N'Không lấy được khóa cập nhật dữ liệu.', 1;

    DECLARE @Today DATE = CONVERT(DATE, GETDATE());
    DECLARE @DayShift INT = DATEDIFF(DAY, CONVERT(DATE, '2026-09-30'), @Today);
    IF EXISTS (SELECT 1 FROM dbo.BOOKING WHERE BookingID = 'BK0001' AND CustomerID = 'CUS001')
        SELECT @DayShift = DATEDIFF(DAY, CONVERT(DATE, '2026-09-05'), CheckInDate)
        FROM dbo.BOOKING WHERE BookingID = 'BK0001';
    DECLARE @ReferenceDate DATE = DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-30'));
    DECLARE @Added TABLE (TableName SYSNAME, RowsAdded INT);
    DECLARE @LabelsChanged INT = 0;
    DECLARE @NewBookings TABLE (BookingID CHAR(6) PRIMARY KEY);
    DECLARE @Actor CHAR(6), @Resolver CHAR(6);
    SELECT TOP (1) @Actor = e.EmployeeID FROM dbo.EMPLOYEE AS e
    JOIN dbo.USERS AS u ON u.UserID = e.UserID
    WHERE u.Role = N'Staff' ORDER BY CASE WHEN u.Username = 'staff' THEN 0 ELSE 1 END, e.EmployeeID;
    SELECT TOP (1) @Resolver = e.EmployeeID FROM dbo.EMPLOYEE AS e
    JOIN dbo.USERS AS u ON u.UserID = e.UserID
    WHERE u.Role IN (N'Manager', N'Staff')
    ORDER BY CASE WHEN u.Role = N'Manager' THEN 0 ELSE 1 END, e.EmployeeID;
    IF @Actor IS NULL OR @Resolver IS NULL
        THROW 52102, N'Cần hồ sơ nhân viên Staff. Hãy chạy ứng dụng trước để tạo tài khoản và hồ sơ nhân viên.', 1;

    DECLARE @Customers TABLE (CustomerID CHAR(6) PRIMARY KEY, FullName NVARCHAR(100), IdentityNumber VARCHAR(20), Phone VARCHAR(15), Email VARCHAR(100), Address NVARCHAR(200), NationalityName NVARCHAR(50));
    INSERT INTO @Customers (CustomerID, FullName, IdentityNumber, Phone, Email, Address, NationalityName)
    VALUES
        (N'CUS001', N'Phạm Minh Tuấn', NULL, N'0903456789', N'tuan.pham@gmail.com', N'Quận 1, TP.HCM', N'Vietnam'),
        (N'CUS002', N'Nguyễn Tuyết Mai', NULL, N'0912888999', N'maituyet92@yahoo.com', N'Quận Cầu Giấy, Hà Nội', N'Vietnam'),
        (N'CUS003', N'Lê Hoàng Nam', NULL, N'0987111222', N'namlh@gmail.com', N'Quận 7, TP.HCM', N'Vietnam'),
        (N'CUS004', N'Trần Thu Hà', NULL, N'0356123456', N'hatran@fpt.com.vn', N'Quận Tây Hồ, Hà Nội', N'Vietnam'),
        (N'CUS005', N'Robert Harrison', NULL, N'0775123456', N'robert.h@outlook.com', N'London, United Kingdom', N'United Kingdom'),
        (N'CUS006', N'Chen Wei', NULL, N'0933444555', N'chenwei88@qq.com', N'Beijing, China', N'China'),
        (N'CUS007', N'Hans Müller', NULL, N'0888777666', N'hans.m@gmail.de', N'Berlin, Germany', N'Germany'),
        (N'CUS008', N'Kim Ji-won', NULL, N'0944555666', N'jiwon.kim@naver.com', N'Seoul, South Korea', N'South Korea'),
        (N'CUS009', N'Jean Dupont', NULL, N'0766112233', N'jean.dupont@france.fr', N'Paris, France', N'France'),
        (N'CUS010', N'Đặng Phương Thảo', NULL, N'0399123456', N'thaophuong@gmail.com', N'Quận Hải Châu, Đà Nẵng', N'Vietnam'),
        (N'CUS011', N'Vũ Đình Tùng', NULL, N'0988123123', N'tung.vu@gmail.com', N'Quận 3, TP.HCM', N'Vietnam'),
        (N'CUS012', N'Lê Thị Thu Hương', NULL, N'0977234234', N'huongle99@yahoo.com', N'Quận Đống Đa, Hà Nội', N'Vietnam'),
        (N'CUS013', N'Nguyễn Quang Hải', NULL, N'0912345345', N'hai.nq@fpt.edu.vn', N'Ninh Kiều, Cần Thơ', N'Vietnam'),
        (N'CUS014', N'Phan Thanh Bình', NULL, N'0933456456', N'binhpt@gmail.com', N'Quận 1, TP.HCM', N'Vietnam'),
        (N'CUS015', N'Đỗ Mỹ Linh', NULL, N'0944567567', N'mylinh.do@gmail.com', N'Quận Hoàn Kiếm, Hà Nội', N'Vietnam'),
        (N'CUS016', N'Bùi Tiến Dũng', NULL, N'0966678678', N'dung.bui@viettel.com.vn', N'Thạch Thất, Hà Nội', N'Vietnam'),
        (N'CUS017', N'Trần Phương Anh', NULL, N'0888789789', N'phuonganh.t@yahoo.com', N'Quận Sơn Trà, Đà Nẵng', N'Vietnam'),
        (N'CUS018', N'Lý Nhã Kỳ', NULL, N'0901890890', N'nhaky.ly@gmail.com', N'Quận 7, TP.HCM', N'Vietnam'),
        (N'CUS019', N'Hoàng Lệ Thu', NULL, N'0922901901', N'thuhl@outlook.com', N'Biên Hòa, Đồng Nai', N'Vietnam'),
        (N'CUS020', N'Ngô Thanh Vân', NULL, N'0933012012', N'van.ngo@gmail.com', N'Quận Phú Nhuận, TP.HCM', N'Vietnam'),
        (N'CUS021', N'Võ Hoàng Yến', NULL, N'0944123123', N'hoangyen.vo@gmail.com', N'Quận 4, TP.HCM', N'Vietnam'),
        (N'CUS022', N'Đinh Ngọc Diệp', NULL, N'0955234234', N'ngocdiep.dinh@yahoo.com', N'Quận Bình Thạnh, TP.HCM', N'Vietnam'),
        (N'CUS023', N'Lương Xuân Trường', NULL, N'0966345345', N'truong.lx@hagl.com.vn', N'Pleiku, Gia Lai', N'Vietnam'),
        (N'CUS024', N'Cao Thái Sơn', NULL, N'0977456456', N'soncao@gmail.com', N'Quận 10, TP.HCM', N'Vietnam'),
        (N'CUS025', N'Hồ Ngọc Hà', NULL, N'0988567567', N'hongocha@gmail.com', N'Quận 2, TP.HCM', N'Vietnam'),
        (N'CUS026', N'Mai Phương Thúy', NULL, N'0999678678', N'thuymp@gmail.com', N'Quận Cầu Giấy, Hà Nội', N'Vietnam'),
        (N'CUS027', N'Dương Trương Thiên Lý', NULL, N'0901789789', N'thienly.dt@gmail.com', N'Đồng Tháp', N'Vietnam'),
        (N'CUS028', N'Tăng Thanh Hà', NULL, N'0912890890', N'hathanh.tang@gmail.com', N'Quận 2, TP.HCM', N'Vietnam'),
        (N'CUS029', N'Phạm Hương', NULL, N'0923901901', N'huong.pham@gmail.com', N'Hải Phòng', N'Vietnam'),
        (N'CUS030', N'Nguyễn Thúc Thùy Tiên', NULL, N'0934012012', N'thuytien.nt@gmail.com', N'Quận Gò Vấp, TP.HCM', N'Vietnam'),
        (N'CUS031', N'Lê Quốc Bảo', NULL, N'0945123123', N'baolq@gmail.com', N'Quận Tân Bình, TP.HCM', N'Vietnam'),
        (N'CUS032', N'Trần Kim Chi', NULL, N'0956234234', N'kimchi.tran@yahoo.com', N'Quận Thanh Xuân, Hà Nội', N'Vietnam'),
        (N'CUS033', N'Phan Đình Phùng', NULL, N'0967345345', N'phungpd@gmail.com', N'Vinh, Nghệ An', N'Vietnam'),
        (N'CUS034', N'Vũ Cát Tường', NULL, N'0978456456', N'cattuong.vu@gmail.com', N'Long Xuyên, An Giang', N'Vietnam'),
        (N'CUS035', N'Đỗ Trọng Hiếu', NULL, N'0989567567', N'hieudo@gmail.com', N'Quận Hai Bà Trưng, Hà Nội', N'Vietnam'),
        (N'CUS036', N'Bùi Anh Tuấn', NULL, N'0990678678', N'anhtuan.bui@gmail.com', N'Quận 1, TP.HCM', N'Vietnam'),
        (N'CUS037', N'Lê Minh Sơn', NULL, N'0902789789', N'sonlm@gmail.com', N'Bắc Ninh', N'Vietnam'),
        (N'CUS038', N'Hoàng Thùy Linh', NULL, N'0913890890', N'thuylinh.hoang@gmail.com', N'Quận Ba Đình, Hà Nội', N'Vietnam'),
        (N'CUS039', N'Nguyễn Trần Trung Quân', NULL, N'0924901901', N'trungquan.nt@gmail.com', N'Quận Đống Đa, Hà Nội', N'Vietnam'),
        (N'CUS040', N'Phạm Tiến Dũng', NULL, N'0935012012', N'dungpt@gmail.com', N'Nha Trang, Khánh Hòa', N'Vietnam'),
        (N'CUS041', N'Emily Johnson', NULL, N'+12025550141', N'emily.j@yahoo.com', N'Los Angeles, USA', N'United States'),
        (N'CUS042', N'Michael Brown', NULL, N'+13125550142', N'michael.b@outlook.com', N'Chicago, USA', N'United States'),
        (N'CUS043', N'Li Na', NULL, N'+8613812340043', N'lina.beijing@qq.com', N'Shanghai, China', N'China'),
        (N'CUS044', N'Wang Lei', NULL, N'+8613912340044', N'wanglei88@wechat.com', N'Guangzhou, China', N'China'),
        (N'CUS045', N'Park Min-young', NULL, N'+821012340045', N'minyoung.park@naver.com', N'Busan, South Korea', N'South Korea'),
        (N'CUS046', N'Lee Jong-suk', NULL, N'+821012340046', N'jongsuk.lee@daum.net', N'Incheon, South Korea', N'South Korea'),
        (N'CUS047', N'Sato Kenji', NULL, N'+819012340047', N'kenji.sato@yahoo.co.jp', N'Tokyo, Japan', N'Japan'),
        (N'CUS048', N'Tanaka Yuki', NULL, N'+819012340048', N'yuki.tanaka@gmail.com', N'Osaka, Japan', N'Japan'),
        (N'CUS049', N'Anna Kowalski', NULL, N'+48500123449', N'anna.k@gmail.com', N'Warsaw, Poland', N'Other'),
        (N'CUS050', N'David Silva', NULL, N'+34600123450', N'david.silva@hotmail.com', N'Madrid, Spain', N'Spain');

    DECLARE @RoomTypes TABLE (RoomTypeID CHAR(4) PRIMARY KEY, TypeName NVARCHAR(50), Capacity INT, Price DECIMAL(12,2), RoomTypeImage NVARCHAR(255), Description NVARCHAR(500));
    INSERT INTO @RoomTypes (RoomTypeID, TypeName, Capacity, Price, RoomTypeImage, Description)
    VALUES
        (N'RT01', N'Phòng đơn', 2, 500000, N'room-type-single.jpg', N'1 giường đôi, tối đa 2 khách'),
        (N'RT02', N'Phòng đôi', 4, 800000, N'room-type-double.jpg', N'2 giường đôi, tối đa 4 khách'),
        (N'RT03', N'Phòng Luxury đơn', 2, 1200000, N'room-type-luxury-single.jpg', N'Phòng cao cấp 1 giường đôi, tối đa 2 khách'),
        (N'RT04', N'Phòng Luxury đôi', 4, 1600000, N'room-type-luxury-double.jpg', N'Phòng cao cấp 2 giường đôi, tối đa 4 khách');

    DECLARE @Rooms TABLE (RoomID CHAR(3) PRIMARY KEY, RoomNumber NVARCHAR(20), RoomImage NVARCHAR(100), Status VARCHAR(20), RoomTypeID CHAR(4));
    INSERT INTO @Rooms (RoomID, RoomNumber, RoomImage, Status, RoomTypeID)
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

    DECLARE @Services TABLE (ServiceID CHAR(4) PRIMARY KEY, ServiceName NVARCHAR(60), UnitPrice DECIMAL(12,2));
    INSERT INTO @Services (ServiceID, ServiceName, UnitPrice)
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

    DECLARE @Methods TABLE (MethodID CHAR(4) PRIMARY KEY, MethodName NVARCHAR(30));
    INSERT INTO @Methods (MethodID, MethodName)
    VALUES
        (N'PT01', N'VietQR'),
        (N'PT02', N'Ví nội bộ'),
        (N'PT03', N'Tiền mặt');

    DECLARE @Bookings TABLE (BookingID CHAR(6) PRIMARY KEY, CustomerID CHAR(6), BookingDate DATETIME, CheckInDate DATE, CheckOutDate DATE, BookingStatus VARCHAR(30), CancellationReason NVARCHAR(200));
    INSERT INTO @Bookings (BookingID, CustomerID, BookingDate, CheckInDate, CheckOutDate, BookingStatus, CancellationReason)
    VALUES
        (N'BK0001', N'CUS001', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-01 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-05', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-07', 120)), N'CHECKED_OUT', NULL),
        (N'BK0002', N'CUS002', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-03 10:15:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-08', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-10', 120)), N'CHECKED_OUT', NULL),
        (N'BK0003', N'CUS003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-10 08:30:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-18', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-20', 120)), N'CONFIRMED', NULL),
        (N'BK0004', N'CUS004', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-11 14:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-20', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-22', 120)), N'CONFIRMED', NULL),
        (N'BK0005', N'CUS005', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-12 16:10:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-21', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-23', 120)), N'PENDING', NULL),
        (N'BK0006', N'CUS006', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-13 11:45:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-24', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-26', 120)), N'CONFIRMED', NULL),
        (N'BK0007', N'CUS007', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-14 09:20:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-25', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-27', 120)), N'CANCELLED', N'Khách thay đổi lịch trình và hủy đặt phòng.'),
        (N'BK0008', N'CUS008', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-15 13:30:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-28', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-01', 120)), N'CHECKED_IN', NULL),
        (N'BK0009', N'CUS009', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-16 15:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-02', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-04', 120)), N'CONFIRMED', NULL),
        (N'BK0010', N'CUS010', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-17 06:30:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-05', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-07', 120)), N'PENDING', NULL),
        (N'BK0011', N'CUS011', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-07-27 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-03', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-05', 120)), N'CHECKED_OUT', NULL),
        (N'BK0012', N'CUS012', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-07-28 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-04', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-07', 120)), N'CHECKED_OUT', NULL),
        (N'BK0013', N'CUS013', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-02 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-09', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-11', 120)), N'CHECKED_OUT', NULL),
        (N'BK0014', N'CUS014', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-05 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-12', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-14', 120)), N'CHECKED_OUT', NULL),
        (N'BK0015', N'CUS015', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-10 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-17', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-20', 120)), N'CHECKED_OUT', NULL),
        (N'BK0016', N'CUS016', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-11 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-18', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-21', 120)), N'CHECKED_OUT', NULL),
        (N'BK0017', N'CUS017', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-16 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-23', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-25', 120)), N'CHECKED_OUT', NULL),
        (N'BK0018', N'CUS018', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-19 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-26', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-29', 120)), N'CHECKED_OUT', NULL),
        (N'BK0019', N'CUS019', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-23 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-30', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-01', 120)), N'CHECKED_OUT', NULL),
        (N'BK0020', N'CUS020', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-24 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-08-31', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-03', 120)), N'CHECKED_OUT', NULL),
        (N'BK0021', N'CUS021', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-28 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-04', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-06', 120)), N'CHECKED_OUT', NULL),
        (N'BK0022', N'CUS022', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-30 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-06', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-08', 120)), N'CHECKED_OUT', NULL),
        (N'BK0023', N'CUS023', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-02 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-09', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-11', 120)), N'CHECKED_OUT', NULL),
        (N'BK0024', N'CUS024', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-04 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-11', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-13', 120)), N'CHECKED_OUT', NULL),
        (N'BK0025', N'CUS025', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-06 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-13', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-16', 120)), N'CHECKED_OUT', NULL),
        (N'BK0026', N'CUS026', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-08 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-15', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-17', 120)), N'CHECKED_OUT', NULL),
        (N'BK0027', N'CUS027', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-10 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-17', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-19', 120)), N'CHECKED_OUT', NULL),
        (N'BK0028', N'CUS028', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-12 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-19', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-21', 120)), N'CHECKED_OUT', NULL),
        (N'BK0029', N'CUS029', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-13 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-20', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-22', 120)), N'CHECKED_OUT', NULL),
        (N'BK0030', N'CUS030', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-14 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-21', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-23', 120)), N'CHECKED_OUT', NULL),
        (N'BK0031', N'CUS031', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-23 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-24', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-26', 120)), N'CONFIRMED', NULL),
        (N'BK0032', N'CUS032', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-20 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-26', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-28', 120)), N'CONFIRMED', NULL),
        (N'BK0033', N'CUS007', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-21 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-27', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-29', 120)), N'CONFIRMED', NULL),
        (N'BK0034', N'CUS034', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-22 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-09-29', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-01', 120)), N'CHECKED_IN', NULL),
        (N'BK0035', N'CUS035', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-23 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-02', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-04', 120)), N'CONFIRMED', NULL),
        (N'BK0036', N'CUS036', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-20 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-05', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-08', 120)), N'PENDING', NULL),
        (N'BK0037', N'CUS037', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-21 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-08', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-10', 120)), N'PENDING', NULL),
        (N'BK0038', N'CUS038', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-22 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-11', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-13', 120)), N'CANCELLED', N'Khách thay đổi lịch trình và hủy đặt phòng.'),
        (N'BK0039', N'CUS038', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-23 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-14', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-16', 120)), N'CONFIRMED', NULL),
        (N'BK0040', N'CUS040', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-20 09:00:00', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-18', 120)), DATEADD(DAY, @DayShift, CONVERT(DATE, '2026-10-21', 120)), N'CONFIRMED', NULL);

    DECLARE @Details TABLE (BookingID CHAR(6), RoomTypeID CHAR(4), Quantity INT, UnitPrice DECIMAL(12,2), PRIMARY KEY (BookingID, RoomTypeID));
    INSERT INTO @Details (BookingID, RoomTypeID, Quantity, UnitPrice)
    VALUES
        (N'BK0001', N'RT01', 1, 500000),
        (N'BK0002', N'RT02', 1, 800000),
        (N'BK0003', N'RT03', 1, 1200000),
        (N'BK0004', N'RT01', 1, 500000),
        (N'BK0004', N'RT02', 1, 800000),
        (N'BK0005', N'RT01', 1, 500000),
        (N'BK0006', N'RT02', 2, 800000),
        (N'BK0007', N'RT04', 1, 1600000),
        (N'BK0008', N'RT02', 1, 800000),
        (N'BK0009', N'RT01', 1, 500000),
        (N'BK0010', N'RT03', 1, 1200000),
        (N'BK0011', N'RT01', 1, 500000),
        (N'BK0012', N'RT02', 1, 800000),
        (N'BK0013', N'RT03', 1, 1200000),
        (N'BK0014', N'RT04', 1, 1600000),
        (N'BK0015', N'RT01', 1, 500000),
        (N'BK0016', N'RT02', 1, 800000),
        (N'BK0017', N'RT03', 1, 1200000),
        (N'BK0018', N'RT04', 1, 1600000),
        (N'BK0019', N'RT01', 1, 500000),
        (N'BK0020', N'RT02', 1, 800000),
        (N'BK0021', N'RT03', 1, 1200000),
        (N'BK0022', N'RT04', 1, 1600000),
        (N'BK0023', N'RT01', 1, 500000),
        (N'BK0024', N'RT02', 1, 800000),
        (N'BK0025', N'RT03', 1, 1200000),
        (N'BK0026', N'RT04', 1, 1600000),
        (N'BK0027', N'RT01', 1, 500000),
        (N'BK0028', N'RT02', 1, 800000),
        (N'BK0029', N'RT03', 1, 1200000),
        (N'BK0030', N'RT04', 1, 1600000),
        (N'BK0031', N'RT01', 1, 500000),
        (N'BK0032', N'RT02', 1, 800000),
        (N'BK0033', N'RT03', 1, 1200000),
        (N'BK0034', N'RT04', 1, 1600000),
        (N'BK0035', N'RT02', 1, 800000),
        (N'BK0036', N'RT01', 1, 500000),
        (N'BK0037', N'RT03', 1, 1200000),
        (N'BK0038', N'RT04', 1, 1600000),
        (N'BK0039', N'RT01', 1, 500000),
        (N'BK0040', N'RT02', 1, 800000);

    DECLARE @Assignments TABLE (BookingID CHAR(6), RoomTypeID CHAR(4), RoomID CHAR(3), AssignedAt DATETIME, PRIMARY KEY (BookingID, RoomTypeID, RoomID));
    INSERT INTO @Assignments (BookingID, RoomTypeID, RoomID, AssignedAt)
    VALUES
        (N'BK0001', N'RT01', N'R01', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-04 15:00:00', 120))),
        (N'BK0002', N'RT02', N'R04', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-07 16:00:00', 120))),
        (N'BK0003', N'RT03', N'R08', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-17 09:00:00', 120))),
        (N'BK0008', N'RT02', N'R05', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-27 10:00:00', 120))),
        (N'BK0011', N'RT01', N'R01', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-02 15:00:00', 120))),
        (N'BK0012', N'RT02', N'R04', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-03 15:00:00', 120))),
        (N'BK0013', N'RT03', N'R08', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-08 15:00:00', 120))),
        (N'BK0014', N'RT04', N'R10', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-11 15:00:00', 120))),
        (N'BK0015', N'RT01', N'R02', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-16 15:00:00', 120))),
        (N'BK0016', N'RT02', N'R05', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-17 15:00:00', 120))),
        (N'BK0017', N'RT03', N'R09', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-22 15:00:00', 120))),
        (N'BK0018', N'RT04', N'R10', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-25 15:00:00', 120))),
        (N'BK0019', N'RT01', N'R01', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-29 15:00:00', 120))),
        (N'BK0020', N'RT02', N'R04', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-30 15:00:00', 120))),
        (N'BK0021', N'RT03', N'R08', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-03 15:00:00', 120))),
        (N'BK0022', N'RT04', N'R10', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-05 15:00:00', 120))),
        (N'BK0023', N'RT01', N'R02', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-08 15:00:00', 120))),
        (N'BK0024', N'RT02', N'R05', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-10 15:00:00', 120))),
        (N'BK0025', N'RT03', N'R09', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-12 15:00:00', 120))),
        (N'BK0026', N'RT04', N'R10', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-14 15:00:00', 120))),
        (N'BK0027', N'RT01', N'R01', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-16 15:00:00', 120))),
        (N'BK0028', N'RT02', N'R04', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-18 15:00:00', 120))),
        (N'BK0029', N'RT03', N'R08', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-19 15:00:00', 120))),
        (N'BK0030', N'RT04', N'R10', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-20 15:00:00', 120))),
        (N'BK0032', N'RT02', N'R06', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-25 15:00:00', 120))),
        (N'BK0034', N'RT04', N'R10', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-28 15:00:00', 120)));

    DECLARE @Orders TABLE (BookingID CHAR(6), ServiceID CHAR(4), Quantity INT, UnitPrice DECIMAL(12,2), PRIMARY KEY (BookingID, ServiceID));
    INSERT INTO @Orders (BookingID, ServiceID, Quantity, UnitPrice)
    VALUES
        (N'BK0001', N'S01', 1, 75000),
        (N'BK0002', N'S02', 2, 250000),
        (N'BK0003', N'S05', 1, 100000),
        (N'BK0004', N'S07', 1, 350000),
        (N'BK0006', N'S06', 1, 150000),
        (N'BK0008', N'S02', 2, 250000),
        (N'BK0009', N'S10', 1, 50000),
        (N'BK0011', N'S01', 1, 75000),
        (N'BK0013', N'S04', 1, 450000),
        (N'BK0015', N'S03', 1, 150000),
        (N'BK0017', N'S02', 2, 200000),
        (N'BK0019', N'S10', 1, 50000),
        (N'BK0021', N'S07', 1, 350000),
        (N'BK0023', N'S09', 2, 350000),
        (N'BK0025', N'S04', 1, 450000),
        (N'BK0027', N'S01', 2, 75000),
        (N'BK0029', N'S03', 1, 150000),
        (N'BK0031', N'S07', 1, 350000),
        (N'BK0032', N'S02', 1, 200000),
        (N'BK0034', N'S09', 1, 350000),
        (N'BK0035', N'S06', 1, 150000),
        (N'BK0039', N'S10', 1, 50000),
        (N'BK0040', N'S04', 1, 450000);

    DECLARE @Payments TABLE (PaymentID CHAR(6) PRIMARY KEY, BookingID CHAR(6), PaymentTime DATETIME, Amount DECIMAL(12,2), MethodID CHAR(4), Note NVARCHAR(200));
    INSERT INTO @Payments (PaymentID, BookingID, PaymentTime, Amount, MethodID, Note)
    VALUES
        (N'PM0001', N'BK0001', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-01 09:05:00', 120)), 1075000, N'PT03', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0002', N'BK0002', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-03 10:20:00', 120)), 2100000, N'PT03', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0003', N'BK0003', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-10 08:35:00', 120)), 750000, N'PT01', N'Thanh toán đặt phòng.'),
        (N'PM0004', N'BK0004', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-11 14:05:00', 120)), 885000, N'PT01', N'Thanh toán đặt phòng.'),
        (N'PM0006', N'BK0006', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-13 11:50:00', 120)), 3350000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0008', N'BK0008', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-15 13:35:00', 120)), 2900000, N'PT03', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0009', N'BK0009', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-16 15:05:00', 120)), 1050000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0011', N'BK0011', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-07-27 09:05:00', 120)), 1075000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0012', N'BK0012', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-07-28 09:05:00', 120)), 2400000, N'PT03', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0013', N'BK0013', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-02 09:05:00', 120)), 2850000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0014', N'BK0014', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-05 09:05:00', 120)), 3200000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0015', N'BK0015', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-10 09:05:00', 120)), 1650000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0016', N'BK0016', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-11 09:05:00', 120)), 2400000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0017', N'BK0017', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-16 09:05:00', 120)), 2800000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0018', N'BK0018', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-19 09:05:00', 120)), 4800000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0019', N'BK0019', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-23 09:05:00', 120)), 1050000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0020', N'BK0020', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-24 09:05:00', 120)), 2400000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0021', N'BK0021', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-28 09:05:00', 120)), 2750000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0022', N'BK0022', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-30 09:05:00', 120)), 3200000, N'PT03', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0023', N'BK0023', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-02 09:05:00', 120)), 1700000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0024', N'BK0024', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-04 09:05:00', 120)), 1600000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0025', N'BK0025', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-06 09:05:00', 120)), 4050000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0026', N'BK0026', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-08 09:05:00', 120)), 3200000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0027', N'BK0027', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-10 09:05:00', 120)), 1150000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0028', N'BK0028', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-12 09:05:00', 120)), 1600000, N'PT03', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0029', N'BK0029', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-13 09:05:00', 120)), 2550000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0030', N'BK0030', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-14 09:05:00', 120)), 3200000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0031', N'BK0031', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-23 09:05:00', 120)), 405000, N'PT03', N'Thanh toán đặt phòng.'),
        (N'PM0032', N'BK0032', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-20 09:05:00', 120)), 1800000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0033', N'BK0033', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-21 09:05:00', 120)), 2400000, N'PT02', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0034', N'BK0034', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-22 09:05:00', 120)), 3550000, N'PT03', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0035', N'BK0035', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-23 09:05:00', 120)), 1750000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0039', N'BK0039', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-23 09:05:00', 120)), 1050000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0040', N'BK0040', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-20 09:05:00', 120)), 2850000, N'PT01', N'Thanh toán tiền phòng và dịch vụ.'),
        (N'PM0041', N'BK0004', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-12 10:00:00', 120)), 2065000, N'PT01', N'Thanh toán phần còn lại của đặt phòng.');

    DECLARE @Invoices TABLE (InvoiceID CHAR(6) PRIMARY KEY, BookingID CHAR(6), InvoiceDate DATETIME, TotalAmount DECIMAL(12,2));
    INSERT INTO @Invoices (InvoiceID, BookingID, InvoiceDate, TotalAmount)
    VALUES
        (N'HD0007', N'BK0001', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-07 11:00:00', 120)), 1075000),
        (N'HD0002', N'BK0002', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-10 11:00:00', 120)), 2100000),
        (N'HD0011', N'BK0011', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-05 11:00:00', 120)), 1075000),
        (N'HD0012', N'BK0012', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-07 11:00:00', 120)), 2400000),
        (N'HD0013', N'BK0013', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-11 11:00:00', 120)), 2850000),
        (N'HD0014', N'BK0014', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-14 11:00:00', 120)), 3200000),
        (N'HD0015', N'BK0015', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-20 11:00:00', 120)), 1650000),
        (N'HD0016', N'BK0016', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-21 11:00:00', 120)), 2400000),
        (N'HD0017', N'BK0017', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-25 11:00:00', 120)), 2800000),
        (N'HD0018', N'BK0018', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-08-29 11:00:00', 120)), 4800000),
        (N'HD0019', N'BK0019', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-01 11:00:00', 120)), 1050000),
        (N'HD0020', N'BK0020', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-03 11:00:00', 120)), 2400000),
        (N'HD0021', N'BK0021', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-06 11:00:00', 120)), 2750000),
        (N'HD0022', N'BK0022', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-08 11:00:00', 120)), 3200000),
        (N'HD0023', N'BK0023', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-11 11:00:00', 120)), 1700000),
        (N'HD0024', N'BK0024', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-13 11:00:00', 120)), 1600000),
        (N'HD0025', N'BK0025', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-16 11:00:00', 120)), 4050000),
        (N'HD0026', N'BK0026', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-17 11:00:00', 120)), 3200000),
        (N'HD0027', N'BK0027', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-19 11:00:00', 120)), 1150000),
        (N'HD0028', N'BK0028', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-21 11:00:00', 120)), 1600000),
        (N'HD0029', N'BK0029', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-22 11:00:00', 120)), 2550000),
        (N'HD0030', N'BK0030', DATEADD(DAY, @DayShift, CONVERT(DATETIME, '2026-09-23 11:00:00', 120)), 3200000);

    DECLARE @Complaints TABLE (CustomerID CHAR(6), Title NVARCHAR(200), Content NVARCHAR(MAX), Status NVARCHAR(30), ReplyMessage NVARCHAR(MAX), CreatedAt DATETIME, PRIMARY KEY (CustomerID, Title));
    INSERT INTO @Complaints (CustomerID, Title, Content, Status, ReplyMessage, CreatedAt)
    VALUES
        (N'CUS003', N'Yêu cầu hỗ trợ đặt phòng', N'Tôi muốn kiểm tra lại thông tin phòng đã được sắp xếp cho booking của mình.', N'Chưa xử lý', NULL, DATEADD(HOUR, 10, DATEADD(DAY, -11, CONVERT(DATETIME, @ReferenceDate)))),
        (N'CUS008', N'Bổ sung khăn tắm', N'Gia đình tôi cần thêm hai khăn tắm cho phòng 202.', N'Đang xử lý', N'Lễ tân đã chuyển yêu cầu đến bộ phận buồng phòng.', DATEADD(HOUR, 10, DATEADD(DAY, 0, CONVERT(DATETIME, @ReferenceDate)))),
        (N'CUS034', N'Điều chỉnh nhiệt độ điều hòa', N'Điều hòa trong phòng 401 chưa đủ mát vào buổi chiều.', N'Đã xử lý', N'Kỹ thuật đã vệ sinh bộ lọc và kiểm tra nhiệt độ; khách xác nhận phòng đã mát.', DATEADD(HOUR, 10, DATEADD(DAY, 0, CONVERT(DATETIME, @ReferenceDate)))),
        (N'CUS001', N'Kiểm tra phí giặt ủi', N'Tôi muốn nhận bảng kê chi tiết số lượng quần áo đã giặt.', N'Đã xử lý', N'Đã gửi bảng kê và giải thích đơn giá giặt ủi cho khách.', DATEADD(HOUR, 10, DATEADD(DAY, -22, CONVERT(DATETIME, @ReferenceDate)))),
        (N'CUS040', N'Xác nhận giờ sử dụng spa', N'Tôi muốn đặt lịch spa lúc 16 giờ trong ngày nhận phòng.', N'Chưa xử lý', NULL, DATEADD(HOUR, 10, DATEADD(DAY, 0, CONVERT(DATETIME, @ReferenceDate)))),
        (N'CUS009', N'Hỗ trợ gửi hành lý', N'Tôi dự kiến đến sớm và muốn gửi hành lý trước giờ nhận phòng.', N'Đang xử lý', N'Lễ tân đã xác nhận có thể giữ hành lý trước giờ nhận phòng.', DATEADD(HOUR, 10, DATEADD(DAY, 0, CONVERT(DATETIME, @ReferenceDate))));

    -- Check existing identifiers before adding any business records.
    IF EXISTS (SELECT 1 FROM @Customers AS s JOIN dbo.CUSTOMER AS t ON t.CustomerID = s.CustomerID
        WHERE t.FullName <> s.FullName OR t.Phone <> s.Phone)
        THROW 52103, N'Mã khách hàng đã thuộc về hồ sơ khác. Không ghi đè thông tin khách.', 1;
    IF EXISTS (SELECT 1 FROM @RoomTypes AS s JOIN dbo.ROOM_TYPE AS t ON t.RoomTypeID = s.RoomTypeID WHERE t.TypeName <> s.TypeName)
        THROW 52104, N'Mã loại phòng đã được dùng cho loại phòng khác.', 1;
    IF EXISTS (SELECT 1 FROM @RoomTypes AS s JOIN dbo.ROOM_TYPE AS t ON t.TypeName = s.TypeName WHERE t.RoomTypeID <> s.RoomTypeID)
        THROW 52105, N'Tên loại phòng đã tồn tại với mã khác.', 1;
    IF EXISTS (SELECT 1 FROM @Rooms AS s JOIN dbo.ROOM AS t ON t.RoomID = s.RoomID
        WHERE t.RoomNumber <> s.RoomNumber OR t.RoomTypeID <> s.RoomTypeID)
        THROW 52106, N'Mã phòng đã được dùng cho số phòng hoặc loại phòng khác.', 1;
    IF EXISTS (SELECT 1 FROM @Rooms AS s JOIN dbo.ROOM AS t ON t.RoomNumber = s.RoomNumber WHERE t.RoomID <> s.RoomID)
        THROW 52107, N'Số phòng đã tồn tại với mã khác.', 1;
    IF EXISTS (SELECT 1 FROM @Services AS s JOIN dbo.SERVICE AS t ON t.ServiceID = s.ServiceID WHERE t.ServiceName <> s.ServiceName)
        THROW 52108, N'Mã dịch vụ đã được dùng cho dịch vụ khác.', 1;
    IF EXISTS (SELECT 1 FROM @Methods AS s JOIN dbo.PAYMENTMETHOD AS t ON t.MethodID = s.MethodID WHERE t.MethodName <> s.MethodName)
        THROW 52109, N'Mã phương thức thanh toán đã được dùng cho phương thức khác.', 1;
    IF EXISTS (SELECT 1 FROM @Bookings AS s JOIN dbo.BOOKING AS t ON t.BookingID = s.BookingID WHERE t.CustomerID <> s.CustomerID)
        THROW 52110, N'Mã booking đã thuộc về khách khác.', 1;

    -- Normalize only the known default labels from the previous file.
    UPDATE dbo.ROOM_TYPE SET TypeName = CASE RoomTypeID
        WHEN 'S001' THEN N'Phòng Standard' WHEN 'S002' THEN N'Phòng Deluxe'
        WHEN 'S003' THEN N'Phòng Suite' WHEN 'S004' THEN N'Phòng Family' END
    WHERE (RoomTypeID = 'S001' AND TypeName = N'[Seed] Standard')
       OR (RoomTypeID = 'S002' AND TypeName = N'[Seed] Deluxe')
       OR (RoomTypeID = 'S003' AND TypeName = N'[Seed] Suite')
       OR (RoomTypeID = 'S004' AND TypeName = N'[Seed] Family');
    SET @LabelsChanged += @@ROWCOUNT;

    DECLARE @RoomLabels TABLE (RoomID CHAR(3) PRIMARY KEY, NewNumber NVARCHAR(20));
    ;WITH Digits AS (
        SELECT v.n FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) AS v(n)
    ), AvailableNumbers AS (
        SELECT CONVERT(NVARCHAR(20), 501 + a.n + 10*b.n + 100*c.n) AS RoomNumber,
            ROW_NUMBER() OVER (ORDER BY 501 + a.n + 10*b.n + 100*c.n) AS rn
        FROM Digits AS a CROSS JOIN Digits AS b CROSS JOIN Digits AS c
        WHERE NOT EXISTS (SELECT 1 FROM dbo.ROOM AS r
                          WHERE r.RoomNumber = CONVERT(NVARCHAR(20), 501 + a.n + 10*b.n + 100*c.n))
    ), PreviousRooms AS (
        SELECT RoomID, ROW_NUMBER() OVER (ORDER BY RoomID) AS rn FROM dbo.ROOM
        WHERE RoomID IN ('S01','S02','S03','S04','S05','S06','S07','S08','S09','S10','S11','S12')
          AND RoomNumber LIKE N'SEED-%'
    )
    INSERT INTO @RoomLabels (RoomID, NewNumber)
    SELECT r.RoomID, n.RoomNumber FROM PreviousRooms AS r JOIN AvailableNumbers AS n ON n.rn = r.rn;
    UPDATE dbo.ROOM SET RoomNumber = labels.NewNumber
    FROM dbo.ROOM JOIN @RoomLabels AS labels ON labels.RoomID = dbo.ROOM.RoomID;
    SET @LabelsChanged += @@ROWCOUNT;

    UPDATE dbo.SERVICE SET ServiceName = CASE ServiceID
        WHEN 'SS01' THEN N'Bữa sáng tại nhà hàng' WHEN 'SS02' THEN N'Giặt ủi nhanh'
        WHEN 'SS03' THEN N'Đưa đón sân bay bằng xe riêng' WHEN 'SS04' THEN N'Massage thư giãn'
        WHEN 'SS05' THEN N'Nước suối minibar' WHEN 'SS06' THEN N'Giường phụ cho trẻ em' END
    WHERE ServiceID IN ('SS01','SS02','SS03','SS04','SS05','SS06') AND ServiceName LIKE N'[[]Seed] %';
    SET @LabelsChanged += @@ROWCOUNT;
    UPDATE dbo.PAYMENTMETHOD SET MethodName = CASE MethodID
        WHEN 'SM01' THEN N'Tiền mặt tại quầy' WHEN 'SM02' THEN N'Chuyển khoản ngân hàng'
        WHEN 'SM03' THEN N'Thẻ ngân hàng' END
    WHERE MethodID IN ('SM01','SM02','SM03') AND MethodName LIKE N'[[]Seed] %';
    SET @LabelsChanged += @@ROWCOUNT;

    DECLARE @Contacts TABLE (CustomerID CHAR(6) PRIMARY KEY, PreviousEmail VARCHAR(100), Email VARCHAR(100));
    INSERT INTO @Contacts VALUES
        ('SC0001','seed01@example.com','minhanh.nguyen@gmail.com'),
        ('SC0002','seed02@example.com','giabao.tran@gmail.com'),
        ('SC0003','seed03@example.com','ngochan.le@gmail.com'),
        ('SC0004','seed04@example.com','quocduy.pham@gmail.com'),
        ('SC0005','seed05@example.com','thaovy.vo@gmail.com'),
        ('SC0006','seed06@example.com','hoangnam.dang@gmail.com'),
        ('SC0007','seed07@example.com','khanhlinh.bui@gmail.com'),
        ('SC0008','seed08@example.com','thanhtung.huynh@gmail.com'),
        ('SC0009','seed09@example.com','emily.johnson@outlook.com'),
        ('SC0010','seed10@example.com','haruka.sato@yahoo.co.jp'),
        ('SC0011','seed11@example.com','hoaiphuong.nguyen@gmail.com');
    UPDATE dbo.CUSTOMER SET Email = c.Email
    FROM dbo.CUSTOMER JOIN @Contacts AS c ON c.CustomerID = dbo.CUSTOMER.CustomerID
    WHERE dbo.CUSTOMER.Email = c.PreviousEmail;
    SET @LabelsChanged += @@ROWCOUNT;
    UPDATE dbo.COMPLAINT SET Title = LTRIM(SUBSTRING(Title, CHARINDEX(N']', Title) + 1, 200))
    WHERE CustomerID IN ('SC0001','SC0002','SC0003','SC0004','SC0005','SC0009')
      AND Title LIKE N'[[]Seed-[0-9][0-9]] %';
    SET @LabelsChanged += @@ROWCOUNT;
    UPDATE dbo.PAYMENT SET Note = N'Thanh toán tiền phòng và dịch vụ.'
    WHERE PaymentID IN ('SP0001','SP0002','SP0003','SP0004','SP0005','SP0006','SP0007','SP0008','SP0009')
      AND Note = N'[Seed] Thanh toán mẫu';
    SET @LabelsChanged += @@ROWCOUNT;

    INSERT INTO dbo.CUSTOMER (CustomerID, FullName, IdentityNumber, Phone, Email, Address, NationalityName)
    SELECT s.CustomerID, s.FullName, s.IdentityNumber, s.Phone, s.Email, s.Address, s.NationalityName FROM @Customers AS s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.CUSTOMER AS t WHERE t.CustomerID = s.CustomerID);
    INSERT INTO @Added VALUES (N'CUSTOMER', @@ROWCOUNT);

    INSERT INTO dbo.ROOM_TYPE (RoomTypeID, TypeName, Capacity, Price, RoomTypeImage, Description)
    SELECT s.RoomTypeID, s.TypeName, s.Capacity, s.Price, s.RoomTypeImage, s.Description FROM @RoomTypes AS s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.ROOM_TYPE AS t WHERE t.RoomTypeID = s.RoomTypeID);
    INSERT INTO @Added VALUES (N'ROOM_TYPE', @@ROWCOUNT);

    INSERT INTO dbo.ROOM (RoomID, RoomNumber, RoomImage, Status, RoomTypeID)
    SELECT s.RoomID, s.RoomNumber, s.RoomImage, s.Status, s.RoomTypeID FROM @Rooms AS s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.ROOM AS t WHERE t.RoomID = s.RoomID);
    INSERT INTO @Added VALUES (N'ROOM', @@ROWCOUNT);

    INSERT INTO dbo.SERVICE (ServiceID, ServiceName, UnitPrice)
    SELECT s.ServiceID, s.ServiceName, s.UnitPrice FROM @Services AS s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.SERVICE AS t WHERE t.ServiceID = s.ServiceID);
    INSERT INTO @Added VALUES (N'SERVICE', @@ROWCOUNT);

    INSERT INTO dbo.PAYMENTMETHOD (MethodID, MethodName)
    SELECT s.MethodID, s.MethodName FROM @Methods AS s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PAYMENTMETHOD AS t WHERE t.MethodID = s.MethodID);
    INSERT INTO @Added VALUES (N'PAYMENTMETHOD', @@ROWCOUNT);

    INSERT INTO dbo.BOOKING (BookingID, CustomerID, EmployeeID, BookingDate, CheckInDate, CheckOutDate, BookingStatus, CancellationReason)
    OUTPUT inserted.BookingID INTO @NewBookings
    SELECT s.BookingID, s.CustomerID, @Actor, s.BookingDate, s.CheckInDate, s.CheckOutDate, s.BookingStatus, s.CancellationReason
    FROM @Bookings AS s WHERE NOT EXISTS (SELECT 1 FROM dbo.BOOKING AS t WHERE t.BookingID = s.BookingID);
    INSERT INTO @Added VALUES (N'BOOKING', @@ROWCOUNT);

    INSERT INTO dbo.BOOKING_DETAIL (BookingID, RoomTypeID, Quantity, UnitPrice)
    SELECT s.BookingID, s.RoomTypeID, s.Quantity, s.UnitPrice FROM @Details AS s
    JOIN @NewBookings AS n ON n.BookingID = s.BookingID;
    INSERT INTO @Added VALUES (N'BOOKING_DETAIL', @@ROWCOUNT);

    INSERT INTO dbo.ROOM_ASSIGNMENT (BookingDetailID, RoomID, EmployeeID, AssignedAt)
    SELECT d.BookingDetailID, s.RoomID, @Actor, s.AssignedAt FROM @Assignments AS s
    JOIN @NewBookings AS n ON n.BookingID = s.BookingID
    JOIN dbo.BOOKING_DETAIL AS d ON d.BookingID = s.BookingID AND d.RoomTypeID = s.RoomTypeID;
    INSERT INTO @Added VALUES (N'ROOM_ASSIGNMENT', @@ROWCOUNT);

    INSERT INTO dbo.BOOKING_SERVICE (BookingID, ServiceID, Quantity, UnitPrice, OrderedByEmployee)
    SELECT s.BookingID, s.ServiceID, s.Quantity, s.UnitPrice, @Actor FROM @Orders AS s
    JOIN @NewBookings AS n ON n.BookingID = s.BookingID;
    INSERT INTO @Added VALUES (N'BOOKING_SERVICE', @@ROWCOUNT);

    IF EXISTS (SELECT 1 FROM @Payments AS s JOIN @NewBookings AS n ON n.BookingID = s.BookingID
        JOIN dbo.PAYMENT AS t ON t.PaymentID = s.PaymentID)
        THROW 52111, N'Mã thanh toán đã thuộc về bản ghi khác.', 1;
    INSERT INTO dbo.PAYMENT (PaymentID, BookingID, PaymentTime, Amount, MethodID, CollectedByEmployee, Note)
    SELECT s.PaymentID, s.BookingID, s.PaymentTime, s.Amount, s.MethodID, @Actor, s.Note FROM @Payments AS s
    JOIN @NewBookings AS n ON n.BookingID = s.BookingID;
    INSERT INTO @Added VALUES (N'PAYMENT', @@ROWCOUNT);

    IF EXISTS (SELECT 1 FROM @Invoices AS s JOIN @NewBookings AS n ON n.BookingID = s.BookingID
        JOIN dbo.INVOICE AS t ON t.InvoiceID = s.InvoiceID)
        THROW 52112, N'Mã hóa đơn đã thuộc về bản ghi khác.', 1;
    INSERT INTO dbo.INVOICE (InvoiceID, BookingID, InvoiceDate, TotalAmount, EmployeeID)
    SELECT s.InvoiceID, s.BookingID, s.InvoiceDate, s.TotalAmount, @Actor FROM @Invoices AS s
    JOIN @NewBookings AS n ON n.BookingID = s.BookingID;
    INSERT INTO @Added VALUES (N'INVOICE', @@ROWCOUNT);

    INSERT INTO dbo.COMPLAINT (CustomerID, Title, Content, Status, ReplyMessage, CreatedAt, EmployeeID, ResolvedByEmployee)
    SELECT s.CustomerID, s.Title, s.Content, s.Status, s.ReplyMessage, s.CreatedAt,
        CASE WHEN s.Status = N'Chưa xử lý' THEN NULL ELSE @Actor END,
        CASE WHEN s.Status = N'Đã xử lý' THEN @Resolver ELSE NULL END
    FROM @Complaints AS s WHERE NOT EXISTS (
        SELECT 1 FROM dbo.COMPLAINT AS t WHERE t.CustomerID = s.CustomerID AND t.Title = s.Title);
    INSERT INTO @Added VALUES (N'COMPLAINT', @@ROWCOUNT);

    -- Validate inserted history against the actual records, including existing reservations.
    IF EXISTS (SELECT 1 FROM dbo.ROOM_ASSIGNMENT AS a
        JOIN dbo.BOOKING_DETAIL AS d ON d.BookingDetailID = a.BookingDetailID
        JOIN @NewBookings AS n ON n.BookingID = d.BookingID
        JOIN dbo.ROOM AS r ON r.RoomID = a.RoomID
        WHERE r.RoomTypeID <> d.RoomTypeID OR r.Status <> 'ACTIVE')
        THROW 52113, N'Phòng được phân không hoạt động hoặc không đúng loại phòng.', 1;
    IF EXISTS (SELECT 1 FROM dbo.ROOM_ASSIGNMENT AS a
        JOIN dbo.BOOKING_DETAIL AS d ON d.BookingDetailID = a.BookingDetailID
        JOIN @NewBookings AS n ON n.BookingID = d.BookingID
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
      AND (b.CheckOutDate > @Today OR b.BookingStatus = 'CHECKED_IN')
      AND EXISTS (SELECT 1 FROM dbo.BOOKING_DETAIL AS nd JOIN @NewBookings AS n ON n.BookingID = nd.BookingID
                  WHERE nd.RoomTypeID = d.RoomTypeID);
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
    FROM dbo.BOOKING AS b JOIN @NewBookings AS n ON n.BookingID = b.BookingID;
    IF EXISTS (SELECT 1 FROM @Totals AS t
        CROSS APPLY (SELECT ISNULL(SUM(p.Amount), 0) AS Paid FROM dbo.PAYMENT AS p WHERE p.BookingID = t.BookingID) AS p
        WHERE p.Paid > t.TotalAmount)
        THROW 52115, N'Tổng thanh toán vượt chi phí phòng và dịch vụ.', 1;
    IF EXISTS (SELECT 1 FROM dbo.INVOICE AS i JOIN @Totals AS t ON t.BookingID = i.BookingID
        WHERE i.TotalAmount <> t.TotalAmount)
        THROW 52116, N'Tổng hóa đơn không khớp giá lịch sử của phòng và dịch vụ.', 1;

    COMMIT TRANSACTION;
    SELECT N'Đã cập nhật dữ liệu khách sạn.' AS Result, @ReferenceDate AS ReferenceDate,
        @Actor AS EmployeeID, @Resolver AS ResolvedByEmployee, @LabelsChanged AS NormalizedLabels;
    SELECT TableName, RowsAdded FROM @Added ORDER BY TableName;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
