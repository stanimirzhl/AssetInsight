using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AssetInsight.Data.Constants.DataConstants.TagConstants;

namespace AssetInsight.Data.Models
{
	public class Tag
	{
		[Key]
		public Guid Id { get; set; }

		[Required]
		[MaxLength(TagNameMaxLength)]
		public string Name { get; set; }

		public virtual ICollection<PostTag> PostTags { get; set; } = new HashSet<PostTag>();
	}
}
