var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. Add Session Support (Remembers the user's login)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 2. Add HttpContextAccessor (Allows our ApiService to read the session)
builder.Services.AddHttpContextAccessor();

// 3. Register the API Service
builder.Services.AddHttpClient();
builder.Services.AddScoped<RaceDay.MVC.Services.ApiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Forces the yellow error screen
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// 4. Enable Session (MUST be before UseRouting)
app.UseSession();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();