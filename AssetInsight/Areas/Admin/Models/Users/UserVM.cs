namespace InfoSurge.Areas.Admin.Models.Users
{
	public class UserVM
	{
		public string Id { get; set; }

		public string UserName { get; set; }

		public string FirstName { get; set; }

		public string LastName { get; set; }

		public string Email { get; set; }

		public int Status { get; set; }

		public List<string> Roles { get; set; } = new List<string>();

		public bool IsUserInRole { get; set; }
	}
}
