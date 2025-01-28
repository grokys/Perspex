using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Threading;

internal class DispatcherTaskScheduler : TaskScheduler
{
    private static DispatcherTaskScheduler? s_uiThread;
    public static DispatcherTaskScheduler UIThread => s_uiThread ??= new DispatcherTaskScheduler(Dispatcher.UIThread); 

    private readonly Dispatcher _dispatcher;
    private readonly SendOrPostCallback _postCallback;

    public DispatcherTaskScheduler(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _postCallback = QueueTaskCallback;
    }

    protected override IEnumerable<Task>? GetScheduledTasks() => null;

    protected override void QueueTask(Task task)
    {
        _dispatcher.Post(_postCallback, task);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        if (!_dispatcher.CheckAccess())
            return false;

        return TryExecuteTask(task);
    }

    private void QueueTaskCallback(object? state) => TryExecuteTask((Task)state!);
}
