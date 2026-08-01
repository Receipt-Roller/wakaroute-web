using wakaroute_web.Services.UnderstandingMaps;
using wakaroute_web.Services.Schools;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IUnderstandingMapProvider, MathUnderstandingMapProvider>();
builder.Services.AddSingleton<IUnderstandingMapProvider, JapaneseUnderstandingMapProvider>();
builder.Services.AddSingleton<IUnderstandingMapProvider, EnglishUnderstandingMapProvider>();
builder.Services.AddSingleton<IUnderstandingMapProvider, ScienceUnderstandingMapProvider>();
builder.Services.AddSingleton<IUnderstandingMapProvider, SocialStudiesUnderstandingMapProvider>();
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
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
