// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using AssetInsight.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
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

		[TempData]
		public string ErrorMessage { get; set; }

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


		public async Task<IActionResult> OnGetAsync(string returnUrl = null)
		{
			if (User.Identity.IsAuthenticated)
			{
				return RedirectToAction("Index", "Home", new { area = "" });
			}

			if (!string.IsNullOrEmpty(ErrorMessage))
			{
				ModelState.AddModelError(string.Empty, ErrorMessage);
			}
			ReturnUrl = returnUrl;
			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

			return Page();
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
		public IHtmlContent GetProviderSvg(string providerName)
		{
			return providerName switch
			{
				"Google" => new HtmlString(@"<svg width=""18"" height=""18"" viewBox=""0 0 48 48"" class=""me-2"">
                    <path fill=""#EA4335"" d=""M24 9.5c3.6 0 6.8 1.2 9.3 3.6l6.9-6.9C35.6 2.3 30.2 0 24 0 14.6 0 6.5 5.4 2.6 13.2l8 6.2C12.6 13.1 17.8 9.5 24 9.5z""/>
                    <path fill=""#4285F4"" d=""M46.5 24.5c0-1.7-.1-3.3-.4-4.9H24v9.3h12.7c-.6 3-2.3 5.6-4.9 7.3l7.6 5.9C43.9 38 46.5 31.7 46.5 24.5z""/>
                    <path fill=""#FBBC05"" d=""M10.6 28.4c-1-3-1-6.3 0-9.3l-8-6.2C-1 18.5-1 29.5 2.6 35.1l8-6.7z""/>
                    <path fill=""#34A853"" d=""M24 48c6.5 0 11.9-2.1 15.8-5.8l-7.6-5.9c-2.1 1.4-4.9 2.2-8.2 2.2-6.2 0-11.4-3.6-13.4-8.8l-8 6.7C6.5 42.6 14.6 48 24 48z""/>
                </svg>"),
				"Facebook" => new HtmlString(@"<svg width='24' height='24' viewBox='0 0 96 96'><path fill='#1877F2' d='M72 0H24C10.7 0 0 10.7 0 24v48c0 13.3 10.7 24 24 24h24V58h-8V46h8v-8c0-7.5 4.5-12 11-12h8v12h-6c-1.5 0-3 .5-3 3v6h9l-1 12h-8v38h12c13.3 0 24-10.7 24-24V24c0-13.3-10.7-24-24-24z'/></svg>"),
				"Microsoft" => new HtmlString(@"<svg width='24' height='24' viewBox='0 0 24 24'><rect x='0' y='0' width='11' height='11' fill='#F35325'/><rect x='13' y='0' width='11' height='11' fill='#81BC06'/><rect x='0' y='13' width='11' height='11' fill='#05A6F0'/><rect x='13' y='13' width='11' height='11' fill='#FFBA08'/></svg>"),
				_ => HtmlString.Empty
			};
		}
	}
}
