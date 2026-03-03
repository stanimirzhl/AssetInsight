using AssetInsight.Data.Models;
using AssetInsight.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Newtonsoft.Json;
using System.Security.Claims;
using static AssetInsight.Areas.Identity.Pages.Account.LoginModel;
using static AssetInsight.Data.Constants.DataConstants;

namespace AssetInsight.Controllers
{
	[AllowAnonymous]
	[Area("Identity")]
	public class AuthController : Controller
	{
		private readonly SignInManager<User> signInManager;
		private readonly UserManager<User> userManager;

		public AuthController(SignInManager<User> signInManager,
			UserManager<User> userManager)
		{
			this.signInManager = signInManager;
			this.userManager = userManager;
		}

		[HttpGet]
		[Route("complete-registration")]
		public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
		{
			returnUrl ??= Url.Content("~/");

			if (remoteError != null)
				return RedirectToAction("Login", "Account", new { area = "Identity" });

			var info = await signInManager.GetExternalLoginInfoAsync();
			if (info == null)
				return RedirectToAction("Login", "Account", new { area = "Identity" });

			var signInResult = await signInManager.ExternalLoginSignInAsync(
				info.LoginProvider,
				info.ProviderKey,
				isPersistent: false);

			if (signInResult.Succeeded)
				return LocalRedirect(returnUrl);

			var email = info.Principal.FindFirstValue(ClaimTypes.Email);

			if (email is null)
			{
				TempData["ErrorMessage"] = Resources.Models.RegisterModel.InputModel.EmailInAccessable;
				return RedirectToAction("Register", "Account", new { area = "Identity" });
			}

			if (!string.IsNullOrEmpty(email))
			{
				var existingUser = await userManager.FindByEmailAsync(email);

				if (existingUser != null)
				{
					var dto = new ExternalLoginTempDto
					{
						Email = existingUser.Email,
						UserName = existingUser.UserName,
						Provider = info.LoginProvider,
						ProviderDisplayName = info.ProviderDisplayName,
						ReturnUrl = returnUrl
					};

					string serializedDto = JsonConvert.SerializeObject(dto);

					TempData["ExternalLogin"] = serializedDto;

					return RedirectToPage("/Account/Login", new { area = "Identity" });
				}
			}

			var model = new ExternalLoginViewModel
			{
				Email = email,
				ReturnUrl = returnUrl,
				LoginProvider = info.LoginProvider,
				ProviderDisplayName = info.ProviderDisplayName,
				LastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty,
				FirstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty,
				UserName = info.Principal.FindFirstValue(ClaimTypes.GivenName) + "_" + info.Principal.FindFirstValue(ClaimTypes.Surname)
			};

			return View("~/Views/Auth/CompleteRegistration.cshtml", model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult ExternalLogin(string provider, string returnUrl = null)
		{
			var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { returnUrl });
			var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
			properties.Items["flow"] = "external-login-completion";
			return Challenge(properties, provider);
		}

		[Route("auth-failed")]
		public IActionResult ExternalLoginFail()
		{
			TempData["ErrorMessage"] = Resources.Models.LoginModel.InputModel.ExternalLoginFailed;
			return RedirectToAction("Login", "Account", new { area = "Identity" });
		}

		[ValidateAntiForgeryToken]
		[Route("registration")]
		public IActionResult ExternalLoginConfirmation(ExternalLoginViewModel model)
		{
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ExternalLoginCompletion(ExternalLoginViewModel model)
		{
			if (!ModelState.IsValid)
				return View(model);

			var info = await signInManager.GetExternalLoginInfoAsync();
			if (info == null)
				return RedirectToAction(nameof(Login));

			if (!info.AuthenticationProperties.Items.ContainsKey("flow"))
			{
				return RedirectToAction(nameof(Login));
			}

			var existingUsername = await userManager.FindByNameAsync(model.UserName);
			if (existingUsername != null)
			{
				ModelState.AddModelError("", "Username already taken.");
				return View(model);
			}

			var user = new User
			{
				UserName = model.UserName,
				Email = model.Email,
				FirstName = model.FirstName,
				LastName = model.LastName,
				EmailConfirmed = true
			};

			var createResult = await userManager.CreateAsync(user);
			if (!createResult.Succeeded)
			{
				foreach (var error in createResult.Errors)
					ModelState.AddModelError("", error.Description);

				return View(model);
			}

			await userManager.AddLoginAsync(user, info);
			await signInManager.SignInAsync(user, false);

			return LocalRedirect(model.ReturnUrl ?? "/");
		}
	}
}
