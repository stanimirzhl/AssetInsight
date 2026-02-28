// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using AssetInsight.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static AssetInsight.Data.Constants.DataConstants.UserConstants;

namespace AssetInsight.Areas.Identity.Pages.Account
{
	public class RegisterModel : PageModel
	{
		private readonly SignInManager<User> _signInManager;
		private readonly UserManager<User> _userManager;
		private readonly IUserStore<User> _userStore;
		private readonly IUserEmailStore<User> _emailStore;
		private readonly ILogger<RegisterModel> _logger;
		private readonly IEmailSender _emailSender;

		public RegisterModel(
			UserManager<User> userManager,
			IUserStore<User> userStore,
			SignInManager<User> signInManager,
			ILogger<RegisterModel> logger,
			IEmailSender emailSender)
		{
			_userManager = userManager;
			_userStore = userStore;
			_emailStore = GetEmailStore();
			_signInManager = signInManager;
			_logger = logger;
			_emailSender = emailSender;
		}

		[BindProperty]
		public InputModel Input { get; set; }

		public string ReturnUrl { get; set; }

		public IList<AuthenticationScheme> ExternalLogins { get; set; }

		public class InputModel
		{
			[Required(
				ErrorMessageResourceName = "UserName_Required",
				ErrorMessageResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
			[StringLength(UserNameMaxLength, MinimumLength = UserNameMinLength,
				ErrorMessageResourceName = "UserName_StringLength",
				ErrorMessageResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
			[Display(
				Name = "UserName",
				ResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
			public string UserName { get; set; }

			[Required(
				ErrorMessageResourceName = "FirstName_Required",
				ErrorMessageResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
			[StringLength(UserFirstNameMaxLength, MinimumLength = UserFirstNameMinLength,
				ErrorMessageResourceName = "FirstName_StringLength",
				ErrorMessageResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
			[Display(
				Name = "FirstName",
				ResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
			public string FirstName { get; set; }

			[Required(
				ErrorMessageResourceName = "LastName_Required",
				ErrorMessageResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
			[StringLength(UserLastNameMaxLength, MinimumLength = UserLastNameMinLength,
				ErrorMessageResourceName = "LastName_StringLength",
				ErrorMessageResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
			[Display(
				Name = "LastName",
				ResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
			public string LastName { get; set; }

			[Required(
				ErrorMessageResourceName = "Email_Required",
				ErrorMessageResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
			[EmailAddress(
				ErrorMessageResourceName = "Email_Invalid",
				ErrorMessageResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
			[Display(
				Name = "Email",
				ResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
			public string Email { get; set; }

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
			public string Password { get; set; }

			[DataType(DataType.Password)]
			[Display(
				Name = "ConfirmPassword",
				ResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
			[Compare("Password",
				ErrorMessageResourceName = "ConfirmPassword_Mismatch",
				ErrorMessageResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
			[Required(
				ErrorMessageResourceName = "ConfirmPassword_Required",
				ErrorMessageResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
			[StringLength(UserPasswordMaxLength, MinimumLength = UserPasswordMinLength,
				ErrorMessageResourceName = "ConfirmPassword_StringLength",
				ErrorMessageResourceType = typeof(Resources.Models.RegisterModel.InputModel))]
			public string ConfirmPassword { get; set; }
		}


		public async Task OnGetAsync(string returnUrl = null)
		{
			ReturnUrl = returnUrl;
			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
		}

		public async Task<IActionResult> OnPostAsync(string returnUrl = null)
		{
			returnUrl ??= Url.Content("~/");
			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
			if (ModelState.IsValid)
			{
				if (await _userManager.FindByEmailAsync(Input.Email) != null)
				{
					ModelState.AddModelError(string.Empty, Resources.Models.RegisterModel.InputModel.EmailAlreadyExists);
					return Page();
				}

				if(await _userManager.FindByNameAsync(Input.UserName) != null)
				{
					ModelState.AddModelError(string.Empty, Resources.Models.RegisterModel.InputModel.UserNameAlreadyExists);
					return Page();
				}

				var user = new User
				{
					FirstName = Input.FirstName,
					LastName = Input.LastName,
					Email = Input.Email,
					UserName = Input.UserName,
				};

				var result = await _userManager.CreateAsync(user, Input.Password);

				if (result.Succeeded)
				{
					_logger.LogInformation("User created a new account with password.");

					await _signInManager.SignInAsync(user, isPersistent: false);
					return LocalRedirect(returnUrl);
				}
				foreach (var error in result.Errors)
				{
					var description = error.Description;
					switch (error.Code)
					{
						case "PasswordRequiresDigit":
							description = Resources.Models.RegisterModel.InputModel.PasswordRequiresDigit;
							break;
						case "PasswordRequiresNonAlphanumeric":
							description = Resources.Models.RegisterModel.InputModel.PasswordRequiresNonAlphanumeric;
							break;
						case "PasswordRequiresUpper":
							description = Resources.Models.RegisterModel.InputModel.PasswordRequiresUpper;
							break;
						case "PasswordRequiresLower":
							description = Resources.Models.RegisterModel.InputModel.PasswordRequiresLower;
							break;
					}

					ModelState.AddModelError(string.Empty, description);
				}
			}

			return Page();
		}

		private IUserEmailStore<User> GetEmailStore()
		{
			if (!_userManager.SupportsUserEmail)
			{
				throw new NotSupportedException("The default UI requires a user store with email support.");
			}
			return (IUserEmailStore<User>)_userStore;
		}
	}
}
