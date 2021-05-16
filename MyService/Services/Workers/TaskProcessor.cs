using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyService.Models;

namespace MyService.Services.Workers
{
    public class TaskProcessor
    {
        private readonly ILogger<TaskProcessor> Logger;

        private readonly Settings Settings;

        public TaskProcessor(ILogger<TaskProcessor> logger, Settings settings)
        {
            this.Logger = logger;
            this.Settings = settings;
        }

        public async Task RunAsync(int number, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            Func<int, int> fibonacci = null;

            fibonacci = (num) =>
            {
                if (num < 2) return 1;
                else return fibonacci(num - 1) + fibonacci(num - 2);
            };

            //чтобы выполнялось в отдельном потоке

            var result = await Task.Run(async () =>
            {
                await Task.Delay(1000);
                return Enumerable.Range(0, number).Select(n => fibonacci(n));
            }, token);

            //запишем в файл

            //здесь надо добавить что-то типо "подожди пока другой поток не закончил"
            using (var writer = new StreamWriter(Settings.ResultPath, true, Encoding.UTF8))
            {
                writer.WriteLine(DateTime.Now.ToString() + " : " + string.Join(" ", result));

            }

            Logger.LogInformation($"Task finished. Result: {string.Join(" ", result)}");
        }
    }
}
