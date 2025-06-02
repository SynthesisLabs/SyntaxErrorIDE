using System;
using System.Net.Http.Headers;
using DotNetEnv;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SyntaxErrorIDE.app.Services;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

builder.Services.AddRazorPages();

builder.Services.AddHttpClient("GitHub", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "SyntaxErrorIDE");
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    
    var token = builder.Configuration["GitHub:Token"];
    if (!string.IsNullOrEmpty(token))
    {
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
    }
});

builder.Services.AddScoped<LoginService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllers();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseSession();

app.MapControllers();

app.MapControllerRoute(
    name: "login",
    pattern: "{controller=Account}/{action=login}/{name?}/{password?}");

app.MapControllerRoute(
    name: "register",
    pattern: "{controller=Account}/{action=register}/{name?}/{email?}/{password?}/{passwordRepeat?}");

app.MapRazorPages();


app.Run();  