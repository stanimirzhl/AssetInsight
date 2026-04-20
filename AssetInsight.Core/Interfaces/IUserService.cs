
using AssetInsight.Core.DTOs.User;
using AssetInsight.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AssetInsight.Core.Interfaces
{
	public interface IUserService
	{
		Task<PagingModel<UserDto>> GetAllUsersPaged(int pageIndex, int pageSize);

		Task<List<SelectListItem>> GetAllRolesIntoSelectList();

		Task<List<string>> GetRoleNamesByUser(User user);

		Task<string> GetRoleNameById(string roleId);

		Task<List<string>> GetRoleIdsByUser(User user);

		Task AddRoleToUser(User user, string roleName);

		Task RemoveRolesFromUser(User user, List<string> roleNames);

		Task ChangeUserPassword(User user, string password);

		Task RefreshSignIn(User user);
	}
}
