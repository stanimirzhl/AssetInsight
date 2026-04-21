using AssetInsight.Data.Models;
using AssetInsight.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AssetInsight.Controllers
{
	[Authorize]
	public class ManageController : Controller
	{
		private readonly UserManager<User> userManager;
		private readonly SignInManager<User> signInManager;

		public ManageController(UserManager<User> userManager, SignInManager<User> signInManager)
		{
			this.userManager = userManager;
			this.signInManager = signInManager;
		}

		[HttpGet]
		[Authorize]
		public async Task<IActionResult> Index()
		{
			var user = await userManager.GetUserAsync(User);
			if (user == null) return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");

			return View(await BuildManageViewModelAsync(user));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ChangePassword(ManageProfileViewModel model)
		{
			var user = await userManager.GetUserAsync(User);
			if (user == null) return NotFound();

			if (!ModelState.IsValid)
			{
				var vm = await BuildManageViewModelAsync(user);
				vm.ChangePassword = model.ChangePassword;
				return View("Index", vm);
			}

			var changePasswordResult = await userManager.ChangePasswordAsync(user, model.ChangePassword.OldPassword, model.ChangePassword.NewPassword);
			if (!changePasswordResult.Succeeded)
			{
				foreach (var error in changePasswordResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
				return View("Index", await BuildManageViewModelAsync(user));
			}

			await signInManager.RefreshSignInAsync(user);
			TempData["StatusMessage"] = "Your password has been changed.";
			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SetPassword(ManageProfileViewModel model)
		{
			var user = await userManager.GetUserAsync(User);
			if (user == null) return NotFound();

			if (!ModelState.IsValid)
			{
				var vm = await BuildManageViewModelAsync(user);
				vm.SetPassword = model.SetPassword;
				return View("Index", vm);
			}

			var addPasswordResult = await userManager.AddPasswordAsync(user, model.SetPassword.NewPassword);
			if (!addPasswordResult.Succeeded)
			{
				foreach (var error in addPasswordResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
				return View("Index", await BuildManageViewModelAsync(user));
			}

			await signInManager.RefreshSignInAsync(user);
			TempData["StatusMessage"] = "Your password has been set. You can now log in without an external provider.";
			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> LinkLogin(string provider)
		{
			var redirectUrl = Url.Action(nameof(LinkLoginCallback));
			var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, userManager.GetUserId(User));
			return new ChallengeResult(provider, properties);
		}

		[HttpGet]
		public async Task<IActionResult> LinkLoginCallback()
		{
			var user = await userManager.GetUserAsync(User);
			if (user == null) return NotFound();

			var info = await signInManager.GetExternalLoginInfoAsync(user.Id.ToString());
			if (info == null)
			{
				TempData["StatusMessage"] = "Error: Could not load external login info.";
				return RedirectToAction(nameof(Index));
			}

			var result = await userManager.AddLoginAsync(user, info);
			if (!result.Succeeded)
			{
				TempData["StatusMessage"] = "Error: The external login was not added. External logins can only be associated with one account.";
				return RedirectToAction(nameof(Index));
			}

			await signInManager.RefreshSignInAsync(user);
			TempData["StatusMessage"] = "The external login was added successfully.";
			return RedirectToAction(nameof(Index));
		}

		private async Task<ManageProfileViewModel> BuildManageViewModelAsync(User user)
		{
			var currentLogins = await userManager.GetLoginsAsync(user);
			var externalSchemes = await signInManager.GetExternalAuthenticationSchemesAsync();
			var otherLogins = externalSchemes.Where(auth => currentLogins.All(ul => auth.Name != ul.LoginProvider)).ToList();

			return new ManageProfileViewModel
			{
				Username = user.UserName!,
				Email = user.Email!,
				HasPassword = await userManager.HasPasswordAsync(user),
				CurrentLogins = currentLogins,
				OtherLogins = otherLogins,
				StatusMessage = TempData["StatusMessage"]?.ToString()
			};
		}
	}
}
