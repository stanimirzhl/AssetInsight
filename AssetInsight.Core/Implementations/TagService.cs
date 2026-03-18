using AssetInsight.Core.DTOs.Tag;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AssetInsight.Core.Implementations
{
	public class TagService : ITagService
	{
		private readonly IRepository<Tag> repository;

		public TagService(IRepository<Tag> tagRepository)
		{
			this.repository = tagRepository;
		}

		public async Task<List<TagDto>> GetAllTagsbyPostId(Guid postId)
		{
			return await repository.AllAsReadOnly()
				.Include(x => x.PostTags)
				.Where(x => x.PostTags.Any(pt => pt.PostId == postId))
				.Select(x => new TagDto
				{
					Id = x.Id,
					Name = x.Name
				})
				.ToListAsync();
		}

		public async Task<List<Guid>> ExtractAndAddTagsIfAny(string content)
		{
			List<string> tags = Regex.Matches(content, @"(?<!\w)#([\p{L}\p{N}_]{3,})")
				.Select(m => m.Value.Trim().ToLower().Remove(0, 1))
				.Distinct()
				.ToList();

			if (tags.Count != 0)
			{
				List<Guid> newTagIds = new List<Guid>();

				foreach (string tag in tags)
				{
					if(!await repository.All().AnyAsync(x => x.Name == tag))
					{
						Tag newTag = new Tag
						{
							Name = tag
						};

						await repository.AddAsync(newTag);

						newTagIds.Add(newTag.Id);
					}
				}

				return newTagIds;
			}

			return [];

		}
	}
}
