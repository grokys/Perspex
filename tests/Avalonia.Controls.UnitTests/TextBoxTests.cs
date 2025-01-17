using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextBoxTests
    {
        [Fact]
        public void Opening_Context_Menu_Does_not_Lose_Selection()
        {
            using (UnitTestApplication.Start(FocusServices))
            {
                var target1 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234",
                    ContextMenu = new TestContextMenu()
                };

                var target2 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "5678"
                };
                
                var sp = new StackPanel();
                sp.Children.Add(target1);
                sp.Children.Add(target2);

                target1.ApplyTemplate();
                target2.ApplyTemplate();
                
                var root = new TestRoot() { Child = sp };

                target1.SelectionStart = 0;
                target1.SelectionEnd = 3;
                
                target1.Focus();
                Assert.False(target2.IsFocused);
                Assert.True(target1.IsFocused);

                target2.Focus();
                
                Assert.Equal("123", target1.SelectedText);
            }
        }

        [Fact]
        public void TextBox_Should_Lose_Focus_When_Disabled()
        {
            using (UnitTestApplication.Start(FocusServices))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate()
                };

                target.ApplyTemplate();

                var root = new TestRoot() { Child = target };

                target.Focus();
                Assert.True(target.IsFocused);
                target.IsEnabled = false;
                Assert.False(target.IsFocused);
                Assert.False(target.IsEnabled);
            }
        }

        [Fact]
        public void Opening_Context_Flyout_Does_not_Lose_Selection()
        {
            using (UnitTestApplication.Start(FocusServices))
            {
                var target1 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234",
                    ContextFlyout = new MenuFlyout
                    {
                        Items =
                        {
                            new MenuItem { Header = "Item 1" },
                            new MenuItem {Header = "Item 2" },
                            new MenuItem {Header = "Item 3" }
                        }
                    }
                };
                              

                target1.ApplyTemplate();

                var root = new TestRoot() { Child = target1 };

                target1.SelectionStart = 0;
                target1.SelectionEnd = 3;

                target1.Focus();
                Assert.True(target1.IsFocused);

                target1.ContextFlyout.ShowAt(target1);

                Assert.Equal("123", target1.SelectedText);
            }
        }

        [Fact]
        public void DefaultBindingMode_Should_Be_TwoWay()
        {
            Assert.Equal(
                BindingMode.TwoWay,
                TextBox.TextProperty.GetMetadata(typeof(TextBox)).DefaultBindingMode);
        }

        [Fact]
        public void TextBox_Ignore_Word_Move_In_Password_Field()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    PasswordChar = '*',
                    Text = "passw0rd"
                };

                target.ApplyTemplate();
                target.Measure(Size.Infinity);
                target.CaretIndex = 8;
                RaiseKeyEvent(target, Key.Left, KeyModifiers.Control);

                Assert.Equal(7, target.CaretIndex);
            }
        }

        [Fact]
        public void CaretIndex_Can_Moved_To_Position_After_The_End_Of_Text_With_Arrow_Key()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234"
                };

                target.ApplyTemplate();
                
                target.Measure(Size.Infinity);
                
                target.CaretIndex = 3;
                RaiseKeyEvent(target, Key.Right, 0);

                Assert.Equal(4, target.CaretIndex);
            }
        }
        
        [Fact]
        public void Control_Backspace_Should_Set_Caret_Position_To_The_Start_Of_The_Deletion()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "First Second Third",
                    SelectionStart = 13,
                    SelectionEnd = 13
                };

                target.CaretIndex = 10;
                target.ApplyTemplate();

                // (First Second |Third)
                RaiseKeyEvent(target, Key.Back, KeyModifiers.Control);
                // (First |Third)
                
                Assert.Equal(6, target.CaretIndex);
            }
        }
        
        [Fact]
        public void Control_Backspace_Should_Remove_The_Double_Whitespace_If_Caret_Index_Was_At_The_End_Of_A_Word()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "First Second Third",
                    SelectionStart = 12,
                    SelectionEnd = 12
                };

                target.ApplyTemplate();
                
                // (First Second| Third)
                RaiseKeyEvent(target, Key.Back, KeyModifiers.Control);
                // (First| Third)

                Assert.Equal("First Third", target.Text);
            }
        }

        [Fact]
        public void Control_Backspace_Undo_Should_Return_Caret_Position()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "First Second Third",
                    SelectionStart = 9,
                    SelectionEnd = 9
                };

                target.ApplyTemplate();
                
                // (First Second| Third)
                RaiseKeyEvent(target, Key.Back, KeyModifiers.Control);
                // (First| Third)
                
                target.Undo();
                // (First Second| Third)

                Assert.Equal(9, target.CaretIndex);
            }
        }

        [Fact]
        public void Press_Ctrl_A_Select_All_Text()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234"
                };
                
                target.ApplyTemplate();

                RaiseKeyEvent(target, Key.A, KeyModifiers.Control);

                Assert.Equal(0, target.SelectionStart);
                Assert.Equal(4, target.SelectionEnd);
            }
        }

        [Fact]
        public void Press_Ctrl_A_Select_All_Null_Text()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate()
                };

                RaiseKeyEvent(target, Key.A, KeyModifiers.Control);

                Assert.Equal(0, target.SelectionStart);
                Assert.Equal(0, target.SelectionEnd);
            }
        }

        [Fact]
        public void Press_Ctrl_Z_Will_Not_Modify_Text()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234"
                };

                RaiseKeyEvent(target, Key.Z, KeyModifiers.Control);

                Assert.Equal("1234", target.Text);
            }
        }

        [Fact]
        public void Control_Backspace_Should_Remove_The_Word_Before_The_Caret_If_There_Is_No_Selection()
        {
            using (UnitTestApplication.Start(Services))
            {
                TextBox textBox = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "First Second Third Fourth",
                    SelectionStart = 5,
                    SelectionEnd = 5
                };
                
                textBox.ApplyTemplate();

                // (First| Second Third Fourth)
                RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
                Assert.Equal(" Second Third Fourth", textBox.Text);

                // ( Second |Third Fourth)
                textBox.CaretIndex = 8;
                RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
                Assert.Equal(" Third Fourth", textBox.Text);

                // ( Thi|rd Fourth)
                textBox.CaretIndex = 4;
                RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
                Assert.Equal(" rd Fourth", textBox.Text);

                // ( rd F[ou]rth)
                textBox.SelectionStart = 5;
                textBox.SelectionEnd = 7;

                RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
                Assert.Equal(" rd Frth", textBox.Text);

                // ( |rd Frth)
                textBox.CaretIndex = 1;
                RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
                Assert.Equal("rd Frth", textBox.Text);
            }
        }

        [Fact]
        public void Control_Delete_Should_Remove_The_Word_After_The_Caret_If_There_Is_No_Selection()
        {
            using (UnitTestApplication.Start(Services))
            {
                TextBox textBox = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "First Second Third Fourth",
                    CaretIndex = 19,
                };
                
                textBox.ApplyTemplate();

                // (First Second Third |Fourth)
                RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
                Assert.Equal("First Second Third ", textBox.Text);

                // (First Second |Third )
                textBox.CaretIndex = 13;
                RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
                Assert.Equal("First Second ", textBox.Text);

                // (First Sec|ond )
                textBox.CaretIndex = 9;
                RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
                Assert.Equal("First Sec", textBox.Text);

                // (Fi[rs]t Sec )
                textBox.SelectionStart = 2;
                textBox.SelectionEnd = 4;

                RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
                Assert.Equal("Fit Sec", textBox.Text);

                // (Fit Sec| )
                textBox.Text += " ";
                textBox.CaretIndex = 7;
                RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
                Assert.Equal("Fit Sec", textBox.Text);
            }
        }

        [Fact]
        public void Setting_SelectionStart_To_SelectionEnd_Sets_CaretPosition_To_SelectionStart()
        {
            using (UnitTestApplication.Start(Services))
            {
                var textBox = new TextBox
                {
                    Text = "0123456789"
                };

                textBox.SelectionStart = 2;
                textBox.SelectionEnd = 2;
                
                Assert.Equal(2, textBox.CaretIndex);
            }
        }

        [Fact]
        public void Setting_Text_Updates_CaretPosition()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Text = "Initial Text",
                    CaretIndex = 11
                };

                var invoked = false;

                target.GetObservable(TextBox.TextProperty).Skip(1).Subscribe(_ =>
                {
                    // Caret index should be set before Text changed notification, as we don't want
                    // to notify with an invalid CaretIndex.
                    Assert.Equal(7, target.CaretIndex);
                    invoked = true;
                });

                target.Text = "Changed";

                Assert.True(invoked);
            }
        }

        [Fact]
        public void Press_Enter_Does_Not_Accept_Return()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    AcceptsReturn = false,
                    Text = "1234"
                };
                
                target.ApplyTemplate();

                RaiseKeyEvent(target, Key.Enter, 0);

                Assert.Equal("1234", target.Text);
            }
        }

        [Fact]
        public void Press_Enter_Add_Default_Newline()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    AcceptsReturn = true
                };
                
                target.ApplyTemplate();

                RaiseKeyEvent(target, Key.Enter, 0);

                Assert.Equal(Environment.NewLine, target.Text);
            }
        }

        [Fact]
        public void Press_Enter_Add_Custom_Newline()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    AcceptsReturn = true,
                    NewLine = "Test"
                };
                
                target.ApplyTemplate();

                RaiseKeyEvent(target, Key.Enter, 0);

                Assert.Equal("Test", target.Text);
            }
        }

        [Theory]
        [InlineData(new object[] { false, TextWrapping.NoWrap, ScrollBarVisibility.Hidden })]
        [InlineData(new object[] { false, TextWrapping.Wrap, ScrollBarVisibility.Disabled })]
        [InlineData(new object[] { true, TextWrapping.NoWrap, ScrollBarVisibility.Auto })]
        [InlineData(new object[] { true, TextWrapping.Wrap, ScrollBarVisibility.Disabled })]
        public void Has_Correct_Horizontal_ScrollBar_Visibility(
            bool acceptsReturn,
            TextWrapping wrapping,
            ScrollBarVisibility expected)
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    AcceptsReturn = acceptsReturn,
                    TextWrapping = wrapping,
                };

                Assert.Equal(expected, ScrollViewer.GetHorizontalScrollBarVisibility(target));
            }
        }

        [Fact]
        public void SelectionEnd_Doesnt_Cause_Exception()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123456789"
                };
                
                target.ApplyTemplate();

                target.SelectionStart = 0;
                target.SelectionEnd = 9;

                target.Text = "123";

                RaiseTextEvent(target, "456");

                Assert.True(true);
            }
        }

        [Fact]
        public void SelectionStart_Doesnt_Cause_Exception()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123456789"
                };
                
                target.ApplyTemplate();

                target.SelectionStart = 8;
                target.SelectionEnd = 9;

                target.Text = "123";

                RaiseTextEvent(target, "456");

                Assert.True(true);
            }
        }

        [Fact]
        public void SelectionStartEnd_Are_Valid_AterTextChange()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123456789"
                };

                target.SelectionStart = 8;
                target.SelectionEnd = 9;

                target.Text = "123";

                Assert.True(target.SelectionStart <= "123".Length);
                Assert.True(target.SelectionEnd <= "123".Length);
            }
        }

        [Fact]
        public void SelectedText_Changes_OnSelectionChange()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123456789"
                };
                
                target.ApplyTemplate();

                Assert.True(target.SelectedText == "");

                target.SelectionStart = 2;
                target.SelectionEnd = 4;

                Assert.True(target.SelectedText == "23");
            }
        }

        [Fact]
        public void SelectedText_EditsText()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123"
                };
                
                target.ApplyTemplate();

                target.SelectedText = "AA";
                Assert.True(target.Text == "AA0123");

                target.SelectionStart = 1;
                target.SelectionEnd = 3;
                target.SelectedText = "BB";

                Assert.True(target.Text == "ABB123");
            }
        }

        [Fact]
        public void SelectedText_CanClearText()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123"
                };
                target.SelectionStart = 1;
                target.SelectionEnd = 3;
                target.SelectedText = "";

                Assert.True(target.Text == "03");
            }
        }

        [Fact]
        public void SelectedText_NullClearsText()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123"
                };
                target.SelectionStart = 1;
                target.SelectionEnd = 3;
                target.SelectedText = null;

                Assert.True(target.Text == "03");
            }
        }

        [Fact]
        public void CoerceCaretIndex_Doesnt_Cause_Exception_with_malformed_line_ending()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123456789\r"
                };
                target.CaretIndex = 11;

                Assert.True(true);
            }
        }
        
        [Theory]
        [InlineData(Key.Up)]
        [InlineData(Key.Down)]
        [InlineData(Key.Home)]
        [InlineData(Key.End)]
        public void Textbox_doesnt_crash_when_Receives_input_and_template_not_applied(Key key)
        {
            using (UnitTestApplication.Start(FocusServices))
            {
                var target1 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234",
                };

                var root = new TestRoot { Child = target1 };

                target1.Focus();
                Assert.True(target1.IsFocused);

                RaiseKeyEvent(target1, key, KeyModifiers.None);
            }
        }

        [Fact]
        public void TextBox_GotFocus_And_LostFocus_Work_Properly()
        {
            using (UnitTestApplication.Start(FocusServices))
            {
                var target1 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234"
                };
                var target2 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "5678"
                };
                var sp = new StackPanel();
                sp.Children.Add(target1);
                sp.Children.Add(target2);

                target1.ApplyTemplate();
                target2.ApplyTemplate();
                
                var root = new TestRoot { Child = sp };

                var gfcount = 0;
                var lfcount = 0;

                target1.GotFocus += (s, e) => gfcount++;
                target2.LostFocus += (s, e) => lfcount++;

                target2.Focus();
                Assert.False(target1.IsFocused);
                Assert.True(target2.IsFocused);

                target1.Focus();
                Assert.False(target2.IsFocused);
                Assert.True(target1.IsFocused);

                Assert.Equal(1, gfcount);
                Assert.Equal(1, lfcount);
            }
        }
        
        [Fact]
        public void TextBox_CaretIndex_Persists_When_Focus_Lost()
        {
            using (UnitTestApplication.Start(FocusServices))
            {
                var target1 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234"
                };
                var target2 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "5678"
                };
                var sp = new StackPanel();
                sp.Children.Add(target1);
                sp.Children.Add(target2);

                target1.ApplyTemplate();
                target2.ApplyTemplate();
                
                var root = new TestRoot { Child = sp };

                target2.Focus();
                target2.CaretIndex = 2;
                Assert.False(target1.IsFocused);
                Assert.True(target2.IsFocused);

                target1.Focus();
                
                Assert.Equal(2, target2.CaretIndex);
            }
        }
        
        [Fact]
        public void TextBox_Reveal_Password_Reset_When_Lost_Focus()
        {
            using (UnitTestApplication.Start(FocusServices))
            {
                var target1 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234",
                    PasswordChar = '*'
                };
                var target2 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "5678"
                };
                var sp = new StackPanel();
                sp.Children.Add(target1);
                sp.Children.Add(target2);

                target1.ApplyTemplate();
                target2.ApplyTemplate();
                
                var root = new TestRoot { Child = sp };

                target1.Focus();
                target1.RevealPassword = true;
                
                target2.Focus();
                
                Assert.False(target1.RevealPassword);
            }
        }

        [Fact]
        public void Setting_Bound_Text_To_Null_Works()
        {
            using (UnitTestApplication.Start(Services))
            {
                var source = new Class1 { Bar = "bar" };
                var target = new TextBox { Template = CreateTemplate(), DataContext = source };

                target.ApplyTemplate();

                target.Bind(TextBox.TextProperty, new Binding("Bar"));

                Assert.Equal("bar", target.Text);
                source.Bar = null;
                Assert.Null(target.Text);
            }
        }
        
        [Theory]
        [InlineData("abc", "d", 3, 0, 0, false, "abc")]
        [InlineData("abc", "dd", 4, 3, 3, false, "abcd")]
        [InlineData("abc", "ddd", 3, 0, 2, true, "ddc")]
        [InlineData("abc", "dddd", 4, 1, 3, true, "addd")]
        [InlineData("abc", "ddddd", 5, 3, 3, true, "abcdd")]
        public void MaxLength_Works_Properly(
            string initalText,
            string textInput,
            int maxLength,
            int selectionStart,
            int selectionEnd,
            bool fromClipboard,
            string expected)
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = initalText,
                    MaxLength = maxLength,
                    SelectionStart = selectionStart,
                    SelectionEnd = selectionEnd
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate()
                };
                topLevel.Content = target;
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                target.Measure(Size.Infinity);
                
                if (fromClipboard)
                {
                    topLevel.Clipboard?.SetTextAsync(textInput).GetAwaiter().GetResult();

                    RaiseKeyEvent(target, Key.V, KeyModifiers.Control);
                    topLevel.Clipboard?.ClearAsync().GetAwaiter().GetResult();
                }
                else
                {
                    RaiseTextEvent(target, textInput);
                }
                
                Assert.Equal(expected, target.Text);
            }
        }

        [Theory]
        [InlineData(Key.X, KeyModifiers.Control)]
        [InlineData(Key.Back, KeyModifiers.None)]
        [InlineData(Key.Delete, KeyModifiers.None)]
        [InlineData(Key.Tab, KeyModifiers.None)]
        [InlineData(Key.Enter, KeyModifiers.None)]
        public void Keys_Allow_Undo(Key key, KeyModifiers modifiers)
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123",
                    AcceptsReturn = true,
                    AcceptsTab = true
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate()
                };
                topLevel.Content = target;
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                target.ApplyTemplate();
                target.SelectionStart = 1;
                target.SelectionEnd = 3;

                RaiseKeyEvent(target, key, modifiers);
                RaiseKeyEvent(target, Key.Z, KeyModifiers.Control); // undo
                Assert.True(target.Text == "0123");
            }
        }

        [Fact]
        public void Setting_SelectedText_Should_Fire_Single_Text_Changed_Notification()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123",
                    AcceptsReturn = true,
                    AcceptsTab = true,
                    SelectionStart = 1,
                    SelectionEnd = 3,
                };

                var values = new List<string>();
                target.GetObservable(TextBox.TextProperty).Subscribe(x => values.Add(x));

                target.SelectedText = "A";

                Assert.Equal(new[] { "0123", "0A3" }, values);
            }
        }

        [Fact]
        public void Entering_Text_With_SelectedText_Should_Fire_Single_Text_Changed_Notification()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "0123",
                    AcceptsReturn = true,
                    AcceptsTab = true,
                    SelectionStart = 1,
                    SelectionEnd = 3,
                };

                var values = new List<string>();
                target.GetObservable(TextBox.TextProperty).Subscribe(x => values.Add(x));

                RaiseTextEvent(target, "A");

                Assert.Equal(new[] { "0123", "0A3" }, values);
            }
        }

        [Fact]
        public void Insert_Multiline_Text_Should_Accept_Extra_Lines_When_AcceptsReturn_Is_True()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    AcceptsReturn = true
                };

                RaiseTextEvent(target, $"123 {Environment.NewLine}456");

                Assert.Equal($"123 {Environment.NewLine}456", target.Text);
            }
        }

        [Fact]
        public void Insert_Multiline_Text_Should_Discard_Extra_Lines_When_AcceptsReturn_Is_False()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    AcceptsReturn = false
                };

                RaiseTextEvent(target, $"123 {"\r"}456");

                Assert.Equal("123 ", target.Text);

                target.Text = "";

                RaiseTextEvent(target, $"123 {"\r\n"}456");

                Assert.Equal("123 ", target.Text);
            }
        }

        [Fact]
        public void Should_Fullfill_MaxLines_Contraint()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "ABC",
                    MaxLines = 1,
                    AcceptsReturn = true
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate()
                };
                topLevel.Content = target;
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                target.ApplyTemplate();
                target.Measure(Size.Infinity);

                var initialHeight = target.DesiredSize.Height;

                topLevel.Clipboard?.SetTextAsync(Environment.NewLine).GetAwaiter().GetResult();

                RaiseKeyEvent(target, Key.V, KeyModifiers.Control);
                topLevel.Clipboard?.ClearAsync().GetAwaiter().GetResult();

                RaiseTextEvent(target, Environment.NewLine);

                target.InvalidateMeasure();
                target.Measure(Size.Infinity);

                Assert.Equal(initialHeight, target.DesiredSize.Height);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void MaxLines_Sets_ScrollViewer_MaxHeight(int maxLines)
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    MaxLines = maxLines,

                    // Define explicit whole number line height for predictable calculations
                    LineHeight = 20
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate(),
                    Content = target
                };
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                var textPresenter = target.FindDescendantOfType<TextPresenter>();
                Assert.Equal("PART_TextPresenter", textPresenter.Name);
                Assert.Equal(new Thickness(0), textPresenter.Margin); // Test assumes no margin on TextPresenter

                var scrollViewer = target.FindDescendantOfType<ScrollViewer>();
                Assert.Equal("PART_ScrollViewer", scrollViewer.Name);
                Assert.Equal(maxLines * target.LineHeight, scrollViewer.MaxHeight);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void MaxLines_Sets_ScrollViewer_MaxHeight_With_TextPresenter_Margin(int maxLines)
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    MaxLines = maxLines,

                    // Define explicit whole number line height for predictable calculations
                    LineHeight = 20
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate(),
                    Content = target
                };
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                var textPresenter = target.FindDescendantOfType<TextPresenter>();
                Assert.Equal("PART_TextPresenter", textPresenter.Name);
                var textPresenterMargin = new Thickness(horizontal: 0, vertical: 3);
                textPresenter.Margin = textPresenterMargin;

                target.InvalidateMeasure();
                target.Measure(Size.Infinity);

                var scrollViewer = target.FindDescendantOfType<ScrollViewer>();
                Assert.Equal("PART_ScrollViewer", scrollViewer.Name);
                Assert.Equal((maxLines * target.LineHeight) + textPresenterMargin.Top + textPresenterMargin.Bottom, scrollViewer.MaxHeight);
            }
        }

        [Fact]
        public void Should_Fullfill_MinLines_Contraint()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "ABC \n DEF \n GHI",
                    MinLines = 3,
                    AcceptsReturn = true
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate()
                };
                topLevel.Content = target;
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                target.ApplyTemplate();
                target.Measure(Size.Infinity);

                var initialHeight = target.DesiredSize.Height;

                target.Text = "";

                target.InvalidateMeasure();
                target.Measure(Size.Infinity);

                Assert.Equal(initialHeight, target.DesiredSize.Height);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void MinLines_Sets_ScrollViewer_MinHeight(int minLines)
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    MinLines = minLines,

                    // Define explicit whole number line height for predictable calculations
                    LineHeight = 20
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate(),
                    Content = target
                };
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                var textPresenter = target.FindDescendantOfType<TextPresenter>();
                Assert.Equal("PART_TextPresenter", textPresenter.Name);
                Assert.Equal(new Thickness(0), textPresenter.Margin); // Test assumes no margin on TextPresenter

                var scrollViewer = target.FindDescendantOfType<ScrollViewer>();
                Assert.Equal("PART_ScrollViewer", scrollViewer.Name);
                Assert.Equal(minLines * target.LineHeight, scrollViewer.MinHeight);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void MinLines_Sets_ScrollViewer_MinHeight_With_TextPresenter_Margin(int minLines)
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    Template = CreateTemplate(),
                    MinLines = minLines,

                    // Define explicit whole number line height for predictable calculations
                    LineHeight = 20
                };

                var impl = CreateMockTopLevelImpl();
                var topLevel = new TestTopLevel(impl.Object)
                {
                    Template = CreateTopLevelTemplate(),
                    Content = target
                };
                topLevel.ApplyTemplate();
                topLevel.LayoutManager.ExecuteInitialLayoutPass();

                var textPresenter = target.FindDescendantOfType<TextPresenter>();
                Assert.Equal("PART_TextPresenter", textPresenter.Name);
                var textPresenterMargin = new Thickness(horizontal: 0, vertical: 3);
                textPresenter.Margin = textPresenterMargin;

                target.InvalidateMeasure();
                target.Measure(Size.Infinity);

                var scrollViewer = target.FindDescendantOfType<ScrollViewer>();
                Assert.Equal("PART_ScrollViewer", scrollViewer.Name);
                Assert.Equal((minLines * target.LineHeight) + textPresenterMargin.Top + textPresenterMargin.Bottom, scrollViewer.MinHeight);
            }
        }

        [Fact]
        public void CanUndo_CanRedo_Is_False_When_Initialized()
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "New Text"
                };

                tb.Measure(Size.Infinity);

                Assert.False(tb.CanUndo);
                Assert.False(tb.CanRedo);
            }
        }

        [Fact]
        public void CanUndo_CanRedo_and_Programmatic_Undo_Redo_Works()
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                };

                tb.Measure(Size.Infinity);

                // See GH #6024 for a bit more insight on when Undo/Redo snapshots are taken:
                // - Every 'Space', but only when space is handled in OnKeyDown - Spaces in TextInput event won't work
                // - Every 7 chars in a long word
                RaiseTextEvent(tb, "ABC");
                RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
                RaiseTextEvent(tb, "DEF");
                RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
                RaiseTextEvent(tb, "123");

                // NOTE: the spaces won't actually add spaces b/c they're sent only as key events and not Text events
                //       so our final text is without spaces
                Assert.Equal("ABCDEF123", tb.Text);

                Assert.True(tb.CanUndo);

                tb.Undo();

                // Undo will take us back one step
                Assert.Equal("ABCDEF", tb.Text);

                Assert.True(tb.CanRedo);

                tb.Redo();

                // Redo should restore us
                Assert.Equal("ABCDEF123", tb.Text);
            }
        }

        [Fact]
        public void Setting_UndoLimit_Clears_Undo_Redo()
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                };

                tb.Measure(Size.Infinity);

                // This is all the same as the above test (CanUndo_CanRedo_and_Programmatic_Undo_Redo_Works)
                // We do this to get the undo/redo stacks in a state where both are active
                RaiseTextEvent(tb, "ABC");
                RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
                RaiseTextEvent(tb, "DEF");
                RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
                RaiseTextEvent(tb, "123");

                Assert.Equal("ABCDEF123", tb.Text);
                Assert.True(tb.CanUndo);
                tb.Undo();
                // Undo will take us back one step
                Assert.Equal("ABCDEF", tb.Text);
                Assert.True(tb.CanRedo);
                tb.Redo();
                // Redo should restore us
                Assert.Equal("ABCDEF123", tb.Text);

                // Change the undo limit, this should clear both stacks setting CanUndo and CanRedo to false
                tb.UndoLimit = 1;

                Assert.False(tb.CanUndo);
                Assert.False(tb.CanRedo);
            }
        }

        [Fact]
        public void Setting_IsUndoEnabled_To_False_Clears_Undo_Redo()
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                };

                tb.Measure(Size.Infinity);

                // This is all the same as the above test (CanUndo_CanRedo_and_Programmatic_Undo_Redo_Works)
                // We do this to get the undo/redo stacks in a state where both are active
                RaiseTextEvent(tb, "ABC");
                RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
                RaiseTextEvent(tb, "DEF");
                RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
                RaiseTextEvent(tb, "123");

                Assert.Equal("ABCDEF123", tb.Text);
                Assert.True(tb.CanUndo);
                tb.Undo();
                // Undo will take us back one step
                Assert.Equal("ABCDEF", tb.Text);
                Assert.True(tb.CanRedo);
                tb.Redo();
                // Redo should restore us
                Assert.Equal("ABCDEF123", tb.Text);

                // Disable Undo/Redo, this should clear both stacks setting CanUndo and CanRedo to false
                tb.IsUndoEnabled = false;

                Assert.False(tb.CanUndo);
                Assert.False(tb.CanRedo);
            }
        }

        [Fact]
        public void UndoLimit_Count_Is_Respected()
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    UndoLimit = 3 // Something small for this test
                };

                tb.Measure(Size.Infinity);

                // Push 3 undoable actions, we should only be able to recover 2
                RaiseTextEvent(tb, "ABC");
                RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
                RaiseTextEvent(tb, "DEF");
                RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
                RaiseTextEvent(tb, "123");

                Assert.Equal("ABCDEF123", tb.Text);

                // Undo will take us back one step
                tb.Undo();                
                Assert.Equal("ABCDEF", tb.Text);

                // Undo again
                tb.Undo();
                Assert.Equal("ABC", tb.Text);

                // We now should not be able to undo again
                Assert.False(tb.CanUndo);
            }
        }

        [Fact]
        public void Should_Move_Caret_To_EndOfLine()
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "AB\nAB"
                };

                tb.Measure(Size.Infinity);

                RaiseKeyEvent(tb, Key.End, KeyModifiers.Shift);

                Assert.Equal(2, tb.CaretIndex);
            }
        }

        [Fact]
        public void TextBox_In_AdornerLayer_Will_Not_Cause_Collection_Modified_In_VisualLayerManager_Measure()
        {
            using (UnitTestApplication.Start(Services))
            {
                var button = new Button();
                var root = new TestRoot()
                {
                    Child = new VisualLayerManager()
                    {
                        Child = button
                    }
                };
                var adorner = new TextBox { Template = CreateTemplate(), Text = "a" };

                var adornerLayer = AdornerLayer.GetAdornerLayer(button);
                adornerLayer.Children.Add(adorner);
                AdornerLayer.SetAdornedElement(adorner, button);

                root.Measure(Size.Infinity);
            }
        }

        [Fact]
        public void TextBox_In_AdornerLayer_Will_Not_Cause_Collection_Modified_In_VisualLayerManager_Arrange()
        {
            using (UnitTestApplication.Start(Services))
            {
                var button = new Button();
                var visualLayerManager = new VisualLayerManager() { Child = button };
                var root = new TestRoot()
                {
                    Child = visualLayerManager
                };
                var adorner = new TextBox { Template = CreateTemplate(), Text = "a" };
                var adornerLayer = AdornerLayer.GetAdornerLayer(button);

                root.Measure(new Size(10, 10));

                adornerLayer.Children.Add(adorner);
                AdornerLayer.SetAdornedElement(adorner, button);

                root.Arrange(new Rect(0, 0, 10, 10));
            }
        }

        [Theory]
        [InlineData("A\nBB\nCCC\nDDDD", 0, 0)]
        [InlineData("A\nBB\nCCC\nDDDD", 1, 2)]
        [InlineData("A\nBB\nCCC\nDDDD", 2, 5)]
        [InlineData("A\nBB\nCCC\nDDDD", 3, 9)]
        [InlineData("واحد\nاثنين\nثلاثة\nأربعة", 0, 0)]
        [InlineData("واحد\nاثنين\nثلاثة\nأربعة", 1, 5)]
        [InlineData("واحد\nاثنين\nثلاثة\nأربعة", 2, 11)]
        [InlineData("واحد\nاثنين\nثلاثة\nأربعة", 3, 17)]
        public void Should_Scroll_Caret_To_Line(string text, int targetLineIndex, int expectedCaretIndex)
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = text
                };
                tb.ApplyTemplate();
                tb.ScrollToLine(targetLineIndex);
                Assert.Equal(expectedCaretIndex, tb.CaretIndex);
            }
        }

        [Fact]
        public void Should_Throw_ArgumentOutOfRange()
        {
            using (UnitTestApplication.Start(Services))
            {
                var tb = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = string.Empty
                };
                tb.ApplyTemplate();

                Assert.Throws<ArgumentOutOfRangeException>(() => tb.ScrollToLine(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => tb.ScrollToLine(1));
            }
        }

        [Fact]
        public void InputMethodClient_SurroundingText_Returns_Empty_For_Empty_Line()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox
            {
                Template = CreateTemplate(),
                Text = "",
                CaretIndex = 0
            };
            textBox.ApplyTemplate();

            var eventArgs = new TextInputMethodClientRequestedEventArgs
            {
                RoutedEvent = InputElement.TextInputMethodClientRequestedEvent
            };
            textBox.RaiseEvent(eventArgs);

            var client = eventArgs.Client;
            Assert.NotNull(client);
            Assert.Equal(string.Empty, client.SurroundingText);
        }
  
        [Fact]
        public void Backspace_Should_Delete_Last_Character_In_Line_And_Keep_Caret_On_Same_Line()
        {
            using var _ = UnitTestApplication.Start(Services);

            var textBox = new TextBox
            {
                Template = CreateTemplate(),
                Text = "a\nb",
                CaretIndex = 3
            };
            textBox.ApplyTemplate();

            var topLevel = new TestTopLevel(CreateMockTopLevelImpl().Object)
            {
                Template = CreateTopLevelTemplate(),
                Content = textBox
            };
            topLevel.ApplyTemplate();
            topLevel.LayoutManager.ExecuteInitialLayoutPass();

            var textPresenter = textBox.FindDescendantOfType<TextPresenter>();
            Assert.NotNull(textPresenter);

            var oldCaretY = textPresenter.GetCursorRectangle().Top;
            Assert.NotEqual(0, oldCaretY);

            RaiseKeyEvent(textBox, Key.Back, KeyModifiers.None);

            Assert.Equal("a\n", textBox.Text);
            Assert.Equal(2, textBox.CaretIndex);
            Assert.Equal(2, textPresenter.CaretIndex);

            var caretY = textPresenter.GetCursorRectangle().Top;
            Assert.Equal(oldCaretY, caretY);
        }

        [Fact]
        public void Losing_Focus_Should_Not_Reset_Selection()
        {
            using (UnitTestApplication.Start(FocusServices))
            {
                var target1 = new TextBox
                {
                    Template = CreateTemplate(),
                    Text = "1234",
                    ClearSelectionOnLostFocus = false
                };

                target1.ApplyTemplate();

                var target2 = new TextBox
                {
                    Template = CreateTemplate(),
                };

                target2.ApplyTemplate();

                var sp = new StackPanel();
                sp.Children.Add(target1);
                sp.Children.Add(target2);

                var root = new TestRoot() { Child = sp };

                target1.SelectionStart = 0;
                target1.SelectionEnd = 4;

                target1.Focus();

                Assert.True(target1.IsFocused);

                Assert.Equal("1234", target1.SelectedText);            

                target2.Focus();

                Assert.Equal("1234", target1.SelectedText);
            }
        }

        private static TestServices FocusServices => TestServices.MockThreadingInterface.With(
            focusManager: new FocusManager(),
            keyboardDevice: () => new KeyboardDevice(),
            keyboardNavigation: () => new KeyboardNavigationHandler(),
            inputManager: new InputManager(),
            standardCursorFactory: Mock.Of<ICursorFactory>(),
            textShaperImpl: new HeadlessTextShaperStub(),
            fontManagerImpl: new HeadlessFontManagerStub());

        private static TestServices Services => TestServices.MockThreadingInterface.With(
            standardCursorFactory: Mock.Of<ICursorFactory>(),
            renderInterface: new HeadlessPlatformRenderInterface(),
            textShaperImpl: new HeadlessTextShaperStub(), 
            fontManagerImpl: new HeadlessFontManagerStub());

        internal static IControlTemplate CreateTemplate()
        {
            return new FuncControlTemplate<TextBox>((control, scope) =>
            new ScrollViewer
            {
                Name = "PART_ScrollViewer",
                Template = new FuncControlTemplate<ScrollViewer>(ScrollViewerTests.CreateTemplate),
                Content = new TextPresenter
                {
                    Name = "PART_TextPresenter",
                    [!!TextPresenter.TextProperty] = new Binding
                    {
                        Path = nameof(TextPresenter.Text),
                        Mode = BindingMode.TwoWay,
                        Priority = BindingPriority.Template,
                        RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                    },
                    [!!TextPresenter.CaretIndexProperty] = new Binding
                    {
                        Path = nameof(TextPresenter.CaretIndex),
                        Mode = BindingMode.TwoWay,
                        Priority = BindingPriority.Template,
                        RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                    }
                }.RegisterInNameScope(scope)
            }.RegisterInNameScope(scope));
        }

        private static void RaiseKeyEvent(TextBox textBox, Key key, KeyModifiers inputModifiers)
        {
            textBox.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                KeyModifiers = inputModifiers,
                Key = key
            });
        }

        private static void RaiseTextEvent(TextBox textBox, string text)
        {
            textBox.RaiseEvent(new TextInputEventArgs
            {
                RoutedEvent = InputElement.TextInputEvent,
                Text = text
            });
        }

        private class Class1 : NotifyingBase
        {
            private int _foo;
            private string _bar;

            public int Foo
            {
                get { return _foo; }
                set { _foo = value; RaisePropertyChanged(); }
            }

            public string Bar
            {
                get { return _bar; }
                set { _bar = value; RaisePropertyChanged(); }
            }
        }

        private class ClipboardStub : IClipboard // in order to get tests working that use the clipboard
        {
            private string _text;

            public Task<string> GetTextAsync() => Task.FromResult(_text);

            public Task SetTextAsync(string text)
            {
                _text = text;
                return Task.CompletedTask;
            }

            public Task ClearAsync()
            {
                _text = null;
                return Task.CompletedTask;
            }
            
            public Task SetDataObjectAsync(IDataObject data) => Task.CompletedTask;

            public Task<string[]> GetFormatsAsync() => Task.FromResult(Array.Empty<string>());

            public Task<object> GetDataAsync(string format) => Task.FromResult((object)null);

            public Task FlushAsync() =>
                Task.CompletedTask;
        }

        private class TestTopLevel : TopLevel
        {
            private readonly ILayoutManager _layoutManager;

            public TestTopLevel(ITopLevelImpl impl, ILayoutManager layoutManager = null)
                : base(impl)
            {
                _layoutManager = layoutManager ?? new LayoutManager(this);
            }

            private protected override ILayoutManager CreateLayoutManager() => _layoutManager;
        }

        private static Mock<ITopLevelImpl> CreateMockTopLevelImpl()
        {
            var clipboard = new Mock<ITopLevelImpl>();
            clipboard.Setup(x => x.Compositor).Returns(RendererMocks.CreateDummyCompositor());
            clipboard.Setup(r => r.TryGetFeature(typeof(IClipboard)))
                .Returns(new ClipboardStub());
            clipboard.SetupGet(x => x.RenderScaling).Returns(1);
            return clipboard;
        }

        private static FuncControlTemplate<TestTopLevel> CreateTopLevelTemplate()
        {
            return new FuncControlTemplate<TestTopLevel>((x, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty],
                }.RegisterInNameScope(scope));
        }

        private class TestContextMenu : ContextMenu
        {
            public TestContextMenu()
            {
                IsOpen = true;
            }
        }
    }
}
