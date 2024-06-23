using Microsoft.AspNetCore.Mvc;
using JwtApi.Repositories;
using JwtApi.DTOs;
using static JwtApi.Responses.CustomResponses;
using Microsoft.AspNetCore.Authorization;

namespace JwtApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccount _account;

        public AccountController(IAccount account)
        {
            _account = account;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<RegistrationResponse>> RegisterAsync(RegisterDTO model)
        {
            var result = await _account.RegisterAsync(model);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> LoginAsync(LoginDTO model)
        {
            var result = await _account.LoginAsync(model);
            return Ok(result);
        }

        [Authorize(Roles = "User, Admin")]
        [HttpPost("logout")]
        public async Task<ActionResult<RegistrationResponse>> LogoutAsync()
        {
            string? authorizationHeader = HttpContext.Request.Headers.Authorization;
            string? expiredAccessToken = null;

            if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
            {
                expiredAccessToken = authorizationHeader.Substring("Bearer ".Length).Trim();
            }
            var result = await _account.LogoutAsync(expiredAccessToken!); // dangerous ! here -- may need revisiting
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] string refreshToken)
        {
            string? authorizationHeader = HttpContext.Request.Headers.Authorization;
            string? expiredAccessToken = null;

            if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
            {
                expiredAccessToken = authorizationHeader.Substring("Bearer ".Length).Trim();
            }
            var result = await _account.ObtainNewAccessToken(expiredAccessToken!, refreshToken);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("confirm-email")]
        public async Task<ActionResult<RegistrationResponse>> ConfirmEmail([FromQuery] string email, [FromQuery] string confirmToken)
        {
            var result = await _account.ConfirmEmail(email, confirmToken);
            return Ok(result);
        }

        [Authorize(Roles = "User, Admin")]
        [HttpPost("change-password")]
        public async Task<ActionResult<RegistrationResponse>> ChangePassword(ChangePwdDTO model)
        {
            var result = await _account.ChangePassword(model);
            return Ok(result);
        }


        [AllowAnonymous]
        [HttpGet("weather")]
        public ActionResult<WeatherForecast[]> GetWeatherForecast()
        {
            var startDate = DateOnly.FromDateTime(DateTime.Now);
            var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
            return Ok(Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = startDate.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = summaries[Random.Shared.Next(summaries.Length)]
            }).ToArray());
        }

        [Authorize(Roles = "User")]
        [HttpGet("cats")]
        public IActionResult GetRandomCats()
        {
            var CatNames = new[] { "Whiskers", "Mittens", "Shadow", "Simba", "Nala", "Oscar", "Luna", "Chloe", "Max", "Bella" };
            var CatBreeds = new[] { "Siamese", "Maine Coon", "Persian", "Ragdoll", "Bengal", "Sphynx", "Abyssinian", "Birman", "Russian Blue", "Scottish Fold" };
            var randomCats = Enumerable.Range(1, 3).Select(index => new
            {
                Name = CatNames[Random.Shared.Next(CatNames.Length)],
                Age = Random.Shared.Next(1, 15),
                Breed = CatBreeds[Random.Shared.Next(CatBreeds.Length)]
            }).ToArray();

            return Ok(randomCats);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("whales")]
        public IActionResult GetRandomWhales()
        {
            var WhaleNames = new[] { "Blue Whale", "Humpback Whale", "Orca", "Sperm Whale", "Beluga Whale", "Gray Whale", "Narwhal", "Bowhead Whale", "Fin Whale", "Minke Whale" };
            var WhaleSpecies = new[] { "Balaenopteridae", "Delphinidae", "Physeteridae", "Monodontidae", "Eschrichtiidae", "Balaenidae" };
            var Random = new Random();
            var randomWhales = Enumerable.Range(1, 3).Select(index => new
            {
                Name = WhaleNames[Random.Next(WhaleNames.Length)],
                Size = Random.Next(10, 30),
                Species = WhaleSpecies[Random.Next(WhaleSpecies.Length)]
            }).ToArray();

            return Ok(randomWhales);
        }
    }
}
