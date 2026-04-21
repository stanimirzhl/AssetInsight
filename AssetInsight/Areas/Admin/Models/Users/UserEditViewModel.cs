using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using static AssetInsight.Data.Constants.DataConstants.UserConstants;

namespace InfoSurge.Areas.Admin.Models.Users
{
	public class UserEditViewModel
	{
		public string Id { get; set; } = string.Empty;

		[Required]
		public string Username { get; set; } = string.Empty;

		[Required]
		[EmailAddress]
		public string Email { get; set; } = string.Empty;

		[Required]
		[Display(Name = "First Name")]
		public string FirstName { get; set; } = string.Empty;

		[Required]
		[Display(Name = "Last Name")]
		public string LastName { get; set; } = string.Empty;

		public List<string> SelectedRoles { get; set; } = new List<string>();
		public IEnumerable<SelectListItem> AllRoles { get; set; }
			= new List<SelectListItem>();
	}
}
