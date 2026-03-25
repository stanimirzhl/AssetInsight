using AssetInsight.Core.DTOs.Image_Error_Dto;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.Interfaces
{
	public interface IImageService
	{
		//Task<ImageUploadResult> AddPhotoAsync(IFormFile image, Guid postId);

		Task<(List<(string, string)>, List<ErrorImageDto>)> UploadPhotosAsync(List<IFormFile> Images, Guid postId);

		Task DeleteAsync(Guid postId, string[] images);
	}
}
