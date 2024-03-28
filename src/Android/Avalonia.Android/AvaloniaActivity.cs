#nullable enable
using System;
using System.Runtime.Versioning;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Android;

public class AvaloniaActivity : AppCompatActivity, IAvaloniaActivity
{
    private EventHandler<ActivatedEventArgs>? _onActivated, _onDeactivated;
    private GlobalLayoutListener? _listener;
    private object? _content;
    internal AvaloniaView? _view;

    public Action<int, Result, Intent?>? ActivityResult { get; set; }
    public Action<int, string[], Permission[]>? RequestPermissionsResult { get; set; }

    public event EventHandler<AndroidBackRequestedEventArgs>? BackRequested;

    public object? Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                if (_view is not null)
                {
                    _view.Content = _content;
                }
            }
        }
    }

    event EventHandler<ActivatedEventArgs>? IAvaloniaActivity.Activated
    {
        add { _onActivated += value; }
        remove { _onActivated -= value; }
    }

    event EventHandler<ActivatedEventArgs>? IAvaloniaActivity.Deactivated
    {
        add { _onDeactivated += value; }
        remove { _onDeactivated -= value; }
    }

    [ObsoletedOSPlatform("android33.0")]
    public override void OnBackPressed()
    {
        var eventArgs = new AndroidBackRequestedEventArgs();

        BackRequested?.Invoke(this, eventArgs);

        if (!eventArgs.Handled)
        {
            base.OnBackPressed();
        }
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        _view = CreateAvaloniaView();
        _view.Content = _content;

        base.OnCreate(savedInstanceState);

        SetContentView(_view);

        _listener = new GlobalLayoutListener(_view);

        _view.ViewTreeObserver?.AddOnGlobalLayoutListener(_listener);
            
        if (Intent?.Data is {} androidUri
            && androidUri.IsAbsolute
            && Uri.TryCreate(androidUri.ToString(), UriKind.Absolute, out var protocolUri))
        {
            _onActivated?.Invoke(this, new ProtocolActivatedEventArgs(ActivationKind.OpenUri, protocolUri));
        }
    }

    protected override void OnStop()
    {
        _onDeactivated?.Invoke(this, new ActivatedEventArgs(ActivationKind.Background));
        base.OnStop();
    }

    protected override void OnStart()
    {
        _onActivated?.Invoke(this, new ActivatedEventArgs(ActivationKind.Background));
        base.OnStart();
    }

    protected override void OnResume()
    {
        base.OnResume();

        // Android only respects LayoutInDisplayCutoutMode value if it has been set once before window becomes visible.
        if (OperatingSystem.IsAndroidVersionAtLeast(28) && Window is { Attributes: { } attributes })
        {
            attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
        }
    }

    protected override void OnDestroy()
    {
        if (_view is not null)
        {
            _view.Content = null;
            _view.ViewTreeObserver?.RemoveOnGlobalLayoutListener(_listener);
        }

        base.OnDestroy();
    }
        
    protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        ActivityResult?.Invoke(requestCode, resultCode, data);
    }

    [SupportedOSPlatform("android23.0")]
    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        RequestPermissionsResult?.Invoke(requestCode, permissions, grantResults);
    }

    protected override void Dispose(bool disposing)
    {
        _view?.Dispose();
        _view = null;
        base.Dispose(disposing);
    }

    private protected virtual AvaloniaView CreateAvaloniaView()
    {
        if (Avalonia.Application.Current is null)
        {
            throw new InvalidOperationException(
                "Avalonia Application was not initialized. Make sure you have created AvaloniaMainActivity.");
        }
        
        return new AvaloniaView(this);
    }

    private class GlobalLayoutListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
    {
        private readonly AvaloniaView _view;

        public GlobalLayoutListener(AvaloniaView view)
        {
            _view = view;
        }

        public void OnGlobalLayout()
        {
            _view.TopLevelImpl?.Resize(_view.TopLevelImpl.ClientSize);
        }
    }
}
