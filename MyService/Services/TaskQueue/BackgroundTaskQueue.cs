using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MyService.Services.TaskQueue
{
    public class BackgroundTaskQueue: IBackgroundTaskQueue
    {
        //для того чтобы несколько воркеров не захватили одну и ту же задачу одновременно
        private ConcurrentQueue<Func<CancellationToken, Task>> WorkItems = new ConcurrentQueue<Func<CancellationToken, Task>>();
        //нужен, чтобы мы случайно не захватили задачу в момент её добавления
        private SemaphoreSlim Signal = new SemaphoreSlim(0);


        public int Size => WorkItems.Count;

        public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            //ждет пока не освободился semaphore
            await Signal.WaitAsync(cancellationToken);
            //и если он освободился, то взять из очереди задачу
            WorkItems.TryDequeue(out var workItem);

            return workItem;
        }

        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
        {
            if (workItem == null)
            {
                //если кинули в вместо задачи null, то надо выдать ошибку
                throw new ArgumentException(nameof(workItem));
            }

            //если всё ок, то надо добавить задачу в очередь
            WorkItems.Enqueue(workItem);
            //как раз используем Semaphore, чтобы мы не захватили задачу в момент её добавления
            Signal.Release();
        }
    }
}
