using AssetInsight.Core.DTOs.TradingStrategy;
using AssetInsight.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Interfaces
{
	public interface IStrategyService
	{
		Task<List<TradingStrategy>> GetAllStrategiesAsync();
		Task<TradingStrategy> GetStrategyByIdAsync(int id);
		Task CreateCustomStrategyAsync(StrategyDto dto, string userId);
		Task<List<TradingStrategy>> GetAllUserStrategiesAsync(string userId);
	}
}
