using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Data.Models
{
	public class Account
	{
		public Guid Id { get; set; }

		public string UserId { get; set; }
		[ForeignKey(nameof(UserId))]
		public User User { get; set; }

		public decimal Balance { get; set; }       
		public decimal ReservedBalance { get; set; } 
		public decimal AvailableBalance => Balance - ReservedBalance;

		public bool IsActive { get; set; }

		public decimal MaxRiskPerTradePercent { get; set; }
		public decimal MaxDailyLoss { get; set; }

		public DateTime CreatedAt { get; set; }
	}
}
