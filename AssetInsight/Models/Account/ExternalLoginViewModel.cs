using System.ComponentModel.DataAnnotations;

namespace AssetInsight.Models.Account
{
	public class ExternalLoginViewModel
	{
		public string LoginProvider { get; set; } = null!;

		public string ProviderDisplayName { get; set; } = null!;

		public string ReturnUrl { get; set; } = null!;

		public string? Email { get; set; }

		[Required(
			ErrorMessageResourceName = "UserName_Required",
			ErrorMessageResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
		[Display(
			Name = "UserName",
			ResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
		public string UserName { get; set; } = null!;

		public string FirstName { get; set; } = null!;
		public string LastName { get; set; } = null!;
	}
}
