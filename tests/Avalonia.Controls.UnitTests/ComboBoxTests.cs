using System.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ComboBoxTests
    {
        MouseTestHelper _helper = new MouseTestHelper();
        
        [Fact]
        public void Clicking_On_Control_Toggles_IsDropDownOpen()
        {
            var target = new ComboBox
            {
                ItemsSource = new[] { "Foo", "Bar" },
            };

            _helper.Down(target);
            _helper.Up(target);
            Assert.True(target.IsDropDownOpen);
            Assert.True(target.Classes.Contains(ComboBox.pcDropdownOpen));

            _helper.Down(target);
            _helper.Up(target);

            Assert.False(target.IsDropDownOpen);
            Assert.True(!target.Classes.Contains(ComboBox.pcDropdownOpen));
        }

        [Fact]
        public void Clicking_On_Control_PseudoClass()
        {
            var target = new ComboBox
            {
                ItemsSource = new[] { "Foo", "Bar" },
            };

            _helper.Down(target);
            Assert.True(target.Classes.Contains(ComboBox.pcPressed));
            _helper.Up(target);
            Assert.True(!target.Classes.Contains(ComboBox.pcPressed));
            Assert.True(target.Classes.Contains(ComboBox.pcDropdownOpen));

            _helper.Down(target);
            Assert.True(!target.Classes.Contains(ComboBox.pcPressed));
            _helper.Up(target);
            Assert.True(!target.Classes.Contains(ComboBox.pcPressed));

            Assert.False(target.IsDropDownOpen);
            Assert.True(!target.Classes.Contains(ComboBox.pcDropdownOpen));
        }

        [Fact]
        public void WrapSelection_Should_Work()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new ComboBox
                {
                    Items =
                    {
                        new ComboBoxItem() { Content = "bla" },
                        new ComboBoxItem() { Content = "dd" },
                        new ComboBoxItem() { Content = "sdf", IsEnabled = false }
                    },
                    Template = GetTemplate(),
                    WrapSelection = true
                };
                var root = new TestRoot(target);
                target.ApplyTemplate();
                ((Control)target.Presenter).ApplyTemplate();
                target.Focus();
                Assert.Equal(target.SelectedIndex, -1);
                Assert.True(target.IsFocused);
                target.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Up,
                });
                Assert.Equal(target.SelectedIndex, 1);
                target.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Down,
                });
                Assert.Equal(target.SelectedIndex, 0);
            }
        }

        [Fact]
        public void Focuses_Next_Item_On_Key_Down()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new ComboBox
                {
                    Items =
                    {
                        new ComboBoxItem() { Content = "bla" },
                        new ComboBoxItem() { Content = "dd", IsEnabled = false },
                        new ComboBoxItem() { Content = "sdf" }
                    },
                    Template = GetTemplate()
                };
                var root = new TestRoot(target);
                target.ApplyTemplate();
                ((Control)target.Presenter).ApplyTemplate();
                target.Focus();
                Assert.Equal(target.SelectedIndex, -1);
                Assert.True(target.IsFocused);
                target.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Down,
                });
                Assert.Equal(target.SelectedIndex, 0);
                target.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Down,
                });
                Assert.Equal(target.SelectedIndex, 2);
            }
        }

        [Fact]
        public void SelectionBoxItem_Is_Rectangle_With_VisualBrush_When_Selection_Is_Control()
        {
            var target = new ComboBox
            {
                Items = { new Canvas() },
                SelectedIndex = 0,
            };
            var root = new TestRoot(target);

            var rectangle = target.GetValue(ComboBox.SelectionBoxItemProperty) as Rectangle;
            Assert.NotNull(rectangle);

            var brush = rectangle.Fill as VisualBrush;
            Assert.NotNull(brush);
            Assert.Same(target.Items[0], brush.Visual);
        }

        [Fact]
        public void SelectionBoxItem_Rectangle_Is_Removed_From_Logical_Tree()
        {
            var target = new ComboBox
            {
                Items = { new Canvas() },
                SelectedIndex = 0,
                Template = GetTemplate(),
            };

            var root = new TestRoot { Child = target };
            target.ApplyTemplate();
            ((Control)target.Presenter).ApplyTemplate();

            var rectangle = target.GetValue(ComboBox.SelectionBoxItemProperty) as Rectangle;
            Assert.True(((ILogical)target).IsAttachedToLogicalTree);
            Assert.True(((ILogical)rectangle).IsAttachedToLogicalTree);

            rectangle.DetachedFromLogicalTree += (s, e) => { };

            root.Child = null;

            Assert.False(((ILogical)target).IsAttachedToLogicalTree);
            Assert.False(((ILogical)rectangle).IsAttachedToLogicalTree);
        }

        private static FuncControlTemplate GetTemplate()
        {
            return new FuncControlTemplate<ComboBox>((parent, scope) =>
            {
                return new Panel
                {
                    Name = "container",
                    Children =
                    {
                        new ContentControl
                        {
                            [!ContentControl.ContentProperty] = parent[!ComboBox.SelectionBoxItemProperty],
                        },
                        new ToggleButton
                        {
                            Name = "toggle",
                        }.RegisterInNameScope(scope),
                        new Popup
                        {
                            Name = "PART_Popup",
                            Child = new ScrollViewer
                            {
                                Name = "PART_ScrollViewer",
                                Content = new ItemsPresenter
                                {
                                    Name = "PART_ItemsPresenter",
                                    ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel()),
                                }.RegisterInNameScope(scope)
                            }.RegisterInNameScope(scope)
                        }.RegisterInNameScope(scope)
                    }
                };
            });
        }

        [Fact]
        public void Detaching_Closed_ComboBox_Keeps_Current_Focus()
        {
            using (UnitTestApplication.Start(TestServices.RealFocus))
            {
                var target = new ComboBox
                {
                    Items = { new Canvas() },
                    SelectedIndex = 0,
                    Template = GetTemplate(),
                };

                var other = new Control { Focusable = true };

                StackPanel panel;

                var root = new TestRoot { Child = panel = new StackPanel { Children = { target, other } } };

                target.ApplyTemplate();
                target.Presenter.ApplyTemplate();

                other.Focus();

                Assert.True(other.IsFocused);

                panel.Children.Remove(target);

                Assert.True(other.IsFocused);
            }
        }

        [Theory]
        [InlineData(-1, 2, "c", "A item", "B item", "C item")]
        [InlineData(0, 1, "b", "A item", "B item", "C item")]
        [InlineData(2, 2, "x", "A item", "B item", "C item")]
        [InlineData(0, 34, "y", "0 item", "1 item", "2 item", "3 item", "4 item", "5 item", "6 item", "7 item", "8 item", "9 item", "A item", "B item", "C item", "D item", "E item", "F item", "G item", "H item", "I item", "J item", "K item", "L item", "M item", "N item", "O item", "P item", "Q item", "R item", "S item", "T item", "U item", "V item", "W item", "X item", "Y item", "Z item")]
        public void TextSearch_Should_Have_Expected_SelectedIndex(
            int initialSelectedIndex,
            int expectedSelectedIndex,
            string searchTerm,
            params string[] items)
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new ComboBox
                {
                    Template = GetTemplate(),                    
                    ItemsSource = items.Select(x => new ComboBoxItem { Content = x }),
                };

                TestRoot root = new(target)
                {
                    ClientSize = new(500,500),
                };
                
                target.ApplyTemplate();
                target.Presenter.ApplyTemplate();
                target.SelectedIndex = initialSelectedIndex;
                root.LayoutManager.ExecuteInitialLayoutPass();

                var args = new TextInputEventArgs
                {
                    Text = searchTerm,
                    RoutedEvent = InputElement.TextInputEvent
                };

                target.RaiseEvent(args);

                Assert.Equal(expectedSelectedIndex, target.SelectedIndex);
            }
        }
        
        [Fact]
        public void SelectedItem_Validation()
        {

            using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
            {
                var target = new ComboBox
                {
                    Template = GetTemplate(),
                };

                target.ApplyTemplate();
                target.Presenter.ApplyTemplate();
                
                var exception = new System.InvalidCastException("failed validation");
                var textObservable = new BehaviorSubject<BindingNotification>(new BindingNotification(exception, BindingErrorType.DataValidationError));
                target.Bind(ComboBox.SelectedItemProperty, textObservable);

                Assert.True(DataValidationErrors.GetHasErrors(target));
                Assert.True(DataValidationErrors.GetErrors(target).SequenceEqual(new[] { exception }));
            }
            
        }

        [Fact]
        public void Close_Window_On_Alt_F4_When_ComboBox_Is_Focus()
        {
            var inputManagerMock = new Moq.Mock<IInputManager>();
            var services = TestServices.StyledWindow.With(inputManager: inputManagerMock.Object);

            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();

                window.KeyDown += (s, e) =>
                 {
                     if (e.Handled == false 
                     && e.KeyModifiers.HasAllFlags(KeyModifiers.Alt) == true 
                     && e.Key == Key.F4 )
                     {
                         e.Handled = true;
                         window.Close();
                     }
                 };

                var count = 0;

                var target = new ComboBox
                {
                    Items = { new Canvas() },
                    SelectedIndex = 0,
                    Template = GetTemplate(),
                };

                window.Content = target;


                window.Closing +=
                    (sender, e) =>
                    {
                        count++;
                    };

                window.Show();

                target.Focus();

                _helper.Down(target);
                _helper.Up(target);
                Assert.True(target.IsDropDownOpen);

                target.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    KeyModifiers = KeyModifiers.Alt,
                    Key = Key.F4
                });


                Assert.Equal(1, count);
            }
        }

        [Fact]
        public void FlowDirection_Of_RectangleContent_Should_Be_LeftToRight()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var target = new ComboBox
            {
                FlowDirection = FlowDirection.RightToLeft,
                Items =
                {
                    new ComboBoxItem()
                    {
                        Content = new Control()
                    }
                },
                Template = GetTemplate()
            };

            var root = new TestRoot(target);
            target.ApplyTemplate();
            target.SelectedIndex = 0;

            var rectangle = target.GetValue(ComboBox.SelectionBoxItemProperty) as Rectangle;

            Assert.Equal(FlowDirection.LeftToRight, rectangle.FlowDirection);
        }

        [Fact]
        public void FlowDirection_Of_RectangleContent_Updated_After_InvalidateMirrorTransform()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var parentContent = new Decorator()
            {
                Child = new Control()
            };
            var target = new ComboBox
            {
                Items = 
                {
                    new ComboBoxItem()
                    {
                        Content = parentContent.Child
                    }
                },
                Template = GetTemplate()
            };

            var root = new TestRoot(target);
            target.ApplyTemplate();
            target.SelectedIndex = 0;

            var rectangle = target.GetValue(ComboBox.SelectionBoxItemProperty) as Rectangle;
            Assert.Equal(FlowDirection.LeftToRight, rectangle.FlowDirection);

            parentContent.FlowDirection = FlowDirection.RightToLeft;
            target.FlowDirection = FlowDirection.RightToLeft;
            
            Assert.Equal(FlowDirection.RightToLeft, rectangle.FlowDirection);
        }

        [Fact]
        public void FlowDirection_Of_RectangleContent_Updated_After_OpenPopup()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parentContent = new Decorator()
                {
                    Child = new Control()
                };
                var target = new ComboBox
                {
                    FlowDirection = FlowDirection.RightToLeft,
                    Items =
                    {
                        new ComboBoxItem()
                        {
                            Content = parentContent.Child,
                            Template = null // ugly hack, so we can "attach" same child to the two different trees
                        }
                    },
                    Template = GetTemplate()
                };

                var root = new TestRoot(target);
                target.ApplyTemplate();
                target.SelectedIndex = 0;

                var rectangle = target.GetValue(ComboBox.SelectionBoxItemProperty) as Rectangle;
                Assert.Equal(FlowDirection.LeftToRight, rectangle.FlowDirection);

                parentContent.FlowDirection = FlowDirection.RightToLeft;

                var popup = target.GetVisualDescendants().OfType<Popup>().First();
                popup.PlacementTarget = new Window();
                popup.Open();
                
                Assert.Equal(FlowDirection.RightToLeft, rectangle.FlowDirection);
            }
        }

        [Fact]
        public void SelectionBoxItemTemplate_Overrides_ItemTemplate()
        {
            IDataTemplate itemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "!" });
            IDataTemplate selectionBoxItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x });
            var target = new ComboBox
            {
                ItemsSource = new []{ "Foo" },
                SelectionBoxItemTemplate = selectionBoxItemTemplate,
                ItemTemplate = itemTemplate,
            };
            
            Assert.Equal(selectionBoxItemTemplate, target.SelectionBoxItemTemplate);
        }
        
        [Fact]
        public void SelectionBoxItemTemplate_Inherits_From_ItemTemplate_When_NotSet()
        {
            IDataTemplate itemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "!" });
            var target = new ComboBox
            {
                ItemsSource = new []{ "Foo" },
                ItemTemplate = itemTemplate,
            };
            
            Assert.Equal(itemTemplate, target.SelectionBoxItemTemplate);
        }

        [Fact]
        public void SelectionBoxItemTemplate_Overrides_ItemTemplate_After_ItemTemplate_Changed()
        {
            IDataTemplate itemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "!" });
            IDataTemplate selectionBoxItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x });
            IDataTemplate itemTemplate2 = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "?" });
            var target = new ComboBox
            {
                ItemsSource = new[] { "Foo" },
                SelectionBoxItemTemplate = selectionBoxItemTemplate,
                ItemTemplate = itemTemplate,
            };

            Assert.Equal(selectionBoxItemTemplate, target.SelectionBoxItemTemplate);
            
            target.ItemTemplate = itemTemplate2;
            
            Assert.Equal(selectionBoxItemTemplate, target.SelectionBoxItemTemplate);
        }

        [Fact]
        public void SelectionBoxItemTemplate_Inherits_From_ItemTemplate_When_ItemTemplate_Changed()
        {
            IDataTemplate itemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "!" });
            IDataTemplate selectionBoxItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x });
            IDataTemplate itemTemplate2 = new FuncDataTemplate<string>((x, _) => new TextBlock { Text = x + "?" });
            var target = new ComboBox { ItemsSource = new[] { "Foo" }, ItemTemplate = itemTemplate, };

            Assert.Equal(itemTemplate, target.SelectionBoxItemTemplate);

            target.ItemTemplate = itemTemplate2;
            target.SelectionBoxItemTemplate = null;

            Assert.Equal(itemTemplate2, target.SelectionBoxItemTemplate);
        }

        [Fact]
        public void DisplayMemberBinding_Is_Not_Applied_To_SelectionBoxItem_Without_Selection()
        {
            var target = new ComboBox
            {
                DisplayMemberBinding = new Binding(),
                ItemsSource = new[] { "foo", "bar" }
            };

            target.SelectedItem = null;
            Assert.Null(target.SelectionBoxItem);

            target.SelectedItem = "foo";
            Assert.NotNull(target.SelectionBoxItem);

            target.SelectedItem = null;
            Assert.Null(target.SelectionBoxItem);
        }
    }
}
