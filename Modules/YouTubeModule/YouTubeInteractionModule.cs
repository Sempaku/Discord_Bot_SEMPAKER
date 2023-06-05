using CliWrap;
using Discord;
using Discord.Audio;
using Discord.Interactions;
using Discord.WebSocket;
using DotNet.Docker.Service;
using System.Reflection.Metadata;
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
        private static bool _isPlaying;
        private static bool _isRecurseHandle;
        private readonly MusicQueueService _musicQueueService;
        private readonly SelectMenuHandler _selectMenuHandler;
        private readonly YoutubeClient _youTubeClient;

        public YouTubeInteractionModule(MusicQueueService musicQueueService, SelectMenuHandler selectMenuHandler)
        {
            _musicQueueService = musicQueueService;
            _selectMenuHandler = selectMenuHandler;
            _youTubeClient = new YoutubeClient();
            _isRecurseHandle = false;
        }

        /// <summary> Обрабатывает команду "play_youtube" для воспроизведения музыки с YouTube. </summary>
        [SlashCommand("play", "Введите url видео для воспроизведения музыки с YouTube")]
        public async Task PlayYouTubeSong(string url)
        {
            // проверка: если метод выполняется по запросу из кода, то взаимодействие не будет
            // отложенным, и не будет выводить сообщения
            if (!_isRecurseHandle)
                await Context.Interaction.DeferAsync();

            if (url.Length > 99)
            {
                await FollowupAsync("Url должен быть меньше 100 знаков!");
                return;
            }

            if (!(await ValidateYouTubeUrl(url)))
            {
                await FollowupAsync("Неверный url");
                return;
            }
            // проверка : находится ли бот в комнате если он уже в комнате, то он не будет
            // переподключаться в другую пока не выйдет из текущей
            if (_isPlaying)
            {
                await FollowupAsync($"Бот уже играет музыку в комнате");
                return;
            }

            SocketGuildUser user = (SocketGuildUser)Context.User;
            SocketVoiceChannel voiceChanel = user.VoiceChannel;
            if (voiceChanel is null)
            {
                if (_isRecurseHandle is false)
                    await FollowupAsync("Войдите в комнату!");
                return;
            }
            _audioClient = await user.VoiceChannel.ConnectAsync();

            _isPlaying = true;
            if (_isRecurseHandle is false)
                await FollowupAsync("Попер дрипчик!!!");

            // Получение аудио потока с помощью библиотеки YoutubeExplode
            StreamManifest streamManifest = await _youTubeClient.Videos.Streams.GetManifestAsync(url);
            IStreamInfo streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            Stream stream = await _youTubeClient.Videos.Streams.GetAsync(streamInfo);

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
                Video videoInfo = await _youTubeClient.Videos.GetAsync(url);
                await Context.Client.SetGameAsync($"{videoInfo.Author} - {videoInfo.Title}", type: ActivityType.Listening);
                // Воспроизведение аудио в голосовом канале
                var audioOutStream = _audioClient.CreatePCMStream(AudioApplication.Voice);
                await audioOutStream.WriteAsync(memoryStream.ToArray(), 0, (int)memoryStream.Length);
                await audioOutStream.FlushAsync();
            }
            finally
            {
                _isRecurseHandle = false;
                _isPlaying = false;
                if (!_musicQueueService.IsEmpty())
                {
                    _isRecurseHandle = true;
                    await PlayYouTubeSong(_musicQueueService.Dequeue());
                }
                else
                {
                    // Остановка воспроизведения и очистка ресурсов по завершении
                    _isRecurseHandle = true;
                    await LeaveFromVoiceRoom();
                }
            }
        }

        /// <summary> Обрабатывает команду "stop_youtube" для остановки текущего трека. </summary>
        [SlashCommand("skip", "Пропускает текущую аудиозапись")]
        public async Task SkipYouTubeSong()
        {
            await Context.Interaction.DeferAsync();
            if (_audioClient != null
                && _audioClient.ConnectionState == ConnectionState.Connected
                && _isPlaying == true)
            {
                await FollowupAsync($"{Context.User.Username} убил весь вайб...");
                // Остановить воспроизведение
                await _audioClient.StopAsync();

                // Очистить состояние и ресурсы
                /*_audioClient?.Dispose();
                _audioClient = null;*/
            }
            else
            {
                // Бот не воспроизводит музыку
                await FollowupAsync("Бот не воспроизводит музыку!");
            }
        }

        [SlashCommand("add", "Добавляет музыку в очередь")]
        public async Task AddUrlInMusicQueue(string url)
        {
            await Context.Interaction.DeferAsync();
            if (url.Length > 99)
            {
                await FollowupAsync("Url должен быть меньше 100 знаков!");
                return;
            }

            if (await ValidateYouTubeUrl(url) is false)
            {
                await FollowupAsync("Неверный url");
                return;
            }

            if (_isPlaying is false)
            {
                await FollowupAsync("Бот сейчас не проигрывает музыку!");
                return;
            }

            StringBuilder sb = new StringBuilder();
            _musicQueueService.Enqueue(url);

            Video video = await _youTubeClient.Videos.GetAsync(url);
            sb.Append($"Видео : {video.Title} [{video.Duration}] добавлено в очередь\n\n");
            StringBuilder sb2 = await PrintQueue();
            await FollowupAsync(sb.Append(sb2).ToString());
        }

        [SlashCommand("remove", "Удаляет музыку из очереди", false, RunMode.Async)]
        public async Task RemoveUrlFromMusicQueue()
        {
            await Context.Interaction.DeferAsync();

            if (_musicQueueService.IsEmpty())
            {
                await FollowupAsync("Очередь пустая!");
                return;
            }

            var selectMenuBuilder = new SelectMenuBuilder()
                .WithPlaceholder("Выберите трек для удаления")
                .WithCustomId("deletetrack")
                .WithMinValues(1)
                .WithMaxValues(1);
            _selectMenuHandler.AddHandler(selectMenuBuilder.CustomId, async component =>
            {
                try
                {
                    var selectedValue = component.Data.Values.First();
                    var videoUrl = selectedValue.Split("!~!").First();
                    _musicQueueService.RemoveFromQueue(videoUrl);
                }
                finally
                {
                    StringBuilder queue = await PrintQueue();
                    await component.FollowupAsync($"Трек удален из очереди...\n" + queue.ToString());
                }
            });

            foreach (var videoUrl in _musicQueueService.GetQueue())
            {
                var videoInfo = await _youTubeClient.Videos.GetAsync(videoUrl);
                var uniqueValue = $"{videoUrl}!~!{Guid.NewGuid()}";
                selectMenuBuilder.AddOption($"{videoInfo.Title} [{videoInfo.Duration}]", uniqueValue);
            }

            ComponentBuilder componentBuilder = new ComponentBuilder()
                .WithSelectMenu(selectMenuBuilder);
            MessageComponent deleteComponent = componentBuilder.Build();
            // Отправляем сообщение с Select Menu
            await FollowupAsync("Какой трек удалять?", components: deleteComponent);
        }

        [SlashCommand("leave", "Бот покидает голосовой канал")]
        public async Task LeaveFromVoiceRoom()
        {
            if (!_isRecurseHandle)
                await Context.Interaction.DeferAsync();

            if (_audioClient is not null)
            {
                _isPlaying = false;
                _musicQueueService.ClearQueue();
                await _audioClient.StopAsync();
                if (!_isRecurseHandle)
                    await FollowupAsync("Бот покинул голосовой канал. Очередь очищена.");
            }
            else
            {
                if (!_isRecurseHandle)
                    await FollowupAsync("Бот не находится в голосовом канале");
            }
            _isRecurseHandle = false;
        }

        [SlashCommand("check-queue", "Выводит очередь воспроизведения")]
        public async Task CheckQueue()
        {
            await Context.Interaction.DeferAsync();

            StringBuilder queue = await PrintQueue();
            await FollowupAsync(queue.ToString());
        }

        [SlashCommand("clear-queue", "Очищается очередь воспроизведения")]
        public async Task ClearQueue()
        {
            await Context.Interaction.DeferAsync();

            _musicQueueService.ClearQueue();
            await FollowupAsync("Очередь успешно очищена");
        }

        /*private async Task SkipMusicInPlayMethod()
        {
            if (_isPlaying)
            {
                // Если операция воспроизведения еще не завершена, ожидаем завершения
                await Task.Delay(10 * 1000); // Пример задержки в 10 секунду
                await SkipMusicInPlayMethod(); // Рекурсивно вызываем сам метод
                return;
            }
            if (_audioClient is not null)
                await _audioClient.StopAsync();
            _audioClient = null;
        }*/

        private async Task<StringBuilder> PrintQueue()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Очередь: \n");
            string[] musicUrlQueue = _musicQueueService.GetQueue().ToArray();

            for (int i = 0; i < musicUrlQueue.Length; i++)
            {
                string videoUrl = musicUrlQueue[i];
                Video videoInfo = await _youTubeClient.Videos.GetAsync(videoUrl);
                sb.Append($"{i + 1} - {videoInfo.Title} [{videoInfo.Duration}]\n");
            }

            return sb;
        }

        private async Task<bool> ValidateYouTubeUrl(string url)
        {
            try
            {
                StreamManifest streamManifest = await _youTubeClient.Videos.Streams.GetManifestAsync(url);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}