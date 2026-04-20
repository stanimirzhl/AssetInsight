using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Data.Models
{
	public class TradingStrategy
	{
		[Key]
		public int Id { get; set; }

		public string Name { get; set; } = null!;

		public string? UserId { get; set; }
		public virtual User? User { get; set; }

		public string DefinitionJson { get; set; } = string.Empty;

		public DateTime CreatedAt { get; set; } = DateTime.Now;
	}
}
