using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    [InvariantCulture]
    public class StyleTests : XamlTestBase
    {
        [Fact]
        public void Color_Can_Be_Added_To_Style_Resources()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
            <Style.Resources>
                <Color x:Key='color'>#ff506070</Color>
            </Style.Resources>
        </Style>
    </UserControl.Styles>
</UserControl>";
                var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
                var color = (Color)((Style)userControl.Styles[0]).Resources["color"];

                Assert.Equal(0xff506070, color.ToUInt32());
            }
        }

        [Fact]
        public void DataTemplate_Can_Be_Added_To_Style_Resources()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
            <Style.Resources>
                <DataTemplate x:Key='dataTemplate'><TextBlock/></DataTemplate>
            </Style.Resources>
        </Style>
    </UserControl.Styles>
</UserControl>";
                var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
                var dataTemplate = (DataTemplate)((Style)userControl.Styles[0]).Resources["dataTemplate"];

                Assert.NotNull(dataTemplate);
            }
        }

        [Fact]
        public void ControlTemplate_Can_Be_Added_To_Style_Resources()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
            <Style.Resources>
                 <ControlTemplate x:Key='controlTemplate' TargetType='{x:Type Button}'>
                    <ContentPresenter Content='{TemplateBinding Content}'/>
                 </ControlTemplate>
            </Style.Resources>
        </Style>
    </UserControl.Styles>
</UserControl>";
                var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
                var controlTemplate = (ControlTemplate)((Style)userControl.Styles[0]).Resources["controlTemplate"];

                Assert.NotNull(controlTemplate);
                Assert.Equal(typeof(Button), controlTemplate.TargetType);
            }
        }

        [Fact]
        public void SolidColorBrush_Can_Be_Added_To_Style_Resources()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
            <Style.Resources>
                <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
            </Style.Resources>
        </Style>
    </UserControl.Styles>
</UserControl>";
                var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
                var brush = (ISolidColorBrush)((Style)userControl.Styles[0]).Resources["brush"];

                Assert.Equal(0xff506070, brush.Color.ToUInt32());
            }
        }

        [Fact]
        public void Setter_Can_Contain_Template()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='ContentControl'>
            <Setter Property='Content'>
                <Template>
                    <TextBlock>Hello World!</TextBlock>
                </Template>
            </Setter>
        </Style>
    </Window.Styles>

    <ContentControl Name='target'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.Find<ContentControl>("target");

                Assert.IsType<TextBlock>(target.Content);
                Assert.Equal("Hello World!", ((TextBlock)target.Content).Text);
            }
        }

        [Fact]
        public void Setter_Value_Is_Bound_Directly_If_The_Target_Type_Derives_From_ITemplate()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector=':is(Control)'>
		  <Setter Property='FocusAdorner'>
			<FocusAdornerTemplate>
			  <Rectangle Stroke='Black'
						 StrokeThickness='1'
						 StrokeDashArray='1,2'/>
			</FocusAdornerTemplate>
		  </Setter>
		</Style>
	</Window.Styles>

    <TextBlock Name='target'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = window.Find<TextBlock>("target");

                Assert.NotNull(target.FocusAdorner);
            }
        }

        [Fact]
        public void Setter_Can_Set_Attached_Property()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Window.Styles>
        <Style Selector='TextBlock'>
            <Setter Property='DockPanel.Dock' Value='Right'/>
        </Style>
    </Window.Styles>
    <TextBlock/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var textBlock = (TextBlock)window.Content;

                window.ApplyTemplate();

                Assert.Equal(Dock.Right, DockPanel.GetDock(textBlock));
            }
        }

        [Fact(Skip = "The animation system currently needs to be able to set any property on any object")]
        public void Disallows_Setting_Non_Registered_Property()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Window.Styles>
        <Style Selector='TextBlock'>
            <Setter Property='Button.IsDefault' Value='True'/>
        </Style>
    </Window.Styles>
    <TextBlock/>
</Window>";
                var ex = Assert.Throws<XmlException>(() => AvaloniaRuntimeXamlLoader.Load(xaml));

                Assert.Equal(
                    "Property 'Button.IsDefault' is not registered on 'Avalonia.Controls.TextBlock'.",
                    ex.InnerException.Message);
            }
        }

        [Fact]
        public void Style_Can_Use_Not_Selector()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border:not(.foo)'>
            <Setter Property='Background' Value='Red'/>
        </Style>
    </Window.Styles>
    <StackPanel>
        <Border Name='foo' Classes='foo bar'/>
        <Border Name='notFoo' Classes='bar'/>
    </StackPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var foo = window.FindControl<Border>("foo");
                var notFoo = window.FindControl<Border>("notFoo");

                Assert.Null(foo.Background);
                Assert.Equal(Colors.Red, ((ISolidColorBrush)notFoo.Background).Color);
            }
        }

        [Fact]
        public void Style_Can_Use_NthChild_Selector()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border.foo:nth-child(2n+1)'>
            <Setter Property='Background' Value='Red'/>
        </Style>
    </Window.Styles>
    <StackPanel>
        <Border x:Name='b1' Classes='foo'/>
        <Border x:Name='b2' />
    </StackPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var b1 = window.FindControl<Border>("b1");
                var b2 = window.FindControl<Border>("b2");

                Assert.Equal(Brushes.Red, b1.Background);
                Assert.Null(b2.Background);
            }
        }

        [Fact]
        public void Style_Can_Use_NthChild_Selector_After_Reorder()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border:nth-child(2n)'>
            <Setter Property='Background' Value='Red'/>
        </Style>
    </Window.Styles>
    <StackPanel x:Name='parent'>
        <Border x:Name='b1' />
        <Border x:Name='b2' />
    </StackPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);

                var parent = window.FindControl<StackPanel>("parent");
                var b1 = window.FindControl<Border>("b1");
                var b2 = window.FindControl<Border>("b2");

                Assert.Null(b1.Background);
                Assert.Equal(Brushes.Red, b2.Background);

                parent.Children.Remove(b1);

                Assert.Null(b1.Background);
                Assert.Null(b2.Background);

                parent.Children.Add(b1);

                Assert.Equal(Brushes.Red, b1.Background);
                Assert.Null(b2.Background);
            }
        }

        [Fact]
        public void Style_Can_Use_NthLastChild_Selector_After_Reorder()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border:nth-last-child(2n)'>
            <Setter Property='Background' Value='Red'/>
        </Style>
    </Window.Styles>
    <StackPanel x:Name='parent'>
        <Border x:Name='b1' />
        <Border x:Name='b2' />
    </StackPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);

                var parent = window.FindControl<StackPanel>("parent");
                var b1 = window.FindControl<Border>("b1");
                var b2 = window.FindControl<Border>("b2");

                Assert.Equal(Brushes.Red, b1.Background);
                Assert.Null(b2.Background);

                parent.Children.Remove(b1);

                Assert.Null(b1.Background);
                Assert.Null(b2.Background);

                parent.Children.Add(b1);

                Assert.Null(b1.Background);
                Assert.Equal(Brushes.Red, b2.Background);
            }
        }

        [Fact]
        public void Style_Can_Use_NthChild_Selector_With_ListBox()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='ListBoxItem:nth-child(2n)'>
            <Setter Property='Background' Value='{Binding}'/>
        </Style>
    </Window.Styles>
    <ListBox x:Name='list' />
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var collection = new ObservableCollection<IBrush>()
                {
                    Brushes.Red, Brushes.Green, Brushes.Blue
                };

                var list = window.FindControl<ListBox>("list");
                list.ItemsSource = collection;

                window.Show();

                IEnumerable<IBrush> GetColors() => list.GetRealizedContainers().Cast<ListBoxItem>().Select(t => t.Background);

                Assert.Equal(new[] { Brushes.Transparent, Brushes.Green, Brushes.Transparent }, GetColors());

                collection.Remove(Brushes.Green);
                window.UpdateLayout();

                Assert.Equal(new[] { Brushes.Transparent, Brushes.Blue }, GetColors());

                collection.Add(Brushes.Violet);
                collection.Add(Brushes.Black);
                window.UpdateLayout();

                Assert.Equal(new[] { Brushes.Transparent, Brushes.Blue, Brushes.Transparent, Brushes.Black }, GetColors());
            }
        }

        [Fact]
        public void Style_Can_Use_Or_Selector_1()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border.foo, Border.bar'>
            <Setter Property='Background' Value='Red'/>
        </Style>
    </Window.Styles>
    <StackPanel>
        <Border Name='foo' Classes='foo'/>
        <Border Name='bar' Classes='bar'/>
        <Border Name='baz' Classes='baz'/>
    </StackPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var foo = window.FindControl<Border>("foo");
                var bar = window.FindControl<Border>("bar");
                var baz = window.FindControl<Border>("baz");

                Assert.Equal(Brushes.Red, foo.Background);
                Assert.Equal(Brushes.Red, bar.Background);
                Assert.Null(baz.Background);
            }
        }

        [Fact]
        public void Style_Can_Use_Or_Selector_2()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Button,Carousel,ListBox'>
            <Setter Property='Background' Value='Red'/>
        </Style>
    </Window.Styles>
    <StackPanel>
        <Button Name='button'/>
        <Carousel Name='carousel'/>
        <ListBox Name='listBox'/>
    </StackPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = window.FindControl<Button>("button");
                var carousel = window.FindControl<Carousel>("carousel");
                var listBox = window.FindControl<ListBox>("listBox");

                Assert.Equal(Brushes.Red, button.Background);
                Assert.Equal(Brushes.Red, carousel.Background);
                Assert.Equal(Brushes.Red, listBox.Background);
            }
        }

        [Fact]
        public void Transitions_Can_Be_Styled()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border'>
            <Setter Property='Transitions'>
                <Transitions>
                    <DoubleTransition Property='Width' Duration='0:0:1'/>
                </Transitions>
            </Setter>
        </Style>
        <Style Selector='Border.foo'>
            <Setter Property='Transitions'>
                <Transitions>
                    <DoubleTransition Property='Height' Duration='0:0:1'/>
                </Transitions>
            </Setter>
        </Style>
    </Window.Styles>
    <Border/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var border = (Border)window.Content;

                Assert.Equal(1, border.Transitions.Count);
                Assert.Equal(Border.WidthProperty, border.Transitions[0].Property);

                border.Classes.Add("foo");

                Assert.Equal(1, border.Transitions.Count);
                Assert.Equal(Border.HeightProperty, border.Transitions[0].Property);

                border.Classes.Remove("foo");

                Assert.Equal(1, border.Transitions.Count);
                Assert.Equal(Border.WidthProperty, border.Transitions[0].Property);
            }
        }

        [Fact]
        public void Style_Can_Use_Class_Selector_With_Dash()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border.foo-bar'>
            <Setter Property='Background' Value='Red'/>
        </Style>
    </Window.Styles>
    <StackPanel>
        <Border Name='foo' Classes='foo-bar'/>
    </StackPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var foo = window.FindControl<Border>("foo");

                Assert.Equal(Colors.Red, ((ISolidColorBrush)foo.Background).Color);
            }
        }

        [Fact]
        public void Style_Can_Use_Pseudolass_Selector_With_Dash()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border:foo-bar'>
            <Setter Property='Background' Value='Red'/>
        </Style>
    </Window.Styles>
    <StackPanel>
        <Border Name='foo'/>
    </StackPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var foo = window.FindControl<Border>("foo");

                Assert.Null(foo.Background);

                ((IPseudoClasses)foo.Classes).Add(":foo-bar");

                Assert.Equal(Colors.Red, ((ISolidColorBrush)foo.Background).Color);
            }
        }

        [Fact]
        public void Can_Use_Nested_Styles()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border'>
            <Style Selector='^.foo'>
                <Setter Property='Background' Value='Red'/>
            </Style>
        </Style>
    </Window.Styles>
    <StackPanel>
        <Border Name='foo'/>
    </StackPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var foo = window.FindControl<Border>("foo");

                Assert.Null(foo.Background);

                foo.Classes.Add("foo");

                Assert.Equal(Colors.Red, ((ISolidColorBrush)foo.Background).Color);
            }
        }
        
        [Fact]
        public void Multiple_Errors_Are_Reported()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='5' />
        <Style Selector='NonExistentType' />
        <Style Selector='Border:normal' />
        <Style Selector='Border+invalid' />
    </Window.Styles>
</Window>";
                var ex = Assert.Throws<AggregateException>(() => (Window)AvaloniaRuntimeXamlLoader.Load(xaml));
                Assert.Collection(
                    ex.InnerExceptions,
                    inner => Assert.IsAssignableFrom<XmlException>(inner),
                    inner => Assert.IsAssignableFrom<XmlException>(inner),
                    inner => Assert.IsAssignableFrom<XmlException>(inner));
            }
        }

        [Fact]
        public void Correctly_Resolve_TemplateBinding_In_Style_With_Template_Selector()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Style xmlns='https://github.com/avaloniaui'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
       xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'
       Selector='u|TestTemplatedControl /template/ Border'>
    <Setter Property='Tag' Value='{{TemplateBinding TestData}}'/>
</Style>";

                var style = (Style)AvaloniaRuntimeXamlLoader.Load(xaml);
                var setter = Assert.IsType<Setter>(Assert.Single(style.Setters));

                Assert.Equal(TestTemplatedControl.TestDataProperty, (setter.Value as TemplateBinding)?.Property);
            }
        }

        [Fact]
        public void Fails_To_Resolve_TemplateBinding_In_Style_Without_Template_Metadata()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Style xmlns='https://github.com/avaloniaui'
       Selector='Border'>
    <Setter Property='Tag' Value='{{TemplateBinding TestData}}'/>
</Style>";

                var exception = Assert.ThrowsAny<XmlException>(() => AvaloniaRuntimeXamlLoader.Load(xaml));
                Assert.Contains("ControlTemplate", exception.Message);
            }
        }

        [Fact]
        public void Can_Use_Classes_In_Setter()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $"""
                            <Window xmlns='https://github.com/avaloniaui'
                                         xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                                         xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
                                <Window.Styles>
                                    <Style Selector="Border">
                                        <Setter Property="(Classes.Banned)" Value='true'/>

                                        <Style Selector="^.Banned">
                                           <Setter Property='Background' Value='Red'/>
                                        </Style>
                                    </Style>
                                </Window.Styles>
                                <Border/>
                            </Window>
                            """;

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var border = window.Content as Border;
                Assert.NotNull(border);
                Assert.Equal(Brushes.Red, border.Background);
            }
        }

        [Fact]
        public void Can_Binding_Classes_In_Setter()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $$"""
                            <Window xmlns='https://github.com/avaloniaui'
                                         xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                                         xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'
                                         xmlns:vm='using:Avalonia.Markup.Xaml.UnitTests'
                                         >
                                <Window.Styles>
                                    <Style Selector="Border" x:DataType='vm:TestViewModel'>
                                        <Setter Property="(Classes.Banned)" Value='{Binding Boolean}'/>

                                        <Style Selector="^.Banned">
                                           <Setter Property='Background' Value='Red'/>
                                        </Style>
                                    </Style>
                                </Window.Styles>
                                <Window.DataContext>
                                   <vm:TestViewModel/>
                                </Window.DataContext>
                                <Border/>
                            </Window>
                            """;

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                window.ApplyTemplate();
                var vm = window.DataContext as TestViewModel;
                Assert.NotNull(vm);

                var border = window.Content as Border;
                Assert.NotNull(border);
                Assert.Null(border.Background);
                vm.Boolean = true;
                Assert.Equal(Brushes.Red, border.Background);
            }
        }

        [Fact]
        public void Fails_Use_Classes_In_Setter_When_Selector_Is_Complex()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $"""
                            <Window xmlns='https://github.com/avaloniaui'
                                         xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                                         xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
                                <Window.Styles>
                                    <Style Selector="Border:pointover">
                                        <Setter Property="(Classes.Banned)" Value='true'/>

                                        <Style Selector="^.Banned">
                                           <Setter Property='Background' Value='Red'/>
                                        </Style>
                                    </Style>
                                </Window.Styles>
                                <Border/>
                            </Window>
                            """;

                var exception = Assert.ThrowsAny<XmlException>(() => AvaloniaRuntimeXamlLoader.Load(xaml));
                Assert.Equal ("Cannot set Classes Binding property '(Classes.Banned)' because the style has an activator. Line 6, position 14.", exception.Message);
            }
        }

        [Theory]
        [InlineData("<Style>", "</Style>")]
        [InlineData("<Style Selector=''>", "</Style>")]
        [InlineData("<Styles><Style>", "</Style></Styles>")]
        [InlineData("<Styles><Style Selector=''>", "</Style></Styles>")]
        public void No_Selector_Should_Target_Parent_Type(string styleStart, string styleEnd)
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var window = (Window)AvaloniaRuntimeXamlLoader.Load(
                $"""
                <Window xmlns="https://github.com/avaloniaui">
                    <Window.Styles>
                        {styleStart}
                            <Setter Property="Title" Value="title set via style!" />
                        {styleEnd}
                    </Window.Styles>
                </Window>
                """);

            Assert.Equal("title set via style!", window.Title);
        }


        [Theory]
        [InlineData("<Style>", "</Style>")]
        [InlineData("<Style Selector=''>", "</Style>")]
        public void No_Selector_Should_Fail_In_Control_Theme(string styleStart, string styleEnd)
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var exception = Assert.ThrowsAny<XmlException>(() => (Window)AvaloniaRuntimeXamlLoader.Load(
                $$"""
                  <Window xmlns="https://github.com/avaloniaui"
                          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                     <Window.Resources>
                          <ControlTheme x:Key="{x:Type Window}" TargetType="Window">
                              {{styleStart}}
                                  <Setter Property="Title" Value="title set via style!" />
                              {{styleEnd}}
                          </ControlTheme>
                      </Window.Resources>
                  </Window>
                  """));

            Assert.Equal("Cannot add a Style without selector to a ControlTheme. Line 5, position 14.", exception.Message);
        }
    }
}
