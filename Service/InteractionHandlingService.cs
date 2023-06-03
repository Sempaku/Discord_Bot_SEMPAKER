using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot_SEMPAKER.Utils;
using DotNet.Docker.Service;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DiscordBot_SEMPAKER.Service
{
    /// <summary>
    /// Представляет сервис, обрабатывающий взаимодействия (интеракции) с клиентом Discord.
    /// </summary>
    public class InteractionHandlingService : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactions;
        private readonly IServiceProvider _services;
        private readonly ILogger<InteractionService> _logger;
        private readonly SelectMenuHandler _selectMenuHandler;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="InteractionHandlingService" /> с
        /// указанными параметрами.
        /// </summary>
        /// <param name="client">       Экземпляр клиента Discord. </param>
        /// <param name="interactions"> Сервис для обработки взаимодействий. </param>
        /// <param name="services">     Провайдер сервисов для внедрения зависимостей. </param>
        /// <param name="logger">       Логгер для записи событий взаимодействий. </param>
        public InteractionHandlingService(
            DiscordSocketClient client,
            InteractionService interactions,
            IServiceProvider services,
            ILogger<InteractionService> logger,
            SelectMenuHandler selectMenuHandler)
        {
            _client = client;
            _interactions = interactions;
            _services = services;
            _logger = logger;
            _selectMenuHandler = selectMenuHandler;

            _interactions.Log += msg => LogHelper.OnLogAsync(_logger, msg);
        }

        /// <summary> Запускает сервис обработки взаимодействий асинхронно. </summary>
        /// <param name="cancellationToken"> Токен отмены для остановки операции. </param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _client.InteractionCreated += HandleInteractions;
            _client.SelectMenuExecuted += _selectMenuHandler.HandleSelectMenu;
            _client.Ready += () => _interactions.RegisterCommandsGloballyAsync();
        }

        /// <summary> Останавливает сервис обработки взаимодействий. </summary>
        /// <param name="cancellationToken"> Токен отмены для остановки операции. </param>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _interactions.Dispose();
            return Task.CompletedTask;
        }

        /// <summary> Обрабатывает входящие взаимодействия с клиентом Discord. </summary>
        /// <param name="interaction"> Входящее взаимодействие. </param>
        private async Task HandleInteractions(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(_client, interaction);
                await _interactions.ExecuteCommandAsync(context, _services);
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.ToString());
                if (interaction.Type == InteractionType.ApplicationCommand)
                {
                    await interaction.GetOriginalResponseAsync()
                        .ContinueWith(msg => msg.Result.DeleteAsync());
                }
            }
        }
    }
}