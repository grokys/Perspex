﻿using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class AccessKeyHandlerTests
    {
        [Fact]
        public void Should_Raise_Key_Events_For_Unregistered_Access_Key()
        {
            var root = new TestRoot();
            var target = new AccessKeyHandler();
            var events = new List<string>();

            target.SetOwner(root);
            root.KeyDown += (s, e) => events.Add($"KeyDown {e.Key}");
            root.KeyUp += (s, e) => events.Add($"KeyUp {e.Key}");

            KeyDown(root, Key.LeftAlt);
            KeyDown(root, Key.A, KeyModifiers.Alt);
            KeyUp(root, Key.A, KeyModifiers.Alt);
            KeyUp(root, Key.LeftAlt);

            Assert.Equal(new[]
            {
                "KeyDown LeftAlt",
                "KeyDown A",
                "KeyUp A",
                "KeyUp LeftAlt",
            }, events);
        }

        [Fact]
        public void Should_Raise_Key_Events_For_Unregistered_Access_Key_With_MainMenu()
        {
            var root = new TestRoot();
            var target = new AccessKeyHandler();
            var menu = Mock.Of<IMainMenu>();
            var events = new List<string>();

            target.SetOwner(root);
            target.MainMenu = menu;
            root.KeyDown += (s, e) => events.Add($"KeyDown {e.Key}");
            root.KeyUp += (s, e) => events.Add($"KeyUp {e.Key}");

            KeyDown(root, Key.LeftAlt);
            KeyDown(root, Key.A, KeyModifiers.Alt);
            KeyUp(root, Key.A, KeyModifiers.Alt);
            KeyUp(root, Key.LeftAlt);

            Assert.Equal(new[]
            {
                "KeyDown LeftAlt",
                "KeyDown A",
                "KeyUp A",
                "KeyUp LeftAlt",
            }, events);
        }

        [Fact]
        public void Should_Raise_Key_Events_For_Alt_Key()
        {
            var root = new TestRoot();
            var target = new AccessKeyHandler();
            var events = new List<string>();

            target.SetOwner(root);
            root.KeyDown += (s, e) => events.Add($"KeyDown {e.Key}");
            root.KeyUp += (s, e) => events.Add($"KeyUp {e.Key}");

            KeyDown(root, Key.LeftAlt);
            KeyUp(root, Key.LeftAlt);

            Assert.Equal(new[]
            {
                "KeyDown LeftAlt",
                "KeyUp LeftAlt",
            }, events);
        }

        [Fact]
        public void Should_Raise_Key_Events_For_Alt_Key_With_MainMenu()
        {
            var root = new TestRoot();
            var target = new AccessKeyHandler();
            var menu = new Mock<IMainMenu>();
            var events = new List<string>();

            menu.SetupAllProperties();
            menu.Setup(x => x.Open()).Callback(() => menu.Setup(x => x.IsOpen).Returns(true));

            target.SetOwner(root);
            target.MainMenu = menu.Object;

            root.KeyDown += (s, e) => events.Add($"KeyDown {e.Key}");
            root.KeyUp += (s, e) => events.Add($"KeyUp {e.Key}");

            KeyDown(root, Key.LeftAlt);
            KeyUp(root, Key.LeftAlt);
            KeyDown(root, Key.LeftAlt);
            KeyUp(root, Key.LeftAlt);

            Assert.Equal(new[]
            {
                "KeyDown LeftAlt",
                "KeyUp LeftAlt",
                "KeyDown LeftAlt",
                "KeyUp LeftAlt",
            }, events);
        }

        [Fact]
        public void Should_Raise_Key_Events_For_Registered_Access_Key()
        {
            var button = new Button();
            var root = new TestRoot(button);
            var target = new AccessKeyHandler();
            var events = new List<string>();

            target.SetOwner(root);
            target.Register('A', button);
            root.KeyDown += (s, e) => events.Add($"KeyDown {e.Key}");
            root.KeyUp += (s, e) => events.Add($"KeyUp {e.Key}");

            KeyDown(root, Key.LeftAlt);
            KeyDown(root, Key.A, KeyModifiers.Alt);
            KeyUp(root, Key.A, KeyModifiers.Alt);
            KeyUp(root, Key.LeftAlt);

            // This differs from WPF which doesn't raise the `A` key event, but matches UWP.
            Assert.Equal(new[]
            {
                "KeyDown LeftAlt",
                "KeyDown A",
                "KeyUp A",
                "KeyUp LeftAlt",
            }, events);
        }

        [Fact]
        public void Should_Raise_AccessKeyPressed_For_Registered_Access_Key()
        {
            var button = new Button();
            var root = new TestRoot(button);
            var target = new AccessKeyHandler();
            var raised = 0;

            target.SetOwner(root);
            target.Register('A', button);
            button.AddHandler(AccessKeyHandler.AccessKeyPressedEvent, (s, e) => ++raised);

            KeyDown(root, Key.LeftAlt);
            Assert.Equal(0, raised);

            KeyDown(root, Key.A, KeyModifiers.Alt);
            Assert.Equal(1, raised);

            KeyUp(root, Key.A, KeyModifiers.Alt);
            KeyUp(root, Key.LeftAlt);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Should_Not_Raise_AccessKeyPressed_For_Registered_Access_Key_When_Not_Effectively_Enabled()
        {
            var button = new Button();
            var root = new TestRoot(button) { IsEnabled = false };
            var target = new AccessKeyHandler();
            var raised = 0;

            target.SetOwner(root);
            target.Register('A', button);
            button.AddHandler(AccessKeyHandler.AccessKeyPressedEvent, (s, e) => ++raised);

            KeyDown(root, Key.LeftAlt);
            Assert.Equal(0, raised);

            KeyDown(root, Key.A, KeyModifiers.Alt);
            Assert.Equal(0, raised);

            KeyUp(root, Key.A, KeyModifiers.Alt);
            KeyUp(root, Key.LeftAlt);

            Assert.Equal(0, raised);
        }

        [Fact]
        public void Should_Open_MainMenu_On_Alt_KeyUp()
        {
            var root = new TestRoot();
            var target = new AccessKeyHandler();
            var menu = new Mock<IMainMenu>();

            target.SetOwner(root);
            target.MainMenu = menu.Object;

            KeyDown(root, Key.LeftAlt);
            menu.Verify(x => x.Open(), Times.Never);

            KeyUp(root, Key.LeftAlt);
            menu.Verify(x => x.Open(), Times.Once);
        }

        [Fact]
        public void Should_Cycle_Focus_When_Accelerator_Has_More_Than_One_Match()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var fileMenuItem = new MenuItem{ Header = "_File", Focusable = true, IsVisible = true };
                var fileItemAccessText = new AccessText { Text = "_File" };
                fileMenuItem.AddLogicalChild(fileItemAccessText);
                
                var findItemAccessText = new AccessText { Text = "_Find" };
                var findItem = new MenuItem{ Header = "_Find" ,Focusable = true, IsVisible = true };
                findItem.AddLogicalChild(findItemAccessText);
                
                var root = new TestRoot
                {
                    Child = new StackPanel
                    {
                        Children =
                        {
                            findItem,
                            fileMenuItem
                        }
                    }
                };
                
                var target = new AccessKeyHandler();
                
                target.SetOwner(root);
                var focusManager = Assert.IsType<FocusManager>(root.FocusManager);
                
                target.Register('F', fileItemAccessText);
                target.Register('F', findItemAccessText);
                
                // focus first item
                KeyDown(root, Key.F, KeyModifiers.Alt);
                var focusedElement = Assert.IsType<MenuItem>(focusManager.GetFocusedElement());
                Assert.Same(fileMenuItem.Header, focusedElement.Header);
                
                // focus next item
                KeyDown(root, Key.F, KeyModifiers.Alt);
                focusedElement = Assert.IsType<MenuItem>(focusManager.GetFocusedElement());
                Assert.Same(findItem.Header, focusedElement.Header);
                
                // focus first item again
                KeyDown(root, Key.F, KeyModifiers.Alt);
                focusedElement = Assert.IsType<MenuItem>(focusManager.GetFocusedElement());
                Assert.Same(fileMenuItem.Header, focusedElement.Header);
            }
        }

        private static void KeyDown(IInputElement target, Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            target.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = key,
                KeyModifiers = modifiers,
            });
        }

        private static void KeyUp(IInputElement target, Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            target.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyUpEvent,
                Key = key,
                KeyModifiers = modifiers,
            });
        }
    }
}
