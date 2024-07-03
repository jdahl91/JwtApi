using JwtApi.Repositories;
using JwtApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
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

    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
    var jwtKey = Environment.GetEnvironmentVariable("Jwt__Key");
    var jwtIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer");
    var jwtAudience = Environment.GetEnvironmentVariable("Jwt__Audience");

    //builder.Services.AddLogging();
    builder.Services.AddScoped(_ => new NpgsqlConnection(connectionString));
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
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
        };
    });
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(@"./keys"))
        .SetApplicationName("PwdMngrWasm");
    builder.Services.AddScoped<IAccount, Account>();
    builder.Services.AddScoped<IPasswordRepository, PasswordRepository>();
    //builder.Services.AddHostedService<TokenCleanupService>();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowBlazorWasmClient", builder =>
        {
            builder.WithOrigins("https://jdahl91.github.io/PwdMngr", "https://localhost:7282")
                .AllowAnyHeader()
                .AllowAnyMethod();
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
