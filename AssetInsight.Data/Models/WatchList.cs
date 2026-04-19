using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Data.Models
{
	public class WatchList
	{
		public int Id { get; set; }

		public string UserId { get; set; }
		public virtual User User { get; set; }

		public string Symbol { get; set; }

		public DateTime AddedOn { get; set; } = DateTime.Now;
	}
}
