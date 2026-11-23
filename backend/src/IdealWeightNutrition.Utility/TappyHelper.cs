using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace IdealWeightNutrition.Utility
{
    public class TappyHelper
    {
        private readonly TappySettings _settings;
        private readonly HttpClient _httpClient;

        public TappyHelper(TappySettings settings)
        {
            _settings = settings;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            // Don't set headers in constructor - set them per request to ensure ASCII-only
        }
        
        /// <summary>
        /// Ensures a string contains only ASCII characters for use in HTTP headers
        /// </summary>
        private static string EnsureAsciiOnly(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            
            // Remove any non-ASCII characters (keep only characters 0-127)
            return new string(value.Where(c => c <= 0x7F).ToArray());
        }
        
        /// <summary>
        /// Checks if a URL is likely a checkout/payment URL (not an image or product URL)
        /// </summary>
        private static bool IsCheckoutUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return false;
            
            var urlLower = url.ToLowerInvariant();
            
            // Exclude image URLs and localhost URLs (which are likely from our own domain)
            if (urlLower.Contains("/images/") || 
                urlLower.Contains(".jpg") || 
                urlLower.Contains(".jpeg") || 
                urlLower.Contains(".png") || 
                urlLower.Contains(".gif") || 
                urlLower.Contains(".webp") ||
                urlLower.Contains("/images/products/") ||
                urlLower.Contains("image_url") ||
                urlLower.Contains("product_url") ||
                urlLower.Contains("localhost") ||
                urlLower.Contains("127.0.0.1") ||
                urlLower.Contains("/products/") ||
                urlLower.StartsWith("http://localhost") ||
                urlLower.StartsWith("https://localhost"))
            {
                return false;
            }
            
            // Prefer URLs that look like checkout/payment URLs
            if (urlLower.Contains("tabby") ||
                urlLower.Contains("checkout") ||
                urlLower.Contains("payment") ||
                urlLower.Contains("pay.") ||
                urlLower.Contains("web_url"))
            {
                return true;
            }
            
            // If URL is from Tabby domain, it's likely a checkout URL
            if (urlLower.Contains("tabby.ai") || urlLower.Contains("tabby.tech"))
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Recursively searches for a checkout URL in a JSON element
        /// Only returns URLs that are clearly checkout/payment URLs (excludes image URLs)
        /// </summary>
        private static string? FindUrlInJsonElement(System.Text.Json.JsonElement element, int depth = 0, int maxDepth = 5)
        {
            if (depth > maxDepth)
                return null;
            
            // Check all properties
            if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    // Only check properties that might contain checkout URLs
                    var propNameLower = prop.Name.ToLowerInvariant();
                    if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var urlValue = prop.Value.GetString();
                        if (!string.IsNullOrEmpty(urlValue) && 
                            (urlValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                             urlValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                        {
                            // Only return if it's clearly a checkout URL (not an image)
                            if (IsCheckoutUrl(urlValue))
                            {
                                return urlValue;
                            }
                        }
                    }
                    
                    // Skip known non-checkout properties
                    if (propNameLower.Contains("image") || 
                        propNameLower.Contains("product") ||
                        propNameLower == "description" ||
                        propNameLower == "title")
                    {
                        continue; // Skip these properties to avoid finding image URLs
                    }
                    
                    // Recursively search nested objects
                    var foundUrl = FindUrlInJsonElement(prop.Value, depth + 1, maxDepth);
                    if (!string.IsNullOrEmpty(foundUrl) && IsCheckoutUrl(foundUrl))
                    {
                        return foundUrl;
                    }
                }
            }
            
            // Check arrays (but skip items that are likely product data)
            if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var foundUrl = FindUrlInJsonElement(item, depth + 1, maxDepth);
                    if (!string.IsNullOrEmpty(foundUrl) && IsCheckoutUrl(foundUrl))
                    {
                        return foundUrl;
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// Create a Tabby checkout session
        /// </summary>
        public async Task<TappyPaymentResponse> CreatePaymentAsync(TappyPaymentRequest request)
        {
            try
            {
                // Clean phone number - remove non-digits
                var cleanPhone = string.IsNullOrEmpty(request.CustomerPhone) 
                    ? "500000001" 
                    : new string(request.CustomerPhone.Where(char.IsDigit).ToArray());
                
                if (string.IsNullOrEmpty(cleanPhone))
                    cleanPhone = "500000001";

                // Build Tabby API request structure - matching exact format from documentation
                var tabbyRequest = new Dictionary<string, object>
                {
                    ["payment"] = new Dictionary<string, object>
                    {
                        ["amount"] = request.Amount.ToString("F2", CultureInfo.InvariantCulture),
                        ["currency"] = request.Currency,
                        ["description"] = request.Description ?? $"Order {request.OrderId}",
                        ["buyer"] = new Dictionary<string, object>
                        {
                            ["name"] = request.CustomerName ?? "Customer",
                            ["email"] = request.CustomerEmail ?? "",
                            ["phone"] = cleanPhone,
                            ["dob"] = request.BuyerDateOfBirth ?? "1990-01-20"
                        },
                        ["shipping_address"] = new Dictionary<string, object>
                        {
                            ["city"] = request.ShippingCity ?? "Dubai",
                            ["address"] = request.ShippingAddress ?? "Dubai",
                            ["zip"] = request.ShippingPostalCode ?? "1111"
                        },
                        ["order"] = new Dictionary<string, object>
                        {
                            ["reference_id"] = request.OrderId,
                            ["updated_at"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                            ["tax_amount"] = (request.TaxAmount ?? 0).ToString("F2", CultureInfo.InvariantCulture),
                            ["shipping_amount"] = (request.ShippingAmount ?? 0).ToString("F2", CultureInfo.InvariantCulture),
                            ["discount_amount"] = (request.DiscountAmount ?? 0).ToString("F2", CultureInfo.InvariantCulture),
                            ["items"] = request.Items?.Select(item => new Dictionary<string, object>
                            {
                                ["reference_id"] = item.ReferenceId,
                                ["title"] = item.Title ?? "Product", // Required
                                ["description"] = item.Description ?? item.Title ?? "Product",
                                ["quantity"] = item.Quantity, // Required
                                ["unit_price"] = item.UnitPrice.ToString("F2", CultureInfo.InvariantCulture), // Required
                                ["discount_amount"] = (item.DiscountAmount ?? 0).ToString("F2", CultureInfo.InvariantCulture),
                                ["image_url"] = item.ImageUrl ?? "https://example.com/",
                                ["product_url"] = item.ProductUrl ?? "https://example.com/",
                                ["category"] = item.Category ?? "General" // Required
                            }).ToArray() ?? Array.Empty<Dictionary<string, object>>()
                        }
                    },
                    ["lang"] = request.Language ?? "en",
                    ["merchant_code"] = _settings.MerchantCode,
                    ["merchant_urls"] = new Dictionary<string, object>
                    {
                        ["success"] = request.ReturnUrl,
                        ["cancel"] = request.CancelUrl,
                        ["failure"] = request.CancelUrl
                    }
                };
                
                // Add token only if provided (optional field)
                if (!string.IsNullOrEmpty(request.Token))
                {
                    tabbyRequest["token"] = request.Token;
                }

                // Don't use PropertyNamingPolicy for Dictionary - keys are used as-is (already in snake_case)
                var options = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = false
                };

                // Validate merchant code
                if (string.IsNullOrEmpty(_settings.MerchantCode))
                {
                    return new TappyPaymentResponse
                    {
                        Success = false,
                        Message = "MerchantCode is required. Please configure it in appsettings.json"
                    };
                }
                
                // Validate items - Tabby requires at least one item with required fields
                if (request.Items == null || !request.Items.Any())
                {
                    return new TappyPaymentResponse
                    {
                        Success = false,
                        Message = "At least one order item is required for Tabby checkout"
                    };
                }
                
                // Validate required item fields
                foreach (var item in request.Items)
                {
                    if (string.IsNullOrWhiteSpace(item.Title))
                    {
                        return new TappyPaymentResponse
                        {
                            Success = false,
                            Message = "Item title is required for all order items"
                        };
                    }
                    if (item.Quantity <= 0)
                    {
                        return new TappyPaymentResponse
                        {
                            Success = false,
                            Message = "Item quantity must be greater than 0"
                        };
                    }
                    if (string.IsNullOrWhiteSpace(item.Category))
                    {
                        return new TappyPaymentResponse
                        {
                            Success = false,
                            Message = "Item category is required for all order items"
                        };
                    }
                }

                var json = JsonSerializer.Serialize(tabbyRequest, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // Create a new request message with clean headers to ensure ASCII-only
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v2/checkout")
                {
                    Content = content
                };
                
                // Set Authorization header with ASCII-only values
                var apiKey = EnsureAsciiOnly(_settings.ApiKey ?? string.Empty);
                if (!string.IsNullOrEmpty(apiKey))
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                var response = await _httpClient.SendAsync(requestMessage);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Log error details for debugging
                if (!response.IsSuccessStatusCode)
                {
                    // Try to parse error response
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(responseContent);
                        var errorRoot = errorDoc.RootElement;
                        var errorMessage = errorRoot.TryGetProperty("error", out var errorProp) 
                            ? errorProp.GetString() 
                            : responseContent;
                        var errorType = errorRoot.TryGetProperty("errorType", out var errorTypeProp) 
                            ? errorTypeProp.GetString() 
                            : "unknown";
                        
                        return new TappyPaymentResponse
                        {
                            Success = false,
                            Message = $"Payment creation failed ({errorType}): {errorMessage}. Request JSON: {json.Substring(0, Math.Min(500, json.Length))}..."
                        };
                    }
                    catch
                    {
                        return new TappyPaymentResponse
                        {
                            Success = false,
                            Message = $"Payment creation failed: {responseContent}. Status: {response.StatusCode}"
                        };
                    }
                }

                if (response.IsSuccessStatusCode)
                {
                    // Parse Tabby response according to API documentation
                    // Response structure may vary: { id, configuration: { available_products: { installments: [{ web_url }] } }, payment: { id }, status }
                    using var doc = JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;
                    
                    // Get status first - check if payment was rejected
                    var status = root.TryGetProperty("status", out var statusProp) 
                        ? statusProp.GetString() 
                        : "created";
                    
                    // Check for rejection status
                    if (status?.ToLowerInvariant() == "rejected")
                    {
                        // Get rejection reason
                        var rejectionReasonCode = root.TryGetProperty("rejection_reason_code", out var rejectionCodeProp) 
                            ? rejectionCodeProp.GetString() 
                            : null;
                        
                        var rejectionReason = root.TryGetProperty("rejection_reason", out var rejectionReasonProp) 
                            ? rejectionReasonProp.GetString() 
                            : null;
                        
                        // Check configuration for detailed rejection reason
                        string? detailedReason = null;
                        if (root.TryGetProperty("configuration", out var rejectionConfigProp))
                        {
                            if (rejectionConfigProp.TryGetProperty("products", out var productsProp))
                            {
                                if (productsProp.TryGetProperty("installments", out var installmentsProp))
                                {
                                    if (installmentsProp.TryGetProperty("rejection_reason", out var installRejectionProp))
                                    {
                                        detailedReason = installRejectionProp.GetString();
                                    }
                                }
                            }
                        }
                        
                        // Build user-friendly error message
                        var errorMessage = "Tabby payment was rejected";
                        if (!string.IsNullOrEmpty(detailedReason))
                        {
                            errorMessage = detailedReason switch
                            {
                                "order_amount_too_low" => "Order amount is too low for Tabby payment. Minimum order amount is required.",
                                _ => $"Tabby payment rejected: {detailedReason}"
                            };
                        }
                        else if (!string.IsNullOrEmpty(rejectionReasonCode))
                        {
                            errorMessage = rejectionReasonCode switch
                            {
                                "under_limit" => "Order amount is below Tabby's minimum limit. Please add more items to your cart.",
                                _ => $"Tabby payment rejected: {rejectionReasonCode}"
                            };
                        }
                        else if (!string.IsNullOrEmpty(rejectionReason))
                        {
                            errorMessage = $"Tabby payment rejected: {rejectionReason}";
                        }
                        
                        // Get session ID and payment ID for reference
                        var rejectedSessionId = root.TryGetProperty("id", out var rejectedIdProp) ? rejectedIdProp.GetString() : null;
                        var rejectedPaymentId = root.TryGetProperty("payment", out var rejectedPaymentProp) 
                            && rejectedPaymentProp.TryGetProperty("id", out var rejectedPaymentIdProp)
                            ? rejectedPaymentIdProp.GetString() 
                            : null;
                        
                        return new TappyPaymentResponse
                        {
                            Success = false,
                            Message = errorMessage,
                            TransactionId = rejectedPaymentId ?? rejectedSessionId,
                            Status = status ?? "rejected"
                        };
                    }
                    
                    // Get session ID
                    var sessionId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    
                    // Get payment ID from payment object
                    var paymentId = root.TryGetProperty("payment", out var paymentProp) 
                        && paymentProp.TryGetProperty("id", out var paymentIdProp)
                        ? paymentIdProp.GetString() 
                        : null;
                    
                    // Try multiple paths to find checkout URL
                    string? checkoutUrl = null;
                    
                    // Path 1: Check top-level web_url or url property
                    if (root.TryGetProperty("web_url", out var topWebUrl))
                    {
                        var url = topWebUrl.GetString();
                        if (IsCheckoutUrl(url))
                        {
                            checkoutUrl = url;
                        }
                    }
                    else if (root.TryGetProperty("url", out var topUrl))
                    {
                        var url = topUrl.GetString();
                        if (IsCheckoutUrl(url))
                        {
                            checkoutUrl = url;
                        }
                    }
                    else if (root.TryGetProperty("checkout_url", out var topCheckoutUrl))
                    {
                        var url = topCheckoutUrl.GetString();
                        if (IsCheckoutUrl(url))
                        {
                            checkoutUrl = url;
                        }
                    }
                    
                    // Path 2: configuration.available_products.installments[0].web_url
                    if (string.IsNullOrEmpty(checkoutUrl) && root.TryGetProperty("configuration", out var configProp))
                    {
                        if (configProp.TryGetProperty("available_products", out var availableProductsProp))
                        {
                            // Check if available_products is an object with installments array
                            if (availableProductsProp.TryGetProperty("installments", out var installmentsProp))
                            {
                                if (installmentsProp.ValueKind == JsonValueKind.Array && installmentsProp.GetArrayLength() > 0)
                                {
                                    var firstInstallment = installmentsProp[0];
                                    if (firstInstallment.TryGetProperty("web_url", out var webUrlProp))
                                    {
                                        var url = webUrlProp.GetString();
                                        if (IsCheckoutUrl(url))
                                        {
                                            checkoutUrl = url;
                                        }
                                    }
                                }
                            }
                            // Also check if available_products itself has web_url
                            if (string.IsNullOrEmpty(checkoutUrl) && availableProductsProp.TryGetProperty("web_url", out var apWebUrl))
                            {
                                var url = apWebUrl.GetString();
                                if (IsCheckoutUrl(url))
                                {
                                    checkoutUrl = url;
                                }
                            }
                        }
                        // Check configuration level for URL
                        if (string.IsNullOrEmpty(checkoutUrl) && configProp.TryGetProperty("web_url", out var configWebUrl))
                        {
                            var url = configWebUrl.GetString();
                            if (IsCheckoutUrl(url))
                            {
                                checkoutUrl = url;
                            }
                        }
                    }
                    
                    // Path 3: Check payment object for URL
                    if (string.IsNullOrEmpty(checkoutUrl) && root.TryGetProperty("payment", out var paymentObj))
                    {
                        if (paymentObj.TryGetProperty("web_url", out var paymentWebUrl))
                        {
                            var url = paymentWebUrl.GetString();
                            if (IsCheckoutUrl(url))
                            {
                                checkoutUrl = url;
                            }
                        }
                        else if (paymentObj.TryGetProperty("url", out var paymentUrl))
                        {
                            var url = paymentUrl.GetString();
                            if (IsCheckoutUrl(url))
                            {
                                checkoutUrl = url;
                            }
                        }
                    }
                    
                    // Path 4: Try to find URL recursively, but ONLY checkout/payment URLs (excludes image URLs)
                    // This is a last resort - we prefer specific paths above
                    if (string.IsNullOrEmpty(checkoutUrl))
                    {
                        checkoutUrl = FindUrlInJsonElement(root);
                    }
                    
                    // Note: status was already retrieved above to check for rejection
                    // Reuse that status value here if needed, or get it again
                    var finalStatus = root.TryGetProperty("status", out var statusProp2) 
                        ? statusProp2.GetString() 
                        : status ?? "created";
                    
                    // Log response structure for debugging if URL is null
                    if (string.IsNullOrEmpty(checkoutUrl))
                    {
                        // Return detailed error with full response for debugging
                        // This will help us see what Tabby is actually returning
                        var errorDetails = responseContent.Length > 4000 
                            ? responseContent.Substring(0, 4000) + "..." 
                            : responseContent;
                        
                        return new TappyPaymentResponse
                        {
                            Success = false,
                            Message = $"Payment created successfully (Status: {finalStatus}, SessionId: {sessionId ?? "null"}, PaymentId: {paymentId ?? "null"}) but checkout URL not found in response. Please check Tabby API response structure. Full response: {errorDetails}",
                            TransactionId = paymentId ?? sessionId,
                            Status = finalStatus ?? "created"
                        };
                    }
                    
                    var paymentResponse = new TappyPaymentResponse
                    {
                        Success = true,
                        PaymentUrl = checkoutUrl,
                        TransactionId = paymentId ?? sessionId, // Prefer payment ID, fallback to session ID
                        Status = finalStatus ?? "created"
                    };

                    return paymentResponse;
                }
                else
                {
                    return new TappyPaymentResponse
                    {
                        Success = false,
                        Message = $"Payment creation failed: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new TappyPaymentResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Verify Tabby payment status
        /// Note: This endpoint requires the payment ID (from payment.id), not the session ID.
        /// This may return 401 if API key doesn't have verification permissions.
        /// Payment status should be verified via callback URL parameters instead.
        /// </summary>
        public async Task<TappyPaymentStatusResponse> VerifyPaymentAsync(string paymentId)
        {
            try
            {
                if (string.IsNullOrEmpty(paymentId))
                {
                    return new TappyPaymentStatusResponse 
                    { 
                        Success = false, 
                        Message = "Payment ID is required for verification" 
                    };
                }

                // Tabby API endpoint: GET /api/v2/payments/{payment_id}
                // Note: This requires the payment ID (from payment.id), not the session ID (from id)
                // IMPORTANT: Verification endpoint requires the secret key (MerchantId), not the public key (ApiKey)
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/payments/{paymentId}");
                
                // Set Authorization header with secret key (MerchantId) for verification
                // The verification endpoint requires secret key, not public key
                var secretKey = EnsureAsciiOnly(_settings.MerchantId ?? string.Empty);
                if (string.IsNullOrEmpty(secretKey))
                {
                    return new TappyPaymentStatusResponse 
                    { 
                        Success = false, 
                        Message = "MerchantId (secret key) is not configured. Verification endpoint requires secret key." 
                    };
                }
                
                // Remove leading colon if present (common configuration mistake)
                secretKey = secretKey.TrimStart(':');
                
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
                
                var response = await _httpClient.SendAsync(requestMessage);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;
                    
                    // Extract status from response
                    // Tabby returns: { "status": "CLOSED", "authorized", "created", etc. }
                    var status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;
                    var statusLower = status?.ToLowerInvariant() ?? "";
                    
                    // Check if payment is authorized/paid
                    // Tabby statuses: CLOSED, AUTHORIZED, CREATED, APPROVED indicate successful payment
                    var isPaid = statusLower == "authorized" || 
                                 statusLower == "created" || 
                                 statusLower == "approved" ||
                                 statusLower == "closed";
                    
                    var statusResponse = new TappyPaymentStatusResponse
                    {
                        Success = true,
                        Status = status,
                        TransactionId = paymentId,
                        IsPaid = isPaid,
                        Message = isPaid ? "Payment verified successfully" : $"Payment status: {status}"
                    };

                    return statusResponse;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // 401 Unauthorized - API key may not have verification permissions
                    // This is acceptable - payment status should be verified via callback URL parameters
                    return new TappyPaymentStatusResponse 
                    { 
                        Success = false, 
                        Message = $"API verification failed (401 Unauthorized). The API key may not have permission to access /api/v2/payments endpoint. Payment ID used: {paymentId}. Use callback URL parameters for payment status instead."
                    };
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // 404 Not Found - Payment ID doesn't exist
                    return new TappyPaymentStatusResponse 
                    { 
                        Success = false,
                        Message = $"Payment not found (404). Payment ID: {paymentId}"
                    };
                }
                else
                {
                    // Other error
                    var errorDetails = responseContent.Length > 500 
                        ? responseContent.Substring(0, 500) + "..." 
                        : responseContent;
                    
                    return new TappyPaymentStatusResponse 
                    { 
                        Success = false, 
                        Message = $"Verification failed: {response.StatusCode}. Response: {errorDetails}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new TappyPaymentStatusResponse 
                { 
                    Success = false, 
                    Message = $"Error verifying payment: {ex.Message}" 
				};
			}
		}

		/// <summary>
		/// Cancel a Tabby payment (before capture)
		/// Note: This only works for authorized but not yet captured payments
		/// Cancels the customer's installment plan
		/// </summary>
		public async Task<TappyCancelResponse> CancelPaymentAsync(string paymentId)
		{
			try
			{
				if (string.IsNullOrEmpty(paymentId))
				{
					return new TappyCancelResponse
					{
						Success = false,
						Message = "Payment ID is required for cancellation"
					};
				}

				// Tabby cancel endpoint: POST /api/v2/payments/{payment_id}/cancel
				var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/v2/payments/{paymentId}/cancel");
				
				var apiKey = EnsureAsciiOnly(_settings.ApiKey ?? string.Empty);
				if (string.IsNullOrEmpty(apiKey))
				{
					return new TappyCancelResponse
					{
						Success = false,
						Message = "API key is not configured"
					};
				}
				
				requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
				requestMessage.Headers.Accept.Clear();
				requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				var response = await _httpClient.SendAsync(requestMessage);
				var responseContent = await response.Content.ReadAsStringAsync();

				if (response.IsSuccessStatusCode)
				{
					using var doc = JsonDocument.Parse(responseContent);
					var root = doc.RootElement;
					
					var status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;
					
					return new TappyCancelResponse
					{
						Success = true,
						Message = "Payment cancelled successfully",
						TransactionId = paymentId,
						Status = status ?? "cancelled"
					};
				}
				else
				{
					// Try to parse error response
					string errorMessage = "Unknown error";
					try
					{
						using var errorDoc = JsonDocument.Parse(responseContent);
						var errorRoot = errorDoc.RootElement;
						errorMessage = errorRoot.TryGetProperty("error", out var errorProp) 
							? errorProp.GetString() 
							: errorRoot.TryGetProperty("message", out var msgProp)
							? msgProp.GetString()
							: responseContent;
					}
					catch
					{
						errorMessage = responseContent;
					}

					return new TappyCancelResponse
					{
						Success = false,
						Message = $"Cancellation failed: {errorMessage} (Status: {response.StatusCode})"
					};
				}
			}
			catch (Exception ex)
			{
				return new TappyCancelResponse
				{
					Success = false,
					Message = $"Error cancelling payment: {ex.Message}"
				};
			}
		}

		/// <summary>
		/// Refund a Tabby payment (after capture)
		/// Note: This only works for captured/paid payments
		/// Supports partial refunds
		/// </summary>
		public async Task<TappyRefundResponse> RefundPaymentAsync(string paymentId, decimal refundAmount, string currency = "AED", string reason = null)
		{
			try
			{
				if (string.IsNullOrEmpty(paymentId))
				{
					return new TappyRefundResponse
					{
						Success = false,
						Message = "Payment ID is required for refund"
					};
				}

				if (refundAmount <= 0)
				{
					return new TappyRefundResponse
					{
						Success = false,
						Message = "Refund amount must be greater than 0"
					};
				}

				// Tabby refund endpoint: POST /api/v2/payments/{payment_id}/refunds
				var refundRequest = new Dictionary<string, object>
				{
					["amount"] = refundAmount.ToString("F2", CultureInfo.InvariantCulture),
					["currency"] = currency
				};

				if (!string.IsNullOrEmpty(reason))
				{
					refundRequest["reason"] = reason;
				}

				var options = new JsonSerializerOptions
				{
					DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
					WriteIndented = false
				};

				var json = JsonSerializer.Serialize(refundRequest, options);
				var content = new StringContent(json, Encoding.UTF8, "application/json");
				
				var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/v2/payments/{paymentId}/refunds")
				{
					Content = content
				};
				
				var apiKey = EnsureAsciiOnly(_settings.ApiKey ?? string.Empty);
				if (string.IsNullOrEmpty(apiKey))
				{
					return new TappyRefundResponse
					{
						Success = false,
						Message = "API key is not configured"
					};
				}
				
				requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
				requestMessage.Headers.Accept.Clear();
				requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				var response = await _httpClient.SendAsync(requestMessage);
				var responseContent = await response.Content.ReadAsStringAsync();

				if (response.IsSuccessStatusCode)
				{
					using var doc = JsonDocument.Parse(responseContent);
					var root = doc.RootElement;
					
					var refundId = root.TryGetProperty("id", out var refundIdProp) 
						? refundIdProp.GetString() 
						: null;
					var status = root.TryGetProperty("status", out var statusProp) 
						? statusProp.GetString() 
						: null;
					
					return new TappyRefundResponse
					{
						Success = true,
						Message = "Refund processed successfully",
						RefundId = refundId,
						TransactionId = paymentId,
						RefundAmount = refundAmount,
						Status = status ?? "refunded"
					};
				}
				else
				{
					// Try to parse error response
					string errorMessage = "Unknown error";
					try
					{
						using var errorDoc = JsonDocument.Parse(responseContent);
						var errorRoot = errorDoc.RootElement;
						errorMessage = errorRoot.TryGetProperty("error", out var errorProp) 
							? errorProp.GetString() 
							: errorRoot.TryGetProperty("message", out var msgProp)
							? msgProp.GetString()
							: responseContent;
					}
					catch
					{
						errorMessage = responseContent;
					}

					return new TappyRefundResponse
					{
						Success = false,
						Message = $"Refund failed: {errorMessage} (Status: {response.StatusCode})"
					};
				}
			}
			catch (Exception ex)
			{
				return new TappyRefundResponse
				{
					Success = false,
					Message = $"Error processing refund: {ex.Message}"
				};
			}
		}
    }

    // Request/Response Models
    public class TappyPaymentRequest
    {
        public string MerchantId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "AED";
        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string ReturnUrl { get; set; }
        public string CancelUrl { get; set; }
        public string Description { get; set; }
        
        // Additional Tabby-specific fields
        public string? BuyerDateOfBirth { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingAddress { get; set; }
        public string? ShippingPostalCode { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? ShippingAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? Language { get; set; }
        public List<TabbyOrderItem>? Items { get; set; }
        public string? Token { get; set; } // Optional token for tokenized payments
    }

    public class TabbyOrderItem
    {
        public string ReferenceId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? ImageUrl { get; set; }
        public string? ProductUrl { get; set; }
        public string? Category { get; set; }
    }

    public class TappyPaymentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string PaymentUrl { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; }
    }

    public class TappyPaymentStatusResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string TransactionId { get; set; }
        public bool IsPaid { get; set; }
    }

    public class TappyCancelResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; }
    }

    public class TappyRefundResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string RefundId { get; set; }
        public string TransactionId { get; set; }
        public decimal RefundAmount { get; set; }
        public string Status { get; set; }
    }
}
