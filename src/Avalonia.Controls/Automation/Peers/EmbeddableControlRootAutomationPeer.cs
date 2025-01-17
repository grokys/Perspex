using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Embedding;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Automation.Peers
{
    public class EmbeddableControlRootAutomationPeer : ContentControlAutomationPeer, IEmbeddedRootProvider
    {
        private Control? _focus;

        public EmbeddableControlRootAutomationPeer(EmbeddableControlRoot owner) : base(owner)
        {
            if (owner.IsVisible)
                StartTrackingFocus();
            else
                owner.Opened += OnOpened;
            owner.Closed += OnClosed;
        }

        public new EmbeddableControlRoot Owner => (EmbeddableControlRoot)base.Owner;

        public event EventHandler? FocusChanged;

        public AutomationPeer? GetFocus() => _focus is object ? GetOrCreate(_focus) : null;

        public AutomationPeer? GetPeerFromPoint(Point p)
        {
            var hit = Owner.GetVisualAt(p)?.FindAncestorOfType<Control>();

            if (hit is null)
                return null;

            var peer = GetOrCreate(hit);
            return peer;
        }

        private void StartTrackingFocus()
        {
            if (KeyboardDevice.Instance is not null)
            {
                KeyboardDevice.Instance.PropertyChanged += KeyboardDevicePropertyChanged;
                OnFocusChanged(KeyboardDevice.Instance.FocusedElement);
            }
        }

        private void StopTrackingFocus()
        {
            if (KeyboardDevice.Instance is not null)
                KeyboardDevice.Instance.PropertyChanged -= KeyboardDevicePropertyChanged;
        }

        private void OnFocusChanged(IInputElement? focus)
        {
            var oldFocus = _focus;
            var c = focus as Control;

            _focus = c?.VisualRoot == Owner ? c : null;

            if (_focus != oldFocus)
            {
                var peer = _focus is object ?
                    _focus == Owner ? this :
                    GetOrCreate(_focus) : null;
                FocusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void KeyboardDevicePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KeyboardDevice.FocusedElement))
            {
                OnFocusChanged(KeyboardDevice.Instance!.FocusedElement);
            }
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            Owner.Opened -= OnOpened;
            StartTrackingFocus();
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            Owner.Closed -= OnClosed;
            StopTrackingFocus();
        }
    }
}
