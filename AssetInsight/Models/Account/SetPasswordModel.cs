using System.ComponentModel.DataAnnotations;
using static AssetInsight.Resources.Models.RegisterModel.InputModel;
using static AssetInsight.Data.Constants.DataConstants.UserConstants;

namespace AssetInsight.Models.Account
{
	public class SetPasswordModel
	{
		[Required(
			ErrorMessageResourceName = "Password_Required",
			ErrorMessageResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
		[StringLength(UserPasswordMaxLength, MinimumLength = UserPasswordMinLength,
			ErrorMessageResourceName = "Password_StringLength",
			ErrorMessageResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
		[DataType(DataType.Password)]
		[Display(
			Name = "Password",
			ResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
		public string NewPassword { get; set; } = string.Empty;

		[Required(
			ErrorMessageResourceName = "ConfirmPassword_Required",
			ErrorMessageResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
		[DataType(DataType.Password)]
		[Display(
			Name = "ConfirmPassword",
			ResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
		[Compare("Password",
			ErrorMessageResourceName = "ConfirmPassword_Mismatch",
			ErrorMessageResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
		[StringLength(UserPasswordMaxLength, MinimumLength = UserPasswordMinLength,
			ErrorMessageResourceName = "ConfirmPassword_StringLength",
			ErrorMessageResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
		public string ConfirmPassword { get; set; } = string.Empty;
	}
}
