using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdealWeightNutrition.Utility
{
	public class GeideaSettings
	{
		public string MerchantPublicKey { get; set; }
		public string MerchantApiPassword { get; set; }
		public string BaseUrl { get; set; } = "https://api.geidea.net";
		public bool UseSandbox { get; set; } = true;
		
		// Optional: Override callback URL for localhost testing (e.g., ngrok URL)
		public string CallbackUrlOverride { get; set; }
	}
}
