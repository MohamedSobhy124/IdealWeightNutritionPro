namespace IdealWeightNutrition.Utility
{
	public class TamaraSettings
	{
		public string ApiToken { get; set; } = string.Empty;
		public string PublicKey { get; set; } = string.Empty;
		public string NotificationToken { get; set; } = string.Empty;
		public string BaseUrl { get; set; } = "https://api.tamara.co";
		public bool Enabled { get; set; } = true;
		public bool UseSandbox { get; set; } = true;
		public string CountryCode { get; set; } = "AE";
		public string Currency { get; set; } = "AED";
		/// <summary>
		/// Minimum order amount required to show Tamara payment option. Default is 0 (no minimum).
		/// </summary>
		public decimal MinimumOrderAmount { get; set; } = 0;
	}
}
