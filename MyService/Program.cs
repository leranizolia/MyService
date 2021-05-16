using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyService.Models;
using MyService.Services;
using MyService.Services.TaskQueue;
using MyService.Services.Workers;

namespace MyService
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration(confBuilder =>
                {
                    confBuilder.AddJsonFile("config.json");
                    confBuilder.AddCommandLine(args);

                })
                .ConfigureLogging((configLogging) =>
                {
                    configLogging.AddConsole();
                    configLogging.AddDebug();
                })
                .ConfigureServices((services) =>
                {
                    //добавляем наш сервис в контейнер, иначе хост не будет знать, как его запустить
                    services.AddHostedService<TaskSchedulerService>();
                    services.AddHostedService<WorkerService>();
                    //singleton позволяет создавать экземпляр определенного типа
                    //то есть если мы создали settings, то нет смысла пересоздавать этот класс каждый раз при запрашивании
                    services.AddSingleton<Settings>();
                    services.AddSingleton<TaskProcessor>();
                    //ОЧЕРЕДЬ ОБЯЗАТЕЛЬНО ДОЛЖНА БЫТЬ ОДНА
                    //если они будут разные, то планировщик будет складывать задачи в одну очередь,
                    //а воркер будет забирать задачи из другой очереди
                    //по правилам надо делать с интерфейсом, чтобы была возможность использовать разные реализации, мокать и прочее
                    services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
                });

            await builder.RunConsoleAsync();
        }
    }
}

