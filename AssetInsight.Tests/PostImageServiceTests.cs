using AssetInsight.Core.DTOs.Post_Image;
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
	public class PostImageServiceTests
	{
		private Mock<IRepository<PostImage>> _repoMock;
		private PostImageService _service;
		private List<PostImage> _images;

		[SetUp]
		public void SetUp()
		{
			_images = new List<PostImage>();
			_repoMock = new Mock<IRepository<PostImage>>();

			_repoMock
				.Setup(r => r.AllAsReadOnly())
				.Returns(() => _images.AsQueryable().BuildMockDbSet().Object);

			_repoMock
				.Setup(r => r.AddAsync(It.IsAny<PostImage>()))
				.Callback((PostImage img) =>
				{
					img.Id = _images.Count + 1;
					_images.Add(img);
				})
				.Returns(Task.CompletedTask);

			_repoMock
				.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
				.ReturnsAsync((int id) => _images.FirstOrDefault(x => x.Id == id));

			_repoMock
				.Setup(r => r.DeleteAsync(It.IsAny<object>()))
				.Callback((object id) =>
				{
					var intId = (int)id;

					var img = _images.FirstOrDefault(x => x.Id == intId);
					if (img != null)
						_images.Remove(img);
				})
				.Returns(Task.CompletedTask);

			_service = new PostImageService(_repoMock.Object);
		}

		[Test]
		public async Task AddAsync_ShouldAddAllImages()
		{
			var postId = Guid.NewGuid();

			var input = new List<(string, string)>
			{
				("url1", "public1"),
				("url2", "public2")
			};

			await _service.AddAsync(input, postId);

			Assert.That(_images.Count, Is.EqualTo(2));
			Assert.That(_images.All(x => x.PostId == postId));
			Assert.That(_images[0].ImgUrl, Is.EqualTo("url1"));
			Assert.That(_images[1].PublicId, Is.EqualTo("public2"));
		}

		[Test]
		public async Task GetAllByPostIdAsync_ShouldReturnOnlyMatchingImages()
		{
			var postId = Guid.NewGuid();
			var otherPostId = Guid.NewGuid();

			_images.Add(new PostImage { Id = 1, ImgUrl = "a", PublicId = "a", PostId = postId });
			_images.Add(new PostImage { Id = 2, ImgUrl = "b", PublicId = "b", PostId = otherPostId });

			var result = await _service.GetAllByPostIdAsync(postId);

			Assert.That(result.Count, Is.EqualTo(1));
			Assert.That(result[0].ImgUrl, Is.EqualTo("a"));
		}

		[Test]
		public async Task GetAllByPostIdAsync_NoImages_ShouldReturnEmpty()
		{
			var result = await _service.GetAllByPostIdAsync(Guid.NewGuid());

			Assert.That(result, Is.Empty);
		}

		[Test]
		public async Task DeleteAsync_ShouldRemoveImagesBelongingToPost()
		{
			var postId = Guid.NewGuid();

			_images.Add(new PostImage { Id = 1, PostId = postId });
			_images.Add(new PostImage { Id = 2, PostId = postId });
			_images.Add(new PostImage { Id = 3, PostId = Guid.NewGuid() });

			await _service.DeleteAsync(postId, new List<int> { 1, 2, 3 });

			Assert.That(_images.Count, Is.EqualTo(1));
			Assert.That(_images.First().Id, Is.EqualTo(3));
		}

		[Test]
		public async Task DeleteAsync_ShouldNotDeleteImagesFromOtherPosts()
		{
			var postId = Guid.NewGuid();
			var otherPostId = Guid.NewGuid();

			_images.Add(new PostImage { Id = 1, PostId = otherPostId });

			await _service.DeleteAsync(postId, new List<int> { 1 });

			Assert.That(_images.Count, Is.EqualTo(1));

			_repoMock.Verify(r => r.DeleteAsync(It.IsAny<object>()), Times.Never);
		}

		[Test]
		public async Task DeleteAsync_ShouldDoNothing_WhenImageDoesNotExist()
		{
			var postId = Guid.NewGuid();

			await _service.DeleteAsync(postId, new List<int> { 99 });

			_repoMock.Verify(r => r.DeleteAsync(It.IsAny<object>()), Times.Never);
		}


	}
}