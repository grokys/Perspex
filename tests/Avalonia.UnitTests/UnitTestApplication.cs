using System;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Controls;
using Avalonia.Threading;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.Input.Platform;
using Avalonia.Animation;
using Avalonia.Media;

namespace Avalonia.UnitTests
{
    public class UnitTestApplication : Application
    {
        private readonly TestServices _services;

        public UnitTestApplication() : this(null)
        {

        }

        public UnitTestApplication(TestServices services)
        {
            _services = services ?? new TestServices();
            AvaloniaLocator.CurrentMutable.BindToSelf<Application>(this);
            RegisterServices();
        }

        static UnitTestApplication()
        {
            AssetLoader.RegisterResUriParsers();
        }

        public static new UnitTestApplication Current => (UnitTestApplication)Application.Current;

        public TestServices Services => _services;

        public static IDisposable Start(TestServices services = null)
        {
            var scope = AvaloniaLocator.EnterScope();
            var oldContext = SynchronizationContext.Current;
            _ = new UnitTestApplication(services);
            Dispatcher.ResetForUnitTests();
            return Disposable.Create(() =>
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    Dispatcher.UIThread.RunJobs();
                }

                ((ToolTipService)AvaloniaLocator.Current.GetService<IToolTipService>())?.Dispose();

                scope.Dispose();
                Dispatcher.ResetForUnitTests();
                SynchronizationContext.SetSynchronizationContext(oldContext);
            });
        }

        public IDisposable StartInstance()
        {
            var scope = AvaloniaLocator.EnterScope();
            var oldContext = SynchronizationContext.Current;
            Dispatcher.ResetForUnitTests();
            return Disposable.Create(() =>
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    Dispatcher.UIThread.RunJobs();
                }

                ((ToolTipService)AvaloniaLocator.Current.GetService<IToolTipService>())?.Dispose();

                scope.Dispose();
                Dispatcher.ResetForUnitTests();
                SynchronizationContext.SetSynchronizationContext(oldContext);
            });
        }

        public override void RegisterServices()
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IAssetLoader>().ToConstant(Services.AssetLoader)
                .Bind<IFocusManager>().ToConstant(Services.FocusManager)
                .Bind<IGlobalClock>().ToConstant(Services.GlobalClock)
                .BindToSelf<IGlobalStyles>(this)
                .Bind<IInputManager>().ToConstant(Services.InputManager)
                .Bind<IToolTipService>().ToConstant(Services.InputManager == null ? null : new ToolTipService(Services.InputManager))
                .Bind<IKeyboardDevice>().ToConstant(Services.KeyboardDevice?.Invoke())
                .Bind<IMouseDevice>().ToConstant(Services.MouseDevice?.Invoke())
                .Bind<IKeyboardNavigationHandler>().ToFunc(Services.KeyboardNavigation ?? (() => null))
                .Bind<IRuntimePlatform>().ToConstant(Services.Platform)
                .Bind<IPlatformRenderInterface>().ToConstant(Services.RenderInterface)
                .Bind<IFontManagerImpl>().ToConstant(Services.FontManagerImpl)
                .Bind<ITextShaperImpl>().ToConstant(Services.TextShaperImpl)
                .Bind<IDispatcherImpl>().ToConstant(Services.DispatcherImpl)
                .Bind<ICursorFactory>().ToConstant(Services.StandardCursorFactory)
                .Bind<IWindowingPlatform>().ToConstant(Services.WindowingPlatform)
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
                .Bind<IPlatformSettings>().ToSingleton<DefaultPlatformSettings>()
                .Bind<IAccessKeyHandler>().ToConstant(Services.AccessKeyHandler)
                ;
            
            // This is a hack to make tests work, we need to refactor the way font manager is registered
            // See https://github.com/AvaloniaUI/Avalonia/issues/10081
            AvaloniaLocator.CurrentMutable.Bind<FontManager>().ToConstant((FontManager)null!);
            var theme = Services.Theme?.Invoke();

            if (theme is Style styles)
            {
                Styles.AddRange(styles.Children);
            }
            else if (theme is not null)
            {
                Styles.Add(theme);
            }
        }
    }
}
