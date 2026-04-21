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
			ValidateStrategyJson(dto.DefinitionJson);

			var strategy = new TradingStrategy
			{
				Name = dto.Name,
				UserId = userId,
				DefinitionJson = dto.DefinitionJson
			};

			await repository.AddAsync(strategy);
		}

		public async Task UpdateCustomStrategyAsync(int id, StrategyDto dto, string userId)
		{
			ValidateStrategyJson(dto.DefinitionJson);

			var strategy = await repository.All().FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId)
				?? throw new UnauthorizedAccessException("Strategy not found or access denied.");

			strategy.Name = dto.Name;
			strategy.DefinitionJson = dto.DefinitionJson;

			await repository.SaveChangesAsync();
		}

		public async Task DeleteStrategyAsync(int id, string userId)
		{
			var strategy = await repository.All().FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId)
				?? throw new UnauthorizedAccessException("Strategy not found or access denied.");

			await repository.DeleteAsync(strategy.Id);
		}

		private void ValidateStrategyJson(string json)
		{
			try
			{
				JsonSerializer.Deserialize<StrategyDefinition>(json, StrategyJsonOptions.Default);
			}
			catch
			{
				throw new Exception("Invalid Strategy Format. Logic tree is corrupted.");
			}
		}
	}
}
