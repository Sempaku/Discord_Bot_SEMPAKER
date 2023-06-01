using AngleSharp.Dom;
using CliWrap;
using Discord;
using Discord.Audio;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using DotNet.Docker.Service;
using System.Diagnostics;
using System.Text;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace DiscordBot_SEMPAKER.Modules.YouTubeModule
{
    /// <summary> Модуль взаимодействия с YouTube для Discord бота. </summary>
    [Group("youtube", "Модуль для работы с YouTube")]
    public class YouTubeInteractionModule : InteractionModuleBase<SocketInteractionContext>
    {
        private static IAudioClient? _audioClient;
        private static bool _isPlaying = false;
        private readonly MusicQueueService _musicQueueService;
        private readonly SelectMenuHandler _selectMenuHandler;

        public YouTubeInteractionModule(MusicQueueService musicQueueService, SelectMenuHandler selectMenuHandler)
        {
            _musicQueueService = musicQueueService;
            _selectMenuHandler = selectMenuHandler;
        }

        public async Task LeaveFromRoom()
        {
            if (_isPlaying)
            {
                // Если операция воспроизведения еще не завершена, ожидаем завершения
                await Task.Delay(10 * 1000); // Пример задержки в 10 секунду
                await LeaveFromRoom(); // Рекурсивно вызываем сам метод
                return;
            }
            if (_audioClient is not null)
                await _audioClient.StopAsync();
            _audioClient = null;
        }

        /// <summary> Обрабатывает команду "play_youtube" для воспроизведения музыки с YouTube. </summary>
        [SlashCommand("play", "Введите url видео для воспроизведения музыки с YouTube")]
        public async Task PlayYouTubeSong(string youtubeUrl, bool isRecurseHandle = false)
        {
            if (isRecurseHandle is false)
                await Context.Interaction.DeferAsync();

            if (_isPlaying is true)
            {
                await FollowupAsync($"Э сука! Бот уже играет музыку в комнате");
                return;
            }

            // проверка : находится ли бот в комнате если он уже в комнате, то он не будет
            // переподключаться в другую пока не выйдет из текущей

            SocketGuildUser user = (SocketGuildUser)Context.User;
            SocketVoiceChannel voiceChanel = user.VoiceChannel;
            if (voiceChanel is null)
            {
                if (isRecurseHandle is false)
                    await FollowupAsync("Ты дебил... В комнату блять зайди!");
                return;
            }
            _audioClient = await user.VoiceChannel.ConnectAsync();

            _isPlaying = true;
            if (isRecurseHandle is false)
                await FollowupAsync("Попер дрипчик!!!");

            // Получение аудио потока с помощью библиотеки YoutubeExplode
            YoutubeClient youTubeClient = new YoutubeClient();
            StreamManifest streamManifest = await youTubeClient.Videos.Streams.GetManifestAsync(youtubeUrl);
            IStreamInfo streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            Stream stream = await youTubeClient.Videos.Streams.GetAsync(streamInfo);

            // Преобразование аудио потока в формат, поддерживаемый Discord, с помощью инструмента ffmpeg
            var memoryStream = new MemoryStream();
#if DEBUG

            await Cli.Wrap(@$"D:\ffmpeg\bin\ffmpeg")
                .WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
                .WithStandardInputPipe(PipeSource.FromStream(stream))
                .WithStandardOutputPipe(PipeTarget.ToStream(memoryStream))
                .ExecuteAsync();
#else
            await Cli.Wrap("ffmpeg")
                .WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
                .WithStandardInputPipe(PipeSource.FromStream(stream))
                .WithStandardOutputPipe(PipeTarget.ToStream(memoryStream))
                .ExecuteAsync();
#endif

            try
            {
                // Воспроизведение аудио в голосовом канале
                var audioOutStream = _audioClient.CreatePCMStream(AudioApplication.Voice);
                await audioOutStream.WriteAsync(memoryStream.ToArray(), 0, (int)memoryStream.Length);
                await audioOutStream.FlushAsync();
            }
            finally
            {
                _isPlaying = false;
                if (!_musicQueueService.IsEmpty())
                {
                    await PlayYouTubeSong(_musicQueueService.Dequeue(), true);
                }
                else
                {
                    // Остановка воспроизведения и очистка ресурсов по завершении
                    await LeaveFromRoom();
                }
            }
        }

        /// <summary> Обрабатывает команду "stop_youtube" для остановки текущего трека. </summary>
        [SlashCommand("stop", "Выгнать ботика из голосового канала")]
        public async Task StopYouTubeSong()
        {
            await Context.Interaction.DeferAsync();
            if (_audioClient != null && _audioClient.ConnectionState == ConnectionState.Connected)
            {
                await FollowupAsync($"{Context.User.Username} убил весь вайб...");
                // Остановить воспроизведение
                await _audioClient.StopAsync();

                // Очистить состояние и ресурсы
                _audioClient?.Dispose();
                _audioClient = null;
            }
            else
            {
                // Бот не воспроизводит музыку
                await RespondAsync("Бот не воспроизводит музыку!");
            }
        }

        [SlashCommand("add", "Добавляет музыку в очередь")]
        public async Task AddUrlInMusicQueue(string url)
        {
            if (_isPlaying is false)
            {
                await RespondAsync("Какая впизду очередь сука! Бот сейчас не проигрывает музыку уебан!");
                return;
            }
            StringBuilder sb = new StringBuilder();
            _musicQueueService.Enqueue(url);

            YoutubeClient youTubeClient = new YoutubeClient();
            Video video = await youTubeClient.Videos.GetAsync(url);
            sb.Append($"Видео : {video.Title} [{video.Duration}] добавлено в очередь\n\n");
            sb.Append("Очередь: \n");
            foreach (var videoUrl in _musicQueueService.GetMusicUrlQueue())
            {
                var videoInfo = await youTubeClient.Videos.GetAsync(videoUrl);
                sb.Append($"{videoInfo.Title} [{videoInfo.Duration}]\n");
            }
            await RespondAsync(sb.ToString());
        }

        [SlashCommand("remove", "Удаляет музыку из очереди", false, RunMode.Async)]
        public async Task RemoveUrlFromMusicQueue()
        {
            if (_musicQueueService.IsEmpty())
            {
                await RespondAsync("Еблан! В очеререди 0 треков! Хули ты там удалять то собрался?");
                return;
            }

            var selectMenuBuilder = new SelectMenuBuilder()
                .WithPlaceholder("Выберите трек для удаления")
                .WithCustomId("deletetrack")
                .WithMinValues(1)
                .WithMaxValues(1);
            _selectMenuHandler.AddHandler(selectMenuBuilder.CustomId, async component => await DeleteTrack(component));

            YoutubeClient youTubeClient = new YoutubeClient();
            foreach (var videoUrl in _musicQueueService.GetMusicUrlQueue())
            {
                var videoInfo = await youTubeClient.Videos.GetAsync(videoUrl);
                selectMenuBuilder.AddOption($"{videoInfo.Title} [{videoInfo.Duration}]", videoUrl);
            }

            var componentBuilder = new ComponentBuilder()
                .WithSelectMenu(selectMenuBuilder);

            // Отправляем сообщение с Select Menu
            await RespondAsync("Какой трек удалять?", components: componentBuilder.Build());
        }

        private async Task DeleteTrack(SocketMessageComponent args)
        {
            _musicQueueService.RemoveFromQueue(args.Data.Values.First());
            await args.RespondAsync($"Трек удален из очереди...");
        }
    }
}