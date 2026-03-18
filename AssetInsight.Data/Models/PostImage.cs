using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Data.Models
{
	public class PostImage
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public string ImgUrl { get; set; }

		[Required]
		public string PublicId { get; set; }

		public Guid PostId { get; set; }
		public virtual Post Post { get; set; }
	}
}
