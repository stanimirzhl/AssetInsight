using AssetInsight.Core.DTOs.Image_Error_Dto;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Implementations
{
	public class ImageService : IImageService
	{
		private readonly Cloudinary? cloudinary;

		public ImageService(Cloudinary? cloudinary = null)
		{
			this.cloudinary = cloudinary;
		}

		public async Task<(List<(string, string)>, List<ErrorImageDto>)> UploadPhotosAsync(List<IFormFile> Images, Guid postId)
		{
			List<(string, string)> uploadResults = new List<(string, string)>();
			List<ErrorImageDto> errorImages = new List<ErrorImageDto>();

			foreach (IFormFile image in Images)
			{
				try
				{
					ImageUploadResult result = await AddPhotoAsync(image, postId);

					uploadResults.Add((result.SecureUrl.ToString(), result.PublicId));
				}
				catch (Exception ex)
				{
					errorImages.Add(new ErrorImageDto
					{
						Name = image.FileName,
						Format = image.ContentType,
						Size = FormatFileSize(image.Length),
						Exception = ex
					});
				}
			}

			return (uploadResults, errorImages);
		}

		public async Task DeleteAsync(Guid postId, string[] images)
		{
			try
			{
				await cloudinary.DeleteResourcesAsync(ResourceType.Image, images);
			}
			catch(Exception ex)
			{
				throw new Exception("Image upload failed: " + ex.Message);
			}
		}

		private string FormatFileSize(long bytes)
		{
			const long KB = 1024;
			const long MB = KB * 1024;

			if (bytes >= MB)
				return $"{(bytes / (double)MB):0.##} MB";
			else
				return $"{(bytes / (double)KB):0.##} KB";
		}

		private async Task<ImageUploadResult> AddPhotoAsync(IFormFile image, Guid postId)
		{
			ImageUploadResult uploadResult = new ImageUploadResult();

			if (image.Length > 0)
			{
				try
				{
					using var stream = image.OpenReadStream();
					var uploadParams = new ImageUploadParams
					{
						File = new FileDescription(image.FileName, stream),
						Transformation = new Transformation().Quality("auto").FetchFormat("auto"),
						Folder = $"Posts/Post-{postId}/Images/"
					};

					uploadResult = await cloudinary.UploadAsync(uploadParams);

				}
				catch (Exception ex)
				{
					throw new Exception("Image upload failed: " + ex.Message);
				}
			}

			return uploadResult;
		}
	}
}
