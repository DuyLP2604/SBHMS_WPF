# SBHMS WPF — sửa lỗi build và kết nối màn hình với EF Core

Bản sửa dựa trên các entity và `Prn212SbhmsContext` có sẵn. **Không thay đổi bất kỳ file nào trong `SHBMS/Model`.** Không thêm migration và không thay đổi database schema.

## Áp dụng bản sửa

Mở terminal tại thư mục chứa `SHBMS_WPF.slnx` và thư mục con `SHBMS`:

```powershell
git apply --check --ignore-space-change "C:\Users\YOUR_NAME\Downloads\SBHMS_WPF_Compile_Fixes.patch"
git apply --ignore-space-change "C:\Users\YOUR_NAME\Downloads\SBHMS_WPF_Compile_Fixes.patch"
dotnet build .\SHBMS\SHBMS.csproj
```

Đổi đường dẫn patch cho đúng máy. Kiểm tra thành công trước khi áp dụng. Nếu source đã thay đổi so với bản gửi, cần đối chiếu các file báo conflict; không dùng lệnh ghi đè cưỡng bức.

## Chạy ứng dụng

- Dùng Windows với .NET 8 SDK và workload **.NET desktop development** trong Visual Studio.
- Có thể mở trực tiếp `SHBMS/SHBMS.csproj` nếu Visual Studio chưa hỗ trợ `.slnx`.
- Chỉnh `SHBMS/appsettings.json`, mục `ConnectionStrings:DefaultConnection`, cho SQL Server trên máy.
- Database cần đúng schema tương ứng với folder Model hiện tại. Bản sửa không tự tạo database và không tự chạy script SQL.
- Nếu bảng `USERS` trống, lần đăng nhập đầu tiên sẽ tạo tài khoản `admin` / `123456` cùng Employee hợp lệ. Nếu đã có user, tài khoản hiện có được giữ nguyên.
- Mật khẩu dùng `PasswordHelper` SHA-256 hiện có của project; không chuyển đổi mật khẩu đang lưu theo thuật toán khác.

`appsettings.json` được copy sang thư mục chạy. `App` đặt thư mục làm việc về thư mục chứa chương trình để context scaffold hiện tại đọc được file cấu hình mà không cần sửa Model. Lỗi kết nối hiển thị ở form đăng nhập hoặc dòng trạng thái của trang.

## Những phần đã sửa

| Phần | Nội dung hiện tại |
| --- | --- |
| MainWindow | Một cửa sổ chung, ContentControl chứa trang, menu theo role, sign out; trang được tạo khi mở |
| Đăng nhập | Service được build; tạo User và Employee cùng một lần lưu; có EmployeeId, Position và khóa ngoại hợp lệ |
| Customer Profiles | Đọc database, tìm kiếm, thêm, sửa, xóa; Copy selected tạo form mới; lưu bằng EF |
| Guest Feedback | Dùng Complaint; đọc Customer liên quan, trả lời, đánh dấu chưa xử lý hoặc đã xử lý |
| Staff Accounts | Admin quản lý User cùng Employee; mật khẩu dùng PasswordBox; không cho sửa role của chính tài khoản đang đăng nhập |
| Room Status | Đọc Room và RoomType; xác định khách đang ở từ RoomAssignment/Booking; cập nhật ACTIVE, MAINTENANCE, INACTIVE |
| Booking Services | Đọc BOOKING_SERVICE; thêm/sửa số lượng/xóa dịch vụ của booking đang CHECKED_IN |
| Guest Stays | **Chỉ xem** booking thật, phòng đã phân, chi phí và trạng thái; chưa nối check-in, checkout, tạo booking walk-in |
| Invoices | **Chỉ xem** Invoice thật và các thông tin booking, khách, phòng, dịch vụ |
| Room Pricing | Admin/Manager sửa RoomType.Price; giá đã chốt trong BookingDetail được giữ nguyên |
| DTO và service khung | Sửa namespace, using, tên context và kiểu string cho các khóa CHAR |

Các field form nằm trong ViewModel. Chọn dòng sẽ sao chép dữ liệu sang field form, chưa sửa database. Khi Save, ViewModel tạo DbContext mới, tìm/thêm entity, gọi SaveChangesAsync rồi tải lại danh sách. Entity scaffold không cần cài INotifyPropertyChanged.

## Phân quyền menu hiện tại

| Menu | Admin | Manager | Staff |
| --- | --- | --- | --- |
| Customer / Guest stays / Room / Booking services / Feedback / Invoices | Có | Có | Có |
| Room pricing | Có | Có | Không |
| Staff accounts | Có | Không | Không |

Ẩn menu dùng binding. Điều hướng và lệnh lưu ở Staff Accounts/Room Pricing cũng kiểm tra quyền. Bảng trên là cấu hình ban đầu cho bản sửa, cần đối chiếu lại UC của nhóm trước khi hoàn thiện phân quyền chi tiết.

## Các giới hạn được giữ rõ ràng

- Model hiện không có trạng thái khóa tài khoản. Chức năng Toggle status demo và cột ACTIVE đã được gỡ khỏi màn hình.
- BOOKING_SERVICE không có trạng thái Pending/InProgress/Resolved hoặc entity ServiceRequest riêng. Màn hình hiện quản lý dịch vụ đặt theo booking, không giả lập trạng thái yêu cầu bằng dữ liệu tạm. Khiếu nại được xử lý riêng ở Guest Feedback.
- Model không có Promotion/PricingRule. Màn hình giá hiện chỉ cập nhật RoomType.Price; chưa có khuyến mãi.
- Check-in/checkout và walk-in trước đây dùng HotelDemoStore. Bản sửa lỗi biên dịch này chuyển trang lưu trú sang xem booking thật, chưa triển khai lại các nghiệp vụ đó.
- Invoice chỉ đọc dữ liệu đã tồn tại; chưa bổ sung quy trình phát hành hóa đơn hoặc thu tiền.
- Những service khung khác AuthenticationService vẫn có NotImplementedException và **không được các màn hình đã sửa gọi đến**. Chúng đã được sửa để biên dịch, không đồng nghĩa nghiệp vụ của chúng đã hoàn tất.
- Các DTO khung chưa được dùng bởi màn hình có thể cần thiết kế lại khi nối các nghiệp vụ còn thiếu.
- Khóa CHAR được cấp mã ở ứng dụng (CUS001/EMP001...), không phải identity. Khóa số identity được SQL Server cấp. Nếu hai máy tạo cùng mã đồng thời, database từ chối mã trùng và thông báo lỗi; tải lại rồi thử lại.

## Kiểm chứng

- Source gốc: build phát hiện 22 lỗi biên dịch.
- Source đã sửa: `dotnet build SHBMS/SHBMS.csproj -p:EnableWindowsTargeting=true` thành công, **0 lỗi**.
- Còn 1 warning `CS8603` tại `Model/Prn212SbhmsContext.cs:62` do khả năng chuỗi kết nối null; giữ nguyên file generated theo yêu cầu.
- Đã đối chiếu SHA-256: toàn bộ file trong Model giống hệt bản gốc.
- Build được kiểm tra bằng SDK 8.0.408, EF Core 8.0.32, có biên dịch C# và XAML. Chưa chạy giao diện Windows hoặc thử CRUD trên SQL Server của nhóm.

Sau khi chạy trên máy nhóm, kiểm tra đăng nhập, mở từng menu, thao tác CRUD và đóng/mở ứng dụng để xác nhận dữ liệu đã lưu. Trang có thể trống nếu database chưa có dữ liệu.
