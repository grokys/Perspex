using System;
using System.Runtime.Versioning;
using Android.Content;
using Android.Content.Res;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Avalonia.Android.Platform;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Android
{
    public class AvaloniaView : FrameLayout
    {
        private EmbeddableControlRoot _root;
        private readonly ViewImpl _view;

        private IDisposable? _timerSubscription;

        // https://learn.microsoft.com/en-us/previous-versions/xamarin/android/internals/architecture#java-activation
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public AvaloniaView(IntPtr javaReference, JniHandleOwnership transfer): base(javaReference, transfer)
        {
        }

        public AvaloniaView(Context context) : this(context, null)
        {
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public AvaloniaView(Context context, IAttributeSet? attrs) : this(context, attrs, 0)
        {
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public AvaloniaView(Context context, IAttributeSet? attrs, int defStyle) : base(context, attrs, defStyle)
        {
            _view = new ViewImpl(this);
            AddView(_view.View);

            _root = new EmbeddableControlRoot(_view);
            _root.Prepare();
            SetBackgroundColor(global::Android.Graphics.Color.Transparent);
            OnConfigurationChanged();
        }

        internal TopLevelImpl TopLevelImpl => _view;
        internal TopLevel? TopLevel => _root;

        public object? Content
        {
            get { return _root.Content; }
            set { _root.Content = value; }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _root?.Dispose();
            _root = null!;
        }

        public override bool DispatchKeyEvent(KeyEvent? e)
        {
            return _view.View.DispatchKeyEvent(e);
        }

        [SupportedOSPlatform("android24.0")]
        public override void OnVisibilityAggregated(bool isVisible)
        {
            base.OnVisibilityAggregated(isVisible);
            OnVisibilityChanged(isVisible);
        }

        protected override void OnVisibilityChanged(View changedView, [GeneratedEnum] ViewStates visibility)
        {
            base.OnVisibilityChanged(changedView, visibility);
            OnVisibilityChanged(visibility == ViewStates.Visible);
        }

        internal void OnVisibilityChanged(bool isVisible)
        {
            if (isVisible && _timerSubscription == null)
            {
                if (AvaloniaLocator.Current.GetService<IRenderTimer>() is ChoreographerTimer timer)
                {
                    _timerSubscription = timer.SubscribeView(this);
                }

                _root.StartRendering();

                if (_view.TryGetFeature<IInsetsManager>(out var insetsManager) == true)
                {
                    (insetsManager as AndroidInsetsManager)?.ApplyStatusBarState();
                }
            }
            else if (!isVisible && _timerSubscription != null)
            {
                _root.StopRendering();
                _timerSubscription?.Dispose();
                _timerSubscription = null;
            }
        }
        
        protected override void OnConfigurationChanged(Configuration? newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            OnConfigurationChanged();
        }

        private void OnConfigurationChanged()
        {
            if (Context is { } context)
            {
                var settings =
                    AvaloniaLocator.Current.GetRequiredService<IPlatformSettings>() as AndroidPlatformSettings;
                settings?.OnViewConfigurationChanged(context);
            }
        }

        class ViewImpl : TopLevelImpl
        {
            public ViewImpl(AvaloniaView avaloniaView) : base(avaloniaView)
            {
                View.Focusable = true;
                View.FocusChange += ViewImpl_FocusChange;
            }

            private void ViewImpl_FocusChange(object? sender, FocusChangeEventArgs e)
            {
                if(!e.HasFocus)
                    LostFocus?.Invoke();
            }
        }
    }
}
