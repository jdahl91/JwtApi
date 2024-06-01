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

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<RegistrationResponse>> RegisterAsync(RegisterDTO model)
        {
            var result = await _account.RegisterAsync(model);
            return Ok(result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> LoginAsync(LoginDTO model)
        {
            var result = await _account.LoginAsync(model);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public ActionResult<LoginResponse> RefreshToken()
        {
            //var result = accountRepo.RefreshToken(model);
            //return Ok(result);
            return Ok();
        }

        // be aware this method is not async as of now
        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public ActionResult<RegistrationResponse> ConfirmEmail(string email, string confirmToken)
        {
            var result = _account.ConfirmEmail(email, confirmToken);
            return Ok(result);
        }

        [Authorize(Roles = "User, Admin")]
        [HttpPost("change-password")]
        public async Task<ActionResult<RegistrationResponse>> ChangePassword(ChangePwdDTO model)
        {
            var result = await _account.ChangePassword(model);
            return Ok(result);
        }

        // Roles work here on the controller, ok
        // this is for demonstration and to remind myself how the controller calls the repository
        [Authorize(Roles = "Admin")]
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
    }
}
