//using JwtApi.DTOs;
//using JwtApi.Responses;
//using System.Runtime.CompilerServices;

//namespace JwtApi.Services
//{
//    public class AccountService : IAccountService
//    {
//        private readonly HttpClient httpClient;
//        private const string BaseUrl = "api/account";

//        public AccountService(HttpClient httpClient)
//        {
//            this.httpClient = httpClient;
//        }

//        public async Task<WeatherForecast[]?> GetWeatherForecasts()
//        {
//            GetProtectedClient();
//            var response = await httpClient.GetAsync($"{BaseUrl}/weather");
//            bool check = CheckIfUnauthorized(response);
//            if (check)
//            {
//                await GetRefreshToken();
//                return await GetWeatherForecasts();
//            }
//            return await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
//        }

//        private async Task GetRefreshToken()
//        {
//        //     var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/refresh-token", new UserSession() { ExpiringJwtToken = Constants.JwtToken });
//        //     var result = await response.Content.ReadFromJsonAsync<CustomResponses.LoginResponse>();
//        //     Constants.JwtToken = result!.JwtToken;
//        }

//        private static bool CheckIfUnauthorized(HttpResponseMessage httpResponseMessage)
//        {
//            if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Unauthorized)
//                return true;
//            else return false;
//        }

//        private void GetProtectedClient()
//        {
//            // if (Constants.JwtToken == "") return;
//            // httpClient.DefaultRequestHeaders.Authorization =
//            //     new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Constants.JwtToken);
//        }

//        public async Task<CustomResponses.RegistrationResponse> ConfirmEmailAddress(string email, string confirmToken)
//        {
//            if (email == null || confirmToken == null)
//                return new CustomResponses.RegistrationResponse(false, "Invalid request.");

//            var response = await httpClient.PostAsync($"{BaseUrl}/confirm-email?email={email}&confirmToken={confirmToken}", null);
//            var result = await response.Content.ReadFromJsonAsync<CustomResponses.RegistrationResponse>();
//            return result!;
//        }

//        public async Task<CustomResponses.LoginResponse> LoginAsync(LoginDTO model)
//        {
//            var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/login", model);
//            var result = await response.Content.ReadFromJsonAsync<CustomResponses.LoginResponse>();
//            return result!; // I added the ! here, not the video
//        }

//        public async Task<CustomResponses.RegistrationResponse> RegisterAsync(RegisterDTO model)
//        {
//            var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/register", model);
//            var result = await response.Content.ReadFromJsonAsync<CustomResponses.RegistrationResponse>();
//            return result!; // I added the ! here, not the video
//        }

//        public async Task<CustomResponses.LoginResponse> RefreshToken(UserSession userSession)
//        {
//            var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/refresh-token", userSession);
//            var result = await response.Content.ReadFromJsonAsync<CustomResponses.LoginResponse>();
//            return result!;
//        }
//    }
//}
