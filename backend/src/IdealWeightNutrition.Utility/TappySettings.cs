namespace IdealWeightNutrition.Utility
{
	public class TappySettings
	{
		public string ApiKey { get; set; } = string.Empty;
		public string MerchantId { get; set; } = string.Empty;
		public string MerchantCode { get; set; } = string.Empty;
		public string BaseUrl { get; set; } = "https://api.tabby.ai";
		public bool Enabled { get; set; } = true;
		/// <summary>
		/// Minimum order amount required to show Tabby payment option. Default is 0 (no minimum).
		/// </summary>
		public decimal MinimumOrderAmount { get; set; } = 0;
	}
}
