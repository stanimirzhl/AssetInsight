using AssetInsight.Core.DTOs.TradingStrategy;
using AssetInsight.Core.Interfaces;
using AssetInsight.Core.StrategyEngine.JSON_Options;
using AssetInsight.Core.StrategyEngine.Nodes;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AssetInsight.Core.Implementations
{
	public class StrategyService : IStrategyService
	{
		private readonly IRepository<TradingStrategy> repository;

		public StrategyService(IRepository<TradingStrategy> repository)
		{
			this.repository = repository;
		}

		public async Task<List<TradingStrategy>> GetAllStrategiesAsync()
		{
			return await repository.AllAsReadOnly().ToListAsync();
		}

		public async Task<List<TradingStrategy>> GetAllUserStrategiesAsync(string userId)
		{
			if (string.IsNullOrEmpty(userId))
			{
				return await repository.AllAsReadOnly().Where(x => x.UserId == null).ToListAsync();
			}
			return await repository
				.AllAsReadOnly()
				.Where(s => s.UserId == userId || s.UserId == null)
				.ToListAsync();
		}

		public async Task<TradingStrategy> GetStrategyByIdAsync(int id)
		{
			var strategy = await repository.AllAsReadOnly().FirstOrDefaultAsync(s => s.Id == id);
			if (strategy == null)
				throw new NoEntityException($"Strategy with ID {id} not found.");
			return strategy;
		}

		public async Task CreateCustomStrategyAsync(StrategyDto dto, string userId)
		{
			try
			{
				JsonSerializer.Deserialize<IStrategyNode>(dto.DefinitionJson, StrategyJsonOptions.Default);
			}
			catch
			{
				throw new Exception("Invalid Strategy Format. Logic tree is corrupted.");
			}

			var strategy = new TradingStrategy
			{
				Name = dto.Name,
				UserId = userId,
				DefinitionJson = dto.DefinitionJson
			};

			await repository.AddAsync(strategy);
		}
	}
}
