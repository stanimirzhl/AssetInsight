using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AssetInsight.Data.Constants.DataConstants.UserConstants;

namespace AssetInsight.Data.Models
{
	public class User : IdentityUser
	{
		[Required]
		[MaxLength(UserFirstNameMaxLength)]
		public string FirstName { get; set; }

		[Required]
		[MaxLength(UserLastNameMaxLength)]
		public string LastName { get; set; }

		public UserStatus Status { get; set; } = UserStatus.Pending;

		[Required]
		[MaxLength(UserNameMaxLength)]
		public string UserName { get; set; }

		public virtual ICollection<SavedPost> SavedPosts { get; set; } = new HashSet<SavedPost>();
		public virtual ICollection<PostReaction> PostReactions { get; set; } = new HashSet<PostReaction>();

		public virtual ICollection<CommentReaction> CommentReactions { get; set; } = new HashSet<CommentReaction>();

		public virtual ICollection<Post> Posts { get; set; } = new HashSet<Post>();

		public virtual ICollection<Comment> Comments { get; set; } = new HashSet<Comment>();

	}
}
