using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Data.Models
{
	public class PostTag
	{
		[Key]
		public Guid Id { get; set; }

		[Required]
		public Guid PostId { get; set; }
		public virtual Post Post { get; set; }

		[Required]
		public Guid TagId { get; set; }
		public virtual Tag Tag { get; set; }
	}
}
