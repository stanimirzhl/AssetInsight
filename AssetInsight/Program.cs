using AssetInsight.Core.Caches;
using AssetInsight.Data;
using AssetInsight.Extensions;
using AssetInsight.Hubs;
using AssetInsight.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

ServiceCollectionExtensions.InitializeLogger(builder.Services.BuildServiceProvider());

builder.Services.AddControllersWithViews();
builder.Services.AddDbServices(builder.Configuration);
builder.Services.AddAzureKeyVaultSecrets(builder.Configuration);
//builder.Configuration.MapCloudinarySecret();
builder.Services.AddCoreServices(builder.Configuration);
builder.Services.AddIdentityServices();
//builder.Configuration.MapFinnhubSecret();
//builder.Configuration.MapGoogleOAuthSecret();
//builder.Configuration.MapFacebookOAuthSecret();
//builder.Configuration.MapMicrosoftOAuthSecret();
builder.Services.Authentication(builder.Configuration);
builder.Services.AddRouteOptions();
builder.Services.AddAccountOptions();
builder.Services.Localization();
builder.Services.AddSignalR();
builder.Services.AddSingleton<NewsCacheService>();
builder.Services.AddHostedService<NewsBackgroundService>();


var app = builder.Build();

app.UseAppLocalization();

await app.ApplyDatabaseMigrations();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.MapHub<NotificationHub>("/notificationHub");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.MapControllerRoute(
	name: "areas",
	pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
