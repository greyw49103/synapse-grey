
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Grey.Interfaces;
using Grey.Services;


namespace Grey.SignalBooster
{
    /// <summary>
    /// Handles quantum flux state propagation from physician records.
    /// </summary>
    class Program
    {
        public const string PhysicianNoteFile = "physician_note1.txt";

        static async Task<int> Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton<INoteService, NoteService>()
                .AddLogging(builder => builder
                    .AddConsole())
                .BuildServiceProvider();

            var noteService = serviceProvider.GetRequiredService<INoteService>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration config = builder.Build();

            string extractUrl = config["ExtractUrl"]!;

            try
            {
                string rootPath = AppContext.BaseDirectory;
                string filePath = Path.Combine(rootPath, PhysicianNoteFile);

                bool successExtract = await noteService.SendDrExtract(filePath, extractUrl);
                if (!successExtract)
                {
                    logger.LogError($"Error sending physician extract.");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error sending physician extract: {ex.Message}");
                return 1;
            }

            return 0;
        }
    }
}
