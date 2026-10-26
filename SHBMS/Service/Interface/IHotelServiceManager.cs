namespace SHBMS.Service.Interface
{
    public interface IHotelServiceManager
    {
        // [UC-18] View Service Availability
        Task<IEnumerable<HotelServiceDto>> GetAvailableServicesAsync();

        // [UC-19] Update Service Availability
        Task<bool> UpdateServiceStatusAsync(int serviceId, bool isAvailable, int stockQuantity);

        // [UC-22] Fulfill Additional Service Order
        Task<bool> OrderAdditionalServiceAsync(ServiceOrderCreateDto dto);
        Task<IEnumerable<ServiceOrderDto>> GetOrdersByStayIdAsync(int stayId);
    }
}