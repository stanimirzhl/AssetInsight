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
using static System.Net.Mime.MediaTypeNames;

namespace AssetInsight.Tests.Core.Implementations
{
	[TestFixture]
	public class SavedPostServiceTests
	{
		private Mock<IRepository<SavedPost>> _repoMock;
		private SavedPostService _service;
		private List<SavedPost> _savedPosts;

		[SetUp]
		public void SetUp()
		{
			_savedPosts = new List<SavedPost>();
			_repoMock = new Mock<IRepository<SavedPost>>();

			_repoMock
				.Setup(r => r.All())
				.Returns(() => _savedPosts.AsQueryable().BuildMockDbSet().Object);

			_repoMock
				.Setup(r => r.AllAsReadOnly())
				.Returns(() => _savedPosts.AsQueryable().BuildMockDbSet().Object);

			_repoMock
				.Setup(r => r.AddAsync(It.IsAny<SavedPost>()))
				.Callback((SavedPost sp) =>
				{
					sp.Id = _savedPosts.Count + 1;
					_savedPosts.Add(sp);
				})
				.Returns(Task.CompletedTask);

			_repoMock
				.Setup(r => r.DeleteAsync(It.IsAny<object>()))
				.Callback((object id) =>
				{
					var intId = (int)id;

					var img = _savedPosts.FirstOrDefault(x => x.Id == intId);
					if (img != null)
						_savedPosts.Remove(img);
				})
				.Returns(Task.CompletedTask);

			_service = new SavedPostService(_repoMock.Object);
		}

		[Test]
		public async Task ToggleSavePost_ShouldSavePost_WhenNotExisting()
		{
			var postId = Guid.NewGuid();
			var userId = "user1";

			var result = await _service.ToggleSavePost(postId, userId);

			Assert.That(result, Is.True);
			Assert.That(_savedPosts.Count, Is.EqualTo(1));

			Assert.That(_savedPosts.First().PostId, Is.EqualTo(postId));
			Assert.That(_savedPosts.First().UserId, Is.EqualTo(userId));

			_repoMock.Verify(r => r.AddAsync(It.IsAny<SavedPost>()), Times.Once);
		}

		[Test]
		public async Task ToggleSavePost_ShouldRemovePost_WhenAlreadySaved()
		{
			var postId = Guid.NewGuid();
			var userId = "user1";

			var saved = new SavedPost
			{
				Id = 1,
				PostId = postId,
				UserId = userId
			};

			_savedPosts.Add(saved);

			var result = await _service.ToggleSavePost(postId, userId);

			Assert.That(result, Is.False);
			Assert.That(_savedPosts, Is.Empty);

			_repoMock.Verify(r => r.DeleteAsync(1), Times.Once);
		}

		[Test]
		public async Task HasUserSavedPost_ShouldReturnTrue_WhenExists()
		{
			var postId = Guid.NewGuid();
			var userId = "user1";

			_savedPosts.Add(new SavedPost
			{
				Id = 1,
				PostId = postId,
				UserId = userId
			});

			var result = await _service.HasUserSavedPost(postId, userId);

			Assert.That(result, Is.True);
		}

		[Test]
		public async Task HasUserSavedPost_ShouldReturnFalse_WhenNotExists()
		{
			var result = await _service.HasUserSavedPost(Guid.NewGuid(), "user1");

			Assert.That(result, Is.False);
		}
	}
}