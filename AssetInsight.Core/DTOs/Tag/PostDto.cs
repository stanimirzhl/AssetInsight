using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.DTOs.Tag
{
	public class PostDto
	{
		public Guid Id { get; set; }

		public string Title { get; set; }

		public string Content { get; set; }

		public string AuthorName { get; set; }

		public DateTime CreatedAt { get; set; }
		public DateTime? EditedAt { get; set; }

		public bool IsLocked { get; set; }

		public int CommentsCount { get; set; }

		public int ReactionsCount { get; set; }
	}
}
