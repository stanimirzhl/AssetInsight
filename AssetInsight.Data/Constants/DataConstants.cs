using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetInsight.Data.Constants
{
	public static class DataConstants
	{
		public static class PostConstants
		{
			public const int PostTitleMaxLength = 300;
			public const int PostTitleMinLength = 5;

			public const int PostContentMaxLength = 5_000;
			public const int PostContentMinLength = 15;
		}

		public static class CommentConstants
		{
			public const int CommentContentMaxLength = 500;
			public const int CommentContentMinLength = 2;
		}

		public static class UserConstants
		{
			public const int UserFirstNameMaxLength = 25;
			public const int UserFirstNameMinLength = 2;

			public const int UserLastNameMaxLength = 30;
			public const int UserLastNameMinLength = 2;

			public const int UserNameMaxLength = 25;
			public const int UserNameMinLength = 4;

			public const int UserPasswordMaxLength = 25;
			public const int UserPasswordMinLength = 5;

			public enum UserStatus
			{
				Pending = 0,
				Approved = 1
			}
		}
	}
}
