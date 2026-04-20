// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using AssetInsight.Data.Models;
using AssetInsight.Models.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static AssetInsight.Data.Constants.DataConstants;
using static AssetInsight.Data.Constants.DataConstants.UserConstants;

namespace AssetInsight.Areas.Identity.Pages.Account
{
	public class LoginModel : PageModel
	{
		private readonly SignInManager<User> _signInManager;
		private readonly ILogger<LoginModel> _logger;

		public LoginModel(SignInManager<User> signInManager, ILogger<LoginModel> logger)
		{
			_signInManager = signInManager;
			_logger = logger;
		}

		[BindProperty]
		public InputModel Input { get; set; }

		public IList<AuthenticationScheme> ExternalLogins { get; set; }

		public string ReturnUrl { get; set; }

		[TempData]
		public string ErrorMessage { get; set; }

		public LoginStep Step { get; set; } = LoginStep.Login;

		[BindProperty]
		[ValidateNever]
		public ConfirmLinkModel ConfirmLink { get; set; }

		[BindProperty]
		[ValidateNever]
		public LinkExternalModel LinkExternal { get; set; }

		public class ConfirmLinkModel
		{
			public string Email { get; set; }
			public string UserName { get; set; }
			public string Provider { get; set; }
			public string ProviderDisplayName { get; set; }
			public string ReturnUrl { get; set; }
			public string FullName { get; set; }
		}

		public class LinkExternalModel
		{
			public string Email { get; set; }

			[Required(ErrorMessageResourceName = "Password_Required", ErrorMessageResourceType = typeof(Resources.Models.LoginModel.InputModel))]
			[DataType(DataType.Password)]
			[Display(Name = "Password",
				ResourceType = typeof(Resources.Models.LoginModel.InputModel))]
			public string LinkPassword { get; set; }

			public string Provider { get; set; }
			public string ReturnUrl { get; set; }
			public string FullName { get; set; }
		}

		public class InputModel
		{

			[Required(ErrorMessageResourceName = "UserNameEmail_Required", ErrorMessageResourceType = typeof(Resources.Models.LoginModel.InputModel))]
			[StringLength(UserNameMaxLength, MinimumLength = UserNameMinLength, ErrorMessageResourceName = "UserNameEmail_StringLength", ErrorMessageResourceType = typeof(Resources.Models.LoginModel.InputModel))]
			[Display(Name = "UserNameEmail",
				ResourceType = typeof(Resources.Models.LoginModel.InputModel))]
			public string UserNameEmail { get; set; }

			[Required(ErrorMessageResourceName = "Password_Required", ErrorMessageResourceType = typeof(Resources.Models.LoginModel.InputModel))]
			[DataType(DataType.Password)]
			[Display(Name = "Password",
				ResourceType = typeof(Resources.Models.LoginModel.InputModel))]
			public string Password { get; set; }
		}

		public async Task<IActionResult> OnGetAsync(string returnUrl = null, LoginStep? step = null)
		{
			if (User.Identity.IsAuthenticated)
			{
				return RedirectToAction("Index", "Home", new { area = "" });
			}

			if (!string.IsNullOrEmpty(ErrorMessage))
			{
				ModelState.AddModelError(string.Empty, ErrorMessage);
			}

			returnUrl ??= Url.Content("~/");

			if (!TempData.ContainsKey("ExternalLogin"))
			{
				await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
			}

			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

			ReturnUrl = returnUrl;

			if (step.HasValue && TempData.TryGetValue("ExternalLogin", out var linkData))
			{
				var dto = JsonConvert.DeserializeObject<ExternalLoginTempDto>(linkData.ToString());
				Step = LoginStep.EnterPassword;

				LinkExternal = new LinkExternalModel
				{
					Email = dto.Email,
					Provider = dto.Provider,
					FullName = dto.FullName,
				};

				TempData.Keep("ExternalLogin");
			}

			if (TempData.TryGetValue("ExternalLogin", out var data) && !step.HasValue)
			{
				if ("true" == HttpContext.Request.Query["clearlogin"].ToString())
				{
					TempData.Remove("ExternalLogin");
					return RedirectToPage();
				}

				var dto = JsonConvert.DeserializeObject<ExternalLoginTempDto>(data.ToString());

				Step = LoginStep.ConfirmLink;

				ConfirmLink = new ConfirmLinkModel
				{
					Email = dto.Email,
					UserName = dto.UserName,
					Provider = dto.Provider,
					ProviderDisplayName = dto.ProviderDisplayName,
					FullName = dto.FullName,
					ReturnUrl = dto.ReturnUrl
				};

				ReturnUrl = dto.ReturnUrl;
				TempData.Keep("ExternalLogin");
			}

			return Page();
		}


		public IActionResult OnPostConfirmLink()
		{
			Step = LoginStep.EnterPassword;

			LinkExternal = new LinkExternalModel
			{
				Email = ConfirmLink.Email,
				Provider = ConfirmLink.Provider,
				FullName = ConfirmLink.FullName,
			};

			TempData.Keep("ExternalLogin");

			return RedirectToPage(new { step = LoginStep.EnterPassword });
		}

		public async Task<IActionResult> OnPostLinkExternalAsync()
		{
			var user = await _signInManager.UserManager.FindByEmailAsync(LinkExternal.Email);

			if (user == null)
				return RedirectToPage("/Account/Login", new { area = "Identity" });

			//if (string.IsNullOrEmpty(LinkExternal.Password))
			//{
			//	ModelState.AddModelError("LinkExternal.Password", Resources.Models.LoginModel.InputModel.Password_Required);
			//	Step = LoginStep.EnterPassword;
			//
			//	return Page();
			//}

			var validPassword = await _signInManager.UserManager.CheckPasswordAsync(user, LinkExternal.LinkPassword);

			if (!validPassword)
			{
				TempData["ErrorMessage"] = Resources.Models.LoginModel.InputModel.InvalidLoginAttempt;
				Step = LoginStep.EnterPassword;
				TempData.Keep("ExternalLogin");
				return RedirectToPage(new { step = LoginStep.EnterPassword });
			}

			var info = await _signInManager.GetExternalLoginInfoAsync();
			if (info == null)
				return RedirectToPage("/Account/Login", new { area = "Identity" });

			await _signInManager.UserManager.AddLoginAsync(user, info);
			await _signInManager.SignInAsync(user, false);

			return LocalRedirect(ReturnUrl ?? "~/");
		}

		public async Task<IActionResult> OnPostAsync(string returnUrl = null)
		{
			returnUrl ??= Url.Content("~/");
			ModelState.Remove(nameof(LinkExternal.LinkPassword));

			if (!ModelState.IsValid)
			{
				ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
				return Page();
			}

			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

			User user = await _signInManager.UserManager.FindByNameAsync(Input.UserNameEmail)
							?? await _signInManager.UserManager.FindByEmailAsync(Input.UserNameEmail);

			if (user is null)
			{
				ModelState.AddModelError(string.Empty, Resources.Models.LoginModel.InputModel.InvalidLoginAttempt);
				return Page();
			}


			if (ModelState.IsValid)
			{
				var result = await _signInManager.PasswordSignInAsync(user, Input.Password, false, false);
				if (result.Succeeded)
				{
					_logger.LogInformation("User logged in.");
					return LocalRedirect(returnUrl);
				}
				else
				{
					ModelState.AddModelError(string.Empty, Resources.Models.LoginModel.InputModel.InvalidLoginAttempt);
					return Page();
				}
			}

			return Page();
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
