
namespace SHBMS.Model
{
    public class GuestFeedback
    {
        public int ComplaintID { get; set; }

        public string CustomerID { get; set; } = "";

        public string GuestName { get; set; } = "";

        public string Content { get; set; } = "";

        public string Status { get; set; } = "Chưa xử lý";

        public string ReplyMessage { get; set; } = "";

        public string RecordedByEmployee { get; set; } = "";
    }
}
