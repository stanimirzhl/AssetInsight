using AssetInsight.Core.DTOs.Tag;
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
	public class TagServiceTests
	{
		private Mock<IRepository<Tag>> _repoMock;
		private TagService _tagService;
		private List<Tag> _tags;

		[SetUp]
		public void SetUp()
		{
			_tags = new List<Tag>();
			_repoMock = new Mock<IRepository<Tag>>();

			_repoMock
				.Setup(r => r.AllAsReadOnly())
				.Returns(() => _tags.AsQueryable().BuildMockDbSet().Object);

			_repoMock
				.Setup(r => r.All())
				.Returns(() => _tags.AsQueryable().BuildMockDbSet().Object);

			_repoMock
				.Setup(r => r.AddAsync(It.IsAny<Tag>()))
				.Callback((Tag tag) =>
				{
					tag.Id = Guid.NewGuid();
					_tags.Add(tag);
				})
				.Returns(Task.CompletedTask);

			_tagService = new TagService(_repoMock.Object);
		}

		[Test]
		public async Task ExtractAndAddTagsIfAny_ShouldAddNewTagsAndReturnIds()
		{
			var content = "this is a #test tag with #CSharp and #dotnet";

			var result = await _tagService.ExtractAndAddTagsIfAny(content);

			Assert.That(result.Count, Is.EqualTo(3));
			Assert.That(_tags.Count, Is.EqualTo(3));

			Assert.That(_tags.Any(t => t.Name == "test"));
			Assert.That(_tags.Any(t => t.Name == "csharp"));
			Assert.That(_tags.Any(t => t.Name == "dotnet"));
		}

		[Test]
		public async Task ExtractAndAddTagsIfAny_ShouldNotDuplicateTags()
		{
			var content = "#test #test #test";

			var result = await _tagService.ExtractAndAddTagsIfAny(content);

			Assert.That(result.Count, Is.EqualTo(1));
			Assert.That(_tags.Count, Is.EqualTo(1));
		}

		[Test]
		public async Task ExtractAndAddTagsIfAny_NoTags_ShouldReturnEmptyList()
		{
			var result = await _tagService.ExtractAndAddTagsIfAny("no hashtags here");

			Assert.That(result, Is.Empty);
			Assert.That(_tags.Count, Is.EqualTo(0));
		}

		[Test]
		public async Task ExtractAndAddTagsIfAny_ShouldReuseExistingTags()
		{
			var existing = new Tag { Id = Guid.NewGuid(), Name = "test" };
			_tags.Add(existing);

			var content = "#test #newtag";

			var result = await _tagService.ExtractAndAddTagsIfAny(content);

			Assert.That(result.Count, Is.EqualTo(2));
			Assert.That(_tags.Count, Is.EqualTo(2));

			_repoMock.Verify(r => r.AddAsync(It.IsAny<Tag>()), Times.Once);
		}

		[Test]
		public async Task GetAllTagsByPostId_ShouldReturnTags()
		{
			var postId = Guid.NewGuid();

			var tag = new Tag
			{
				Id = Guid.NewGuid(),
				Name = "test",
				PostTags = new List<PostTag>
				{
					new PostTag { PostId = postId }
				}
			};

			_tags.Add(tag);

			var result = await _tagService.GetAllTagsbyPostId(postId);

			Assert.That(result.Count, Is.EqualTo(1));
			Assert.That(result[0].Name, Is.EqualTo("test"));
		}
	}
}