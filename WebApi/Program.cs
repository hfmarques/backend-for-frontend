using WebApi;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add HttpClient for API calls
builder.Services.AddHttpClient();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "cookie";
    })
    .AddCookie("cookie", options =>
    {
        options.Cookie.Name = "__Host-AuthCookie";
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.Path = "/";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(1);
        options.SlidingExpiration = true;
        options.LoginPath = "/bff/SignInUser";
    });

builder.Services.AddAuthorization();

builder.Services.AddOpenIdConnectAccessTokenManagement(o =>
{
    o.RefreshBeforeExpiration = TimeSpan.FromSeconds(15);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localtest.me:5001")
            .AllowAnyMethod()
            .WithHeaders("X-CSRF", "Content-Type")
            .AllowCredentials()
            // Optionally cache for 10 minutes
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseCors();
app.CheckForCsrfHeader();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.RunAsync();