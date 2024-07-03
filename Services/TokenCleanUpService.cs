using Npgsql;

namespace JwtApi.Services
{
    public class TokenCleanupService : IHostedService, IDisposable
    {
        private Timer? _timer = null;
        //private readonly ILogger<TokenCleanupService> _logger;
        private readonly NpgsqlConnection _connection;

        public TokenCleanupService(IConfiguration configuration, NpgsqlConnection connection) // ILogger<TokenCleanupService> logger, 
        {
            //_logger = logger;
            _connection = connection;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //_logger.LogInformation("Token Cleanup Service is starting.");

            var now = DateTime.Now;
            var nextMidnight = now.Date.AddDays(1); // Midnight of the next day
            var timeToFirstRun = nextMidnight - now;

            _timer = new Timer(DoWork, null, timeToFirstRun, TimeSpan.FromDays(1));

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            //_logger.LogInformation("Token Cleanup Service is working.");

            try
            {
                    await _connection.OpenAsync();

                    var deleteEmailConfirmTokensSql = @"
                    DELETE FROM emailconfirmtokens 
                    WHERE expirydate < NOW();";

                    using (var emailConfirmCommand = new NpgsqlCommand(deleteEmailConfirmTokensSql, _connection))
                    {
                        await emailConfirmCommand.ExecuteNonQueryAsync();
                    }

                    var deleteRefreshTokensSql = @"
                    DELETE FROM refreshtokens 
                    WHERE expirydate < NOW();";

                    using (var refreshCommand = new NpgsqlCommand(deleteRefreshTokensSql, _connection))
                    {
                        await refreshCommand.ExecuteNonQueryAsync();
                    }

                    await _connection.CloseAsync();

                //_logger.LogInformation("Token Cleanup Service completed work.");
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "An error occurred while cleaning up tokens.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //_logger.LogInformation("Token Cleanup Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
