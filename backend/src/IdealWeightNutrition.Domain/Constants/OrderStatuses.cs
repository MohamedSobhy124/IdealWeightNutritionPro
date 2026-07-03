namespace IdealWeightNutrition.Domain.Constants;

public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Paid = "Paid";
    public const string Processing = "Processing";
    public const string Shipped = "Shipped";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
    public const string Refunded = "Refunded";
    public const string PartiallyRefunded = "PartiallyRefunded";
    public const string Returned = "Returned";
    public const string ReturnApproved = "ReturnApproved";
    public const string ReturnRequested = "ReturnRequested";
}

public static class PaymentStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string DelayedPayment = "ApprovedForDelayedPayment";
    public const string Cancelled = "Cancelled";
    public const string Rejected = "Rejected";
    public const string Refunded = "Refunded";
    public const string PartiallyRefunded = "PartiallyRefunded";
}

public static class PaymentMethods
{
    public const string Cod = "COD";
    public const string Geidea = "Geidea";
    public const string Tamara = "Tamara";
    public const string Tabby = "Tabby";
}
