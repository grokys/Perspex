using System;
using System.Diagnostics;
using Avalonia.Controls.Platform;
using Avalonia.Reactive;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using System.Collections.Generic;
using Avalonia.Diagnostics;
using Avalonia.Metadata;

namespace Avalonia.Headless
{
    // We need to hide this class as an implementation detail.
    [Unstable(ObsoletionMessages.MayBeRemovedInAvalonia12)]
    public static class AvaloniaHeadlessPlatform
    {
        internal static Compositor? Compositor { get; private set; }

        internal class RenderTimer : DefaultRenderTimer
        {
            private readonly TimeProvider _timeProvider;
            private readonly int _framesPerSecond;
            private Action? _forceTick; 
            protected override IDisposable StartCore(Action<TimeSpan> tick)
            {
                var startingTimestamp = _timeProvider.GetTimestamp();
                _forceTick = () => tick(_timeProvider.GetElapsedTime(startingTimestamp));

                var timer = new DispatcherTimer(DispatcherPriority.UiThreadRender)
                {
                    Interval = TimeSpan.FromSeconds(1.0 / _framesPerSecond),
                    Tag = "HeadlessRenderTimer"
                };
                timer.Tick += (s, e) => tick(_timeProvider.GetElapsedTime(startingTimestamp));
                timer.Start();

                return Disposable.Create(() =>
                {
                    _forceTick = null;
                    timer.Stop();
                });
            }

            public RenderTimer(TimeProvider timeProvider, int framesPerSecond) : base(framesPerSecond)
            {
                _timeProvider = timeProvider;
                _framesPerSecond = framesPerSecond;
            }

            public override bool RunsInBackground => false;

            public void ForceTick() => _forceTick?.Invoke();
        }

        private class HeadlessWindowingPlatform : IWindowingPlatform
        {
            readonly PixelFormat _frameBufferFormat;
            public HeadlessWindowingPlatform(PixelFormat frameBufferFormat)
            {
                _frameBufferFormat = frameBufferFormat;
            }
            public IWindowImpl CreateWindow() => new HeadlessWindowImpl(false, _frameBufferFormat);
            public ITopLevelImpl CreateEmbeddableTopLevel() => CreateEmbeddableWindow();

            public IWindowImpl CreateEmbeddableWindow() => throw new PlatformNotSupportedException();

            public IPopupImpl CreatePopup() => new HeadlessWindowImpl(true, _frameBufferFormat);

            public ITrayIconImpl? CreateTrayIcon() => null;
        }

        internal static void Initialize(AvaloniaHeadlessPlatformOptions opts)
        {
            var timeProvider = new HeadlessTimeProvider(opts.UseRealtimeTimeProvider);

            AvaloniaLocator.CurrentMutable
                .BindToSelf(opts)
                .BindToSelf(timeProvider)
                .Bind<IDispatcherImpl>().ToConstant(new ManagedDispatcherImpl(timeProvider, null))
                .Bind<IClipboard>().ToSingleton<HeadlessClipboardStub>()
                .Bind<ICursorFactory>().ToSingleton<HeadlessCursorFactoryStub>()
                .Bind<IPlatformSettings>().ToSingleton<DefaultPlatformSettings>()
                .Bind<IPlatformIconLoader>().ToSingleton<HeadlessIconLoaderStub>()
                .Bind<IKeyboardDevice>().ToConstant(new KeyboardDevice())
                .Bind<IRenderTimer>().ToConstant(new RenderTimer(timeProvider, opts.Fps))
                .Bind<IWindowingPlatform>().ToConstant(new HeadlessWindowingPlatform(opts.FrameBufferFormat))
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
                .Bind<KeyGestureFormatInfo>().ToConstant(new KeyGestureFormatInfo(new Dictionary<Key, string>() { }));
            Compositor = new Compositor(RenderLoop.LocatorAutoInstance, gpu: null, timeProvider: timeProvider);
        }

        /// <summary>
        /// Forces renderer to process a rendering timer tick.
        /// Use this method before calling <see cref="HeadlessWindowExtensions.GetLastRenderedFrame"/>. 
        /// </summary>
        /// <param name="count">Count of frames to be ticked on the timer.</param>
        [Obsolete("Use Dispatcher.UIThread.PulseFrames instead.")]
        public static void ForceRenderTimerTick(int count = 1)
        {
            var timer = (RenderTimer)AvaloniaLocator.Current.GetRequiredService<IRenderTimer>();
            for (var c = 0; c < count; c++)
                timer?.ForceTick();
        }
    }

    /// <summary>
    /// Provides configuration options for running Avalonia applications in headless mode.
    /// </summary>
    public class AvaloniaHeadlessPlatformOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the application should use headless drawing. Enabled by default.
        /// </summary>
        /// <remarks>
        /// Disable it, if you intend to setup skia rendering via `.UseSkia` and need to render frames.
        /// </remarks>
        public bool UseHeadlessDrawing { get; set; } = true;

        /// <summary>
        /// Gets or sets the pixel format of the frame buffer. Default is <see cref="PixelFormat.Rgba8888"/>
        /// </summary>
        /// <remarks>
        /// Is used only when headless drawing is disabled, and skia (or other drawing backend) is used.  
        /// </remarks>
        public PixelFormat FrameBufferFormat { get; set; } = PixelFormat.Rgba8888;

        /// <summary>
        /// Gets or sets the frames per second (FPS) for the headless rendering. Default is 60.
        /// </summary>
        /// <remarks>
        /// This API also directly affects extension methods like <see cref="HeadlessExtensions.PulseRenderFrames"/>.
        /// </remarks>
        public int Fps { get; set; } = 60;

        /// <summary>
        /// Gets or sets a value indicating whether to use the real-time time provider. Default is true.
        /// </summary>
        /// <remarks>
        /// When enabled, dispatcher and render timer will tick in real time as headless platform is running.
        /// Operators like <see cref="System.Threading.Tasks.Task.Delay(TimeSpan)"/> can be used to control timers.
        /// When disabled, headless time provider will be only controlled via <see cref="HeadlessExtensions.PulseTime"/> and <see cref="HeadlessExtensions.PulseRenderFrames"/> APIs. 
        /// </remarks>
        public bool UseRealtimeTimeProvider { get; set; } = true;
    }

    /// <summary>
    /// Provides extension methods for configuring the Avalonia application to run in headless mode.
    /// </summary>
    public static class AvaloniaHeadlessPlatformExtensions
    {
        /// <summary>
        /// Configures the <see cref="AppBuilder"/> to use headless mode with the specified options.
        /// </summary>
        /// <param name="builder">The <see cref="AppBuilder"/> to configure.</param>
        /// <param name="options">The options for configuring the headless platform. If null, default options will be used.</param>
        /// <returns>The configured <see cref="AppBuilder"/>.</returns>
        public static AppBuilder UseHeadless(this AppBuilder builder, AvaloniaHeadlessPlatformOptions? options = null)
        {
            options ??= new AvaloniaHeadlessPlatformOptions();

            if (options.UseHeadlessDrawing)
                builder = builder.UseRenderingSubsystem(HeadlessPlatformRenderInterface.Initialize, "Headless");
            return builder
                .UseStandardRuntimePlatformSubsystem()
                .UseWindowingSubsystem(() => AvaloniaHeadlessPlatform.Initialize(options), "Headless");
        }
    }
}
