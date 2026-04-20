using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.DTOs.TradingStrategy
{
	public class StrategyDto
	{
		public string Name { get; set; } = null!;

		public string DefinitionJson { get; set; } = string.Empty;
	}
}
