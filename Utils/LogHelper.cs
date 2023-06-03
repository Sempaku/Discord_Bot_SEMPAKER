using Discord;
using Microsoft.Extensions.Logging;

namespace DiscordBot_SEMPAKER.Utils
{
    /// <summary>
    /// Данный код представляет вспомогательный класс LogHelper, который содержит метод OnLogAsync
    /// для обработки и журналирования сообщений о событиях в приложении с использованием интерфейса ILogger.
    /// </summary>
    public static class LogHelper
    {
        /// <summary>
        /// Метод осуществляет переключение в зависимости от серьезности сообщения (msg.Severity) и
        /// выполняет соответствующие действия
        /// </summary>
        /// <param name="logger">
        /// Объект, реализующий интерфейс ILogger, который используется для журналирования сообщений.
        /// </param>
        /// <param name="msg">   
        /// Объект типа LogMessage, представляющий сообщение о событии, которое нужно обработать и зарегистрировать.
        /// </param>
        /// <returns>
        /// Возвращается задача Task.CompletedTask, чтобы указать, что метод завершил свою работу.
        /// </returns>
        public static Task OnLogAsync(ILogger logger, LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Verbose:
                    logger.LogInformation("{Message}", msg.ToString());
                    break;

                case LogSeverity.Info:
                    logger.LogInformation("{Message}", msg.ToString());
                    break;

                case LogSeverity.Warning:
                    logger.LogWarning("{Message}", msg.ToString());
                    break;

                case LogSeverity.Error:
                    logger.LogError("{Message}", msg.ToString());
                    break;

                case LogSeverity.Critical:
                    logger.LogCritical("{Message}", msg.ToString());
                    break;
            }
            return Task.CompletedTask;
        }
    }
}