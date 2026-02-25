using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core
{
	public class PagingModel<T>
	{
		public List<T> Items { get; set; } = new List<T>();
		public int PageIndex { get; set; }
		public int TotalPages { get; set; }
		public int TotalCount { get; set; }
		public int PageSize { get; set; }

		public PagingModel(List<T> items, int count, int pageIndex, int pageSize)
		{
			Items = items;
			TotalCount = count;
			PageIndex = pageIndex;
			PageSize = pageSize;
			TotalPages = (int)Math.Ceiling(count / (double)pageSize);
		}

		public bool HasPreviousPage => PageIndex > 1;
		public bool HasNextPage => PageIndex < TotalPages;

		public static async Task<PagingModel<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
		{
			int count = await source.CountAsync();
			List<T> items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

			return new PagingModel<T>(items, count, pageIndex, pageSize);
		}
		public PagingModel<TResult> Map<TResult>(Func<T, TResult> mapFunc)
		{
			List<TResult> mappedItems = Items.Select(mapFunc).ToList();
			return new PagingModel<TResult>(mappedItems, TotalCount, PageIndex, PageSize);
		}
	}
}
