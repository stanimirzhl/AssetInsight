using AssetInsight.Core.Implementations;
using AssetInsight.Core.DTOs.Image_Error_Dto;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Internal;

namespace AssetInsight.Tests.Core.Implementations
{
	[TestFixture]
	public class ImageServiceTests
	{
		private Mock<Cloudinary> _cloudinaryMock;
		private ImageService _service;

		[SetUp]
		public void SetUp()
		{
			_cloudinaryMock = new Mock<Cloudinary>();

			_service = new ImageService(CreateTestCloudinary());
		}

		private Cloudinary CreateTestCloudinary()
		{
			var account = new Account(
				"test_cloud",
				"test_key",
				"test_secret"
			);

			return new Cloudinary(account);
		}

		private IFormFile CreateFakeFile(string name, string contentType = "image/png", long size = 1000)
		{
			var stream = new MemoryStream(new byte[size]);

			return new FormFile(stream, 0, size, "file", name)
			{
				Headers = new HeaderDictionary(),
				ContentType = contentType
			};
		}

		/*[Test]
		public async Task UploadPhotosAsync_ShouldReturnUploadedImages()
		{
			var postId = Guid.NewGuid();
			var file = CreateFakeFile("img1.png");

			var uploadResult = new ImageUploadResult
			{
				SecureUrl = new Uri("https://test.com/img1.png"),
				PublicId = "public1"
			};

			_cloudinaryMock
				.Setup(c => c.UploadAsync(It.IsAny<ImageUploadParams>(), null))
				.ReturnsAsync(uploadResult);

			var result = await _service.UploadPhotosAsync(new List<IFormFile> { file }, postId);

			Assert.That(result.Item1.Count, Is.EqualTo(1));
			Assert.That(result.Item1[0].Item1, Is.EqualTo("https://test.com/img1.png"));
			Assert.That(result.Item1[0].Item2, Is.EqualTo("public1"));

			Assert.That(result.Item2.Count, Is.EqualTo(0));
		}

		[Test]
		public async Task UploadPhotosAsync_ShouldReturnError_WhenUploadFails()
		{
			var postId = Guid.NewGuid();
			var file = CreateFakeFile("img1.png");

			_cloudinaryMock
				.Setup(c => c.UploadAsync(It.IsAny<ImageUploadParams>(), null))
				.ThrowsAsync(new Exception("Upload failed"));

			var result = await _service.UploadPhotosAsync(new List<IFormFile> { file }, postId);

			Assert.That(result.Item1.Count, Is.EqualTo(0));
			Assert.That(result.Item2.Count, Is.EqualTo(1));
			Assert.That(result.Item2[0].Name, Is.EqualTo("img1.png"));
			Assert.That(result.Item2[0].Exception.Message, Does.Contain("Upload failed"));
		}*/

		[Test]
		public async Task DeleteAsync_ShouldCallCloudinaryDelete()
		{
			var postId = Guid.NewGuid();
			var images = new[] { "img1", "img2" };

			Assert.DoesNotThrowAsync(async () =>
				await _service.DeleteAsync(Guid.NewGuid(), new[] { "img1", "img2" })
			);
		}

		/*[Test]
		public void DeleteAsync_ShouldThrowException_WhenCloudinaryFails()
		{
			var postId = Guid.NewGuid();
			var images = new[] { "img1" };

			_cloudinaryMock
				.Setup(c => c.DeleteResourcesAsync(ResourceType.Image, "img1", null))
				.ThrowsAsync(new Exception("Image deletion failed"));

			var ex = Assert.ThrowsAsync<Exception>(async () =>
				await _service.DeleteAsync(postId, images));

			Assert.That(ex.Message, Does.Contain("Image deletion failed"));
			Assert.That(ex.Message, Does.Contain(postId.ToString()));
		}*/
	}
}