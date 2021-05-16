using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyService.Models;
using MyService.Services.TaskQueue;
using MyService.Services.Workers;

namespace MyService.Services
{
    //сервис набивает очередь задач с таймером (периодически запускается, делает работу и возвращает рез)
    public class TaskSchedulerService: IHostedService, IDisposable
    {
        private Timer Timer;

        private readonly IServiceProvider Services;

        private readonly Settings Settings;

        private readonly ILogger<TaskSchedulerService> Logger;

        private readonly Random Random = new Random();

        private readonly object SyncRoot = new object();

        public TaskSchedulerService(IServiceProvider services)
        {
            this.Services = services;
            this.Settings = Services.GetRequiredService<Settings>();
            this.Logger = services.GetRequiredService<ILogger<TaskSchedulerService>>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {

            var interval = Settings?.RunInterval ?? 0;

            if (interval == 0)
            {
                Logger.LogWarning("CheckInterval is not defined in settings. Set to default: 60 seconds");
                interval = 60;
            }

            Timer = new Timer(
                (e) => ProcessTask(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(interval));

            return Task.CompletedTask;
        }

        //надо сделать так, чтобы 2 процесса обрабатывающих одно и то же, начинают рабоать параллельно
        private void ProcessTask()
        {
            if (Monitor.TryEnter(SyncRoot))
            {
                Logger.LogInformation($"Process task started");

                //этот сервис раз в n секунд (см. файл конфигурации)
                //будет вызывать метод dowork...
                for (int i = 0; i < 20; i++)
                {
                    DoWork();
                }

                Logger.LogInformation($"Process task finished");
                Monitor.Exit(SyncRoot);
            }

            else
            {
                Logger.LogInformation($"Processing is currently in progress. Skipped Task");
            };
        }

        private void DoWork()
        {
            //... который будет генерировать случайное число...
            var number = Random.Next(20);

            //...создавать процессор...

            //здесь создали процесс, генерирующий задачи
            var processor = Services.GetRequiredService<TaskProcessor>();

            //...этот процессор запускать - он будет считать ряд фибоначчи, писать в файл
            //писать в консоль, и эту задачу мы помещаем в очередь

            //здесь создали очередь, куда будем складывать задачи
            var queue = Services.GetRequiredService<IBackgroundTaskQueue>();

            //здесь токена еще нет, потому что мы задачи в очередь только передаем
            //токен появляется, когда воркер реализуем
            queue.QueueBackgroundWorkItem(token =>
            {
                return processor.RunAsync(number, token);
            });
        }

        //здесь generic host передает
        public Task StopAsync(CancellationToken cancellationToken)
        {
            Timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        //эта штука нужна, чтобы в момент завершения работы таймер остановился и не держал ресурсы
        public void Dispose()
        {
            Timer?.Dispose();
        }
    }
}
