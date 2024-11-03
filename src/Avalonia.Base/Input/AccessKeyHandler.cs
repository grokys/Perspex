using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Handles access keys for a window.
    /// </summary>
    internal class AccessKeyHandler : IAccessKeyHandler
    {
        /// <summary>
        /// Defines the AccessKeyPressed attached event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> AccessKeyPressedEvent =
            RoutedEvent.Register<RoutedEventArgs>(
                "AccessKeyPressed",
                RoutingStrategies.Bubble,
                typeof(AccessKeyHandler));

        /// <summary>
        /// The registered access keys.
        /// </summary>
        private readonly List<(string AccessKey, IInputElement Element)> _registered = new();

        /// <summary>
        /// The window to which the handler belongs.
        /// </summary>
        private IInputRoot? _owner;

        /// <summary>
        /// Whether access keys are currently being shown;
        /// </summary>
        private bool _showingAccessKeys;

        /// <summary>
        /// Whether to ignore the Alt KeyUp event.
        /// </summary>
        private bool _ignoreAltUp;

        /// <summary>
        /// Whether the AltKey is down.
        /// </summary>
        private bool _altIsDown;

        /// <summary>
        /// Element to restore following AltKey taking focus.
        /// </summary>
        private IInputElement? _restoreFocusElement;

        /// <summary>
        /// The window's main menu.
        /// </summary>
        private IMainMenu? _mainMenu;

        /// <summary>
        /// Gets or sets the window's main menu.
        /// </summary>
        public IMainMenu? MainMenu
        {
            get => _mainMenu;
            set
            {
                if (_mainMenu != null)
                {
                    _mainMenu.Closed -= MainMenuClosed;
                }

                _mainMenu = value;

                if (_mainMenu != null)
                {
                    _mainMenu.Closed += MainMenuClosed;
                }
            }
        }

        /// <summary>
        /// Sets the owner of the access key handler.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <remarks>
        /// This method can only be called once, typically by the owner itself on creation.
        /// </remarks>
        public void SetOwner(IInputRoot owner)
        {
            if (_owner != null)
            {
                throw new InvalidOperationException("AccessKeyHandler owner has already been set.");
            }

            _owner = owner ?? throw new ArgumentNullException(nameof(owner));

            _owner.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
            _owner.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble);
            _owner.AddHandler(InputElement.KeyUpEvent, OnPreviewKeyUp, RoutingStrategies.Tunnel);
            _owner.AddHandler(InputElement.PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);
        }

        /// <summary>
        /// Registers an input element to be associated with an access key.
        /// </summary>
        /// <param name="accessKey">The access key.</param>
        /// <param name="element">The input element.</param>
        public void Register(char accessKey, IInputElement element)
        {
            var existing = _registered.FirstOrDefault(x => x.Item2 == element);

            if (existing != default)
            {
                _registered.Remove(existing);
            }

            _registered.Add((accessKey.ToString().ToUpperInvariant(), element));
        }

        /// <summary>
        /// Unregisters the access keys associated with the input element.
        /// </summary>
        /// <param name="element">The input element.</param>
        public void Unregister(IInputElement element)
        {
            foreach (var i in _registered.Where(x => x.Item2 == element).ToList())
            {
                _registered.Remove(i);
            }
        }

        /// <summary>
        /// Called when a key is pressed in the owner window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
            {
                _altIsDown = true;

                if (MainMenu == null || !MainMenu.IsOpen)
                {
                    var focusManager = FocusManager.GetFocusManager(e.Source as IInputElement);
                    
                    // TODO: Use FocusScopes to store the current element and restore it when context menu is closed.
                    // Save currently focused input element.
                    _restoreFocusElement = focusManager?.GetFocusedElement();

                    // When Alt is pressed without a main menu, or with a closed main menu, show
                    // access key markers in the window (i.e. "_File").
                    _owner!.ShowAccessKeys = _showingAccessKeys = true;
                }
                else
                {
                    // If the Alt key is pressed and the main menu is open, close the main menu.
                    CloseMenu();
                    _ignoreAltUp = true;

                    _restoreFocusElement?.Focus();
                    _restoreFocusElement = null;
                }
            }
            else if (_altIsDown)
            {
                _ignoreAltUp = true;
            }
        }

        /// <summary>
        /// Called when a key is pressed in the owner window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnKeyDown(object? sender, KeyEventArgs e)
        {
            bool menuIsOpen = MainMenu?.IsOpen == true;

            if (e.KeyModifiers.HasAllFlags(KeyModifiers.Alt) && !e.KeyModifiers.HasAllFlags(KeyModifiers.Control) || menuIsOpen)
            {
                // If any other key is pressed with the Alt key held down, or the main menu is open,
                // find all controls who have registered that access key.

                var text = e.KeySymbol;
                var matches = _registered
                    .Where(x => string.Equals(x.AccessKey, text, StringComparison.OrdinalIgnoreCase)
                        && x.Element.IsEffectivelyVisible
                        && x.Element.IsEffectivelyEnabled)
                    .Select(x => x.Element);

                // If the menu is open, only match controls in the menu's visual tree.
                if (menuIsOpen)
                {
                    matches = matches.Where(x => x is not null && ((Visual)MainMenu!).IsLogicalAncestorOf((Visual)x));
                }

                var match = matches.FirstOrDefault();

                // If there was a match, raise the AccessKeyPressed event on it.
                if (match is not null)
                {
                    match.RaiseEvent(new RoutedEventArgs(AccessKeyPressedEvent));
                }
            }
        }

        /// <summary>
        /// Handles the Alt/F10 keys being released in the window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnPreviewKeyUp(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftAlt:
                case Key.RightAlt:
                    _altIsDown = false;

                    if (_ignoreAltUp)
                    {
                        _ignoreAltUp = false;
                    }
                    else if (_showingAccessKeys && MainMenu != null)
                    {
                        MainMenu.Open();
                    }
                    if (MainMenu is null)
                    {
                        _owner!.ShowAccessKeys = _showingAccessKeys = false;
                    }
                    break;
            }
        }

        /// <summary>
        /// Handles pointer presses in the window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnPreviewPointerPressed(object? sender, PointerEventArgs e)
        {
            if (_showingAccessKeys)
            {
                _owner!.ShowAccessKeys = false;
            }
        }

        /// <summary>
        /// Closes the <see cref="MainMenu"/> and performs other bookeeping.
        /// </summary>
        private void CloseMenu()
        {
            MainMenu!.Close();
            _owner!.ShowAccessKeys = _showingAccessKeys = false;
        }

        private void MainMenuClosed(object? sender, EventArgs e)
        {
            _owner!.ShowAccessKeys = false;
        }
    }
}
