namespace InfoSurge.Areas.Admin.Models.Users
{
	public class UserListViewModel
	{
		public string Id { get; set; } = string.Empty;
		public string Username { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string FullName { get; set; } = string.Empty;
		public IList<string> Roles { get; set; } = new List<string>();
	}
}
