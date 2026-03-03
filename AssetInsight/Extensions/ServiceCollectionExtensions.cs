using AssetInsight.Core.Implementations;
using AssetInsight.Core.Interfaces;
using AssetInsight.Data;
using AssetInsight.Data.Common;
using AssetInsight.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

		public static IServiceCollection Authentication(this IServiceCollection services,
			IConfiguration configuration)
		{
			services.AddAuthentication()
			.AddCookie()
			.AddGoogle(options =>
			{
				options.ClientId = configuration["Authentication:Google:ClientId"];
				options.ClientSecret = configuration["Authentication:Google:ClientSecret"];

				options.Events.OnRemoteFailure = HandleRemoteFailure;
			})
			.AddFacebook(options =>
			{
				options.AppId = configuration["Authentication:Facebook:ClientId"];
				options.AppSecret = configuration["Authentication:Facebook:ClientSecret"];

				options.Events.OnRemoteFailure = HandleRemoteFailure;
			})
			.AddMicrosoftAccount(options =>
			{
				options.ClientId = configuration["Authentication:Microsoft:ClientId"];
				options.ClientSecret = configuration["Authentication:Microsoft:ClientSecret"];

				options.Events.OnRemoteFailure = HandleRemoteFailure;
			});

			return services;
		}

		private static Task HandleRemoteFailure(RemoteFailureContext context)
		{
			var logger = context.HttpContext.RequestServices
				.GetRequiredService<ILoggerFactory>()
				.CreateLogger("ExternalAuthentication");

			logger.LogWarning(context.Failure,
				"External authentication failed for provider {Provider}",
				context.Scheme.Name);

			context.Response.Redirect("/auth-failed");
			context.HandleResponse();
			return Task.CompletedTask;
		}

		public static IServiceCollection AddIdentityServices(this IServiceCollection services)
		{
			services.AddDefaultIdentity<User>(options =>
			{
				options.SignIn.RequireConfirmedAccount = false;
				options.Password.RequireDigit = true;
				options.Password.RequireNonAlphanumeric = true;
				options.Password.RequireLowercase = true;
				options.Password.RequireUppercase = true;
				options.Password.RequiredLength = 5;
				options.User.RequireUniqueEmail = true;
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

		public static IServiceCollection AddRouteOptions(this IServiceCollection services)
		{
			services.Configure<RouteOptions>(options =>
			 {
				 options.LowercaseUrls = true;
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
