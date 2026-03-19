using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Core.DTOs.Image_Error_Dto
{
	public class ErrorImageDto
	{
		public string Name { get; set; }
		public string Format { get; set; }
		public string Size { get; set; }
		public Exception Exception { get; set; }

		public override string ToString()
		{
			return $"File name: {this.Name}, content format: {this.Format}, size: {this.Size}";
		}
	}
}
