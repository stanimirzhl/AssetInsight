using AssetInsight.Core.Interfaces;
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

		public async Task<ImageUploadResult> AddPhotoAsync(List<IFormFile> Images)
		{
			var uploadResult = new ImageUploadResult();

			/*if (file.Length > 0)
			{
				using var stream = file.OpenReadStream();
				var uploadParams = new ImageUploadParams
				{
					File = new FileDescription(file.FileName, stream),
					Transformation = new Transformation().Height(500).Width("value").Crop("fill").Gravity("face"),
					Folder = "my-app-uploads" // Optional: specify a folder
				};
				uploadResult = await cloudinary.UploadAsync(uploadParams);
			}*/

			return uploadResult;
		}
	}
}
