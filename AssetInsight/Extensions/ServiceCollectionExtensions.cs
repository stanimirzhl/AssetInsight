using AssetInsight.Data;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetInsight.Extensions
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddDbServices(this IServiceCollection services, IConfiguration config)
		{
			string connectionString = config.GetConnectionString("AssetInsightContextConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
			services.AddDbContext<AssetInsightDbContext>(options =>
				options.UseSqlServer(connectionString));

			services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

			return services;
		}

		public static IServiceCollection AddIdentityServices(this IServiceCollection services)
		{
			services.AddDefaultIdentity<User>(options =>
			{
				options.SignIn.RequireConfirmedAccount = false;
				options.Password.RequireDigit = false;
				options.Password.RequireNonAlphanumeric = false;
				options.Password.RequireLowercase = false;
				options.Password.RequireUppercase = false;
				options.Password.RequiredLength = 5;
			})
			.AddRoles<IdentityRole>()
			.AddEntityFrameworkStores<AssetInsightDbContext>();

			return services;
		}
	}
}
