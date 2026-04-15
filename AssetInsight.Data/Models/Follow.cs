using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Data.Models
{
	public class Follow
	{
		[Key]
		public int Id { get; set; }

		public string FollowerId { get; set; }
		public virtual User Follower { get; set; }

		public string FollowedUserId { get; set; }
		public virtual User FollowedUser { get; set; }
	}
}
