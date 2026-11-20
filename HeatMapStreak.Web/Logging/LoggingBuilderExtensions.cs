using Microsoft.Extensions.Logging;

namespace HeatMapStreak.Web.Logging;

public static class LoggingBuilderExtensions
{
    /// <summary>
    /// Appends application logs to <c>{log dir}/app-yyyy-MM-dd.log</c>.
    /// The directory defaults to <c>~/magnus/heatmap-streak/logs</c> and can be
    /// overridden with <c>Logging:File:Path</c>.
    /// </summary>
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, IConfiguration configuration)
    {
        builder.Services.AddSingleton<ILoggerProvider>(_ =>
        {
            var configured = configuration["Logging:File:Path"];
            var directory = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "magnus", "heatmap-streak", "logs")
                : configured;

            return new FileLoggerProvider(directory);
        });

        return builder;
    }
}
