using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Data.Common
{
	public interface IRepository<TEntity> where TEntity : class
	{
		IQueryable<TEntity> All();

		IQueryable<TEntity> AllAsReadOnly();

		Task AddAsync(TEntity entity);

		Task<int> SaveChangesAsync();

		Task<TEntity?> GetByIdAsync(object id);

		Task DeleteAsync(object id);

		Task RemoveRange(IEnumerable<TEntity> entities);
	}
}
