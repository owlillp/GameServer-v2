namespace StoreService.Domain.Orders;

public enum OrderStatus
{
    PENDING,
    RESERVED,
    GRANT_REQUESTED,
    COMPLETED,
    FAILED,
}
