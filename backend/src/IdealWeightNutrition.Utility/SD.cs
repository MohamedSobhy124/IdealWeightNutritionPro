using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdealWeightNutrition.Utility
{
    public class SD
    {
        public const string Role_Customer = "Customer";
        public const string Role_Company = "Company";
        public const string Role_Admin = "Admin";
        public const string Role_Employee = "Employee";

		public const string StatusPending = "Pending";
		public const string StatusApproved = "Approved";
		public const string StatusPaid = "Paid";
		public const string StatusInProcess = "Processing";
		public const string StatusInPreparingShiping = "Preparing Shiping";
		public const string StatusShipped = "Shipped";
		public const string StatusOutForDelivery = "OutForDelivery";
		public const string StatusDelivered = "Delivered";
		public const string StatusCancelled = "Cancelled";
		public const string StatusReturned = "Returned";
		public const string StatusRefunded = "Refunded";
		public const string StatusReturnRequested = "ReturnRequested";
		public const string StatusReturnApproved = "ReturnApproved";
		public const string StatusReturnRejected = "ReturnRejected";
		public const string StatusReturnProcessing = "ReturnProcessing";
		public const string StatusReturnCompleted = "ReturnCompleted";
		
		// Return Request Status
		public const string ReturnStatusPending = "Pending";
		public const string ReturnStatusApproved = "Approved";
		public const string ReturnStatusRejected = "Rejected";
		public const string ReturnStatusProcessing = "Processing";
		public const string ReturnStatusCompleted = "Completed";
		public const string ReturnStatusCancelled = "Cancelled";
		
		// Refund Status
		public const string RefundStatusPending = "Pending";
		public const string RefundStatusProcessed = "Processed";
		public const string RefundStatusFailed = "Failed";

		public const string PaymentStatusPending = "Pending";
		public const string PaymentStatusPaid = "Paid";
		public const string PaymentStatusDelayedPayment = "ApprovedForDelayedPayment";
		public const string PaymentStatusRejected = "Rejected";
		public const string PaymentStatusCancelled = "Cancelled";
		public const string PaymentStatusRefunded = "Refunded";
		public const string PaymentStatusPartiallyRefunded = "PartiallyRefunded";
		public const string PaymentStatusAuthorized = "Authorized"; // Payment authorized but not yet captured

		// Payment Methods
		public const string PaymentMethodGeidea = "Geidea";
		public const string PaymentMethodTappy = "Tabby";
		public const string PaymentMethodTamara = "Tamara";

		public const string SessionCart = "SessionShoppingCart";

	}
}
