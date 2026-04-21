using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace AssetInsight.Models.Account
{
	public class ManageProfileViewModel
	{
		public string Username { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;

		public bool HasPassword { get; set; }
		public ChangePasswordModel ChangePassword { get; set; } = new();
		public SetPasswordModel SetPassword { get; set; } = new();

		public IList<UserLoginInfo> CurrentLogins { get; set; } = new List<UserLoginInfo>();
		public IList<AuthenticationScheme> OtherLogins { get; set; } = new List<AuthenticationScheme>();

		public string? StatusMessage { get; set; }
	}
}
