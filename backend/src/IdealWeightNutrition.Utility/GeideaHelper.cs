using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Globalization;

namespace IdealWeightNutrition.Utility
{
	public class GeideaHelper
	{
		private readonly GeideaSettings _settings;
		private readonly HttpClient _httpClient;

		public GeideaHelper(GeideaSettings settings)
		{
			_settings = settings;
			_httpClient = new HttpClient();
		}

		private string GetBaseUrl()
		{
			if (!string.IsNullOrEmpty(_settings.BaseUrl))
			{
				return _settings.BaseUrl;
			}
			
			return "https://api.geidea.ae";
		}

		private string GetEndpoint()
		{
			return "/payment-intent/api/v2/direct/session";
		}

		/// <summary>
		/// Extracts the order amount from a Geidea order element.
		/// Handles both numeric and string representations.
		/// Returns null when no amount field is found or cannot be parsed.
		/// </summary>
		private static decimal? TryGetOrderAmount(System.Text.Json.JsonElement order)
		{
			foreach (var name in new[] { "amount", "totalAmount", "orderAmount" })
			{
				if (!order.TryGetProperty(name, out var amountProp))
					continue;

				if (amountProp.ValueKind == System.Text.Json.JsonValueKind.Number &&
					amountProp.TryGetDecimal(out var amount))
				{
					return amount;
				}

				if (amountProp.ValueKind == System.Text.Json.JsonValueKind.String &&
					decimal.TryParse(amountProp.GetString(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
				{
					return parsed;
				}
			}

			return null;
		}

        /// <summary>
        /// Generate Geidea signature using HMAC SHA256
        /// According to working example: merchantPublicKey + amount + currency + merchantReferenceId + timestamp
        /// Amount is in smallest currency unit (cents for AED, e.g., 1850 for 18.50 AED)
        /// </summary>
        static string GenerateSignature(string merchantPublicKey, decimal orderAmount, string orderCurrency, string? orderMerchantReferenceId, string apiPassword, string timeStamp)
        {
            var amountStr = orderAmount.ToString("F2", CultureInfo.InvariantCulture);
            var data = $"{merchantPublicKey}{amountStr}{orderCurrency}{orderMerchantReferenceId}{timeStamp}";
            using var hmacSha256 = new HMACSHA256(Encoding.UTF8.GetBytes(apiPassword));
            var hash = hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);

        }

        /// <summary>
        /// Create a Geidea payment session/order
        /// </summary>
        public async Task<GeideaPaymentResponse> CreatePaymentAsync(GeideaPaymentRequest request)
		{
			try
			{
				if (string.IsNullOrEmpty(_settings.MerchantPublicKey) || string.IsNullOrEmpty(_settings.MerchantApiPassword))
				{
					return new GeideaPaymentResponse
					{
						Success = false,
						Message = "Geidea credentials are not configured. Please check MerchantPublicKey and MerchantApiPassword in appsettings.json"
					};
				}

				decimal amountInSmallestUnit = (request.Amount);
				string currency = request.Currency ?? "AED";
				
				DateTime timestampDate = DateTimeHelper.Now;
				string timestamp = timestampDate.ToString("M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);
				
				string signature = GenerateSignature(_settings.MerchantPublicKey, amountInSmallestUnit, currency, request.OrderId, _settings.MerchantApiPassword, timestamp);
				
				var callbackUrl = request.ReturnUrl ?? request.CancelUrl ?? "";
				
				if (string.IsNullOrEmpty(callbackUrl))
				{
					return new GeideaPaymentResponse
					{
						Success = false,
						Message = "Callback URL is required for Geidea payment"
					};
				}
				
				
				var geideaRequest = new Dictionary<string, object>
				{
					["amount"] = amountInSmallestUnit,
					["currency"] = currency,
					["timestamp"] = timestamp,
					["merchantReferenceId"] = request.OrderId,
					["signature"] = signature,
					["paymentOperation"] = "Pay",
					["callbackUrl"] = callbackUrl
				};
				
				if (!string.IsNullOrEmpty(request.Language))
				{
					geideaRequest["language"] = request.Language;
				}
				else
				{
					geideaRequest["language"] = "en";  
				}
				
				geideaRequest["appearance"] = new Dictionary<string, object>
				{
					["uiMode"] = "modal"
				};
				
				if (!string.IsNullOrEmpty(request.ReturnUrl))
				{
					geideaRequest["returnUrl"] = request.ReturnUrl;
				}
				
				if (!string.IsNullOrEmpty(request.CustomerEmail) || !string.IsNullOrEmpty(request.CustomerPhone))
				{
					var customer = new Dictionary<string, object>();
					if (!string.IsNullOrEmpty(request.CustomerEmail))
						customer["email"] = request.CustomerEmail;
					if (!string.IsNullOrEmpty(request.CustomerPhone))
					{
						customer["phoneNumber"] = request.CustomerPhone;
						if (request.CustomerPhone.StartsWith("+"))
						{
							var phoneParts = request.CustomerPhone.Split(' ');
							if (phoneParts.Length > 0)
							{
								customer["phoneCountryCode"] = phoneParts[0];
							}
						}
					}
					if (customer.Count > 0)
						geideaRequest["customer"] = customer;
				}
				
				geideaRequest["initiatedBy"] = "Internet";

				var options = new JsonSerializerOptions
				{
					DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
					WriteIndented = false
				};

				var json = JsonSerializer.Serialize(geideaRequest, options);
				var content = new StringContent(json, Encoding.UTF8, "application/json");

				var publicKey = (_settings.MerchantPublicKey ?? "").Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");
				var apiPassword = (_settings.MerchantApiPassword ?? "").Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");
				
				if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(apiPassword))
				{
					return new GeideaPaymentResponse
					{
						Success = false,
						Message = "Geidea credentials are empty. Please check MerchantPublicKey and MerchantApiPassword in appsettings.json"
					};
				}
				
				if (publicKey.Length != 36 || !publicKey.Contains("-"))
				{
					return new GeideaPaymentResponse
					{
						Success = false,
						Message = $"Geidea MerchantPublicKey format appears invalid. Expected UUID format (36 characters with hyphens). Got: {publicKey.Substring(0, Math.Min(20, publicKey.Length))}..."
					};
				}
				
				var credentials = $"{publicKey}:{apiPassword}";
				var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
				
				string baseUrl = GetBaseUrl();
				string endpoint = GetEndpoint();
				string fullUrl = $"{baseUrl}{endpoint}";
				var requestMessage = new HttpRequestMessage(HttpMethod.Post, fullUrl)
				{
					Content = content
				};
				requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
				requestMessage.Headers.Accept.Clear();
				requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				var response = await _httpClient.SendAsync(requestMessage);
				var responseContent = await response.Content.ReadAsStringAsync();

				if (!response.IsSuccessStatusCode)
				{
					if (responseContent.Contains("<html>") || responseContent.Contains("Request Rejected"))
					{
						string callbackUrlNote = callbackUrl.Contains("localhost") 
							? " NOTE: Geidea does NOT allow localhost callback URLs. Use ngrok or a public URL for testing."
							: "";
						
						return new GeideaPaymentResponse
						{
							Success = false,
							Message = $"Payment creation failed: Request was rejected by server (Status: {response.StatusCode}).{callbackUrlNote} " +
									  $"Request URL: {fullUrl}. " +
									  $"Response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}"
						};
					}

					if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
					{
						try
						{
							using var errorDoc = JsonDocument.Parse(responseContent);
							var errorRoot = errorDoc.RootElement;
							var errorMessage = errorRoot.TryGetProperty("detailedResponseMessage", out var msgProp)
								? msgProp.GetString()
								: errorRoot.TryGetProperty("responseMessage", out var respMsgProp)
								? respMsgProp.GetString()
								: errorRoot.TryGetProperty("message", out var msgProp2)
								? msgProp2.GetString()
								: responseContent;

							return new GeideaPaymentResponse
							{
								Success = false,
								Message = $"Geidea Authentication Failed (401 Unauthorized): {errorMessage}. "
							};
						}
						catch
						{
							return new GeideaPaymentResponse
							{
								Success = false,
								Message = $"Geidea Authentication Failed (401 Unauthorized): {responseContent}. " +
										  $"Please verify your MerchantPublicKey and MerchantApiPassword are correct."
							};
						}
					}

					try
					{
						using var errorDoc = JsonDocument.Parse(responseContent);
						var errorRoot = errorDoc.RootElement;
						var errorMessage = errorRoot.TryGetProperty("detailedResponseMessage", out var msgProp)
							? msgProp.GetString()
							: errorRoot.TryGetProperty("responseMessage", out var respMsgProp)
							? respMsgProp.GetString()
							: errorRoot.TryGetProperty("message", out var msgProp2)
							? msgProp2.GetString()
							: responseContent;

						return new GeideaPaymentResponse
						{
							Success = false,
							Message = $"Payment creation failed: {errorMessage}. Status: {response.StatusCode}"
						};
					}
					catch
					{
						return new GeideaPaymentResponse
						{
							Success = false,
							Message = $"Payment creation failed: {responseContent}. Status: {response.StatusCode}"
						};
					}
				}

				using var doc = JsonDocument.Parse(responseContent);
				var root = doc.RootElement;

				var responseCode = root.TryGetProperty("responseCode", out var codeProp) ? codeProp.GetString() : null;
				var detailedResponseCode = root.TryGetProperty("detailedResponseCode", out var detailCodeProp) ? detailCodeProp.GetString() : null;
				
				if (responseCode != "000" || detailedResponseCode != "000")
				{
					var errorMessage = root.TryGetProperty("detailedResponseMessage", out var msgProp)
						? msgProp.GetString()
						: root.TryGetProperty("responseMessage", out var respMsgProp)
						? respMsgProp.GetString()
						: "Unknown error";
					
					return new GeideaPaymentResponse
					{
						Success = false,
						Message = $"Geidea API Error: {errorMessage} (Code: {responseCode}, Detail: {detailedResponseCode})"
					};
				}

				string redirectUrl = null;
				string sessionId = null;
				string orderId = null;

				if (root.TryGetProperty("redirectUrl", out var redirectUrlProp))
				{
					redirectUrl = redirectUrlProp.GetString();
				}
				else if (root.TryGetProperty("checkoutUrl", out var checkoutUrlProp))
				{
					redirectUrl = checkoutUrlProp.GetString();
				}
				else if (root.TryGetProperty("paymentUrl", out var paymentUrlProp))
				{
					redirectUrl = paymentUrlProp.GetString();
				}
				else if (root.TryGetProperty("url", out var urlProp))
				{
					redirectUrl = urlProp.GetString();
				}

				if (root.TryGetProperty("session", out var sessionProp))
				{
					if (sessionProp.TryGetProperty("id", out var sessionIdProp))
					{
						sessionId = sessionIdProp.GetString();
					}
				}
				
				if (string.IsNullOrEmpty(sessionId) && root.TryGetProperty("sessionId", out var sessionIdDirectProp))
				{
					sessionId = sessionIdDirectProp.GetString();
				}

				if (root.TryGetProperty("orderId", out var orderIdProp))
				{
					orderId = orderIdProp.GetString();
				}

				if (string.IsNullOrEmpty(redirectUrl) && !string.IsNullOrEmpty(sessionId))
				{
					string apiBaseUrl = GetBaseUrl();
					string checkoutBaseUrl;
					
					if (apiBaseUrl.Contains("ksamerchant"))
					{
						checkoutBaseUrl = "https://www.ksamerchant.geidea.net/hpp/checkout";
					}
					else if (apiBaseUrl.Contains("geidea.ae", StringComparison.OrdinalIgnoreCase))
					{
						// UAE accounts (api.geidea.ae) use the UAE hosted payment page.
						checkoutBaseUrl = "https://payments.geidea.ae/hpp/checkout";
					}
					else
					{
						checkoutBaseUrl = "https://www.merchant.geidea.net/hpp/checkout";
					}
					
					redirectUrl = $"{checkoutBaseUrl}/?sessionId={sessionId}";
				}

				if (string.IsNullOrEmpty(redirectUrl) && string.IsNullOrEmpty(sessionId))
				{
					return new GeideaPaymentResponse
					{
						Success = false,
						Message = $"Payment session created but neither redirectUrl nor session ID found in response. Response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}"
					};
				}

				if (string.IsNullOrEmpty(redirectUrl) && !string.IsNullOrEmpty(sessionId))
				{
					redirectUrl = $"https://www.merchant.geidea.net/hpp/checkout/?sessionId={sessionId}";
				}

				return new GeideaPaymentResponse
				{
					Success = true,
					PaymentUrl = redirectUrl ?? "", 
					TransactionId = sessionId ?? orderId ?? "", 
					Status = "Created"
				};
			}
			catch (Exception ex)
			{
				return new GeideaPaymentResponse
				{
					Success = false,
					Message = $"Error creating payment: {ex.Message}"
				};
			}
		}
 

		/// <summary>
		/// Verify Geidea payment status using merchant reference ID (order ID)
		/// Uses the correct Geidea API endpoint: /pgw/api/v1/direct/order?MerchantReferenceId={id}
		/// </summary>
		public async Task<GeideaPaymentStatusResponse> VerifyPaymentAsync(string merchantReferenceId)
		{
			try
			{
				if (string.IsNullOrEmpty(merchantReferenceId))
				{
					return new GeideaPaymentStatusResponse
					{
						Success = false,
						Message = "Merchant reference ID (Order ID) is required for verification"
					};
				}

				var credentials = $"{_settings.MerchantPublicKey}:{_settings.MerchantApiPassword}";
				var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
				
				string baseUrl = GetBaseUrl();
				
				string fullUrl = $"{baseUrl}/pgw/api/v1/direct/order?MerchantReferenceId={Uri.EscapeDataString(merchantReferenceId)}";
				
				var requestMessage = new HttpRequestMessage(HttpMethod.Get, fullUrl);
				requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
				requestMessage.Headers.Accept.Clear();
				requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				var response = await _httpClient.SendAsync(requestMessage);
				var responseContent = await response.Content.ReadAsStringAsync();

				if (response.IsSuccessStatusCode)
				{
					using var doc = JsonDocument.Parse(responseContent);
					var root = doc.RootElement;

					var responseCode = root.TryGetProperty("responseCode", out var rcProp) ? rcProp.GetString() : null;
					var detailedResponseCode = root.TryGetProperty("detailedResponseCode", out var drcProp) ? drcProp.GetString() : null;
					
					if (responseCode != "000" || detailedResponseCode != "000")
					{
						// Extract detailed error message
						var errorMessage = root.TryGetProperty("detailedResponseMessage", out var drmProp) 
							? drmProp.GetString() 
							: root.TryGetProperty("responseMessage", out var rmProp) 
							? rmProp.GetString() 
							: $"API returned error code: {responseCode}/{detailedResponseCode}";
						
						return new GeideaPaymentStatusResponse
						{
							Success = false,
							Message = errorMessage,
							Status = responseCode
						};
					}

					if (!root.TryGetProperty("orders", out var ordersProp) || ordersProp.ValueKind != JsonValueKind.Array)
					{
						return new GeideaPaymentStatusResponse
						{
							Success = false,
							Message = "No orders found in response",
							Status = "NoOrders"
						};
					}

				bool isPaid = false;
				string orderStatus = null;
				string orderId = null;
				string detailedStatus = null;
				string failureReason = null;
				decimal? paidAmount = null;
				
				foreach (var order in ordersProp.EnumerateArray())
				{
					var status = order.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;
					detailedStatus = order.TryGetProperty("detailedStatus", out var detailedStatusProp) ? detailedStatusProp.GetString() : null;
					var currentOrderId = order.TryGetProperty("orderId", out var oidProp) ? oidProp.GetString() : null;
					var currentAmount = TryGetOrderAmount(order);
					
					// Extract failure reason if available
					if (!string.IsNullOrEmpty(status) && !status.Equals("Success", StringComparison.OrdinalIgnoreCase))
					{
						failureReason = order.TryGetProperty("responseMessage", out var respMsgProp) 
							? respMsgProp.GetString() 
							: order.TryGetProperty("detailedResponseMessage", out var detRespMsgProp) 
							? detRespMsgProp.GetString() 
							: null;
					}
					
					if (status?.Equals("Success", StringComparison.OrdinalIgnoreCase) == true ||
						detailedStatus?.Equals("Paid", StringComparison.OrdinalIgnoreCase) == true)
					{
						isPaid = true;
						orderStatus = status ?? detailedStatus;
						orderId = currentOrderId;
						paidAmount = currentAmount;
						break; // Use the first (most recent) successful order
					}
					
					if (string.IsNullOrEmpty(orderStatus))
					{
						orderStatus = status ?? detailedStatus;
						orderId = currentOrderId;
						paidAmount = currentAmount;
					}
				}

					// Build detailed message for non-paid status
					string message;
					if (isPaid)
					{
						message = "Payment verified successfully";
					}
					else
					{
						var statusParts = new List<string>();
						if (!string.IsNullOrEmpty(orderStatus))
							statusParts.Add($"Status: {orderStatus}");
						if (!string.IsNullOrEmpty(detailedStatus) && detailedStatus != orderStatus)
							statusParts.Add($"Detailed Status: {detailedStatus}");
						if (!string.IsNullOrEmpty(failureReason))
							statusParts.Add($"Reason: {failureReason}");
						
						message = statusParts.Count > 0 
							? string.Join(". ", statusParts) 
							: $"Payment status: {orderStatus ?? "Unknown"}";
					}

					return new GeideaPaymentStatusResponse
					{
						Success = true,
						Status = orderStatus ?? detailedStatus,
						TransactionId = orderId ?? merchantReferenceId,
						IsPaid = isPaid,
						PaidAmount = isPaid ? paidAmount : null,
						Message = message
					};
				}
				else
				{
					// Try to extract error message from response body
					string errorMessage = $"Verification failed: {response.StatusCode}";
					try
					{
						if (!string.IsNullOrEmpty(responseContent))
						{
							using var doc = JsonDocument.Parse(responseContent);
							var root = doc.RootElement;
							
							var detailedMsg = root.TryGetProperty("detailedResponseMessage", out var drmProp) 
								? drmProp.GetString() 
								: null;
							var responseMsg = root.TryGetProperty("responseMessage", out var rmProp) 
								? rmProp.GetString() 
								: null;
							
							if (!string.IsNullOrEmpty(detailedMsg))
								errorMessage = detailedMsg;
							else if (!string.IsNullOrEmpty(responseMsg))
								errorMessage = responseMsg;
							else
								errorMessage = $"{errorMessage}. Response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}";
						}
					}
					catch
					{
						// If parsing fails, include raw response (truncated)
						errorMessage = $"{errorMessage}. Response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}";
					}
					
					return new GeideaPaymentStatusResponse
					{
						Success = false,
						Message = errorMessage,
						Status = response.StatusCode.ToString()
					};
				}
			}
			catch (Exception ex)
			{
				return new GeideaPaymentStatusResponse
				{
					Success = false,
					Message = $"Error verifying payment: {ex.Message}"
				};
			}
		}

		/// <summary>
		/// Cancel a Geidea payment (before capture)
		/// Note: This only works for authorized but not yet captured payments
		/// </summary>
		public async Task<GeideaCancelResponse> CancelPaymentAsync(string orderId, string transactionId = null, string reason = "CancelledByUser")
		{
			try
			{
				if (string.IsNullOrEmpty(orderId) && string.IsNullOrEmpty(transactionId))
				{
					return new GeideaCancelResponse
					{
						Success = false,
						Message = "Order ID or Transaction ID is required for cancellation"
					};
				}

				// Geidea cancel endpoint: POST /pgw/api/v1/direct/cancel
				// Use transaction ID if available, otherwise use order ID
				var identifier = !string.IsNullOrEmpty(transactionId) ? transactionId : orderId;
				
				var credentials = $"{_settings.MerchantPublicKey}:{_settings.MerchantApiPassword}";
				var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
				
				string baseUrl = GetBaseUrl();
				// Use the correct cancel endpoint format
				string fullUrl = $"{baseUrl}/pgw/api/v1/direct/cancel";
				
				// Create request body with orderId and reason
				var requestBody = new
				{
					orderId = identifier,
					reason = reason ?? "CancelledByUser"
				};
				
				var jsonBody = JsonSerializer.Serialize(requestBody);
				var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
				
				var requestMessage = new HttpRequestMessage(HttpMethod.Post, fullUrl)
				{
					Content = content
				};
				
				requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
				requestMessage.Headers.Accept.Clear();
				requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				var response = await _httpClient.SendAsync(requestMessage);
				var responseContent = await response.Content.ReadAsStringAsync();

				if (response.IsSuccessStatusCode)
				{
					using var doc = JsonDocument.Parse(responseContent);
					var root = doc.RootElement;

					var responseCode = root.TryGetProperty("responseCode", out var rcProp) ? rcProp.GetString() : null;
					var detailedResponseCode = root.TryGetProperty("detailedResponseCode", out var drcProp) ? drcProp.GetString() : null;
					
					if (responseCode == "000" && detailedResponseCode == "000")
					{
						return new GeideaCancelResponse
						{
							Success = true,
							Message = "Payment cancelled successfully",
							TransactionId = identifier
						};
					}
					else
					{
						var errorMessage = root.TryGetProperty("detailedResponseMessage", out var msgProp)
							? msgProp.GetString()
							: root.TryGetProperty("responseMessage", out var respMsgProp)
							? respMsgProp.GetString()
							: "Unknown error";
						
						return new GeideaCancelResponse
						{
							Success = false,
							Message = $"Cancellation failed: {errorMessage} (Code: {responseCode}, Detail: {detailedResponseCode})"
						};
					}
				}
				else
				{
					// Check for NotFound (404) or other specific status codes
					if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
					{
						// Try to extract error message from response
						string errorMessage = "NotFound";
						try
						{
							if (!string.IsNullOrEmpty(responseContent))
							{
								using var doc = JsonDocument.Parse(responseContent);
								var root = doc.RootElement;
								
								if (root.TryGetProperty("detailedResponseMessage", out var msgProp))
								{
									errorMessage = msgProp.GetString() ?? "NotFound";
								}
								else if (root.TryGetProperty("responseMessage", out var respMsgProp))
								{
									errorMessage = respMsgProp.GetString() ?? "NotFound";
								}
								else if (root.TryGetProperty("message", out var messageProp))
								{
									errorMessage = messageProp.GetString() ?? "NotFound";
								}
							}
						}
						catch
						{
							// If parsing fails, use default message
						}
						
						return new GeideaCancelResponse
						{
							Success = false,
							Message = $"Cancellation failed: {errorMessage}. Response: {responseContent}"
						};
					}
					
					return new GeideaCancelResponse
					{
						Success = false,
						Message = $"Cancellation failed: {response.StatusCode}. Response: {responseContent}"
					};
				}
			}
			catch (Exception ex)
			{
				return new GeideaCancelResponse
				{
					Success = false,
					Message = $"Error cancelling payment: {ex.Message}"
				};
			}
		}

		/// <summary>
		/// Refund a Geidea payment (after capture)
		/// Note: This only works for captured/paid payments
		/// </summary>
		public async Task<GeideaRefundResponse> RefundPaymentAsync(string orderId, decimal refundAmount, string currency = "AED", string transactionId = null, string reason = null)
		{
			try
			{
				if (string.IsNullOrEmpty(orderId) && string.IsNullOrEmpty(transactionId))
				{
					return new GeideaRefundResponse
					{
						Success = false,
						Message = "Order ID or Transaction ID is required for refund"
					};
				}

				if (refundAmount <= 0)
				{
					return new GeideaRefundResponse
					{
						Success = false,
						Message = "Refund amount must be greater than 0"
					};
				}

				var identifier = !string.IsNullOrEmpty(transactionId) ? transactionId : orderId;
				
				var credentials = $"{_settings.MerchantPublicKey}:{_settings.MerchantApiPassword}";
				var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
				
				string baseUrl = GetBaseUrl();
				string fullUrl = $"{baseUrl}/pgw/api/v1/direct/order/{Uri.EscapeDataString(identifier)}/refund";
				
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
				
				var requestMessage = new HttpRequestMessage(HttpMethod.Post, fullUrl)
				{
					Content = content
				};
				requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
				requestMessage.Headers.Accept.Clear();
				requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				var response = await _httpClient.SendAsync(requestMessage);
				var responseContent = await response.Content.ReadAsStringAsync();

				if (response.IsSuccessStatusCode)
				{
					using var doc = JsonDocument.Parse(responseContent);
					var root = doc.RootElement;

					var responseCode = root.TryGetProperty("responseCode", out var rcProp) ? rcProp.GetString() : null;
					var detailedResponseCode = root.TryGetProperty("detailedResponseCode", out var drcProp) ? drcProp.GetString() : null;
					
					if (responseCode == "000" && detailedResponseCode == "000")
					{
						var refundId = root.TryGetProperty("refundId", out var refundIdProp) 
							? refundIdProp.GetString() 
							: null;
						
						return new GeideaRefundResponse
						{
							Success = true,
							Message = "Refund processed successfully",
							RefundId = refundId,
							TransactionId = identifier,
							RefundAmount = refundAmount
						};
					}
					else
					{
						var errorMessage = root.TryGetProperty("detailedResponseMessage", out var msgProp)
							? msgProp.GetString()
							: root.TryGetProperty("responseMessage", out var respMsgProp)
							? respMsgProp.GetString()
							: "Unknown error";
						
						return new GeideaRefundResponse
						{
							Success = false,
							Message = $"Refund failed: {errorMessage} (Code: {responseCode}, Detail: {detailedResponseCode})"
						};
					}
				}
				else
				{
					return new GeideaRefundResponse
					{
						Success = false,
						Message = $"Refund failed: {response.StatusCode}. Response: {responseContent}"
					};
				}
			}
			catch (Exception ex)
			{
				return new GeideaRefundResponse
				{
					Success = false,
					Message = $"Error processing refund: {ex.Message}"
				};
			}
		}
	
	}

	// Request/Response Models
	public class GeideaPaymentRequest
	{
		public decimal Amount { get; set; }
		public string Currency { get; set; } = "AED";
		public string OrderId { get; set; }
		public string CustomerName { get; set; }
		public string CustomerEmail { get; set; }
		public string CustomerPhone { get; set; }
		public string ReturnUrl { get; set; }
		public string CancelUrl { get; set; }
		public string? BillingAddress { get; set; }
		public string? BillingCity { get; set; }
		public string? BillingState { get; set; }
		public string? BillingPostalCode { get; set; }
		public string? BillingCountryCode { get; set; }
		public string? Language { get; set; }
		public string? PaymentOperation { get; set; }
		public List<GeideaOrderItem>? Items { get; set; }
	}

	public class GeideaOrderItem
	{
		public string Name { get; set; }
		public string? Description { get; set; }
		public int Quantity { get; set; }
		public decimal Price { get; set; }
		public string? Sku { get; set; }
	}

	public class GeideaPaymentResponse
	{
		public bool Success { get; set; }
		public string Message { get; set; }
		public string PaymentUrl { get; set; }
		public string TransactionId { get; set; }
		public string Status { get; set; }
	}

	public class GeideaPaymentStatusResponse
	{
		public bool Success { get; set; }
		public string Message { get; set; }
		public string Status { get; set; }
		public string TransactionId { get; set; }
		public bool IsPaid { get; set; }
		/// <summary>
		/// Paid amount as reported by Geidea for the matched order (in the same
		/// major-unit scale the payment was created with, e.g. 18.50 = 18.50 AED).
		/// Null when the amount could not be extracted from the response.
		/// </summary>
		public decimal? PaidAmount { get; set; }
	}

	public class GeideaCancelResponse
	{
		public bool Success { get; set; }
		public string Message { get; set; }
		public string TransactionId { get; set; }
	}

	public class GeideaRefundResponse
	{
		public bool Success { get; set; }
		public string Message { get; set; }
		public string RefundId { get; set; }
		public string TransactionId { get; set; }
		public decimal RefundAmount { get; set; }
	}
}
