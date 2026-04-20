using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using static AssetInsight.Areas.Identity.Pages.Account.RegisterModel;
using static AssetInsight.Data.Constants.DataConstants.UserConstants;

namespace InfoSurge.Areas.Admin.Models.Users
{
	public class EditUserFormModel
	{
		[Required(
				ErrorMessageResourceName = "UserName_Required",
				ErrorMessageResourceType = typeof(InputModel))]
		[StringLength(UserNameMaxLength, MinimumLength = UserNameMinLength,
				ErrorMessageResourceName = "UserName_StringLength",
				ErrorMessageResourceType = typeof(InputModel))]
		[Display(
				Name = "UserName",
				ResourceType = typeof(InputModel))]
		public string UserName { get; set; }

		[Required(
			ErrorMessageResourceName = "FirstName_Required",
			ErrorMessageResourceType = typeof(InputModel))]
		[StringLength(UserFirstNameMaxLength, MinimumLength = UserFirstNameMinLength,
			ErrorMessageResourceName = "FirstName_StringLength",
			ErrorMessageResourceType = typeof(InputModel))]
		[Display(
			Name = "FirstName",
			ResourceType = typeof(InputModel))]
		public string FirstName { get; set; }

		[Required(
			ErrorMessageResourceName = "LastName_Required",
			ErrorMessageResourceType = typeof(InputModel))]
		[StringLength(UserLastNameMaxLength, MinimumLength = UserLastNameMinLength,
			ErrorMessageResourceName = "LastName_StringLength",
			ErrorMessageResourceType = typeof(InputModel))]
		[Display(
			Name = "LastName",
			ResourceType = typeof(InputModel))]
		public string LastName { get; set; }

		[Required(
			ErrorMessageResourceName = "Email_Required",
			ErrorMessageResourceType = typeof(InputModel))]
		[EmailAddress(
			ErrorMessageResourceName = "Email_Invalid",
			ErrorMessageResourceType = typeof(InputModel))]
		[Display(
			Name = "Email",
			ResourceType = typeof(InputModel))]
		public string Email { get; set; }

		[Required(
			ErrorMessageResourceName = "Password_Required",
			ErrorMessageResourceType = typeof(InputModel))]
		[StringLength(UserPasswordMaxLength, MinimumLength = UserPasswordMinLength,
			ErrorMessageResourceName = "Password_StringLength",
			ErrorMessageResourceType = typeof(InputModel))]
		[DataType(DataType.Password)]
		[Display(
			Name = "Password",
			ResourceType = typeof(InputModel))]
		public string Password { get; set; }

		[DataType(DataType.Password)]
		[Display(
			Name = "ConfirmPassword",
			ResourceType = typeof(InputModel))]
		[Compare("Password",
			ErrorMessageResourceName = "ConfirmPassword_Mismatch",
			ErrorMessageResourceType = typeof(InputModel))]
		[Required(
			ErrorMessageResourceName = "ConfirmPassword_Required",
			ErrorMessageResourceType = typeof(InputModel))]
		[StringLength(UserPasswordMaxLength, MinimumLength = UserPasswordMinLength,
			ErrorMessageResourceName = "ConfirmPassword_StringLength",
			ErrorMessageResourceType = typeof(InputModel))]
		public string ConfirmPassword { get; set; }

		public List<SelectListItem>? Roles { get; set; } = new List<SelectListItem>();

		public List<string>? SelectedRolesIds { get; set; } = new List<string>();

		public List<string>? RoleIdsToRemove { get; set; } = new List<string>();
	}
}
