using AssetInsight.Core.Implementations;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AssetInsight.Extensions
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddCoreServices(this IServiceCollection services)
		{
			services.AddScoped<IPostService, PostService>();

			services.AddMvc(options =>
				options
				.Filters
				.Add(new AutoValidateAntiforgeryTokenAttribute()));

			services.AddResponseCompression();

			return services;
		}

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

		public static IServiceCollection AddAccountOptions(this IServiceCollection services)
		{
			services.ConfigureApplicationCookie(options =>
			{
				options.LoginPath = "/Account/Login";
				options.LogoutPath = "/Account/Logout";
				options.AccessDeniedPath = "/Account/AccessDenied";
				options.ReturnUrlParameter = "ReturnUrl";
			});

			return services;
		}

		public static IServiceCollection Localization(this IServiceCollection services)
		{
			services.AddLocalization(options =>
			   options.ResourcesPath = "Resources");

			services.AddControllersWithViews()
				.AddViewLocalization()
				.AddDataAnnotationsLocalization(options =>
				{
					options.DataAnnotationLocalizerProvider = (type, factory) =>
					{
						var typeName = type.DeclaringType != null
							? $"{type.DeclaringType.Name}.{type.Name}"
							: type.Name;

						return factory.Create(typeName, type.Assembly.GetName().Name);
					};
				});

			return services;
		}

		public static IApplicationBuilder UseAppLocalization(this IApplicationBuilder app)
		{
			var supportedCultures = new[]
			{
				new CultureInfo("en"),
				new CultureInfo("bg"),
				new CultureInfo("de"),
				new CultureInfo("fr"),
				new CultureInfo("es")
			};

			var localizationOptions = new RequestLocalizationOptions
			{
				DefaultRequestCulture = new RequestCulture("en"),
				SupportedCultures = supportedCultures,
				SupportedUICultures = supportedCultures,

				RequestCultureProviders = new List<IRequestCultureProvider>
				{
					 new CookieRequestCultureProvider(),            
				     new AcceptLanguageHeaderRequestCultureProvider()
				}
			};

			return app.UseRequestLocalization(localizationOptions);
		}

		public static async Task ApplyDatabaseMigrations(this IHost app)
		{
			using IServiceScope scope = app.Services.CreateScope();
			AssetInsightDbContext db = scope.ServiceProvider.GetRequiredService<AssetInsightDbContext>();
			RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
			UserManager<User> userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

			await db.Database.MigrateAsync();
		}
	}
}
