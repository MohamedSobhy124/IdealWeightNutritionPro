using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tamara.Net.ClientSDK;
using Tamara.Net.ClientSDK.Models.Payment;
using Tamara.Net.SDK.Consumer;
using Tamara.Net.SDK.Models.Common;
using Tamara.Net.SDK.Models.Order;
using Tamara.Net.SDK.Models.Payment;

namespace IdealWeightNutrition.Utility
{
    public class TamaraHelper
    {
        private readonly TamaraSettings _settings;
        private readonly ITamaraApiClient _apiClient;
        private readonly ILogger<TamaraHelper> _logger;

        public TamaraHelper(TamaraSettings settings, ILogger<TamaraHelper> logger = null)
        {
            _settings = settings;
            _logger = logger;

            // Create API configuration from settings
            var apiConfiguration = new ApiConfiguration
            {
                BaseUrl = !string.IsNullOrEmpty(_settings.BaseUrl)
                    ? _settings.BaseUrl
                    : (_settings.UseSandbox
                        ? "https://api-sandbox.tamara.co"
                        : "https://api.tamara.co"),
                ApiToken = _settings.ApiToken,
                RequestTimeout = 30
            };

            // Create logger factory if logger is not provided
            ILoggerFactory loggerFactory = null;
            if (logger != null)
            {
                loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            }

            // Create API client using factory
            _apiClient = CheckoutServiceFactory.CreateClient(apiConfiguration, loggerFactory?.CreateLogger<TamaraHelper>());
        }

        /// <summary>
        /// Create a Tamara checkout session using the official SDK
        /// </summary>
        public async Task<TamaraPaymentResponse> CreateCheckoutAsync(TamaraPaymentRequest request)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(request.OrderReferenceId))
                    throw new ArgumentException("OrderReferenceId is required");
                if (request.TotalAmount == null || request.TotalAmount.Amount <= 0)
                    throw new ArgumentException("TotalAmount must be greater than 0");
                if (request.Consumer == null)
                    throw new ArgumentException("Consumer information is required");
                if (request.Items == null || !request.Items.Any())
                    throw new ArgumentException("At least one item is required");

                // Map our request model to SDK's Order model
                var order = MapToSdkOrder(request);

                // Create checkout using SDK
                var response = await _apiClient.CreateCheckout(order);

                // Map SDK response to our response model
                if (response.IsSuccess() && response.Data != null)
                {
                    return new TamaraPaymentResponse
                    {
                        Success = true,
                        CheckoutUrl = response.Data.CheckoutUrl,
                        OrderId = response.Data.OrderId,
                        CheckoutId = response.Data.CheckoutId,
                        Status = "created"
                    };
                }
                else
                {
                    var errorMessage = response.Meta != null && response.Meta.Errors != null && response.Meta.Errors.Errors != null
                        ? string.Join(", ", response.Meta.Errors.Errors.Select(m => $"{m.ErrorCode}: {m.ToString()}"))
                        : "Unknown error from Tamara";
                    
                    return new TamaraPaymentResponse
                    {
                        Success = false,
                        Message = $"Checkout creation failed: {errorMessage}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating Tamara checkout");
                return new TamaraPaymentResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Authorize Tamara order using the official SDK
        /// </summary>
        public async Task<TamaraAuthorizationResponse> AuthorizeOrderAsync(string orderId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(orderId))
                {
                    return new TamaraAuthorizationResponse
                    {
                        Success = false,
                        Message = "Order ID cannot be null or empty"
                    };
                }

                var response = await _apiClient.AuthoriseOrder(orderId);

                if (response.IsSuccess() && response.Data != null)
                {
                    return new TamaraAuthorizationResponse
                    {
                        Success = true,
                        OrderId = response.Data.OrderId,
                        Status = response.Data.OrderStatus
                    };
                }
                else
                {
                    var errorMessage = "Unknown error from Tamara";
                    var isNotFound = false;
                    
                    if (response.Meta != null && response.Meta.Errors != null && response.Meta.Errors.Errors != null)
                    {
                        errorMessage = string.Join(", ", response.Meta.Errors.Errors.Select(m => 
                            !string.IsNullOrEmpty(m.ErrorCode) 
                                ? $"{m.ErrorCode}: { m.ToString()}" 
                                : m.ToString() ?? m.ToString()));
                        
                        // Check if any error indicates NotFound
                        isNotFound = response.Meta.Errors.Errors.Any(e => 
                            (e.ErrorCode?.Contains("NotFound", StringComparison.OrdinalIgnoreCase) == true) ||
                            (e.ErrorCode?.Contains("404", StringComparison.OrdinalIgnoreCase) == true) ||
                            (e.ToString()?.Contains("NotFound", StringComparison.OrdinalIgnoreCase) == true) ||
                            (e.ToString()?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true));
                    }
                    else if (response.Meta != null && response.Meta.Errors != null && response.Meta.Errors.Message != null)
                    {
                        errorMessage = response.Meta.Errors.Message ?? string.Empty;
                        isNotFound = errorMessage.Contains("NotFound", StringComparison.OrdinalIgnoreCase) ||
                                    errorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                                    errorMessage.Contains("404", StringComparison.OrdinalIgnoreCase);
                    }
                    
                    // Check response status code if available
                    try
                    {
                        var statusCodeProp = response.GetType().GetProperty("StatusCode");
                        if (statusCodeProp != null)
                        {
                            var statusCode = statusCodeProp.GetValue(response);
                            if (statusCode != null)
                            {
                                var statusCodeValue = statusCode.ToString();
                                if (statusCodeValue == "404" || statusCodeValue == "NotFound")
                                {
                                    isNotFound = true;
                                    if (string.IsNullOrEmpty(errorMessage) || errorMessage == "Unknown error from Tamara")
                                    {
                                        errorMessage = $"Order not found in Tamara system. OrderId: {orderId}";
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore reflection errors
                    }

                    if (isNotFound)
                    {
                        _logger?.LogWarning("Tamara order not found: {OrderId}. This may indicate the order ID is incorrect or the order was not created in Tamara.", orderId);
                        errorMessage = $"Order not found in Tamara system. Please verify the order ID: {orderId}";
                    }
                    else
                    {
                        _logger?.LogWarning("Tamara authorization failed for order {OrderId}: {ErrorMessage}", orderId, errorMessage);
                    }

                    return new TamaraAuthorizationResponse
                    {
                        Success = false,
                        Message = errorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                var isNotFound = false;
                
                // Check if this is a NotFound error
                if (ex.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("404", StringComparison.OrdinalIgnoreCase) ||
                    ex.GetType().Name.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                {
                    isNotFound = true;
                }
                
                // Try to extract more detailed error information from Tamara SDK exceptions
                if (ex.GetType().FullName?.Contains("Tamara") == true)
                {
                    // Check if exception has inner exception with more details
                    if (ex.InnerException != null)
                    {
                        errorMessage = $"{ex.Message} - {ex.InnerException.Message}";
                        if (ex.InnerException.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase) ||
                            ex.InnerException.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                            ex.InnerException.Message.Contains("404", StringComparison.OrdinalIgnoreCase))
                        {
                            isNotFound = true;
                        }
                    }
                    
                    // Try to get error details from exception properties using reflection
                    try
                    {
                        var errorCodeProp = ex.GetType().GetProperty("ErrorCode");
                        var statusCodeProp = ex.GetType().GetProperty("StatusCode");
                        var errorMessageProp = ex.GetType().GetProperty("ErrorMessage") ?? ex.GetType().GetProperty("Message");
                        
                        if (errorCodeProp != null && errorCodeProp.GetValue(ex) != null)
                        {
                            var code = errorCodeProp.GetValue(ex)?.ToString();
                            var msg = errorMessageProp?.GetValue(ex)?.ToString() ?? ex.Message;
                            errorMessage = $"{code}: {msg}";
                            
                            if (code?.Contains("NotFound", StringComparison.OrdinalIgnoreCase) == true ||
                                code?.Contains("404", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                isNotFound = true;
                            }
                        }
                        
                        if (statusCodeProp != null && statusCodeProp.GetValue(ex) != null)
                        {
                            var statusCode = statusCodeProp.GetValue(ex)?.ToString();
                            if (statusCode == "404" || statusCode == "NotFound")
                            {
                                isNotFound = true;
                            }
                        }
                    }
                    catch
                    {
                        // If reflection fails, use the exception message
                    }
                }

                if (isNotFound)
                {
                    _logger?.LogWarning("Tamara order not found during authorization: {OrderId}. Error: {ErrorMessage}", orderId, errorMessage);
                    errorMessage = $"Order not found in Tamara system. Please verify the order ID: {orderId}. Error: {errorMessage}";
                }
                else
                {
                    _logger?.LogError(ex, "Error authorizing Tamara order {OrderId}: {ErrorMessage}", orderId, errorMessage);
                }
                
                return new TamaraAuthorizationResponse
                {
                    Success = false,
                    Message = $"Authorization failed: {errorMessage}"
                };
            }
        }

        /// <summary>
        /// Get order details from Tamara using the official SDK
        /// </summary>
        public async Task<TamaraOrderDetailsResponse> GetOrderDetailsAsync(string orderId)
        {
            try
            {
                var response = await _apiClient.GetOrderDetails(orderId);

                if (response.IsSuccess() && response.Data != null)
                {
                    var orderDetails = new TamaraOrderDetailsResponse
                    {
                        Success = true,
                        OrderId = response.Data.Id,
                        Status = response.Data.Status,
                        PaymentStatus = response.Data.Status
                    };
                    
                    // Try to extract refund information using reflection
                    try
                    {
                        var dataType = response.Data.GetType();
                        
                        // Try to get TotalAmount (original order amount)
                        var totalAmountProp = dataType.GetProperty("TotalAmount") ?? 
                                             dataType.GetProperty("total_amount") ??
                                             dataType.GetProperty("Amount");
                        if (totalAmountProp != null)
                        {
                            var totalAmountObj = totalAmountProp.GetValue(response.Data);
                            if (totalAmountObj != null)
                            {
                                var amountProp = totalAmountObj.GetType().GetProperty("Amount") ?? 
                                                totalAmountObj.GetType().GetProperty("amount");
                                if (amountProp != null)
                                {
                                    var amountValue = amountProp.GetValue(totalAmountObj);
                                    if (amountValue != null && decimal.TryParse(amountValue.ToString(), out var totalAmount))
                                    {
                                        orderDetails.TotalAmount = totalAmount;
                                    }
                                }
                            }
                        }
                        
                        // Try to get RefundedAmount or RemainingRefundableAmount
                        var refundedAmountProp = dataType.GetProperty("RefundedAmount") ?? 
                                                 dataType.GetProperty("refunded_amount") ??
                                                 dataType.GetProperty("TotalRefundedAmount");
                        var remainingAmountProp = dataType.GetProperty("RemainingRefundableAmount") ?? 
                                                 dataType.GetProperty("remaining_refundable_amount") ??
                                                 dataType.GetProperty("RemainingAmount");
                        
                        if (refundedAmountProp != null)
                        {
                            var refundedAmountObj = refundedAmountProp.GetValue(response.Data);
                            if (refundedAmountObj != null && decimal.TryParse(refundedAmountObj.ToString(), out var refundedAmount))
                            {
                                orderDetails.RefundedAmount = refundedAmount;
                                if (orderDetails.TotalAmount.HasValue)
                                {
                                    orderDetails.RemainingRefundableAmount = orderDetails.TotalAmount.Value - refundedAmount;
                                }
                            }
                        }
                        else if (remainingAmountProp != null)
                        {
                            var remainingAmountObj = remainingAmountProp.GetValue(response.Data);
                            if (remainingAmountObj != null)
                            {
                                // Could be a Money object
                                var amountProp = remainingAmountObj.GetType().GetProperty("Amount") ?? 
                                                remainingAmountObj.GetType().GetProperty("amount");
                                if (amountProp != null)
                                {
                                    var amountValue = amountProp.GetValue(remainingAmountObj);
                                    if (amountValue != null && decimal.TryParse(amountValue.ToString(), out var remainingAmount))
                                    {
                                        orderDetails.RemainingRefundableAmount = remainingAmount;
                                    }
                                }
                                else if (decimal.TryParse(remainingAmountObj.ToString(), out var remainingAmount))
                                {
                                    orderDetails.RemainingRefundableAmount = remainingAmount;
                                }
                            }
                        }
                        
                        // If we have TotalAmount but no refund info, assume full amount is refundable
                        if (orderDetails.TotalAmount.HasValue && !orderDetails.RemainingRefundableAmount.HasValue)
                        {
                            orderDetails.RemainingRefundableAmount = orderDetails.TotalAmount.Value;
                        }
                    }
                    catch (Exception reflectionEx)
                    {
                        _logger?.LogWarning(reflectionEx, "Could not extract refund information from Tamara order details for {OrderId}", orderId);
                        // Continue without refund information
                    }
                    
                    return orderDetails;
                }
                else
                {
                    var errorMessage = response.Meta != null && response.Meta.Errors != null && response.Meta.Errors.Errors != null
                        ? string.Join(", ", response.Meta.Errors.Errors.Select(m => $"{m.ErrorCode}: {m.ToString()}"))
                        : "Unknown error from Tamara";

                    return new TamaraOrderDetailsResponse
                    {
                        Success = false,
                        Message = errorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting Tamara order details for {OrderId}", orderId);
                return new TamaraOrderDetailsResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Capture payment for an authorized order using the official SDK
        /// </summary>
        public async Task<TamaraCaptureResponse> CaptureOrderAsync(string orderId, TamaraCaptureRequest captureRequest)
        {
            try
            {
                // Map our capture request to SDK's CaptureRequest
                var sdkCaptureRequest = new CaptureRequest
                {
                    OrderId = orderId,
                    TotalAmount = new Money
                    {
                        Amount = (float)captureRequest.TotalAmount.Amount,
                        Currency = captureRequest.TotalAmount.Currency
                    },
                    ShippingInfo = captureRequest.ShippingInfo != null ? new ShippingInfo
                    {
                        ShippedAt = DateTime.TryParse(captureRequest.ShippingInfo.ShippedAt, out var shippedDate) ? shippedDate : (DateTime?)null,
                        ShippingCompany = captureRequest.ShippingInfo.ShippingCompany
                    } : null,
                    TaxAmount = captureRequest.TaxAmount != null ? new Money
                    {
                        Amount = (float)captureRequest.TaxAmount.Amount,
                        Currency = captureRequest.TaxAmount.Currency
                    } : null,
                    ShippingAmount = captureRequest.ShippingAmount != null ? new Money
                    {
                        Amount = (float)captureRequest.ShippingAmount.Amount,
                        Currency = captureRequest.ShippingAmount.Currency
                    } : null,
                    DiscountAmount = captureRequest.Discount != null ? new Money
                    {
                        Amount = (float)captureRequest.Discount.Amount.Amount,
                        Currency = captureRequest.Discount.Amount.Currency
                    } : null,
                    Items = captureRequest.Items?.Select(item => new OrderItem
                    {
                        ReferenceId = item.ReferenceId,
                        Type = item.Type,
                        Name = item.Name,
                        Sku = item.Sku,
                        Quantity = item.Quantity,
                        UnitPrice = new Money
                        {
                            Amount = (float)item.UnitPrice.Amount,
                            Currency = item.UnitPrice.Currency
                        },
                        DiscountAmount = item.DiscountAmount != null ? new Money
                        {
                            Amount = (float)item.DiscountAmount.Amount,
                            Currency = item.DiscountAmount.Currency
                        } : null,
                        TaxAmount = item.TaxAmount != null ? new Money
                        {
                            Amount = (float)item.TaxAmount.Amount,
                            Currency = item.TaxAmount.Currency
                        } : null,
                        TotalAmount = new Money
                        {
                            Amount = (float)item.TotalAmount.Amount,
                            Currency = item.TotalAmount.Currency
                        }
                    }).ToList()
                };

                var response = await _apiClient.Capture(sdkCaptureRequest);

                if (response.IsSuccess() && response.Data != null)
                {
                    return new TamaraCaptureResponse
                    {
                        Success = true,
                        OrderId = response.Data.OrderId,
                        CaptureId = response.Data.CaptureId,
                        Status = "captured"
                    };
                }
                else
                {
                    var errorMessage = response.Meta != null && response.Meta.Errors != null && response.Meta.Errors.Errors != null
                        ? string.Join(", ", response.Meta.Errors.Errors.Select(m => $"{m.ErrorCode}: {m.ToString()}"))
                        : "Unknown error from Tamara";

                    return new TamaraCaptureResponse
                    {
                        Success = false,
                        Message = errorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error capturing Tamara order {OrderId}", orderId);
                return new TamaraCaptureResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Cancel an authorized order using the official SDK
        /// </summary>
        public async Task<TamaraCancelResponse> CancelOrderAsync(string orderId, TamaraCancelRequest cancelRequest)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(orderId))
                {
                    return new TamaraCancelResponse
                    {
                        Success = false,
                        Message = "Order ID cannot be null or empty"
                    };
                }

                // First, check the current order status to see if cancellation is allowed
                _logger?.LogInformation("Checking Tamara order status before cancellation: {OrderId}", orderId);
                var orderDetailsResponse = await GetOrderDetailsAsync(orderId);
                
                if (orderDetailsResponse.Success && !string.IsNullOrEmpty(orderDetailsResponse.Status))
                {
                    var currentStatus = orderDetailsResponse.Status.ToLower();
                    _logger?.LogInformation("Tamara order {OrderId} current status: {Status}", orderId, currentStatus);
                    
                    // Check if order is already cancelled
                    if (currentStatus.Contains("cancelled") || currentStatus.Contains("canceled"))
                    {
                        _logger?.LogWarning("Tamara order {OrderId} is already cancelled. Status: {Status}", orderId, currentStatus);
                        return new TamaraCancelResponse
                        {
                            Success = true, // Consider this a success since order is already cancelled
                            OrderId = orderId,
                            Status = SD.StatusCancelled,
                            Message = "Order is already cancelled in Tamara"
                        };
                    }
                    
                    // Check if order is in "approved" status - Tamara doesn't allow direct cancellation from approved
                    if (currentStatus.Contains("approved"))
                    {
                        _logger?.LogWarning("Tamara order {OrderId} is in approved status and cannot be cancelled directly. Status: {Status}", orderId, currentStatus);
                        return new TamaraCancelResponse
                        {
                            Success = false,
                            Message = $"Order cannot be cancelled. Current status: {orderDetailsResponse.Status}. " +
                                    "Orders in 'approved' status cannot be cancelled directly in Tamara. " +
                                    "If the order has been captured, you may need to process a refund instead. " +
                                    "Please check the order status in Tamara dashboard for available actions."
                        };
                    }
                    
                    // Check if order is in a state that doesn't allow cancellation
                    if (currentStatus.Contains("captured") || currentStatus.Contains("shipped") || 
                        currentStatus.Contains("delivered") || currentStatus.Contains("refunded"))
                    {
                        _logger?.LogWarning("Tamara order {OrderId} cannot be cancelled. Current status: {Status}", orderId, currentStatus);
                        return new TamaraCancelResponse
                        {
                            Success = false,
                            Message = $"Order cannot be cancelled. Current status: {orderDetailsResponse.Status}. " +
                                    "Orders that are captured, shipped, delivered, or refunded cannot be cancelled. " +
                                    "If the order has been captured, you may need to process a refund instead."
                        };
                    }
                }

                // Map our cancel request to SDK's CancelOrderRequest
                var sdkCancelRequest = new CancelOrderRequest
                {
                    OrderId = orderId,
                    TotalAmount = new Money
                    {
                        Amount = (float)cancelRequest.TotalAmount.Amount,
                        Currency = cancelRequest.TotalAmount.Currency
                    },
                    Items = cancelRequest.Items?.Select(item => new OrderItem
                    {
                        ReferenceId = item.ReferenceId,
                        Type = item.Type,
                        Name = item.Name,
                        Sku = item.Sku,
                        Quantity = item.Quantity,
                        UnitPrice = new Money
                        {
                            Amount = (float)item.UnitPrice.Amount,
                            Currency = item.UnitPrice.Currency
                        },
                        DiscountAmount = item.DiscountAmount != null ? new Money
                        {
                            Amount = (float)item.DiscountAmount.Amount,
                            Currency = item.DiscountAmount.Currency
                        } : null,
                        TaxAmount = item.TaxAmount != null ? new Money
                        {
                            Amount = (float)item.TaxAmount.Amount,
                            Currency = item.TaxAmount.Currency
                        } : null,
                        TotalAmount = new Money
                        {
                            Amount = (float)item.TotalAmount.Amount,
                            Currency = item.TotalAmount.Currency
                        }
                    }).ToList()
                };

                var response = await _apiClient.CancelOrder(sdkCancelRequest);

                if (response.IsSuccess() && response.Data != null)
                {
                    return new TamaraCancelResponse
                    {
                        Success = true,
                        OrderId = response.Data.OrderId,
                        CancelId = response.Data.CancelId,
                        Status = SD.StatusCancelled
                    };
                }
                else
                {
                    var errorMessage = "Unknown error from Tamara";
                    var isTransitionError = false;
                    
                    if (response.Meta != null && response.Meta.Errors != null && response.Meta.Errors.Errors != null)
                    {
                        var errors = response.Meta.Errors.Errors.ToList();
                        errorMessage = string.Join(", ", errors.Select(m => 
                            !string.IsNullOrEmpty(m.ErrorCode) 
                                ? $"{m.ErrorCode}: {m.ToString()}" 
                                : m.ToString()));
                        
                        // Check for transition_not_allowed error
                        isTransitionError = errors.Any(e => 
                            e.ErrorCode?.Contains("transition_not_allowed", StringComparison.OrdinalIgnoreCase) == true ||
                            e.ErrorCode?.Contains("conflict", StringComparison.OrdinalIgnoreCase) == true ||
                            e.ToString()?.Contains("transition_not_allowed", StringComparison.OrdinalIgnoreCase) == true ||
                            e.ToString()?.Contains("conflict", StringComparison.OrdinalIgnoreCase) == true);
                    }
                    else if (response.Meta != null && response.Meta.Errors != null && response.Meta.Errors.Message != null)
                    {
                        errorMessage = response.Meta.Errors.Message ?? string.Empty;
                        isTransitionError = errorMessage.Contains("transition_not_allowed", StringComparison.OrdinalIgnoreCase) ||
                                          errorMessage.Contains("conflict", StringComparison.OrdinalIgnoreCase);
                    }

                    if (isTransitionError)
                    {
                        _logger?.LogWarning("Tamara order {OrderId} cancellation failed: transition not allowed. Error: {ErrorMessage}", orderId, errorMessage);
                        
                        // Check if the error specifically mentions "approved" status
                        var isApprovedStatusError = errorMessage.Contains("approved", StringComparison.OrdinalIgnoreCase) ||
                                                   errorMessage.Contains("cannot be moved to the status canceled from approved", StringComparison.OrdinalIgnoreCase);
                        
                        if (isApprovedStatusError)
                        {
                            errorMessage = $"Order cannot be cancelled. The order is in 'approved' status and Tamara does not allow direct cancellation from this status. " +
                                         $"If the order has been captured, you may need to process a refund instead. " +
                                         $"Please check the order status in Tamara dashboard for available actions. " +
                                         $"Error details: {errorMessage}";
                        }
                        else
                        {
                            errorMessage = $"Order cannot be cancelled. The order is in a state that does not allow cancellation. " +
                                         $"This usually means the order has already been cancelled, captured, shipped, or refunded. " +
                                         $"Error details: {errorMessage}";
                        }
                    }
                    else
                    {
                        _logger?.LogWarning("Tamara order {OrderId} cancellation failed: {ErrorMessage}", orderId, errorMessage);
                    }

                    return new TamaraCancelResponse
                    {
                        Success = false,
                        Message = errorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                var isTransitionError = false;
                
                // Check if exception indicates transition_not_allowed
                if (ex.Message.Contains("transition_not_allowed", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase) ||
                    ex.GetType().Name.Contains("Conflict", StringComparison.OrdinalIgnoreCase))
                {
                    isTransitionError = true;
                }
                
                // Try to extract more details from Tamara SDK exceptions
                if (ex.GetType().FullName?.Contains("Tamara") == true)
                {
                    if (ex.InnerException != null)
                    {
                        errorMessage = $"{ex.Message} - {ex.InnerException.Message}";
                        if (ex.InnerException.Message.Contains("transition_not_allowed", StringComparison.OrdinalIgnoreCase) ||
                            ex.InnerException.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase))
                        {
                            isTransitionError = true;
                        }
                    }
                    
                    // Try to get error details from exception properties
                    try
                    {
                        var errorCodeProp = ex.GetType().GetProperty("ErrorCode");
                        var statusCodeProp = ex.GetType().GetProperty("StatusCode");
                        
                        if (errorCodeProp != null && errorCodeProp.GetValue(ex) != null)
                        {
                            var code = errorCodeProp.GetValue(ex)?.ToString();
                            if (code?.Contains("transition_not_allowed", StringComparison.OrdinalIgnoreCase) == true ||
                                code?.Contains("conflict", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                isTransitionError = true;
                            }
                        }
                        
                        if (statusCodeProp != null && statusCodeProp.GetValue(ex) != null)
                        {
                            var statusCode = statusCodeProp.GetValue(ex)?.ToString();
                            if (statusCode == "409" || statusCode == "Conflict")
                            {
                                isTransitionError = true;
                            }
                        }
                    }
                    catch
                    {
                        // If reflection fails, use the exception message
                    }
                }

                if (isTransitionError)
                {
                    _logger?.LogWarning("Tamara order {OrderId} cancellation failed: transition not allowed. Error: {ErrorMessage}", orderId, errorMessage);
                    
                    // Check if the error specifically mentions "approved" status
                    var isApprovedStatusError = errorMessage.Contains("approved", StringComparison.OrdinalIgnoreCase) ||
                                               errorMessage.Contains("cannot be moved to the status canceled from approved", StringComparison.OrdinalIgnoreCase);
                    
                    if (isApprovedStatusError)
                    {
                        errorMessage = $"Order cannot be cancelled. The order is in 'approved' status and Tamara does not allow direct cancellation from this status. " +
                                     $"If the order has been captured, you may need to process a refund instead. " +
                                     $"Please check the order status in Tamara dashboard for available actions. " +
                                     $"Error: {errorMessage}";
                    }
                    else
                    {
                        errorMessage = $"Order cannot be cancelled. The order is in a state that does not allow cancellation. " +
                                     $"This usually means the order has already been cancelled, captured, shipped, or refunded. " +
                                     $"Error: {errorMessage}";
                    }
                }
                else
                {
                    _logger?.LogError(ex, "Error cancelling Tamara order {OrderId}: {ErrorMessage}", orderId, errorMessage);
                }
                
                return new TamaraCancelResponse
                {
                    Success = false,
                    Message = $"Cancellation failed: {errorMessage}"
                };
            }
        }

        /// <summary>
        /// Refund a captured order using the official SDK
        /// </summary>
        public async Task<TamaraRefundResponse> RefundOrderAsync(string orderId, TamaraRefundRequest refundRequest)
        {
            try
            {
                // Map our refund request to SDK's RefundRequest
                var sdkRefundRequest = new RefundRequest
                {
                    OrderId = orderId,
                    TotalAmount = new Money
                    {
                        Amount = (float)refundRequest.TotalAmount.Amount,
                        Currency = refundRequest.TotalAmount.Currency
                    },
                    Comment = refundRequest.Comment
                };

                var response = await _apiClient.Refund(sdkRefundRequest);

                if (response.IsSuccess() && response.Data != null)
                {
                    return new TamaraRefundResponse
                    {
                        Success = true,
                        RefundId = response.Data.RefundId,
                        CaptureId = response.Data.CaptureId,
                        Status = "refunded"
                    };
                }
                else
                {
                    var errorMessage = response.Meta != null && response.Meta.Errors != null && response.Meta.Errors.Errors != null
                        ? string.Join(", ", response.Meta.Errors.Errors.Select(m => $"{m.ErrorCode}: {m.ToString()}"))
                        : "Unknown error from Tamara";
                    
                    // Extract more detailed error information
                    if (response.Meta != null && response.Meta.Errors != null && response.Meta.Errors.Errors != null)
                    {
                        var errorCodes = response.Meta.Errors.Errors.Select(m => m.ErrorCode?.ToString() ?? "").ToList();
                        var errorMessages = response.Meta.Errors.Errors.Select(m => 
                        {
                            var msg = m.ToString();
                            // Remove raw type names from error messages
                            if (msg.Contains("Tamara.Net.SDK.Models.Exception.ErrorResult"))
                            {
                                msg = msg.Replace("Tamara.Net.SDK.Models.Exception.ErrorResult", "").Trim();
                                if (msg.StartsWith(":"))
                                {
                                    msg = msg.Substring(1).Trim();
                                }
                            }
                            return msg;
                        }).ToList();
                        
                        // Check for specific refund amount error
                        if (errorCodes.Any(c => c.Contains("refund_amount_greater_than_remaining_captured_amount", StringComparison.OrdinalIgnoreCase)) ||
                            errorMessages.Any(m => m.Contains("refund_amount_greater_than_remaining_captured_amount", StringComparison.OrdinalIgnoreCase)))
                        {
                            // Provide a user-friendly message instead of raw error code
                            errorMessage = "The refund amount exceeds the remaining refundable amount. " +
                                         "This order may have already been partially refunded.";
                        }
                        else
                        {
                            // Use cleaned error messages
                            errorMessage = string.Join(", ", errorMessages.Where(m => !string.IsNullOrWhiteSpace(m)));
                            if (string.IsNullOrWhiteSpace(errorMessage))
                            {
                                errorMessage = string.Join(", ", errorCodes.Where(c => !string.IsNullOrWhiteSpace(c)));
                            }
                        }
                    }
                    
                    // Clean up final error message
                    if (errorMessage.Contains("Tamara.Net.SDK.Models.Exception.ErrorResult"))
                    {
                        errorMessage = errorMessage.Replace("Tamara.Net.SDK.Models.Exception.ErrorResult", "").Trim();
                        if (errorMessage.StartsWith(":"))
                        {
                            errorMessage = errorMessage.Substring(1).Trim();
                        }
                    }

                    return new TamaraRefundResponse
                    {
                        Success = false,
                        Message = errorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error refunding Tamara order {OrderId}", orderId);
                
                var errorMessage = ex.Message;
                
                // Try to extract more details from Tamara SDK exceptions
                if (ex.GetType().FullName?.Contains("Tamara") == true)
                {
                    try
                    {
                        var errorCodeProp = ex.GetType().GetProperty("ErrorCode");
                        var errorMessageProp = ex.GetType().GetProperty("ErrorMessage") ?? ex.GetType().GetProperty("Message");
                        
                        if (errorCodeProp != null && errorCodeProp.GetValue(ex) != null)
                        {
                            var code = errorCodeProp.GetValue(ex)?.ToString();
                            var msg = errorMessageProp?.GetValue(ex)?.ToString() ?? ex.Message;
                            
                            // Check for refund amount error
                            if (code?.Contains("refund_amount_greater_than_remaining_captured_amount", StringComparison.OrdinalIgnoreCase) == true ||
                                msg?.Contains("refund_amount_greater_than_remaining_captured_amount", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                errorMessage = "The refund amount exceeds the remaining refundable amount. " +
                                             "This order may have already been partially refunded.";
                            }
                            else if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(msg))
                            {
                                errorMessage = $"{code}: {msg}";
                            }
                            else if (!string.IsNullOrEmpty(msg))
                            {
                                errorMessage = msg;
                            }
                        }
                    }
                    catch
                    {
                        // If reflection fails, use the exception message
                    }
                }
                
                // Check if this is the refund amount error (fallback)
                if (errorMessage == ex.Message && ex.Message != null && 
                    (ex.Message.Contains("refund_amount_greater_than_remaining_captured_amount", StringComparison.OrdinalIgnoreCase) ||
                     (ex.Message.Contains("remaining", StringComparison.OrdinalIgnoreCase) && 
                      ex.Message.Contains("refund", StringComparison.OrdinalIgnoreCase))))
                {
                    errorMessage = "The refund amount exceeds the remaining refundable amount. " +
                                 "This order may have already been partially refunded.";
                }
                
                // Clean up error message - remove raw type names
                if (errorMessage.Contains("Tamara.Net.SDK.Models.Exception.ErrorResult"))
                {
                    errorMessage = errorMessage.Replace("Tamara.Net.SDK.Models.Exception.ErrorResult", "").Trim();
                    if (errorMessage.StartsWith(":"))
                    {
                        errorMessage = errorMessage.Substring(1).Trim();
                    }
                    
                    if (string.IsNullOrWhiteSpace(errorMessage))
                    {
                        errorMessage = "An error occurred while processing the refund with Tamara.";
                    }
                }
                
                return new TamaraRefundResponse
                {
                    Success = false,
                    Message = errorMessage
                };
            }
        }

        /// <summary>
        /// Check if Tamara payment is available for a given amount using the official SDK
        /// </summary>
        public async Task<bool> IsPaymentAvailableAsync(decimal amount, string countryCode = "AE")
        {
            try
            {
                var response = await _apiClient.PaymentOptions(new PaymentOptionsRequest
                {
                    Country = countryCode,
                    OrderValue = new Money
                    {
                        Amount = (float)amount,
                        Currency = _settings.Currency ?? "AED"
                    }
                });

                return response.IsSuccess() && response.Data != null && response.Data.HasAvailablePaymentOptions;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking Tamara payment availability");
                return false;
            }
        }

        /// <summary>
        /// Map our TamaraPaymentRequest to SDK's Order model
        /// </summary>
        private Order MapToSdkOrder(TamaraPaymentRequest request)
        {
            var order = new Order
            {
                ReferenceId = request.OrderReferenceId,
                TotalAmount = new Money
                {
                    Amount = (float)request.TotalAmount.Amount,
                    Currency = request.TotalAmount.Currency
                },
                Description = request.Description,
                CountryCode = request.CountryCode,
                PaymentType = request.PaymentType,
                Instalments = request.Instalments,
                Locale = request.Locale,
                Platform = request.Platform,
                IsMobile = request.IsMobile ?? false,
                MerchantUrl = new MerchantUrl
                {
                    SuccessUrl = request.MerchantUrl.Success,
                    FailureUrl = request.MerchantUrl.Failure,
                    CancelUrl = request.MerchantUrl.Cancel,
                    NotificationUrl = request.MerchantUrl.Notification
                },
                Consumer = new Consumer
                {
                    FirstName = request.Consumer.FirstName,
                    LastName = request.Consumer.LastName,
                    PhoneNumber = request.Consumer.PhoneNumber,
                    Email = request.Consumer.Email
                },
                Items = request.Items.Select(item => new OrderItem
                {
                    ReferenceId = item.ReferenceId,
                    Type = item.Type,
                    Name = item.Name,
                    Sku = item.Sku,
                    Quantity = item.Quantity,
                    UnitPrice = new Money
                    {
                        Amount = (float)item.UnitPrice.Amount,
                        Currency = item.UnitPrice.Currency
                    },
                    DiscountAmount = item.DiscountAmount != null ? new Money
                    {
                        Amount = (float)item.DiscountAmount.Amount,
                        Currency = item.DiscountAmount.Currency
                    } : null,
                    TaxAmount = item.TaxAmount != null ? new Money
                    {
                        Amount = (float)item.TaxAmount.Amount,
                        Currency = item.TaxAmount.Currency
                    } : null,
                    TotalAmount = new Money
                    {
                        Amount = (float)item.TotalAmount.Amount,
                        Currency = item.TotalAmount.Currency
                    }
                }).ToList()
            };

            // Add optional fields
            if (request.BillingAddress != null)
            {
                order.BillingAddress = new Address
                {
                    FirstName = request.BillingAddress.FirstName,
                    LastName = request.BillingAddress.LastName,
                    Line1 = request.BillingAddress.Line1,
                    Line2 = request.BillingAddress.Line2,
                    City = request.BillingAddress.City,
                    Region = request.BillingAddress.Region,
                    PostalCode = request.BillingAddress.PostalCode,
                    CountryCode = request.BillingAddress.CountryCode,
                    PhoneNumber = request.BillingAddress.PhoneNumber
                };
            }

            if (request.ShippingAddress != null)
            {
                order.ShippingAddress = new Address
                {
                    FirstName = request.ShippingAddress.FirstName,
                    LastName = request.ShippingAddress.LastName,
                    Line1 = request.ShippingAddress.Line1,
                    Line2 = request.ShippingAddress.Line2,
                    City = request.ShippingAddress.City,
                    Region = request.ShippingAddress.Region,
                    PostalCode = request.ShippingAddress.PostalCode,
                    CountryCode = request.ShippingAddress.CountryCode,
                    PhoneNumber = request.ShippingAddress.PhoneNumber
                };
            }

            if (request.TaxAmount != null)
            {
                order.TaxAmount = new Money
                {
                    Amount = (float)request.TaxAmount.Amount,
                    Currency = request.TaxAmount.Currency
                };
            }

            if (request.ShippingAmount != null)
            {
                order.ShippingAmount = new Money
                {
                    Amount = (float)request.ShippingAmount.Amount,
                    Currency = request.ShippingAmount.Currency
                };
            }

            if (request.Discount != null)
            {
                order.DiscountAmount = new Discount
                {
                    Name = request.Discount.Name,
                    Amount = new Money
                    {
                        Amount = (float)request.Discount.Amount.Amount,
                        Currency = request.Discount.Amount.Currency
                    }
                };
            }

            return order;
        }
    }

    // Request/Response Models for Tamara (kept for backward compatibility)
    public class TamaraPaymentRequest
    {
        [JsonPropertyName("order_reference_id")]
        public string OrderReferenceId { get; set; }

        [JsonPropertyName("order_number")]
        public string? OrderNumber { get; set; }

        [JsonPropertyName("total_amount")]
        public TamaraAmount TotalAmount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; } = "AE";

        [JsonPropertyName("payment_type")]
        public string PaymentType { get; set; } = "PAY_BY_INSTALMENTS";

        [JsonPropertyName("instalments")]
        public int? Instalments { get; set; }

        [JsonPropertyName("locale")]
        public string Locale { get; set; } = "en_US";

        [JsonPropertyName("platform")]
        public string? Platform { get; set; }

        [JsonPropertyName("is_mobile")]
        public bool? IsMobile { get; set; }

        [JsonPropertyName("merchant_url")]
        public TamaraMerchantUrl MerchantUrl { get; set; }

        [JsonPropertyName("consumer")]
        public TamaraConsumer Consumer { get; set; }

        [JsonPropertyName("billing_address")]
        public TamaraAddress BillingAddress { get; set; }

        [JsonPropertyName("shipping_address")]
        public TamaraAddress ShippingAddress { get; set; }

        [JsonPropertyName("items")]
        public List<TamaraItem> Items { get; set; }

        [JsonPropertyName("tax_amount")]
        public TamaraAmount TaxAmount { get; set; }

        [JsonPropertyName("shipping_amount")]
        public TamaraAmount ShippingAmount { get; set; }

        [JsonPropertyName("discount")]
        public TamaraDiscount? Discount { get; set; }
    }

    public class TamaraDiscount
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("amount")]
        public TamaraAmount Amount { get; set; }
    }

    public class TamaraAmount
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "AED";
    }

    public class TamaraMerchantUrl
    {
        [JsonPropertyName("success")]
        public string Success { get; set; }

        [JsonPropertyName("failure")]
        public string Failure { get; set; }

        [JsonPropertyName("cancel")]
        public string Cancel { get; set; }

        [JsonPropertyName("notification")]
        public string Notification { get; set; }
    }

    public class TamaraConsumer
    {
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }
    }

    public class TamaraItem
    {
        [JsonPropertyName("reference_id")]
        public string ReferenceId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "Physical";

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("sku")]
        public string? Sku { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("unit_price")]
        public TamaraAmount UnitPrice { get; set; }

        [JsonPropertyName("discount_amount")]
        public TamaraAmount DiscountAmount { get; set; }

        [JsonPropertyName("tax_amount")]
        public TamaraAmount TaxAmount { get; set; }

        [JsonPropertyName("total_amount")]
        public TamaraAmount TotalAmount { get; set; }
    }

    public class TamaraPaymentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        [JsonPropertyName("checkout_url")]
        public string CheckoutUrl { get; set; }

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; }

        [JsonPropertyName("checkout_id")]
        public string CheckoutId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    public class TamaraAuthorizationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    public class TamaraOrderDetailsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("payment_status")]
        public string PaymentStatus { get; set; }
        
        [JsonPropertyName("total_amount")]
        public decimal? TotalAmount { get; set; }
        
        [JsonPropertyName("refunded_amount")]
        public decimal? RefundedAmount { get; set; }
        
        [JsonPropertyName("remaining_refundable_amount")]
        public decimal? RemainingRefundableAmount { get; set; }
    }

    // Capture Request/Response Models
    public class TamaraCaptureRequest
    {
        [JsonPropertyName("total_amount")]
        public TamaraAmount TotalAmount { get; set; }

        [JsonPropertyName("shipping_info")]
        public TamaraShippingInfo ShippingInfo { get; set; }

        [JsonPropertyName("tax_amount")]
        public TamaraAmount TaxAmount { get; set; }

        [JsonPropertyName("shipping_amount")]
        public TamaraAmount ShippingAmount { get; set; }

        [JsonPropertyName("discount")]
        public TamaraDiscount? Discount { get; set; }

        [JsonPropertyName("items")]
        public List<TamaraItem> Items { get; set; }
    }

    public class TamaraShippingInfo
    {
        [JsonPropertyName("shipped_at")]
        public string ShippedAt { get; set; }

        [JsonPropertyName("shipping_company")]
        public string? ShippingCompany { get; set; }
    }

    public class TamaraCaptureResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; }

        [JsonPropertyName("capture_id")]
        public string CaptureId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    // Cancel Request/Response Models
    public class TamaraCancelRequest
    {
        [JsonPropertyName("total_amount")]
        public TamaraAmount TotalAmount { get; set; }

        [JsonPropertyName("items")]
        public List<TamaraItem> Items { get; set; }
    }

    public class TamaraCancelResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; }

        [JsonPropertyName("cancel_id")]
        public string CancelId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    // Refund Request/Response Models
    public class TamaraRefundRequest
    {
        [JsonPropertyName("total_amount")]
        public TamaraAmount TotalAmount { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }
    }

    public class TamaraRefundResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        [JsonPropertyName("refund_id")]
        public string RefundId { get; set; }

        [JsonPropertyName("capture_id")]
        public string CaptureId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    // Billing/Shipping Address Model
    public class TamaraAddress
    {
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("line1")]
        public string Line1 { get; set; }

        [JsonPropertyName("line2")]
        public string? Line2 { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("postal_code")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; }

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }
    }
}
