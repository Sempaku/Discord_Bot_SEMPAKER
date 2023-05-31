using CliWrap;
using Discord;
using Discord.Audio;
using Discord.Interactions;
using Discord.WebSocket;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace DiscordBot_SEMPAKER.Modules.InteractionModule
{
    /// <summary> Модуль взаимодействия с YouTube для Discord бота. </summary>
    public class YouTubeInteractionModule : InteractionModuleBase<SocketInteractionContext>
    {
        private static IAudioClient? _audioClient;
        private bool _isPlaying;

        /// <summary>
        /// Обрабатывает команду "join" для подключения бота к голосовой комнате, где находится пользователь.
        /// </summary>
        [SlashCommand("join", "Подключить бота к голосовой комнате, где находится пользователь", false, RunMode.Async)]
        public async Task JoinToRoom()
        {
            SocketGuildUser user = (SocketGuildUser)Context.User;
            SocketVoiceChannel? voiceChannel = user.VoiceChannel;
            if (_audioClient is null)
            {
                _audioClient = await voiceChannel.ConnectAsync();
            }
            else
            {
                await Context.Channel.SendMessageAsync("Бот уже находится в голосовом канале");
            }
        }

        /// <summary> Обрабатывает команду "leave" для выхода бота из голосовой комнаты. </summary>
        [SlashCommand("leave", "Бот покидает голосовую комнату", false, RunMode.Async)]
        public async Task HandleLeaveFromRoom()
        {
            if (_audioClient is null)
            {
                await Context.Channel.SendMessageAsync("Бот не находится в комнате!");
                return;
            }
            if (_isPlaying)
            {
                // Если операция воспроизведения еще не завершена, ожидаем завершения
                await Task.Delay(10 * 1000); // Пример задержки в 10 секунду
                await HandleLeaveFromRoom(); // Рекурсивно вызываем сам метод
                return;
            }

            await _audioClient.StopAsync();
        }

        /// <summary> Обрабатывает команду "play_youtube" для воспроизведения музыки с YouTube. </summary>
        [SlashCommand("play_youtube", "Введите url видео для воспроизведения музыки с YouTube")]
        public async Task PlayYouTubeSong(string url)
        {
            if (_audioClient is null)
            {
                await Context.Channel.SendMessageAsync("Бот не находится в комнате!");
                return;
            }

            string projectDirectory = Environment.CurrentDirectory;
            _isPlaying = true;

            // Получение аудио потока с помощью библиотеки YoutubeExplode
            YoutubeClient youTubeClient = new YoutubeClient();
            StreamManifest streamManifest = await youTubeClient.Videos.Streams.GetManifestAsync(url);
            IStreamInfo streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            Stream stream = await youTubeClient.Videos.Streams.GetAsync(streamInfo);

            // Преобразование аудио потока в формат, поддерживаемый Discord, с помощью инструмента ffmpeg
            var memoryStream = new MemoryStream();
            await Cli.Wrap("ffmpeg")
                .WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
                .WithStandardInputPipe(PipeSource.FromStream(stream))
                .WithStandardOutputPipe(PipeTarget.ToStream(memoryStream))
                .ExecuteAsync();

            try
            {
                // Воспроизведение аудио в голосовом канале
                var audioOutStream = _audioClient.CreatePCMStream(AudioApplication.Voice);
                await audioOutStream.WriteAsync(memoryStream.ToArray(), 0, (int)memoryStream.Length);
                await audioOutStream.FlushAsync();
                _isPlaying = false;
            }
            finally
            {
                // Остановка воспроизведения и очистка ресурсов по завершении
                await HandleLeaveFromRoom();
            }
        }

        /// <summary> Обрабатывает команду "stop_youtube" для остановки текущего трека. </summary>
        [SlashCommand("stop_youtube", "Остановить текущий трек")]
        public async Task StopYouTubeSong()
        {
            if (_audioClient != null
                && _audioClient.ConnectionState == ConnectionState.Connected)
            {
                // Остановить воспроизведение
                await _audioClient.StopAsync();

                // Очистить состояние и ресурсы
                _audioClient.Dispose();
                _audioClient = null;
            }
            else
            {
                // Бот не воспроизводит музыку
                await Context.Channel.SendMessageAsync("Бот не воспроизводит музыку!");
            }
        }
    }
}