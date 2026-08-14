using wakaroute_web.Services.UnderstandingMaps;
using wakaroute_web.Services.Schools;
using wakaroute_web.Services.Manabu2;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/xml"]);
});
builder.Services.AddSingleton<IUnderstandingMapProvider, MathUnderstandingMapProvider>();
builder.Services.AddSingleton<IUnderstandingMapProvider, JapaneseUnderstandingMapProvider>();
builder.Services.AddSingleton<IUnderstandingMapProvider, EnglishUnderstandingMapProvider>();
builder.Services.AddSingleton<IUnderstandingMapProvider, ScienceUnderstandingMapProvider>();
builder.Services.AddSingleton<IUnderstandingMapProvider, SocialStudiesUnderstandingMapProvider>();
builder.Services.Configure<Manabu2Options>(builder.Configuration.GetSection(Manabu2Options.SectionName));
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient(Manabu2CatalogClient.HttpClientName);
builder.Services.AddSingleton<Manabu2CatalogClient>();
builder.Services.AddSingleton<IUnderstandingMapCatalog, Manabu2UnderstandingMapCatalog>();
builder.Services.AddSingleton<ISchoolCatalog, JsonSchoolCatalog>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.Use(async (context, next) =>
{
    var extension = Path.GetExtension(context.Request.Path.Value ?? string.Empty).ToLowerInvariant();
    var isVersionedAsset = context.Request.Query.ContainsKey("v") &&
        extension is ".css" or ".js" or ".svg" or ".png" or ".jpg" or ".jpeg" or ".webp" or ".woff2" or ".ico";
    if (isVersionedAsset)
    {
        context.Response.OnStarting(() =>
        {
            if (context.Response.StatusCode == StatusCodes.Status200OK)
            {
                context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
            }
            return Task.CompletedTask;
        });
    }

    await next();
});
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
