using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Parking.Core.Services;
using Parking.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ParkingDbContext>(o => o.UseSqlite(DbPaths.ConnectionString));
builder.Services.AddDefaultIdentity<IdentityUser>(o =>
{
    o.SignIn.RequireConfirmedAccount = false;
    o.Password.RequireNonAlphanumeric = false;
    o.Password.RequiredLength = 6;
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<ParkingDbContext>();
builder.Services.AddScoped<IParkingRepository, EfParkingRepository>();
builder.Services.AddScoped<ParkingService>(sp => new ParkingService(sp.GetRequiredService<IParkingRepository>()));
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())   // BD + roles + usuario admin
{
    var sp = scope.ServiceProvider;
    await sp.GetRequiredService<ParkingDbContext>().Database.EnsureCreatedAsync();
    var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var r in new[] { "Admin", "Operador" })
        if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new IdentityRole(r));
    var users = sp.GetRequiredService<UserManager<IdentityUser>>();
    if (await users.FindByEmailAsync("admin@parking.com") is null)
    {
        var u = new IdentityUser { UserName = "admin@parking.com", Email = "admin@parking.com", EmailConfirmed = true };
        await users.CreateAsync(u, "Admin123!");
        await users.AddToRoleAsync(u, "Admin");
    }
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();

// Todo usuario autenticado sin rol recibe "Operador" automáticamente
app.Use(async (ctx, next) =>
{
    if (ctx.User.Identity?.IsAuthenticated == true && !ctx.User.IsInRole("Admin") && !ctx.User.IsInRole("Operador"))
    {
        var um = ctx.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
        var u = await um.GetUserAsync(ctx.User);
        if (u is not null)
        {
            await um.AddToRoleAsync(u, "Operador");
            await ctx.RequestServices.GetRequiredService<SignInManager<IdentityUser>>().RefreshSignInAsync(u);
            ctx.Response.Redirect(ctx.Request.Path + ctx.Request.QueryString);
            return;
        }
    }
    await next();
});

app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Parking}/{action=Index}/{id?}");
app.MapRazorPages();
app.Run();
