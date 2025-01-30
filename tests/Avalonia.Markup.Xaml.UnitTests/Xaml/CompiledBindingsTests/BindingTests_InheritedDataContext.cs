using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml.CompiledBindingsTests
{
    // Unit Tests for Github Issue 17755 
    public class BindingTests_InheritedDataContext : XamlTestBase
    {
        [Fact]
        public void Binding_Inherited_Data_Context_Properly_Notifies_Children()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {

                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'
        xmlns:datalocal='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml.CompiledBindingsTests'
        DataContext='{Binding Source={x:Static datalocal:TestingViewModelLocator.ModelLocator}, Path=TestingViewModelInstance}'
>
    <Grid DataContext='{Binding .}'>
        <DockPanel DataContext='{Binding .}'>
            <Grid DataContext='{Binding .}'>
                <ListBox x:Name='testingListBox'
                    DataContext='{Binding .}'
                    ItemsSource='{Binding ItemsList}'
                    />
            </Grid>
        </DockPanel>
    </Grid>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                Assert.NotNull(window);
                window.Show();

                var listBox = window.Get<ListBox>("testingListBox");
                Assert.NotNull(listBox);
                Assert.NotNull(listBox.DataContext);
                Assert.NotNull(listBox.ItemsSource);
            }
        }

        [Fact]
        public void Compiled_Binding_Inherited_Data_Context_Properly_Notifies_Children()
        {
            UnitTestApplication testApp = new(TestServices.StyledWindow);
            testApp.Resources.Add("ViewModelLocator", new TestingViewModelLocator());
            using (testApp.StartInstance())
            {
                TestWindow testWindow = new();
                testWindow.Show();
                Assert.IsType<TestingViewModel>(testWindow.DataContext);
                var rootControl = testWindow.TestRootControlInstance;
                Assert.NotNull(rootControl);
                Assert.IsType<TestingViewModel>(rootControl.DataContext);
                var childControl = rootControl.TestChildControlInstance;
                Assert.NotNull(childControl);
                Assert.IsType<TestingViewModel>(childControl.DataContext);
                var testListBox = childControl.TestListBox;
                Assert.NotNull(testListBox);
                Assert.IsType<TestingViewModel>(testListBox.DataContext);
                Assert.NotNull(testListBox.ItemsSource);
            }
        }
    }

    // to facilitate Binding Source as a Static Resource 
    public class TestingViewModelLocator
    {
        public TestingViewModel TestingViewModelInstance { get; set; } = new();

        public static TestingViewModelLocator ModelLocator { get; set; } = new();
    }

    public class TestingViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public AvaloniaList<string> ItemsList { get; private set; } = new();

        public TestingViewModel()
        {
            for (var i = 0; i < 20; i++)
            {
                ItemsList.Add(i.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
