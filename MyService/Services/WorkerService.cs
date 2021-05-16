using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyService.Models;
using MyService.Services.TaskQueue;

namespace MyService.Services
{
    //здесь не ihostedservic
    //background наследуется от ihostedservice, но он нужен долгое время
    //в отличие от 
    public class WorkerService: BackgroundService
    {
        private readonly IBackgroundTaskQueue TaskQueue;

        private readonly ILogger<WorkerService> Logger;

        private readonly Settings Settings;

        public WorkerService(IBackgroundTaskQueue taskQueue, ILogger<WorkerService> logger, Settings settings)
        {
            this.TaskQueue = taskQueue;
            this.Logger = logger;
            this.Settings = settings;
        }

        //token присылает host, когда мы отменяем работу сервиса через crl+c
        protected override async Task ExecuteAsync(CancellationToken token)
        {
            var workersCount = Settings.WorkersCount;
            var workers = Enumerable.Range(0, workersCount).Select(num => RunInstance(num, token));

            //запустит несколько потоков, в каждом из которых будет крутиться runinstance
            await Task.WhenAll(workers);
        }

        private async Task RunInstance(int num, CancellationToken token)
        {
            Logger.LogInformation($"#{num} is starting.");

            while (!token.IsCancellationRequested)
            {
                //берем задачу (делегат, который получил планировщик) из очереди с задачами
                var workItem = await TaskQueue.DequeueAsync(token);

                try
                {
                    Logger.LogInformation($"#{num}: Processing task. Queue size: {TaskQueue.Size}");
                    await workItem(token);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"#{num}: Error occured executing task.");
                }    
            }

            Logger.LogInformation($"#{num} is stopping.");
        }
    }
}
