namespace SBHMS.DTO
{
    public enum RequestStatusEnum
    {
        Pending,     // Đang chờ
        InProgress,  // Đang xử lý
        Resolved,    // Đã giải quyết
        Rejected     // Bị từ chối
    }
}