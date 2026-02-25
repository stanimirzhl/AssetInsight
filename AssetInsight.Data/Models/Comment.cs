using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AssetInsight.Data.Constants.DataConstants.CommentConstants;

namespace AssetInsight.Data.Models
{
	public class Comment
	{
		[Key]
		public Guid Id { get; set; }

		[Required]
		[MaxLength(CommentContentMaxLength)]
		public string Content { get; set; }

		[ForeignKey(nameof(Author))]
		public string? AuthorId { get; set; }
		public virtual User? Author { get; set; }

		[Required]
		[ForeignKey(nameof(Post))]
		public Guid PostId { get; set; }
		public virtual Post Post { get; set; }

		public Guid? ParentCommentId { get; set; }
		public virtual Comment? ParentComment { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;
		public DateTime? EditedAt { get; set; }

		public bool IsDeleted { get; set; } = false;

		public virtual ICollection<Comment> Replies { get; set; } = new HashSet<Comment>();
		public virtual ICollection<CommentReaction> Reactions { get; set; } = new HashSet<CommentReaction>();
	}
}
