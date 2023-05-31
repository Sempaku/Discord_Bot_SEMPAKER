using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot_SEMPAKER.Service;
using DotNet.Docker.Modules.YouTubeModule;
using DotNet.Docker.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DiscordBot_SEMPAKER;

public class Program
{
    private static async Task Main(string[] args)
    {
        // для создания хоста, который обеспечивает основную конфигурацию приложения.
        using IHost host = Host.CreateDefaultBuilder(args)
            // ConfigureServices добавляются и настраиваются сервисы, необходимые для работы бота. В
            // данном случае добавляются и настраиваются следующие сервисы:
            .ConfigureServices(services =>
            {
                services.AddSingleton<MusicQueueService>();
                services.AddSingleton<SelectMenuHandler>();
                // Сервис для работы с Discord API и взаимодействия с Discord-сервером.
                services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    UseInteractionSnowflakeDate = false
                }));
                // Сервис для обработки взаимодействий (slash-команд) с ботом.
                services.AddSingleton<InteractionService>();
                // Зарегистрированный сервис, который будет обрабатывать взаимодействия с ботом и
                // выполнять соответствующие действия.
                services.AddHostedService<InteractionHandlingService>();
                // Зарегистрированный сервис, который будет запускать Discord-клиент и связывать его
                // с Discord-сервером.
                services.AddHostedService<DiscordStartupService>();
            })
            .Build();
        // Используется метод Build для создания хоста на основе настроек, и затем вызывается метод
        // RunAsync для запуска хоста и начала выполнения бота.
        await host.RunAsync();
    }
}