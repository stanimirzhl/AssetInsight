using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Data.Models
{
	public class SavedPost
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public Guid PostId { get; set; }
		public virtual Post Post { get; set; }

		[Required]
		public string UserId { get; set; }
		public virtual User User { get; set; }
	}
}
