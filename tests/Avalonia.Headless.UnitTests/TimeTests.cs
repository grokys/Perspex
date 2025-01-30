using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Avalonia.Headless.UnitTests;

public class TimeTests
{
    private const int MsInHour = 60 * 60 * 1000;

#if NUNIT
    [AvaloniaTheory, Timeout(10000), TestCase(1 * MsInHour), TestCase(10 * MsInHour), TestCase(100 * MsInHour), TestCase(int.MaxValue)]
#elif XUNIT
    [AvaloniaTheory(Timeout = 10000), InlineData(1 * MsInHour), InlineData(10 * MsInHour), InlineData(100 * MsInHour), InlineData(int.MaxValue)]
#endif
    public void Should_Pulse_Time_To_Skip_Hours(int milliseconds)
    {
        var interval = TimeSpan.FromMilliseconds(milliseconds);

        var triggered = false;
        using var _ = DispatcherTimer.RunOnce(() =>
        {
            triggered = true;
        }, interval);

        Assert.False(triggered);

        Dispatcher.UIThread.PulseTime(interval);

        Assert.True(triggered);
    }

#if NUNIT
    [AvaloniaTest, Timeout(10000)]
#elif XUNIT
    [AvaloniaFact(Timeout = 10000)]
#endif
    public void Doesnt_Trigger_Timers_Yet_From_The_Future()
    {
        var triggered = false;
        using var _ = DispatcherTimer.RunOnce(() =>
        {
            triggered = true;
        }, TimeSpan.FromSeconds(10));

        Assert.False(triggered);

        Dispatcher.UIThread.PulseTime(TimeSpan.FromSeconds(5));

        Assert.False(triggered);
    }

#if NUNIT
    [AvaloniaTest, Timeout(10000)]
#elif XUNIT
    [AvaloniaFact(Timeout = 10000)]
#endif
    // This tests is a copy of an actual Avalonia animations tests - Check_Initial_Inter_and_Trailing_Delay_Values
    public void Should_Be_Possible_To_Pulse_Animations()
    {
        var keyframe1 = new KeyFrame()
        {
            Setters = { new Setter(Layoutable.WidthProperty, 200d), }, Cue = new Cue(1d)
        };

        var keyframe2 = new KeyFrame()
        {
            Setters = { new Setter(Layoutable.WidthProperty, 100d), }, Cue = new Cue(0d)
        };

        var animation = new Animation.Animation()
        {
            Duration = TimeSpan.FromSeconds(3),
            Delay = TimeSpan.FromSeconds(3),
            DelayBetweenIterations = TimeSpan.FromSeconds(3),
            IterationCount = new IterationCount(2),
            Children = { keyframe2, keyframe1 }
        };

        var border = new Border() { Height = 100d, Width = 100d };

        var animationRun = animation.RunAsync(border);

        border.Measure(Size.Infinity);
        border.Arrange(new Rect(border.DesiredSize));

        // Initial Delay.
        Dispatcher.UIThread.Idle();
        Assert.True(Math.Abs(100d - border.Width) < double.Epsilon);

        Dispatcher.UIThread.PulseTime(TimeSpan.FromSeconds(6));

        // First Inter-Iteration delay.
        Dispatcher.UIThread.PulseTime(TimeSpan.FromSeconds(2));
        Assert.True(Math.Abs(200d - border.Width) < double.Epsilon);

        // Trailing Delay should be non-existent.
        Dispatcher.UIThread.PulseTime(TimeSpan.FromSeconds(6));
        Assert.True(animationRun.Status == TaskStatus.RanToCompletion);
        Assert.True(Math.Abs(100d - border.Width) < double.Epsilon);
    }

#if NUNIT
    [AvaloniaTest, Timeout(10000)]
#elif XUNIT
    [AvaloniaFact(Timeout = 10000)]
#endif
    // This tests is a copy of an actual Avalonia animations tests - Check_Initial_Inter_and_Trailing_Delay_Values
    public void Should_Be_Possible_To_Set_Custom_Time_Provider_Impl()
    {
        var timeProvider = new FakeTimeProvider();
        Dispatcher.UIThread.SetTimeProvider(timeProvider);

        var keyframe1 = new KeyFrame()
        {
            Setters = { new Setter(Layoutable.WidthProperty, 200d), }, Cue = new Cue(1d)
        };

        var keyframe2 = new KeyFrame()
        {
            Setters = { new Setter(Layoutable.WidthProperty, 100d), }, Cue = new Cue(0d)
        };

        var animation = new Animation.Animation()
        {
            Duration = TimeSpan.FromSeconds(3),
            Delay = TimeSpan.FromSeconds(3),
            DelayBetweenIterations = TimeSpan.FromSeconds(3),
            IterationCount = new IterationCount(2),
            Children = { keyframe2, keyframe1 }
        };

        var border = new Border() { Height = 100d, Width = 100d };

        var animationRun = animation.RunAsync(border);

        border.Measure(Size.Infinity);
        border.Arrange(new Rect(border.DesiredSize));

        // Initial Delay.
        Dispatcher.UIThread.Idle();

        Assert.True(Math.Abs(100d - border.Width) < double.Epsilon);

        timeProvider.CurrentTime = TimeSpan.FromSeconds(6);
        Dispatcher.UIThread.Idle();

        // First Inter-Iteration delay.
        timeProvider.CurrentTime = TimeSpan.FromSeconds(8);
        Dispatcher.UIThread.Idle();

        Assert.True(Math.Abs(200d - border.Width) < double.Epsilon);

        // Trailing Delay should be non-existent.
        timeProvider.CurrentTime = TimeSpan.FromSeconds(14);
        Dispatcher.UIThread.Idle();

        Assert.True(animationRun.Status == TaskStatus.RanToCompletion);
        Assert.True(Math.Abs(100d - border.Width) < double.Epsilon);
    }

#if NUNIT
    [AvaloniaTest, Timeout(10000)]
#elif XUNIT
    [AvaloniaFact(Timeout = 10000)]
#endif
    public async Task Should_Be_Possible_To_Use_System_Timer()
    {
        Dispatcher.UIThread.SetTimeProvider(TimeProvider.System);

        var interval = TimeSpan.FromMilliseconds(300);

        var triggered = false;
        using var _ = DispatcherTimer.RunOnce(() =>
        {
            triggered = true;
        }, interval);

        Assert.False(triggered);

        await Task.Delay(interval + TimeSpan.FromMilliseconds(100));

        Assert.True(triggered);
    }

#if NUNIT
    [AvaloniaTheory, Timeout(10000), TestCase(1), TestCase(10), TestCase(60)]
#elif XUNIT
    [AvaloniaTheory(Timeout = 10000), InlineData(1), InlineData(10), InlineData(60)]
#endif
    public void AnimationFrame_Should_Tick_When_Time_Is_Pulsed(int framesCount)
    {
        var continueTicks = true;
        try
        {
            var ticks = new HashSet<TimeSpan>();
            var window = new Window
            {
                Content = new Button { Content = "Hello" }, SizeToContent = SizeToContent.WidthAndHeight
            };

            window.Show();

            // At least one frame was scheduled at this point, so let's skip it before counting new frames.
            Dispatcher.UIThread.Idle();

            window.RequestAnimationFrame(OnRequestAnimationFrame);

            Dispatcher.UIThread.PulseRenderFrames(framesCount);

            Assert.True(ticks.Count == framesCount);

            void OnRequestAnimationFrame(TimeSpan tick)
            {
                ticks.Add(tick);
                if (continueTicks)
                {
                    window.RequestAnimationFrame(OnRequestAnimationFrame);
                }
            }
        }
        finally
        {
            continueTicks = false;   
        }
    }

    private class FakeTimeProvider : TimeProvider
    {
        public TimeSpan CurrentTime { get; set; }

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => CurrentTime.Ticks;
    }
}
