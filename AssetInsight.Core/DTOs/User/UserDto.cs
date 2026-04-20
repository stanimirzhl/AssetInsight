using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.DTOs.User
{
	public class UserDto
	{
		public string Id { get; set; }

		public string UserName { get; set; }

		public string Email { get; set; }

		public string FirstName { get; set; }

		public string LastName { get; set; }

		public List<string> Roles { get; set; } = new List<string>();
	}
}
