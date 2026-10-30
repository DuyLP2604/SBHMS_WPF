using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace SHBMS.Model
{
    public class AppDbContext : DbContext
    {
        //Hàm dựng
        public AppDbContext() { }

        //Tạo các collection ánh xạ đến các bảng trong CSDL
        public DbSet<Booking> booking { get; set; }


        //Tạo phương thức lấy chuỗi kết nối từ file appsettings.json
        private string GetConnectionString()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .Build();
            var connectionstring = configuration["ConnectionStrings:DefaultConnection"];
            return connectionstring;
        }


        //Override lại phương thức OnConfiguring để thiết lập kết nối cho CSDL
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(GetConnectionString());
        }

        //Override lại phương thức OnModelCreating nếu cần cấu hình mô hình dữ liệu.

    }


}
