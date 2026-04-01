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
	public class SavedPostService : ISavedPostService
	{
		private readonly IRepository<SavedPost> repository;

		public SavedPostService(IRepository<SavedPost> repository)
		{
			this.repository = repository;
		}

		public async Task<bool> ToggleSavePost(Guid postId, string userId)
		{
			SavedPost existing = await repository.All()
				.FirstOrDefaultAsync(s => s.PostId == postId && s.UserId == userId);

			if (existing != null)
			{
				await repository.DeleteAsync(existing.Id);
				return false;
			}
			else
			{
				await repository.AddAsync(new SavedPost { PostId = postId, UserId = userId });
				return true;
			}
		}

		public async Task<bool> HasUserSavedPost(Guid postId, string userId)
		{
			return await repository.All()
				.AnyAsync(s => s.PostId == postId && s.UserId == userId);
		}
	}
}
