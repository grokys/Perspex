using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Reactive;
using Avalonia.Threading;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Handles binding to <see cref="Task"/>s for the '^' stream binding operator.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = TrimmingMessages.IgnoreNativeAotSupressWarningMessage)]
    [RequiresUnreferencedCode(TrimmingMessages.StreamPluginRequiresUnreferencedCodeMessage)]
    internal class TaskStreamPlugin : IStreamPlugin
    {
        /// <summary>
        /// Checks whether this plugin handles the specified value.
        /// </summary>
        /// <param name="reference">A weak reference to the value.</param>
        /// <returns>True if the plugin can handle the value; otherwise false.</returns>
        public virtual bool Match(WeakReference<object?> reference)
        {
            reference.TryGetTarget(out var target);

            return target is Task;
        }

        /// <summary>
        /// Starts producing output based on the specified value.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <returns>
        /// An observable that produces the output for the value.
        /// </returns>
        public virtual IObservable<object?> Start(WeakReference<object?> reference)
        {
            reference.TryGetTarget(out var target);

            if (target is Task task)
            {
                var resultProperty = task.GetType().GetRuntimeProperty("Result");

                if (resultProperty != null)
                {
                    switch (task.Status)
                    {
                        case TaskStatus.RanToCompletion:
                        case TaskStatus.Faulted:
                            return HandleCompleted(task);
                        default:
                            var subject = new LightweightSubject<object?>();
                            task.ContinueWith(
                                    x => HandleCompleted(task).Subscribe(subject),
                                    DispatcherTaskScheduler.UIThread)
                                .ConfigureAwait(false);
                            return subject;
                    }
                }
            }

            return Observable.Empty<object?>();
        }

        [RequiresUnreferencedCode(TrimmingMessages.StreamPluginRequiresUnreferencedCodeMessage)]
        private static IObservable<object?> HandleCompleted(Task task)
        {
            var resultProperty = task.GetType().GetRuntimeProperty("Result");

            if (resultProperty != null)
            {
                switch (task.Status)
                {
                    case TaskStatus.RanToCompletion:
                        return Observable.Return(resultProperty.GetValue(task));
                    case TaskStatus.Faulted:
                        return Observable.Return(new BindingNotification(task.Exception!, BindingErrorType.Error));
                    default:
                        throw new AvaloniaInternalException("HandleCompleted called for non-completed Task.");
                }
            }

            return Observable.Empty<object>();
        }
    }
}
