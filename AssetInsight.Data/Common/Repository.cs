using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Data.Common
{
	public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
	{
		private readonly AssetInsightDbContext dbContext;
		private readonly DbSet<TEntity> dbSet;

		public Repository(AssetInsightDbContext dbContext)
		{
			this.dbContext = dbContext;
			this.dbSet = dbContext.Set<TEntity>();
		}

		public IQueryable<TEntity> All()
		{
			return dbSet;
		}

		public IQueryable<TEntity> AllAsReadOnly()
		{
			return dbSet.AsNoTracking();
		}

		public async Task AddAsync(TEntity entity)
		{
			await dbSet.AddAsync(entity);
			await this.SaveChangesAsync();
		}

		public async Task<int> SaveChangesAsync()
		{
			return await dbContext.SaveChangesAsync();
		}

		public async Task<TEntity?> GetByIdAsync(object id)
		{
			return await dbSet.FindAsync(id);
		}

		public async Task DeleteAsync(object id)
		{
			TEntity? entity = await GetByIdAsync(id);

			if (entity != null)
			{
				dbSet.Remove(entity);
				await this.SaveChangesAsync();
			}
		}

		public async Task RemoveRange(IEnumerable<TEntity> entities)
		{
			dbSet.RemoveRange(entities);

			await this.SaveChangesAsync();
		}
	}
}
