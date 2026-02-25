using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Data.Models
{
	public class CommentReaction
	{
		[Key]
		public int Id { get; set; }

		public Guid CommentId { get; set; }
		public virtual Comment Comment { get; set; }

		public string UserId { get; set; }
		public virtual User User { get; set; }

		[Required]
		public bool IsUpVote { get; set; }
	}
}
