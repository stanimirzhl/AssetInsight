using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Implementations
{
	public class WatchListService : IWatchListService
	{
		private readonly IRepository<WatchList> repository;

		public WatchListService(IRepository<WatchList> repository)
		{
			this.repository = repository;
		}

		public async Task<bool> ToggleWatchList(string userId, string symbol)
		{
			var existing = repository.All().FirstOrDefault(w => w.UserId == userId && w.Symbol == symbol);
			if (existing != null)
			{
				await repository.DeleteAsync(existing.Id);
				return false;
			}
			else
			{
				var newEntry = new WatchList { UserId = userId, Symbol = symbol };
				await repository.AddAsync(newEntry);
				return true;
			}
		}

		public async Task<bool> IsFollowing(string userId, string symbol)
		{
			return await repository.All().AnyAsync(w => w.UserId == userId && w.Symbol == symbol);
		}
	}
}
