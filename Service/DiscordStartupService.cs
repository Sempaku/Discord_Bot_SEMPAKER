using Discord.WebSocket;
using DiscordBot_SEMPAKER.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBot_SEMPAKER.Service
{
    /// <summary>
    /// Представляет сервис, управляющий запуском и остановкой клиента Discord при запуске и
    /// завершении приложения.
    /// </summary>
    public class DiscordStartupService : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly ILogger<DiscordSocketClient> _logger;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="DiscordStartupService" /> с указанным
        /// клиентом Discord и логгером.
        /// </summary>
        /// <param name="client"> Экземпляр клиента Discord </param>
        /// <param name="logger"> Логгер для записи событий клиента </param>
        public DiscordStartupService(DiscordSocketClient client, ILogger<DiscordSocketClient> logger)
        {
            _client = client;
            _logger = logger;

            _client.Log += msg => LogHelper.OnLogAsync(_logger, msg);
        }

        /// <summary> Запускает клиент Discord асинхронно. </summary>
        /// <param name="cancellationToken"> Токен отмены для остановки операции. </param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _client.LoginAsync(Discord.TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_TOKEN_SEMPAKER"));
            await _client.StartAsync();
        }

        /// <summary> Останавливает клиент Discord асинхронно. </summary>
        /// <param name="cancellationToken"> Токен отмены для остановки операции. </param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.LogoutAsync();
            await _client.StopAsync();
        }
    }
}