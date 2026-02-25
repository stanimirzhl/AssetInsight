using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AssetInsight.Data.Constants.DataConstants.PostConstants;

namespace AssetInsight.Data.Models
{
	public class Post
	{
		[Key]
		public Guid Id { get; set; }

		[Required]
		[MaxLength(PostTitleMaxLength)]
		public string Title { get; set; }

		[Required]
		[MaxLength(PostContentMaxLength)]
		public string Content { get; set; }

		[ForeignKey(nameof(Author))]
		public string? AuthorId { get; set; }
		public virtual User? Author { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;
		public DateTime? EditedAt { get; set; }

		public bool IsLocked { get; set; }

		public virtual ICollection<Comment> Comments { get; set; } = new HashSet<Comment>();
		public virtual ICollection<SavedPost> SavedPosts { get; set; } = new HashSet<SavedPost>();
		public virtual ICollection<PostTag> PostTags { get; set; } = new HashSet<PostTag>();
		public virtual ICollection<PostReaction> Reactions { get; set; } = new HashSet<PostReaction>();
	}
}
