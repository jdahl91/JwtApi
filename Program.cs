using JwtApi.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

try
{
    Console.WriteLine("Launching Web API.");
    var builder = WebApplication.CreateBuilder(args);
    
    // Add services to the container.
    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    //builder.Services.AddEndpointsApiExplorer();
    //builder.Services.AddSwaggerGen();
    // builder.Configuration.AddEnvironmentVariables();

    // var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
    var jwtKey = Environment.GetEnvironmentVariable("Jwt__Key"); // builder.Configuration["Jwt:Key"];
    var jwtIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer"); // builder.Configuration["Jwt:Issuer"];
    var jwtAudience = Environment.GetEnvironmentVariable("Jwt__Audience"); // builder.Configuration["Jwt:Audience"];

    builder.Services.AddScoped<NpgsqlConnection>(_ => new NpgsqlConnection(connectionString));
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
        .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer, // builder.Configuration["Jwt:Issuer"],
            ValidAudience = jwtAudience, // builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!)) // builder.Configuration["Jwt:Key"]
        };
    });
    builder.Services.AddScoped<IAccount, Account>();
    builder.Services.AddScoped<IPasswordRepository, PasswordRepository>();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowBlazorWasmClient", builder =>
        {
            builder.WithOrigins("https://localhost:7282").AllowAnyHeader().AllowAnyMethod();  // Blazor WASM app origin
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    //if (app.Environment.IsDevelopment())
    //{
    //    app.UseSwagger();
    //    app.UseSwaggerUI();
    //}

    app.UseHttpsRedirection();
    app.UseCors("AllowBlazorWasmClient");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    //using (var scope = app.Services.CreateScope())
    //{
    //    var service = scope.ServiceProvider.GetRequiredService<IAccount>();
    //    await service.SeedAdminUser();
    //}
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}
finally
{
    Console.WriteLine("\nGoodbye.");
}
