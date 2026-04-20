using AssetInsight.Core.DTOs.User;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AssetInsight.Core.Implementations
{
	public class UserService : IUserService
	{
		private readonly UserManager<User> userManager;
		private readonly SignInManager<User> signInManager;
		private readonly RoleManager<IdentityRole> roleManager;

		public UserService(UserManager<User> userManager,
			SignInManager<User> signInManager,
			RoleManager<IdentityRole> roleManager)
		{
			this.userManager = userManager;
			this.signInManager = signInManager;
			this.roleManager = roleManager;
		}

		public async Task<PagingModel<UserDto>> GetAllUsersPaged(int pageIndex, int pageSize)
		{
			PagingModel<User> pagedUsers = await PagingModel<User>.CreateAsync(userManager.Users, pageIndex, pageSize);

			List<UserDto> userDtos = new List<UserDto>();

			foreach (User user in pagedUsers.Items)
			{
				List<string> roles = await GetRoleNamesByUser(user);

				UserDto dto = new UserDto
				{
					Id = user.Id,
					UserName = user.UserName,
					Email = user.Email,
					Roles = roles,
					FirstName = user.FirstName,
					LastName = user.LastName
				};
				userDtos.Add(dto);
			}

			return new PagingModel<UserDto>(
				userDtos,
				pagedUsers.TotalCount,
				pagedUsers.PageIndex,
				pagedUsers.PageSize
			);
		}

		public async Task<List<string>> GetRoleNamesByUser(User user)
		{
			IList<string> roles = await userManager.GetRolesAsync(user);

			return roles.ToList();
		}

		public async Task<List<SelectListItem>> GetAllRolesIntoSelectList()
		{
			return await roleManager.Roles
				.Select(x => new SelectListItem
				{
					Value = x.Id,
					Text = x.Name
				})
				.ToListAsync();
		}

		public async Task<List<string>> GetRoleIdsByUser(User user)
		{
			IList<string> roleNames = await userManager.GetRolesAsync(user);

			List<string> roles = await roleManager.Roles
				.Where(x => roleNames.Contains(x.Name))
				.Select(r => r.Id)
				.ToListAsync();

			return roles;
		}

		public async Task<string> GetRoleNameById(string roleId)
		{
			IdentityRole role = await roleManager.FindByIdAsync(roleId) ?? throw new NoEntityException("Ролята не е в системата!");

			return role.Name;
		}

		public async Task AddRoleToUser(User user, string roleName)
		{
			await userManager.AddToRoleAsync(user, roleName);
		}

		public async Task ChangeUserPassword(User user, string password)
		{
			string resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

			await userManager.ResetPasswordAsync(user, resetToken, password);
		}

		public async Task RemoveRolesFromUser(User user, List<string> roleNames)
		{
			await userManager.RemoveFromRolesAsync(user, roleNames);
		}

		public async Task RefreshSignIn(User user)
		{
			await signInManager.RefreshSignInAsync(user);
		}
	}
}
