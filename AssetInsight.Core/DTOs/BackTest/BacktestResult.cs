using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.DTOs.BackTest
{
	public class BacktestResult
	{
		public decimal FinalBalance { get; set; }

		public List<string> Logs { get; set; } = new();
	}
}
