using System;
using Avalonia.Input.Raw;

namespace Avalonia.Input
{
    internal class PointerOverPreProcessor : IObserver<RawInputEventArgs>
    {
        private IPointerDevice? _lastActivePointerDevice;
        private (IPointer pointer, PixelPoint position)? _currentPointer;
        private PixelPoint? _lastKnownPosition;

        private readonly IInputRoot _inputRoot;

        public PointerOverPreProcessor(IInputRoot inputRoot)
        {
            _inputRoot = inputRoot ?? throw new ArgumentNullException(nameof(inputRoot));
        }

        public PixelPoint? LastPosition => _lastKnownPosition;
        
        public void OnCompleted()
        {
            ClearPointerOver();
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(RawInputEventArgs value)
        {
            if (value is RawPointerEventArgs args
                && args.Root == _inputRoot
                && value.Device is IPointerDevice pointerDevice)
            {
                if (pointerDevice != _lastActivePointerDevice)
                {
                    ClearPointerOver();

                    // Set last active device before processing input, because ClearPointerOver might be called and clear last device.
                    _lastActivePointerDevice = pointerDevice;
                }

                if (args.Type is RawPointerEventType.LeaveWindow or RawPointerEventType.NonClientLeftButtonDown
                    or RawPointerEventType.TouchCancel or RawPointerEventType.TouchEnd)
                {
                    if (_currentPointer is var (lastPointer, lastPosition))
                    {
                        _currentPointer = null;
                        ClearPointerOver(lastPointer, args.Root, 0, PointToClient(args.Root, lastPosition),
                            new PointerPointProperties(args.InputModifiers, args.Type.ToUpdateKind()),
                            args.InputModifiers.ToKeyModifiers());
                    }
                }
                else if (args.Type is RawPointerEventType.TouchBegin or RawPointerEventType.TouchUpdate && args.Root is Visual visual)
                {
                    _lastKnownPosition = visual.PointToScreen(args.Position);
                }
                else if (args.Type is RawPointerEventType.TouchCancel or RawPointerEventType.TouchEnd
                    && pointerDevice.TryGetPointer(args) is { } p)
                {
                    _currentPointer = null;
                    ClearPointerOver(p, args.Root, 0, args.Position,
                        new PointerPointProperties(args.InputModifiers, args.Type.ToUpdateKind()),
                        args.InputModifiers.ToKeyModifiers());
                }
                else if (pointerDevice.TryGetPointer(args) is { } pointer &&
                    pointer.Type != PointerType.Touch)
                {
                    // See PointerOver_Or_Exited_Should_Ignore_Capture test.
                    // Following UWP behavior, PointerEntered and PointerExited events should always ignore captured element. 
                    var element = args.InputHitTestResult.firstEnabledAncestor;

                    SetPointerOver(pointer, args.Root, element, args.Timestamp, args.Position,
                        new PointerPointProperties(args.InputModifiers, args.Type.ToUpdateKind()),
                        args.InputModifiers.ToKeyModifiers());
                }
            }
        }

        public void SceneInvalidated(Rect dirtyRect)
        {
            if (_currentPointer is (var pointer, var position))
            {
                var clientPoint = PointToClient(_inputRoot, position);

                if (dirtyRect.Contains(clientPoint))
                {
                    var element = _inputRoot.InputHitTest(clientPoint);
                    SetPointerOver(pointer, _inputRoot, element, 0, clientPoint, PointerPointProperties.None, KeyModifiers.None);
                }
                else if (!((Visual)_inputRoot).Bounds.Contains(clientPoint))
                {
                    ClearPointerOver(pointer, _inputRoot, 0, clientPoint, PointerPointProperties.None, KeyModifiers.None);
                }
            }
        }

        private void ClearPointerOver()
        {
            if (_currentPointer is (var pointer, var position))
            {
                var clientPoint = PointToClient(_inputRoot, position);
                ClearPointerOver(pointer, _inputRoot, 0, clientPoint, PointerPointProperties.None, KeyModifiers.None);
            }
            _currentPointer = null;
            _lastActivePointerDevice = null;
        }

        private void ClearPointerOver(IPointer pointer, IInputRoot root,
            ulong timestamp, Point? position, PointerPointProperties properties, KeyModifiers inputModifiers)
        {
            var element = root.PointerOverElement;
            if (element is null)
            {
                return;
            }

            // Do not pass rootVisual, when we have unknown position,
            // so GetPosition won't return invalid values.
#pragma warning disable CS0618
            var e = new PointerEventArgs(InputElement.PointerExitedEvent, element, pointer,
                position.HasValue ? root as Visual : null, position.HasValue ? position.Value : default,
                timestamp, properties, inputModifiers);
#pragma warning restore CS0618

            if (element is Visual v && !v.IsAttachedToVisualTree)
            {
                // element has been removed from visual tree so do top down cleanup
                if (root.IsPointerOver)
                {
                    ClearChildrenPointerOver(e, root, true);
                }
            }
            while (element != null)
            {
                e.Source = element;
                e.Handled = false;
                element.RaiseEvent(e);
                element = GetVisualParent(element);
            }

            root.PointerOverElement = null;
            _lastActivePointerDevice = null;
            _currentPointer = null;
        }

        private void ClearChildrenPointerOver(PointerEventArgs e, IInputElement element, bool clearRoot)
        {
            if (element is Visual v)
            {
                foreach (var el in v.VisualChildren)
                {
                    if (el is IInputElement { IsPointerOver: true } child)
                    {
                        ClearChildrenPointerOver(e, child, true);
                        break;
                    }
                }
            }
            
            if (clearRoot)
            {
                e.Source = element;
                e.Handled = false;
                element.RaiseEvent(e);
            }
        }

        private void SetPointerOver(IPointer pointer, IInputRoot root, IInputElement? element,
            ulong timestamp, Point position, PointerPointProperties properties, KeyModifiers inputModifiers)
        {
            var pointerOverElement = root.PointerOverElement;
            var screenPosition = ((Visual)root).PointToScreen(position);
            _lastKnownPosition = screenPosition;

            if (element != pointerOverElement)
            {
                if (element != null)
                {
                    SetPointerOverToElement(pointer, root, element, timestamp, position, properties, inputModifiers);
                }
                else
                {
                    ClearPointerOver(pointer, root, timestamp, position, properties, inputModifiers);
                }
            }

            _currentPointer = (pointer, screenPosition);
        }

        private void SetPointerOverToElement(IPointer pointer, IInputRoot root, IInputElement element,
            ulong timestamp, Point position, PointerPointProperties properties, KeyModifiers inputModifiers)
        {
            IInputElement? branch = null;

            IInputElement? el = element;

            while (el != null)
            {
                if (el.IsPointerOver)
                {
                    branch = el;
                    break;
                }
                el = GetVisualParent(el);
            }

            el = root.PointerOverElement;

#pragma warning disable CS0618
            var e = new PointerEventArgs(InputElement.PointerExitedEvent, el, pointer, (Visual)root, position,
                timestamp, properties, inputModifiers);
#pragma warning restore CS0618
            if (el is Visual v && branch != null && !v.IsAttachedToVisualTree)
            {
                ClearChildrenPointerOver(e, branch, false);
            }

            while (el != null && el != branch)
            {
                e.Source = el;
                e.Handled = false;
                el.RaiseEvent(e);
                el = GetVisualParent(el);
            }

            el = root.PointerOverElement = element;

            e.RoutedEvent = InputElement.PointerEnteredEvent;

            while (el != null && el != branch)
            {
                e.Source = el;
                e.Handled = false;
                el.RaiseEvent(e);
                el = GetVisualParent(el);
            }
        }

        private static IInputElement? GetVisualParent(IInputElement e)
        {
            return (e as Visual)?.VisualParent as IInputElement;
        }

        private static Point PointToClient(IInputRoot root, PixelPoint p)
        {
            return ((Visual)root).PointToClient(p);
        }
    }
}
