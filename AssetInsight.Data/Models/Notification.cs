using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Data.Models
{
	public class Notification
	{
		public int Id { get; set; }
		public string ReceiverId { get; set; }
		public string Message { get; set; }
		public string TargetUrl { get; set; }
		public bool IsRead { get; set; } = false;
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}
