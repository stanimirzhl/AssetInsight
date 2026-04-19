using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Interfaces
{
	public interface IWatchListService
	{
		Task<bool> ToggleWatchList(string userId, string symbol);
		Task<bool> IsFollowing(string userId, string symbol);
	}
}
