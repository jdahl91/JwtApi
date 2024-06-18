using JwtApi.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

try
{
    var builder = WebApplication.CreateBuilder(args);
    
    // May need this but lets try without it first
    // builder.Configuration.AddEnvironmentVariables();

    // Add services to the container.
    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    //builder.Services.AddEndpointsApiExplorer();
    //builder.Services.AddSwaggerGen();
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddScoped<NpgsqlConnection>(_ => new NpgsqlConnection(connectionString));

    //var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY"); // builder.Configuration["Jwt:Key"];
    //var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"); // builder.Configuration["Jwt:Issuer"];
    //var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"); // builder.Configuration["Jwt:Audience"];
    
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
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
    builder.Services.AddScoped<IAccount, Account>();
    builder.Services.AddScoped<IPasswordRepository, PasswordRepository>();
    
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    //if (app.Environment.IsDevelopment())
    //{
    //    app.UseSwagger();
    //    app.UseSwaggerUI();
    //}

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    using (var scope = app.Services.CreateScope())
    {
        var service = scope.ServiceProvider.GetRequiredService<IAccount>();
        await service.SeedAdminUser();
    }
    app.Run();
}
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine(ex.Message);
}
finally
{
    System.Diagnostics.Debug.WriteLine("Goodbye.");
}
