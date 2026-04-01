using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

builder.Services.AddDbContext<AppDbContext>(op =>
{
    op.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    op.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

builder.Services.AddIdentity<Userinfo, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();