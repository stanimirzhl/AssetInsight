namespace AssetInsight.Models.Account
{
	public class ExternalLoginTempDto
	{
		public string Email { get; set; }
		public string UserName { get; set; }
		public string Provider { get; set; }
		public string ProviderDisplayName { get; set; }
		public string ReturnUrl { get; set; }
	}
}
