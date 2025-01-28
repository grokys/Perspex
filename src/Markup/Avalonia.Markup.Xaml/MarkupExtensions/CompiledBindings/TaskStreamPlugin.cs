using System;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;
using Avalonia.Reactive;
using Avalonia.Threading;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    internal class TaskStreamPlugin<T> : IStreamPlugin
    {
        public bool Match(WeakReference<object?> reference)
        {
            return reference.TryGetTarget(out var target) && target is Task<T>;
        }

        public IObservable<object?> Start(WeakReference<object?> reference)
        {
            if(!(reference.TryGetTarget(out var target) && target is Task<T> task))
            {
                return Observable.Empty<object?>();
            }

            switch (task.Status)
            {
                case TaskStatus.RanToCompletion:
                case TaskStatus.Faulted:
                    return HandleCompleted(task);
                default:
                    var subject = new LightweightSubject<object?>();
                    task.ContinueWith(
                            _ => HandleCompleted(task).Subscribe(subject),
                            DispatcherTaskScheduler.UIThread)
                        .ConfigureAwait(false);
                    return subject;
            }
        }
        
        
        private static IObservable<object?> HandleCompleted(Task<T> task)
        {
            switch (task.Status)
            {
                case TaskStatus.RanToCompletion:
                    return Observable.Return((object?)task.Result);
                case TaskStatus.Faulted:
                    return Observable.Return(new BindingNotification(task.Exception!, BindingErrorType.Error));
                default:
                    throw new AvaloniaInternalException("HandleCompleted called for non-completed Task.");
            }
        }
    }
}
