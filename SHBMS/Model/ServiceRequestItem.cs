using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SHBMS.Model
{
    public class ServiceRequestItem
    {
        public int RequestId { get; set; }
        public string RoomNumber { get; set; } = "";
        public string RequestType { get; set; } = "";
        public string RequestDetails { get; set; } = "";
        public string Notes { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
    }
}
