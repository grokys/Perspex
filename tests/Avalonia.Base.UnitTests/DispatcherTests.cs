using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Threading;
using Avalonia.Utilities;
using Xunit;
namespace Avalonia.Base.UnitTests;

public partial class DispatcherTests
{
    class SimpleDispatcherImpl : IDispatcherImpl, IDispatcherImplWithPendingInput
    {
        private readonly Thread _loopThread = Thread.CurrentThread;
        private readonly object _lock = new();
        public bool CurrentThreadIsLoopThread => Thread.CurrentThread == _loopThread;
        public void Signal()
        {
            lock (_lock)
                AskedForSignal = true;
        }

        public event Action Signaled;
        public event Action Timer;
        public long? NextTimer { get; private set; }
        public bool AskedForSignal { get; private set; }
        
        public void UpdateTimer(long? dueTimeInTicks)
        {
            NextTimer = dueTimeInTicks;
        }

        public long Now { get; set; }

        public void ExecuteSignal()
        {
            lock (_lock)
            {
                if (!AskedForSignal)
                    return;
                AskedForSignal = false;
            }
            Signaled?.Invoke();
        }

        public void ExecuteTimer()
        {
            if (NextTimer == null)
                return;
            Now = NextTimer.Value;
            Timer?.Invoke();
        }

        public bool CanQueryPendingInput => TestInputPending != null;
        public bool HasPendingInput => TestInputPending == true;
        public bool? TestInputPending { get; set; }
    }

    class SimpleDispatcherWithBackgroundProcessingImpl : SimpleDispatcherImpl, IDispatcherImplWithExplicitBackgroundProcessing
    {
        public bool AskedForBackgroundProcessing { get; private set; }
        public event Action ReadyForBackgroundProcessing;
        public void RequestBackgroundProcessing()
        {
            if (!CurrentThreadIsLoopThread)
                throw new InvalidOperationException();
            AskedForBackgroundProcessing = true;
        }

        public void FireBackgroundProcessing()
        {
            if(!AskedForBackgroundProcessing)
                return;
            AskedForBackgroundProcessing = false;
            ReadyForBackgroundProcessing?.Invoke();
        }
    }
    
    class SimpleControlledDispatcherImpl : SimpleDispatcherWithBackgroundProcessingImpl, IControlledDispatcherImpl
    {
        private readonly bool _useTestTimeout = true;
        private readonly CancellationToken? _cancel;
        public int RunLoopCount { get; private set; }
        
        public SimpleControlledDispatcherImpl()
        {
            
        }

        public SimpleControlledDispatcherImpl(CancellationToken cancel, bool useTestTimeout = false)
        {
            _useTestTimeout = useTestTimeout;
            _cancel = cancel;
        }
        
        public void RunLoop(CancellationToken token)
        {
            RunLoopCount++;
            var st = Stopwatch.StartNew();
            while (!token.IsCancellationRequested || _cancel?.IsCancellationRequested == true)
            {
                FireBackgroundProcessing();
                ExecuteSignal();
                if (_useTestTimeout)
                    Assert.True(st.ElapsedMilliseconds < 4000, "RunLoop exceeded test time quota");
                else
                    Thread.Sleep(10);
            }
        }
    }
    
    
    [Fact]
    public void DispatcherExecutesJobsAccordingToPriority()
    {
        var impl = new SimpleDispatcherImpl();
        var disp = new Dispatcher(impl);
        var actions = new List<string>();
        disp.Post(()=>actions.Add("Background"), DispatcherPriority.Background);
        disp.Post(()=>actions.Add("Render"), DispatcherPriority.Render);
        disp.Post(()=>actions.Add("Input"), DispatcherPriority.Input);
        Assert.True(impl.AskedForSignal);
        impl.ExecuteSignal();
        Assert.Equal(new[] { "Render", "Input", "Background" }, actions);
    }
    
    [Fact]
    public void DispatcherPreservesOrderWhenChangingPriority()
    {
        var impl = new SimpleDispatcherImpl();
        var disp = new Dispatcher(impl);
        var actions = new List<string>();
        var toPromote = disp.InvokeAsync(()=>actions.Add("PromotedRender"), DispatcherPriority.Background);
        var toPromote2 = disp.InvokeAsync(()=>actions.Add("PromotedRender2"), DispatcherPriority.Input);
        disp.Post(() => actions.Add("Render"), DispatcherPriority.Render);
        
        toPromote.Priority = DispatcherPriority.Render;
        toPromote2.Priority = DispatcherPriority.Render;
        
        Assert.True(impl.AskedForSignal);
        impl.ExecuteSignal();
        
        Assert.Equal(new[] { "PromotedRender", "PromotedRender2", "Render" }, actions);
    }

    [Fact]
    public void DispatcherStopsItemProcessingWhenInteractivityDeadlineIsReached()
    {
        var impl = new SimpleDispatcherImpl();
        var disp = new Dispatcher(impl);
        var actions = new List<int>();
        for (var c = 0; c < 10; c++)
        {
            var itemId = c;
            disp.Post(() =>
            {
                actions.Add(itemId);
                impl.Now += 20;
            }, DispatcherPriority.Background);
        }

        Assert.False(impl.AskedForSignal);
        Assert.NotNull(impl.NextTimer);

        for (var c = 0; c < 4; c++)
        {
            Assert.NotNull(impl.NextTimer);
            Assert.False(impl.AskedForSignal);
            impl.ExecuteTimer();
            Assert.False(impl.AskedForSignal);
            impl.ExecuteSignal();
            var expectedCount = (c + 1) * 3;
            if (c == 3)
                expectedCount = 10;
            
            Assert.Equal(Enumerable.Range(0, expectedCount), actions);
            Assert.False(impl.AskedForSignal);
            if (c < 3)
            {
                Assert.True(impl.NextTimer > impl.Now);
            }
            else
                Assert.Null(impl.NextTimer);
        }
    }
    
    
    [Fact]
    public void DispatcherStopsItemProcessingWhenInputIsPending()
    {
        var impl = new SimpleDispatcherImpl();
        impl.TestInputPending = true;
        var disp = new Dispatcher(impl);
        var actions = new List<int>();
        for (var c = 0; c < 10; c++)
        {
            var itemId = c;
            disp.Post(() =>
            {
                actions.Add(itemId);
                if (itemId == 0 || itemId == 3 || itemId == 7)
                    impl.TestInputPending = true;
            }, DispatcherPriority.Background);
        }
        Assert.False(impl.AskedForSignal);
        Assert.NotNull(impl.NextTimer);
        impl.TestInputPending = false;

        for (var c = 0; c < 4; c++)
        {
            Assert.NotNull(impl.NextTimer);
            impl.ExecuteTimer();
            Assert.False(impl.AskedForSignal);
            var expectedCount = c switch
            {
                0 => 1,
                1 => 4,
                2 => 8,
                3 => 10,
                _ => throw new InvalidOperationException($"Unexpected value {c}")
            };
            
            Assert.Equal(Enumerable.Range(0, expectedCount), actions);
            Assert.False(impl.AskedForSignal);
            if (c < 3)
            {
                Assert.True(impl.NextTimer > impl.Now);
                impl.Now = impl.NextTimer.Value + 1;
            }
            else
                Assert.Null(impl.NextTimer);

            impl.TestInputPending = false;
        }
    }

    [Theory,
     InlineData(false, false),
     InlineData(false, true),
     InlineData(true, false),
     InlineData(true, true)]
    public void CanWaitForDispatcherOperationFromTheSameThread(bool controlled, bool foreground)
    {
        var impl = controlled ? new SimpleControlledDispatcherImpl() : new SimpleDispatcherImpl();
        var disp = new Dispatcher(impl);
        bool finished = false;

        disp.InvokeAsync(() => finished = true,
            foreground ? DispatcherPriority.Default : DispatcherPriority.Background).Wait();

        Assert.True(finished);
        if (controlled) 
            Assert.Equal(foreground ? 0 : 1, ((SimpleControlledDispatcherImpl)impl).RunLoopCount);
    }


    class DispatcherServices : IDisposable
    {
        private readonly IDisposable _scope;

        public DispatcherServices(IDispatcherImpl impl)
        {
            _scope = AvaloniaLocator.EnterScope();
            AvaloniaLocator.CurrentMutable.Bind<IDispatcherImpl>().ToConstant(impl);
            Dispatcher.ResetForUnitTests();
            SynchronizationContext.SetSynchronizationContext(null);
        }
        
        public void Dispose()
        {
            Dispatcher.ResetForUnitTests();
            _scope.Dispose();
            SynchronizationContext.SetSynchronizationContext(null);
        }
    }
    
    [Fact]
    public void ExitAllFramesShouldExitAllFramesAndBeAbleToContinue()
    {
        using (new DispatcherServices(new SimpleControlledDispatcherImpl()))
        {
            var actions = new List<string>();
            var disp = Dispatcher.UIThread;
            disp.Post(() =>
            {
                actions.Add("Nested frame");
                Dispatcher.UIThread.MainLoop(CancellationToken.None);
                actions.Add("Nested frame exited");
            });
            disp.Post(() =>
            {
                actions.Add("ExitAllFrames");
                disp.ExitAllFrames();
            });


            disp.MainLoop(CancellationToken.None);
            
            Assert.Equal(new[] { "Nested frame", "ExitAllFrames", "Nested frame exited" }, actions);
            actions.Clear();
            
            var secondLoop = new CancellationTokenSource();
            disp.Post(() =>
            {
                actions.Add("Callback after exit");
                secondLoop.Cancel();
            });
            disp.MainLoop(secondLoop.Token);
            Assert.Equal(new[] { "Callback after exit" }, actions);
        }
    }
    
        
    [Fact]
    public void ShutdownShouldExitAllFramesAndNotAllowNewFrames()
    {
        using (new DispatcherServices(new SimpleControlledDispatcherImpl()))
        {
            var actions = new List<string>();
            var disp = Dispatcher.UIThread;
            disp.Post(() =>
            {
                actions.Add("Nested frame");
                Dispatcher.UIThread.MainLoop(CancellationToken.None);
                actions.Add("Nested frame exited");
            });
            disp.Post(() =>
            {
                actions.Add("Shutdown");
                disp.BeginInvokeShutdown(DispatcherPriority.Normal);
            });
            
            disp.Post(() =>
            {
                actions.Add("Nested frame after shutdown");
                // This should exit immediately and not run any jobs
                Dispatcher.UIThread.MainLoop(CancellationToken.None);
                actions.Add("Nested frame after shutdown exited");
            });
            
            var criticalFrameAfterShutdown = new DispatcherFrame(false);
            disp.Post(() =>
            {
                actions.Add("Critical frame after shutdown");
                
                Dispatcher.UIThread.PushFrame(criticalFrameAfterShutdown);
                actions.Add("Critical frame after shutdown exited");
            });
            disp.Post(() =>
            {
                actions.Add("Stop critical frame");
                criticalFrameAfterShutdown.Continue = false;
            });

            disp.MainLoop(CancellationToken.None);

            Assert.Equal(new[]
            {
                "Nested frame", 
                "Shutdown",
                // Normal nested frames are supposed to exit immediately
                "Nested frame after shutdown", "Nested frame after shutdown exited",
                // if frame is configured to not answer dispatcher requests, it should be allowed to run
                "Critical frame after shutdown", "Stop critical frame", "Critical frame after shutdown exited",
                // After 3-rd level frames have exited, the normal nested frame exits too
                "Nested frame exited"
            }, actions);
            actions.Clear();
            
            disp.Post(()=>actions.Add("Frame after shutdown finished"));
            Assert.Throws<InvalidOperationException>(() => disp.MainLoop(CancellationToken.None));
            Assert.Empty(actions);
        }
    }

    class WaitHelper : SynchronizationContext, NonPumpingLockHelper.IHelperImpl
    {
        public int WaitCount;
        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            WaitCount++;
            return base.Wait(waitHandles, waitAll, millisecondsTimeout);
        }
    }
    
    [Fact]
    public void DisableProcessingShouldStopProcessing()
    {
        using (new DispatcherServices(new SimpleControlledDispatcherImpl()))
        {
            var helper = new WaitHelper();
            AvaloniaLocator.CurrentMutable.Bind<NonPumpingLockHelper.IHelperImpl>().ToConstant(helper);
            using (Dispatcher.UIThread.DisableProcessing())
            {
                Assert.True(SynchronizationContext.Current is NonPumpingSyncContext);
                Assert.Throws<InvalidOperationException>(() => Dispatcher.UIThread.MainLoop(CancellationToken.None));
                Assert.Throws<InvalidOperationException>(() => Dispatcher.UIThread.RunJobs());
            }

            var avaloniaContext = new AvaloniaSynchronizationContext(Dispatcher.UIThread, DispatcherPriority.Default, true);
            SynchronizationContext.SetSynchronizationContext(avaloniaContext);

            var waitHandle = new ManualResetEvent(true);
            
            helper.WaitCount = 0;
            waitHandle.WaitOne(100);
            Assert.Equal(0, helper.WaitCount);
            using (Dispatcher.UIThread.DisableProcessing())
            {
                Assert.Equal(avaloniaContext, SynchronizationContext.Current);
                waitHandle.WaitOne(100);
                Assert.Equal(1, helper.WaitCount);
            }
        }
    }

    [Fact]
    public void DispatcherOperationsHaveContextWithProperPriority()
    {
        using (new DispatcherServices(new SimpleControlledDispatcherImpl()))
        {
            SynchronizationContext.SetSynchronizationContext(null);
            var disp = Dispatcher.UIThread;
            var priorities = new List<DispatcherPriority>();

            void DumpCurrentPriority() =>
                priorities.Add(((AvaloniaSynchronizationContext)SynchronizationContext.Current!).Priority);
                
                
            disp.Post(DumpCurrentPriority, DispatcherPriority.Normal);
            disp.Post(DumpCurrentPriority, DispatcherPriority.Loaded);
            disp.Post(DumpCurrentPriority, DispatcherPriority.Input);
            disp.Post(() =>
            {
                DumpCurrentPriority();
                disp.ExitAllFrames();
            }, DispatcherPriority.Background);
            disp.MainLoop(CancellationToken.None);

            disp.Invoke(DumpCurrentPriority, DispatcherPriority.Send);
            disp.Invoke(() =>
            {
                DumpCurrentPriority();
                return 1;
            }, DispatcherPriority.Send);

            Assert.Equal(
                new[]
                {
                    DispatcherPriority.Normal, DispatcherPriority.Loaded, DispatcherPriority.Input, DispatcherPriority.Background,
                    DispatcherPriority.Send, DispatcherPriority.Send,
                },
                priorities);


        }
    }

    [Fact]
    public void DispatcherInvokeAsyncUnwrapsTasks()
    {
        int asyncMethodStage = 0;
        
        async Task AsyncMethod()
        {
            asyncMethodStage = 1;
            await Task.Delay(200);
            asyncMethodStage = 2;
        }
        
        async Task<int> AsyncMethodWithResult()
        {
            await Task.Delay(100);
            return 1;
        }
        
        async Task Test()
        {
            await Dispatcher.UIThread.InvokeAsync(AsyncMethod);
            Assert.Equal(2, asyncMethodStage);
            Assert.Equal(1, await Dispatcher.UIThread.InvokeAsync(AsyncMethodWithResult));
            asyncMethodStage = 0;
            
            await Dispatcher.UIThread.InvokeAsync(AsyncMethod, DispatcherPriority.Default);
            Assert.Equal(2, asyncMethodStage);
            Assert.Equal(1, await Dispatcher.UIThread.InvokeAsync(AsyncMethodWithResult, DispatcherPriority.Default));
            
            Dispatcher.UIThread.ExitAllFrames();
        }
        
        using (new DispatcherServices(new ManagedDispatcherImpl(null)))
        {
            var t = Test();
            var cts = new CancellationTokenSource();
            Task.Delay(3000).ContinueWith(_ => cts.Cancel());
            Dispatcher.UIThread.MainLoop(cts.Token);
            Assert.True(t.IsCompletedSuccessfully);
            t.GetAwaiter().GetResult();
        }
    }

    [Fact]
    public async Task DispatcherResumeContinuesOnUIThread()
    {
        using var services = new DispatcherServices(new SimpleControlledDispatcherImpl());

        var tokenSource = new CancellationTokenSource();
        var workload = Dispatcher.UIThread.InvokeAsync(
            async () =>
            {
                Assert.True(Dispatcher.UIThread.CheckAccess());

                await Task.Delay(1).ConfigureAwait(false);
                Assert.False(Dispatcher.UIThread.CheckAccess());

                await Dispatcher.UIThread.Resume();
                Assert.True(Dispatcher.UIThread.CheckAccess());

                tokenSource.Cancel();
            });

        Dispatcher.UIThread.MainLoop(tokenSource.Token);
    }

    [Fact]
    public async Task DispatcherYieldContinuesOnUIThread()
    {
        using var services = new DispatcherServices(new SimpleControlledDispatcherImpl());

        var tokenSource = new CancellationTokenSource();
        var workload = Dispatcher.UIThread.InvokeAsync(
            async () =>
            {
                Assert.True(Dispatcher.UIThread.CheckAccess());

                await Dispatcher.Yield();
                Assert.True(Dispatcher.UIThread.CheckAccess());

                tokenSource.Cancel();
            });

        Dispatcher.UIThread.MainLoop(tokenSource.Token);
    }

    [Fact]
    public async Task DispatcherYieldThrowsOnNonUIThread()
    {
        using var services = new DispatcherServices(new SimpleControlledDispatcherImpl());

        var tokenSource = new CancellationTokenSource();
        var workload = Dispatcher.UIThread.InvokeAsync(
            async () =>
            {
                Assert.True(Dispatcher.UIThread.CheckAccess());

                await Task.Delay(1).ConfigureAwait(false);
                Assert.False(Dispatcher.UIThread.CheckAccess());
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await Dispatcher.Yield());

                tokenSource.Cancel();
            });

        Dispatcher.UIThread.MainLoop(tokenSource.Token);
    }

    [Fact]
    public async Task AwaitWithPriorityRunsOnUIThread()
    {
        static async Task<int> Workload()
        {
            await Task.Delay(1).ConfigureAwait(false);
            Assert.False(Dispatcher.UIThread.CheckAccess());

            return Thread.CurrentThread.ManagedThreadId;
        }

        using var services = new DispatcherServices(new SimpleControlledDispatcherImpl());

        var tokenSource = new CancellationTokenSource();
        var workload = Dispatcher.UIThread.InvokeAsync(
            async () =>
            {
                Assert.True(Dispatcher.UIThread.CheckAccess());
                Task taskWithoutResult = Workload();

                await Dispatcher.UIThread.AwaitWithPriority(taskWithoutResult, DispatcherPriority.Default);

                Assert.True(Dispatcher.UIThread.CheckAccess());
                Task<int> taskWithResult = Workload();

                await Dispatcher.UIThread.AwaitWithPriority(taskWithResult, DispatcherPriority.Default);

                Assert.True(Dispatcher.UIThread.CheckAccess());

                tokenSource.Cancel();
            });

        Dispatcher.UIThread.MainLoop(tokenSource.Token);
    }
}
