using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyService.Services.TaskQueue
{
    public interface IBackgroundTaskQueue
    {
        //позволяет узнать размер очереди
        int Size { get; }

        //метод который позволяет принимать задачи
        //будет принимать делегат, который будет потом вызываться
        void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);

        //извлекает делегат из очереди и исполняет его
        //будет потокобезопасной
        Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellation);
    }
}
