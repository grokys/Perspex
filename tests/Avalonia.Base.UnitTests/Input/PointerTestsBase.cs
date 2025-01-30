#nullable enable
using System;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;

namespace Avalonia.Base.UnitTests.Input;

public abstract class PointerTestsBase : ScopedTestBase
{
    private protected static void SetHit(Mock<IHitTester> renderer, Control? hit)
    {
        renderer.Setup(x => x.HitTest(It.IsAny<Point>(), It.IsAny<Visual>(), It.IsAny<Func<Visual, bool>>()))
            .Returns(hit is null ? Array.Empty<Control>() : new[] { hit });

        renderer.Setup(x => x.HitTestFirst(It.IsAny<Point>(), It.IsAny<Visual>(), It.IsAny<Func<Visual, bool>>()))
            .Returns(hit);
    }

    protected static void SetMove(Mock<IPointerDevice> deviceMock, IInputRoot root, IInputElement element)
    {
        deviceMock.Setup(d => d.ProcessRawEvent(It.IsAny<RawPointerEventArgs>()))
            .Callback(() => element.RaiseEvent(CreatePointerMovedArgs(root, element)));
    }

    private protected static Mock<IWindowImpl> CreateTopLevelImplMock()
    {
        var impl = new Mock<IWindowImpl>();
        impl.DefaultValue = DefaultValue.Mock;
        impl.SetupAllProperties();
        impl.SetupGet(r => r.RenderScaling).Returns(1);
        impl.Setup(r => r.TryGetFeature(It.IsAny<Type>())).Returns(null);
        impl.Setup(r => r.Compositor).Returns(RendererMocks.CreateDummyCompositor());
        impl.Setup(r => r.PointToScreen(It.IsAny<Point>())).Returns<Point>(p => new PixelPoint((int)p.X, (int)p.Y));
        impl.Setup(r => r.PointToClient(It.IsAny<PixelPoint>())).Returns<PixelPoint>(p => new Point(p.X, p.Y));
        
        var screen1 = new Mock<Screen>(1.75, new PixelRect(new PixelSize(1920, 1080)), new PixelRect(new PixelSize(1920, 966)), true);
        var screens = new Mock<IScreenImpl>();
        screens.Setup(x => x.ScreenFromWindow(It.IsAny<IWindowBaseImpl>())).Returns(screen1.Object);
        impl.Setup(x => x.TryGetFeature(It.Is<Type>(t => t == typeof(IScreenImpl)))).Returns(screens.Object);

        return impl;
    }

    private protected static TopLevel CreateInputRoot(IWindowImpl impl, Control child, IHitTester hitTester)
    {
        var root = new Window(impl)
        {
            Width = 100,
            Height = 100,
            Content = child,
            Template = new FuncControlTemplate<Window>((w, _) => new ContentPresenter { Content = w.Content }),
            HitTesterOverride = hitTester
        };
        root.Show();
        return root;
    }

    protected static RawPointerEventArgs CreateRawPointerArgs(
        IPointerDevice pointerDevice,
        IInputRoot root,
        RawPointerEventType type,
        Point? position = default)
    {
        return new RawPointerEventArgs(pointerDevice, 0, root, type, position ?? default, default);
    }

    protected static RawPointerEventArgs CreateRawPointerMovedArgs(
        IPointerDevice pointerDevice,
        IInputRoot root,
        Point? position = null)
    {
        return new RawPointerEventArgs(pointerDevice, 0, root, RawPointerEventType.Move,
            position ?? default, default);
    }

    protected static PointerEventArgs CreatePointerMovedArgs(
        IInputRoot root, IInputElement? source, Point? position = null)
    {
        return new PointerEventArgs(InputElement.PointerMovedEvent, source, new Mock<IPointer>().Object, (Visual)root,
            position ?? default, default, PointerPointProperties.None, KeyModifiers.None);
    }

    protected static Mock<IPointerDevice> CreatePointerDeviceMock(
        IPointer? pointer = null,
        PointerType pointerType = PointerType.Mouse)
    {
        if (pointer is null)
        {
            var pointerMock = new Mock<IPointer>();
            pointerMock.SetupGet(p => p.Type).Returns(pointerType);
            pointer = pointerMock.Object;
        }

        var pointerDevice = new Mock<IPointerDevice>();
        pointerDevice.Setup(d => d.TryGetPointer(It.IsAny<RawPointerEventArgs>()))
            .Returns(pointer);

        return pointerDevice;
    }
    
    protected static void AddEnteredExitedHandlers(
        EventHandler<PointerEventArgs> handler,
        params IInputElement[] controls)
    {
        foreach (var c in controls)
        {
            c.PointerEntered += handler;
            c.PointerExited += handler;
            c.PointerMoved += handler;
        }
    }

    protected static void AddPressedReleasedHandlers(
        EventHandler<PointerEventArgs> handler,
        params IInputElement[] controls)
    {
        foreach (var c in controls)
        {
            c.PointerPressed += (s, a) => handler(s, a);
            c.PointerReleased += (s, a) => handler(s, a);
        }
    }
}
