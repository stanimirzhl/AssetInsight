using AssetInsight.Core.Implementations;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AssetInsight.Tests.Core.Implementations
{
	[TestFixture]
	public class PostTagServiceTests
	{
		private Mock<IRepository<PostTag>> _repoMock;
		private PostTagService _service;
		private List<PostTag> _postTags;

		[SetUp]
		public void SetUp()
		{
			_postTags = new List<PostTag>();
			_repoMock = new Mock<IRepository<PostTag>>();

			_repoMock
				.Setup(r => r.AllAsReadOnly())
				.Returns(() => _postTags.AsQueryable().BuildMockDbSet().Object);

			_repoMock
				.Setup(r => r.All())
				.Returns(() => _postTags.AsQueryable().BuildMockDbSet().Object);

			_repoMock
				.Setup(r => r.AddAsync(It.IsAny<PostTag>()))
				.Callback((PostTag pt) =>
				{
					pt.Id = Guid.NewGuid();
					_postTags.Add(pt);
				})
				.Returns(Task.CompletedTask);

			_repoMock
				.Setup(r => r.SaveChangesAsync())
				.ReturnsAsync(1);

			_repoMock
				.Setup(r => r.RemoveRange(It.IsAny<IEnumerable<PostTag>>()))
				.Callback((IEnumerable<PostTag> items) =>
				{
					var toRemove = items.ToList();

					foreach (var item in toRemove)
					{
						_postTags.Remove(item);
					}
				})
				.Returns(Task.CompletedTask);

			_service = new PostTagService(_repoMock.Object);
		}

		[Test]
		public async Task AddAsync_ShouldAddAllPostTagsAndSaveChanges()
		{
			var postId = Guid.NewGuid();
			var tagIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

			await _service.AddAsync(postId, tagIds);

			Assert.That(_postTags.Count, Is.EqualTo(2));

			Assert.That(_postTags.All(x => x.PostId == postId));
			Assert.That(_postTags.Select(x => x.TagId), Is.EquivalentTo(tagIds));

			_repoMock.Verify(r => r.AddAsync(It.IsAny<PostTag>()), Times.Exactly(2));
			_repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
		}

		[Test]
		public async Task GetAllTagIdsByPostIdAsync_ShouldReturnOnlyMatchingTagIds()
		{
			var postId = Guid.NewGuid();

			_postTags.Add(new PostTag { PostId = postId, TagId = Guid.NewGuid() });
			_postTags.Add(new PostTag { PostId = postId, TagId = Guid.NewGuid() });
			_postTags.Add(new PostTag { PostId = Guid.NewGuid(), TagId = Guid.NewGuid() });

			var result = await _service.GetAllTagIdsByPostIdAsync(postId);

			Assert.That(result.Count, Is.EqualTo(2));
			Assert.That(result.All(id => _postTags.Any(pt => pt.TagId == id && pt.PostId == postId)));
		}

		[Test]
		public async Task GetAllTagIdsByPostIdAsync_NoMatches_ShouldReturnEmpty()
		{
			var result = await _service.GetAllTagIdsByPostIdAsync(Guid.NewGuid());

			Assert.That(result, Is.Empty);
		}

		[Test]
		public async Task DeleteAsync_ShouldRemoveMatchingPostTags()
		{
			var tagId1 = Guid.NewGuid();
			var tagId2 = Guid.NewGuid();

			_postTags.Add(new PostTag { Id = Guid.NewGuid(), TagId = tagId1 });
			_postTags.Add(new PostTag { Id = Guid.NewGuid(), TagId = tagId2 });
			_postTags.Add(new PostTag { Id = Guid.NewGuid(), TagId = Guid.NewGuid() });

			await _service.DeleteAsync(new List<Guid> { tagId1, tagId2 });

			Assert.That(_postTags.Count, Is.EqualTo(1));
			Assert.That(_postTags.First().TagId != tagId1 && _postTags.First().TagId != tagId2);

			_repoMock.Verify(r => r.RemoveRange(It.IsAny<IQueryable<PostTag>>()), Times.Once);
		}


	}
}