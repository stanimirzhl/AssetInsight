using AssetInsight.Areas.Admin.Models;
using AssetInsight.Areas.Admin.Models.Users;
using AssetInsight.Data;
using AssetInsight.Data.Models;
using InfoSurge.Areas.Admin.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetInsight.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class UsersController : Controller
	{
		private readonly UserManager<User> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly AssetInsightDbContext dbContext;

		public UsersController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, AssetInsightDbContext dbContext)
		{
			_userManager = userManager;
			_roleManager = roleManager;
			this.dbContext = dbContext;
		}

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var users = await _userManager.Users.ToListAsync();
			var model = new List<UserListViewModel>();

			foreach (var user in users)
			{
				model.Add(new UserListViewModel
				{
					Id = user.Id.ToString(),
					Username = user.UserName!,
					Email = user.Email!,
					FullName = $"{user.FirstName} {user.LastName}",
					Roles = await _userManager.GetRolesAsync(user)
				});
			}

			return View(model);
		}

		[HttpGet]
		public async Task<IActionResult> Create()
		{
			var model = new UserCreateViewModel
			{
				AllRoles = await _roleManager.Roles
					.Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(r.Name, r.Name))
					.ToListAsync()
			};
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(UserCreateViewModel model)
		{
			if (!ModelState.IsValid)
			{
				model.AllRoles = await _roleManager.Roles
					.Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(r.Name, r.Name))
					.ToListAsync();
				return View(model);
			}

			var user = new User { UserName = model.Username, Email = model.Email, FirstName = model.FirstName, LastName = model.LastName };
			var result = await _userManager.CreateAsync(user, model.Password);

			if (result.Succeeded)
			{
				if (model.SelectedRoles != null && model.SelectedRoles.Any())
				{
					await _userManager.AddToRolesAsync(user, model.SelectedRoles);
				}

				TempData["Success"] = "User created successfully.";
				return RedirectToAction(nameof(Index));
			}

			foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
			return View(model);
		}

		[HttpGet]
		public async Task<IActionResult> Edit(string id)
		{
			var user = await _userManager.FindByIdAsync(id);
			if (user == null) return NotFound();

			var userRoles = await _userManager.GetRolesAsync(user);
			var allRoles = await _roleManager.Roles.ToListAsync();

			var model = new UserEditViewModel
			{
				Id = user.Id.ToString(),
				Username = user.UserName!,
				Email = user.Email!,
				FirstName = user.FirstName,
				LastName = user.LastName,
				SelectedRoles = userRoles.ToList(),
				AllRoles = allRoles.Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
				{
					Text = r.Name,
					Value = r.Name,
					Selected = userRoles.Contains(r.Name!)
				})
			};

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(UserEditViewModel model)
		{
			if (!ModelState.IsValid) return View(model);

			var user = await _userManager.FindByIdAsync(model.Id);
			if (user == null) return NotFound();

			user.UserName = model.Username;
			user.Email = model.Email;
			user.FirstName = model.FirstName;
			user.LastName = model.LastName;

			var result = await _userManager.UpdateAsync(user);

			if (result.Succeeded)
			{
				var currentRoles = await _userManager.GetRolesAsync(user);
				await _userManager.RemoveFromRolesAsync(user, currentRoles);

				if (model.SelectedRoles != null)
				{
					await _userManager.AddToRolesAsync(user, model.SelectedRoles);
				}

				TempData["Success"] = "User updated successfully.";
				return RedirectToAction(nameof(Index));
			}

			foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(string id)
		{
			var user = await _userManager.FindByIdAsync(id);
			if (user != null)
			{
				if (user.Id.ToString() == _userManager.GetUserId(User))
				{
					TempData["Error"] = "You cannot delete your own account.";
					return RedirectToAction(nameof(Index));
				}

				var follows = dbContext.Follows.Where(f => f.FollowerId == id || f.FollowedUserId == id);
				dbContext.Follows.RemoveRange(follows);

				await dbContext.SaveChangesAsync();

				await _userManager.DeleteAsync(user);
				TempData["Success"] = "User deleted successfully.";
			}
			return RedirectToAction(nameof(Index));
		}
	}
}