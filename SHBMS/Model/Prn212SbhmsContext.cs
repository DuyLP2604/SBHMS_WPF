using Microsoft.EntityFrameworkCore;

namespace SHBMS.Model;

public partial class Prn212SbhmsContext : DbContext
{
    public Prn212SbhmsContext()
    {
    }

    public Prn212SbhmsContext(DbContextOptions<Prn212SbhmsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingDetail> BookingDetails { get; set; }

    public virtual DbSet<BookingService> BookingServices { get; set; }

    public virtual DbSet<Complaint> Complaints { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Paymentmethod> Paymentmethods { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomAssignment> RoomAssignments { get; set; }

    public virtual DbSet<RoomType> RoomTypes { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VwBookingDetail> VwBookingDetails { get; set; }

    public virtual DbSet<VwBookingList> VwBookingLists { get; set; }

    public virtual DbSet<VwRoomList> VwRoomLists { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=ASUS-TUF-A15;Initial Catalog=PRN212_SBHMS;Integrated Security=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("BOOKING", tb => tb.HasTrigger("trg_BOOKING_BlockPhysicalDelete"));

            entity.HasIndex(e => new { e.IsDeleted, e.BookingStatus, e.CheckInDate, e.CheckOutDate }, "IX_BOOKING_Active_Status_Dates");

            entity.HasIndex(e => e.CustomerId, "IX_BOOKING_CustomerID");

            entity.Property(e => e.BookingId)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("BookingID");
            entity.Property(e => e.BookingDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.BookingStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.CancellationReason).HasMaxLength(200);
            entity.Property(e => e.CheckInDeadline)
                .HasPrecision(0)
                .HasComputedColumnSql("(isnull([CheckInDeadlineOverride],dateadd(minute,(855),CONVERT([datetime2](0),[CheckInDate]))))", true);
            entity.Property(e => e.CheckInDeadlineOverride).HasPrecision(0);
            entity.Property(e => e.CustomerId)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("CustomerID");
            entity.Property(e => e.DeletedAt).HasPrecision(0);
            entity.Property(e => e.DeletedByEmployee)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("EmployeeID");

            entity.HasOne(d => d.Customer).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BOOKING_CUSTOMER");

            entity.HasOne(d => d.DeletedByEmployeeNavigation).WithMany(p => p.BookingDeletedByEmployeeNavigations)
                .HasForeignKey(d => d.DeletedByEmployee)
                .HasConstraintName("FK_BOOKING_DeletedByEmployee");

            entity.HasOne(d => d.Employee).WithMany(p => p.BookingEmployees)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BOOKING_EMPLOYEE");
        });

        modelBuilder.Entity<BookingDetail>(entity =>
        {
            entity.ToTable("BOOKING_DETAIL");

            entity.HasIndex(e => e.RoomTypeId, "IX_BOOKING_DETAIL_RoomTypeID");

            entity.HasIndex(e => new { e.BookingId, e.RoomTypeId }, "UQ_BOOKING_DETAIL_Booking_RoomType").IsUnique();

            entity.Property(e => e.BookingDetailId).HasColumnName("BookingDetailID");
            entity.Property(e => e.BookingId)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("BookingID");
            entity.Property(e => e.RoomTypeId)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("RoomTypeID");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BOOKING_DETAIL_BOOKING");

            entity.HasOne(d => d.RoomType).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.RoomTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BOOKING_DETAIL_ROOMTYPE");
        });

        modelBuilder.Entity<BookingService>(entity =>
        {
            entity.HasKey(e => new { e.BookingId, e.ServiceId });

            entity.ToTable("BOOKING_SERVICE");

            entity.Property(e => e.BookingId)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("BookingID");
            entity.Property(e => e.ServiceId)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ServiceID");
            entity.Property(e => e.OrderedByEmployee)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingServices)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_B_SERVICE_BOOKING");

            entity.HasOne(d => d.OrderedByEmployeeNavigation).WithMany(p => p.BookingServices)
                .HasForeignKey(d => d.OrderedByEmployee)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_B_SERVICE_EMP");

            entity.HasOne(d => d.Service).WithMany(p => p.BookingServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_B_SERVICE_SERVICE");
        });

        modelBuilder.Entity<Complaint>(entity =>
        {
            entity.ToTable("COMPLAINT");

            entity.Property(e => e.ComplaintId).HasColumnName("ComplaintID");
            entity.Property(e => e.CustomerId)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("CustomerID");
            entity.Property(e => e.RecordedByEmployee)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.ResolvedByEmployee)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Chưa xử lý");

            entity.HasOne(d => d.Customer).WithMany(p => p.Complaints)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_COMPLAINT_CUS");

            entity.HasOne(d => d.RecordedByEmployeeNavigation).WithMany(p => p.Complaints)
                .HasForeignKey(d => d.RecordedByEmployee)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_COMPLAINT_REC_EMP");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("CUSTOMER");

            entity.HasIndex(e => e.IdentityNumber, "UX_CUSTOMER_Identity_NotNull")
                .IsUnique()
                .HasFilter("([IdentityNumber] IS NOT NULL)");

            entity.Property(e => e.CustomerId)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("CustomerID");
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IdentityNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nationality).HasMaxLength(50);
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("EMPLOYEE");

            entity.HasIndex(e => e.UserId, "UQ_EMPLOYEE_User").IsUnique();

            entity.Property(e => e.EmployeeId)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("EmployeeID");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Position).HasMaxLength(50);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithOne(p => p.Employee)
                .HasForeignKey<Employee>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EMPLOYEE_USERS");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("INVOICE");

            entity.Property(e => e.InvoiceId)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("InvoiceID");
            entity.Property(e => e.BookingId)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("BookingID");
            entity.Property(e => e.InvoiceDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IssuedByEmployee)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Booking).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_INVOICE_BOOKING");

            entity.HasOne(d => d.IssuedByEmployeeNavigation).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.IssuedByEmployee)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_INVOICE_EMP");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("PAYMENT");

            entity.Property(e => e.PaymentId)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("PaymentID");
            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.BookingId)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("BookingID");
            entity.Property(e => e.CollectedByEmployee)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.MethodId)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MethodID");
            entity.Property(e => e.Note).HasMaxLength(200);
            entity.Property(e => e.PaymentTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAYMENT_BOOKING");

            entity.HasOne(d => d.CollectedByEmployeeNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.CollectedByEmployee)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAYMENT_EMP");

            entity.HasOne(d => d.Method).WithMany(p => p.Payments)
                .HasForeignKey(d => d.MethodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAYMENT_METHOD");
        });

        modelBuilder.Entity<Paymentmethod>(entity =>
        {
            entity.HasKey(e => e.MethodId);

            entity.ToTable("PAYMENTMETHOD");

            entity.Property(e => e.MethodId)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MethodID");
            entity.Property(e => e.MethodName).HasMaxLength(30);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("ROOM");

            entity.HasIndex(e => new { e.RoomTypeId, e.Status }, "IX_ROOM_RoomType_Status");

            entity.HasIndex(e => e.RoomNumber, "UQ_ROOM_Number").IsUnique();

            entity.Property(e => e.RoomId)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("RoomID");
            entity.Property(e => e.RoomNumber).HasMaxLength(20);
            entity.Property(e => e.RoomTypeId)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("RoomTypeID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE");

            entity.HasOne(d => d.RoomType).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.RoomTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ROOM_ROOM_TYPE");
        });

        modelBuilder.Entity<RoomAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentId);

            entity.ToTable("ROOM_ASSIGNMENT");

            entity.Property(e => e.AssignmentId).HasColumnName("AssignmentID");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.AssignedByEmployee)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.BookingDetailId).HasColumnName("BookingDetailID");
            entity.Property(e => e.RoomId)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("RoomID");

            entity.HasOne(d => d.AssignedByEmployeeNavigation).WithMany(p => p.RoomAssignments)
                .HasForeignKey(d => d.AssignedByEmployee)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ASSIGNMENT_EMP");

            entity.HasOne(d => d.BookingDetail).WithMany(p => p.RoomAssignments)
                .HasForeignKey(d => d.BookingDetailId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ASSIGNMENT_DETAIL");

            entity.HasOne(d => d.Room).WithMany(p => p.RoomAssignments)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ASSIGNMENT_ROOM");
        });

        modelBuilder.Entity<RoomType>(entity =>
        {
            entity.ToTable("ROOM_TYPE", tb => tb.HasTrigger("trg_ROOM_TYPE_ProtectBookedCapacity"));

            entity.HasIndex(e => e.TypeName, "UQ_ROOM_TYPE_TypeName").IsUnique();

            entity.Property(e => e.RoomTypeId)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("RoomTypeID");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TypeName).HasMaxLength(50);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.ToTable("SERVICE");

            entity.Property(e => e.ServiceId)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ServiceID");
            entity.Property(e => e.ServiceName).HasMaxLength(60);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 2)");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("USERS");

            entity.HasIndex(e => e.Username, "UQ_USERS_Username").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwBookingDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_BookingDetail");

            entity.Property(e => e.BookingDetailId).HasColumnName("BookingDetailID");
            entity.Property(e => e.BookingId)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("BookingID");
            entity.Property(e => e.LineTotal).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.RoomTypeId)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("RoomTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(50);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 2)");
        });

        modelBuilder.Entity<VwBookingList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_BookingList");

            entity.Property(e => e.BookingDate).HasColumnType("datetime");
            entity.Property(e => e.BookingId)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("BookingID");
            entity.Property(e => e.BookingStatus)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CancellationReason).HasMaxLength(200);
            entity.Property(e => e.CheckInDeadline).HasPrecision(0);
            entity.Property(e => e.CheckInDeadlineOverride).HasPrecision(0);
            entity.Property(e => e.CreatedByEmployeeName).HasMaxLength(100);
            entity.Property(e => e.CustomerId)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("CustomerID");
            entity.Property(e => e.CustomerName).HasMaxLength(100);
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("EmployeeID");
            entity.Property(e => e.PaidAmount).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.RoomSubtotal).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.ServiceSubtotal).HasColumnType("decimal(38, 4)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(38, 4)");
        });

        modelBuilder.Entity<VwRoomList>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_RoomList");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.RoomId)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("RoomID");
            entity.Property(e => e.RoomNumber).HasMaxLength(20);
            entity.Property(e => e.RoomTypeId)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("RoomTypeID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TypeName).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
