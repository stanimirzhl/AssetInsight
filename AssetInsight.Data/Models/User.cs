using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Data.Models
{
	public class User : IdentityUser
	{
		[Required]
		//todo length
		public string FirstName { get; set; }

		[Required]
		//todo length
		public string LastName { get; set; }

	}
}
