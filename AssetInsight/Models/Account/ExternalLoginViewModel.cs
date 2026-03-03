using System.ComponentModel.DataAnnotations;

namespace AssetInsight.Models.Account
{
	public class ExternalLoginViewModel
	{
		public string LoginProvider { get; set; } = null!;

		public string ProviderDisplayName { get; set; } = null!;

		public string ReturnUrl { get; set; } = null!;

		public string? Email { get; set; }

		[Required]
		public string UserName { get; set; } = null!;

		public string FirstName { get; set; } = null!;
		public string LastName { get; set; } = null!;
	}
}
