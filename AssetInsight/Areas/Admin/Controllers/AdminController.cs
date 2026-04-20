using AssetInsight.Core;
using AssetInsight.Core.DTOs.User;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Models;
using InfoSurge.Areas.Admin.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace InfoSurge.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Administrator")]
	public class AdminController : Controller
	{
		private readonly IUserService userService;
		private readonly ILogger<AdminController> logger;
		private readonly UserManager<User> userManager;

		public AdminController(IUserService userService,
			ILogger<AdminController> logger,
			UserManager<User> userManager)
		{
			this.userService = userService;
			this.logger = logger;
			this.userManager = userManager;
		}

		[HttpGet]
		public IActionResult DashBoard()
		{
			return View("~/Areas/Admin/Views/DashBoard.cshtml");
		}

		[HttpGet]
		public async Task<IActionResult> AllUsers(int pageIndex = 1, int pageSize = 6)
		{
			PagingModel<UserDto> userDtos = await userService.GetAllUsersPaged(pageIndex, pageSize);

			PagingModel<UserVM> pagedUsers = userDtos.Map( x => new UserVM
			{
				Id = x.Id,
				UserName = x.UserName,
				Email = x.Email,
				Roles = x.Roles,
				FirstName = x.FirstName,
				LastName = x.LastName,
				IsUserInRole = x.Roles.Contains("User")
			});

			return View("~/Areas/Admin/Views/Users/AllUsers.cshtml", pagedUsers);
		}

		[HttpGet]
		public async Task<IActionResult> Create()
		{
			List<SelectListItem> roles = await userService.GetAllRolesIntoSelectList();

			return View("~/Areas/Admin/Views/Users/Create.cshtml", new EditUserFormModel
			{
				Roles = roles
			});
		}
		[HttpPost]
		public async Task<IActionResult> Create(EditUserFormModel formModel)
		{
			if (!ModelState.IsValid)
			{
				List<SelectListItem> roles = await userService.GetAllRolesIntoSelectList();
				formModel.Roles = roles;
				return View("~/Areas/Admin/Views/Users/Create.cshtml", formModel);
			}

			if (await userManager.FindByNameAsync(formModel.UserName) != null)
			{
				ModelState.AddModelError(string.Empty, "Потребител с това име вече съществува!");
				List<SelectListItem> roles = await userService.GetAllRolesIntoSelectList();
				formModel.Roles = roles;
				return View("~/Areas/Admin/Views/Users/Create.cshtml", formModel);
			}

			if (await userManager.FindByEmailAsync(formModel.Email) != null)
			{
				ModelState.AddModelError(string.Empty, "Потребител с този имейл вече съществува!");
				List<SelectListItem> roles = await userService.GetAllRolesIntoSelectList();
				formModel.Roles = roles;
				return View("~/Areas/Admin/Views/Users/Create.cshtml", formModel);
			}

			User user = new User
			{
				UserName = formModel.UserName,
				Email = formModel.Email,
				FirstName = formModel.FirstName,
				LastName = formModel.LastName,
			};

			IdentityResult result = await userManager.CreateAsync(user, formModel.Password);

			if (result.Succeeded)
			{

				if (formModel.SelectedRolesIds.Count > 0)
				{
					foreach (string roleId in formModel.SelectedRolesIds)
					{
						try
						{
							string roleName = await userService.GetRoleNameById(roleId);

							await userService.AddRoleToUser(user, roleName);
						}
						catch (NoEntityException ex)
						{
							logger.LogError(ex.Message, ex);

							return BadRequest();
						}
					}
				}
			}
			return RedirectToAction("AllUsers", "Admin", new { area = "Admin" });
		}

		[HttpGet]
		public async Task<IActionResult> Edit(string id)
		{
			try
			{
				User user = await userManager.FindByIdAsync(id);

				EditUserFormModel formModel = new EditUserFormModel
				{
					UserName = user.UserName,
					Email = user.Email,
					FirstName = user.FirstName,
					LastName = user.LastName,
					Roles = await userService.GetAllRolesIntoSelectList(),
					SelectedRolesIds = await userService.GetRoleIdsByUser(user)
				};

				return View("~/Areas/Admin/Views/Users/Edit.cshtml", formModel);
			}
			catch (NoEntityException ex)
			{

				return Unauthorized();
			}
		}
		[HttpPost]
		public async Task<IActionResult> Edit(EditUserFormModel formModel, string id)
		{
			if (!ModelState.IsValid)
			{
				List<SelectListItem> roles = await userService.GetAllRolesIntoSelectList();
				formModel.Roles = roles;
				formModel.SelectedRolesIds = await userService.GetRoleIdsByUser(await userManager.FindByIdAsync(id));
				return View("~/Areas/Admin/Views/Users/Edit.cshtml", formModel);
			}

			try
			{
				User user = await userManager.FindByIdAsync(id);

				if (await userManager.FindByNameAsync(formModel.UserName) != null && user.UserName != formModel.UserName)
				{
					ModelState.AddModelError(string.Empty, "Потребител с това име вече съществува!");
					List<SelectListItem> roles = await userService.GetAllRolesIntoSelectList();
					formModel.Roles = roles;
					formModel.SelectedRolesIds = await userService.GetRoleIdsByUser(await userManager.FindByIdAsync(id));
					return View("~/Areas/Admin/Views/Users/Edit.cshtml", formModel);
				}

				if (await userManager.FindByEmailAsync(formModel.Email) != null && user.Email != formModel.Email)
				{
					ModelState.AddModelError(string.Empty, "Потребител с този имейл вече съществува!");
					List<SelectListItem> roles = await userService.GetAllRolesIntoSelectList();
					formModel.Roles = roles;
					formModel.SelectedRolesIds = await userService.GetRoleIdsByUser(await userManager.FindByIdAsync(id));
					return View("~/Areas/Admin/Views/Users/Edit.cshtml", formModel);
				}

				if (!string.IsNullOrEmpty(formModel.Password))
				{
					await userService.ChangeUserPassword(user, formModel.Password);
				}

				IdentityResult result = await userManager.UpdateAsync(user);

				if (result.Succeeded)
				{
					List<string> originalRoles = await userService.GetRoleIdsByUser(user);

					List<string> adminChosenRoles = formModel.SelectedRolesIds;

					List<string> rolesToAdd = adminChosenRoles.Except(originalRoles).ToList();
					List<string> rolesToRemove = originalRoles.Except(adminChosenRoles).ToList();

					foreach (string role in rolesToAdd)
					{
						string roleName = await userService.GetRoleNameById(role);

						await userService.AddRoleToUser(user, roleName);
					}

					List<string> roleNamesToRemove = new List<string>();
					foreach (string role in rolesToRemove)
					{
						string roleName = await userService.GetRoleNameById(role);

						roleNamesToRemove.Add(roleName);
					}
					await userService.RemoveRolesFromUser(user, roleNamesToRemove);

					if (rolesToAdd.Count > 0 || rolesToRemove.Count > 0)
					{
						await userService.RefreshSignIn(user);
					}

				}
				return RedirectToAction("AllUsers", "Admin", new { area = "Admin" });
			}
			catch (NoEntityException ex)
			{
				return BadRequest();
			}
			catch (Exception ex)
			{
				logger.LogError(ex.Message, ex);

				return RedirectToAction("Error", "Home", new { code = 500 });
			}
		}

		[HttpPost]
		public async Task<IActionResult> Delete(string userId)
		{
			string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			if (userId == currentUserId)
			{
				return RedirectToAction("AllUsers", "Admin", new { area = "Admin" });
			}

			try
			{
				User user = await userManager.FindByIdAsync(userId);

				await userManager.DeleteAsync(user);

				return RedirectToAction("AllUsers", "Admin", new { area = "Admin" });
			}
			catch (NoEntityException ex)
			{
				return BadRequest();
			}
		}

		/*[HttpPost]
		public async Task<IActionResult> Approve(string userId)
		{
			try
			{
				User user = await userManager.FindByIdAsync(userId);

				await userManager.UpdateAsync(user);

				await userManager.AddToRoleAsync(user, "Approved");

				return RedirectToAction("AllUsers", "Admin", new { area = "Admin" });
			}
			catch (NoEntityException ex)
			{
				return BadRequest();
			}
			catch (Exception ex)
			{
				logger.LogError(ex.Message, ex);

				return RedirectToAction("Error", "Home", new { code = 500 });
			}
		}

		[HttpPost]
		public async Task<IActionResult> Reject(string userId)
		{
			try
			{
				User user = await accountService.GetCurrentUserById(userId);

				await accountService.Delete(user);

				return RedirectToAction("AllUsers", "Admin", new { area = "Admin" });
			}
			catch (NoEntityException ex)
			{
				return BadRequest();
			}
			catch (Exception ex)
			{
				logger.LogError(ex.Message, ex);

				return RedirectToAction("Error", "Home", new { code = 500 });
			}
		}*/
	}
}
