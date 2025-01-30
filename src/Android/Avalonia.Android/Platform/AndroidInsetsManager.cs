using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Views.Animations;
using AndroidX.Core.Graphics;
using AndroidX.Core.View;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Animation.Easings;
using Avalonia.Controls.Platform;
using Avalonia.Media;
using Avalonia.Threading;
using AndroidWindow = Android.Views.Window;

namespace Avalonia.Android.Platform
{
    internal sealed class AndroidInsetsManager : WindowInsetsAnimationCompat.Callback, IInsetsManager, IOnApplyWindowInsetsListener, ViewTreeObserver.IOnGlobalLayoutListener, IInputPane
    {
        private readonly Activity _activity;
        private readonly TopLevelImpl _topLevel;
        private bool _displayEdgeToEdge;
        private bool? _systemUiVisibility;
        private SystemBarTheme? _statusBarTheme;
        private bool? _isDefaultSystemBarLightTheme;
        private Color? _systemBarColor;
        private InputPaneState _state;
        private Rect _previousRect;
        private Insets? _previousImeInset;
        private readonly bool _usesLegacyLayouts;

        private AndroidWindow Window => _activity.Window ?? throw new InvalidOperationException("Activity.Window must be set."); 
        
        public event EventHandler<SafeAreaChangedArgs>? SafeAreaChanged;
        public event EventHandler<InputPaneStateEventArgs>? StateChanged;

        public InputPaneState State
        {
            get => _state; set
            {
                var oldState = _state;
                _state = value;

                if (oldState != value && Build.VERSION.SdkInt <= BuildVersionCodes.Q)
                {
                    var currentRect = OccludedRect;
                    NotifyStateChanged(value, _previousRect, currentRect, TimeSpan.Zero, null);
                    _previousRect = currentRect;
                }
            }
        }

        public bool DisplayEdgeToEdge
        {
            get => _displayEdgeToEdge;
            set
            {
                _displayEdgeToEdge = value;

                if (OperatingSystem.IsAndroidVersionAtLeast(28) && Window.Attributes is { } attributes)
                {
                    attributes.LayoutInDisplayCutoutMode = value ? LayoutInDisplayCutoutMode.ShortEdges : LayoutInDisplayCutoutMode.Default;
                }

                WindowCompat.SetDecorFitsSystemWindows(Window, !value);

                if (value)
                {
                    Window.AddFlags(WindowManagerFlags.TranslucentStatus);
                    Window.AddFlags(WindowManagerFlags.TranslucentNavigation);
                }
                else
                {
                    SystemBarColor = _systemBarColor;
                }
            }
        }

        internal AndroidInsetsManager(Activity activity, TopLevelImpl topLevel) : base(DispatchModeStop)
        {
            _activity = activity;
            _topLevel = topLevel;

            ViewCompat.SetOnApplyWindowInsetsListener(Window.DecorView, this);

            if (Build.VERSION.SdkInt < BuildVersionCodes.R)
            {
                _usesLegacyLayouts = true;
                _activity.Window?.DecorView.ViewTreeObserver?.AddOnGlobalLayoutListener(this);
            }

            DisplayEdgeToEdge = false;

            ViewCompat.SetWindowInsetsAnimationCallback(Window.DecorView, this);
        }

        public Thickness SafeAreaPadding
        {
            get
            {
                var insets = ViewCompat.GetRootWindowInsets(Window.DecorView);

                if (insets != null)
                {
                    var renderScaling = _topLevel.RenderScaling;

                    var inset = insets.GetInsets(
                        _displayEdgeToEdge ?
                            WindowInsetsCompat.Type.StatusBars() | WindowInsetsCompat.Type.NavigationBars() |
                            WindowInsetsCompat.Type.DisplayCutout() : 0);

                    return new Thickness(inset.Left / renderScaling,
                        inset.Top / renderScaling,
                        inset.Right / renderScaling,
                        inset.Bottom / renderScaling);
                }

                return default;
            }
        }

        public Rect OccludedRect
        {
            get
            {
                var insets = ViewCompat.GetRootWindowInsets(Window.DecorView);

                if (insets != null)
                {
                    var navbarInset = insets.GetInsets(WindowInsetsCompat.Type.NavigationBars()).Bottom;

                    var height = Math.Max((float)((insets.GetInsets(WindowInsetsCompat.Type.Ime()).Bottom - navbarInset) / _topLevel.RenderScaling), 0);

                    return new Rect(0, _topLevel.ClientSize.Height - SafeAreaPadding.Bottom - height, _topLevel.ClientSize.Width, height);
                }

                return default;
            }
        }

        public WindowInsetsCompat OnApplyWindowInsets(View v, WindowInsetsCompat insets)
        {
            insets = ViewCompat.OnApplyWindowInsets(v, insets);
            NotifySafeAreaChanged(SafeAreaPadding);

            if (_previousRect == default)
            {
                _previousRect = OccludedRect;
            }

            State = insets.IsVisible(WindowInsetsCompat.Type.Ime()) ? InputPaneState.Open : InputPaneState.Closed;

            // Workaround for weird inset values for android 11
            if(Build.VERSION.SdkInt == BuildVersionCodes.R)
            {
                var imeInset = insets.GetInsets(WindowInsetsCompat.Type.Ime());
                if(_previousImeInset == default)
                    _previousImeInset = imeInset;
                if(imeInset.Bottom != _previousImeInset.Bottom)
                {
                    NotifyStateChanged(State, _previousRect, OccludedRect, TimeSpan.Zero, null);
                }
                _previousImeInset = imeInset;
            }

            return insets;
        }

        internal void SetDefaultSystemLightMode(bool islightMode)
        {
            _isDefaultSystemBarLightTheme = islightMode;
        }

        private void NotifySafeAreaChanged(Thickness safeAreaPadding)
        {
            Dispatcher.UIThread.Send(_ => SafeAreaChanged?.Invoke(this, new SafeAreaChangedArgs(safeAreaPadding)));
        }
        
        private void NotifyStateChanged(InputPaneState newState, Rect? startRect, Rect endRect, TimeSpan animationDuration, IEasing? easing)
        {
            Dispatcher.UIThread.Send(_ => StateChanged?.Invoke(this, new InputPaneStateEventArgs(newState, startRect, endRect, animationDuration, easing)));
        }

        public void OnGlobalLayout()
        {
            NotifySafeAreaChanged(SafeAreaPadding);

            if (_usesLegacyLayouts)
            {
                var insets = ViewCompat.GetRootWindowInsets(Window.DecorView);
                State = insets?.IsVisible(WindowInsetsCompat.Type.Ime()) == true ? InputPaneState.Open : InputPaneState.Closed;
            }
        }

        public SystemBarTheme? SystemBarTheme
        {
            get
            {
                try
                {
                    var compat = new WindowInsetsControllerCompat(Window, _topLevel.View);

                    return compat.AppearanceLightStatusBars ? Controls.Platform.SystemBarTheme.Light : Controls.Platform.SystemBarTheme.Dark;
                }
                catch (Exception)
                {
                    return Controls.Platform.SystemBarTheme.Light;
                }
            }
            set
            {
                _statusBarTheme = value;

                var compat = new WindowInsetsControllerCompat(Window, _topLevel.View);

                if (_isDefaultSystemBarLightTheme == null)
                {
                    _isDefaultSystemBarLightTheme = compat.AppearanceLightStatusBars;
                }

                if (value == null)
                {
                    value = _isDefaultSystemBarLightTheme.Value ? Controls.Platform.SystemBarTheme.Light : Controls.Platform.SystemBarTheme.Dark;
                }

                compat.AppearanceLightStatusBars = value == Controls.Platform.SystemBarTheme.Light;
                compat.AppearanceLightNavigationBars = value == Controls.Platform.SystemBarTheme.Light;
            }
        }

        public bool? IsSystemBarVisible
        {
            get
            {
                if (_activity.Window == null)
                {
                    return true;
                }
                var compat = ViewCompat.GetRootWindowInsets(_activity.Window.DecorView);

                return compat?.IsVisible(WindowInsetsCompat.Type.StatusBars() | WindowInsetsCompat.Type.NavigationBars());
            }
            set
            {
                _systemUiVisibility = value;

                var compat = WindowCompat.GetInsetsController(Window, _topLevel.View);

                if (value == null || value.Value)
                {
                    compat?.Show(WindowInsetsCompat.Type.StatusBars() | WindowInsetsCompat.Type.NavigationBars());
                }
                else
                {
                    compat?.Hide(WindowInsetsCompat.Type.StatusBars() | WindowInsetsCompat.Type.NavigationBars());

                    if (compat != null)
                    {
                        compat.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
                    }
                }
            }
        }

        public Color? SystemBarColor
        {
            get => _systemBarColor;
            set
            {
                _systemBarColor = value;

                if (_systemBarColor is { } color && !_displayEdgeToEdge && _activity.Window != null)
                {
                    _activity.Window.ClearFlags(WindowManagerFlags.TranslucentStatus);
                    _activity.Window.ClearFlags(WindowManagerFlags.TranslucentNavigation);
                    _activity.Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

                    var androidColor = global::Android.Graphics.Color.Argb(color.A, color.R, color.G, color.B);
                    _activity.Window.SetStatusBarColor(androidColor);

                    if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    {
                        // As we can only change the navigation bar's foreground api 26 and newer, we only change the background color if running on those versions
                        _activity.Window.SetNavigationBarColor(androidColor);
                    }
                }
            }
        }

        internal void ApplyStatusBarState()
        {
            IsSystemBarVisible = _systemUiVisibility;
            SystemBarTheme = _statusBarTheme;
            SystemBarColor = _systemBarColor;
        }

        public override WindowInsetsAnimationCompat.BoundsCompat OnStart(WindowInsetsAnimationCompat animation, WindowInsetsAnimationCompat.BoundsCompat bounds)
        {
            if ((animation.TypeMask & WindowInsetsCompat.Type.Ime()) != 0)
            {
                var insets = ViewCompat.GetRootWindowInsets(Window.DecorView);

                if (insets != null)
                {
                    var navbarInset = insets.GetInsets(WindowInsetsCompat.Type.NavigationBars()).Bottom;
                    var height = Math.Max(0, (float)((bounds.LowerBound.Bottom - navbarInset) / _topLevel.RenderScaling));
                    var upperRect = new Rect(0, _topLevel.ClientSize.Height - SafeAreaPadding.Bottom - height, _topLevel.ClientSize.Width, height);
                    height = Math.Max(0, (float)((bounds.UpperBound.Bottom - navbarInset) / _topLevel.RenderScaling));
                    var lowerRect = new Rect(0, _topLevel.ClientSize.Height - SafeAreaPadding.Bottom - height, _topLevel.ClientSize.Width, height);

                    var duration = TimeSpan.FromMilliseconds(animation.DurationMillis);

                    bool isOpening = State == InputPaneState.Open;
                    NotifyStateChanged(State, isOpening ? upperRect : lowerRect, isOpening ? lowerRect : upperRect, duration,
                        animation.Interpolator is { } interpolator ? new AnimationEasing(interpolator) : null);
                }
            }

            return base.OnStart(animation, bounds);
        }

        public override WindowInsetsCompat OnProgress(WindowInsetsCompat insets, IList<WindowInsetsAnimationCompat> runningAnimations)
        {
            return insets;
        }
    }

    internal sealed class AnimationEasing : Easing
    {
        private readonly IInterpolator _interpolator;

        public AnimationEasing(IInterpolator interpolator)
        {
            _interpolator = interpolator;
        }

        public override double Ease(double progress)
        {
            return _interpolator.GetInterpolation((float)progress);
        }
    }
}
