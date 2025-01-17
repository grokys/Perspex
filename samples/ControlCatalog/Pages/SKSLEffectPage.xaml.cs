using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia.Effects;
using Avalonia.Styling;
using SkiaSharp;

namespace ControlCatalog.Pages
{
    public partial class SKSLEffectPage : UserControl
    {
        public SKSLEffectPage()
        {
            InitializeComponent();
            InitEffect();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitEffect()
        {
            var rectangle1 = this.FindControl<Rectangle>("Rectangle1")!;
            var shaderBuilder = CreateSimpleShaderBuilder();
            if (shaderBuilder != null)
            {
                rectangle1.Effect = new SKSLEffect(shaderBuilder)
                {
                    ChildShaderNames = ["src"],
                    Inputs = [null],
                };
            }

            var rectangle2 = this.FindControl<Rectangle>("Rectangle2")!;
            try
            {
                var effect = new DissolveSKSLEffect();
                effect.Progress = 0.5f;
                effect[!DissolveSKSLEffect.ResolutionProperty] = new Binding
                {
                    Source = rectangle2,
                    Path = "Bounds.Size",
                };
                rectangle2.Effect = effect;

                var animation = new Animation
                {
                    Duration = TimeSpan.FromSeconds(2),
                    IterationCount = IterationCount.Infinite,
                    PlaybackDirection = PlaybackDirection.Alternate,
                    Children = {
                        new KeyFrame
                        {
                            Setters =
                            {
                                new Setter(DissolveSKSLEffect.ProgressProperty, 0f),
                            },
                            KeyTime = TimeSpan.FromSeconds(0),
                        },
                        new KeyFrame
                        {
                            Setters =
                            {
                                new Setter(DissolveSKSLEffect.ProgressProperty, 1f),
                            },
                            KeyTime = TimeSpan.FromSeconds(2),
                        }
                    }
                };

                _ = animation.RunAsync(effect);
            }
            catch
            {
                // Do not crash.
            }
        }

        private SKRuntimeShaderBuilder? CreateSimpleShaderBuilder()
        {
            var sksl = @"
                uniform shader src;

                float4 main(float2 coord) {
                    return src.eval(coord).bgra;
                }
";
            var effect = SKRuntimeEffect.CreateShader(sksl, out var str);
            if (effect != null)
            {
                return new SKRuntimeShaderBuilder(effect);
            }
            else
            {
                return null;
            }
        }
    }

    public class DissolveSKSLEffect : SKSLEffect
    {
        public static readonly StyledProperty<float> ProgressProperty = AvaloniaProperty.Register<DissolveSKSLEffect, float>(nameof(Progress), default);

        public float Progress
        {
            get => GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public static readonly StyledProperty<Size> ResolutionProperty = AvaloniaProperty.Register<DissolveSKSLEffect, Size>(nameof(Resolution), default);

        public Size Resolution
        {
            get => GetValue(ResolutionProperty);
            set => SetValue(ResolutionProperty, value);
        }

        public DissolveSKSLEffect() : base(CreateShaderBuilder())
        {
            ChildShaderNames = ["src"];
            Inputs = [null];
            AffectsRender<SKSLEffect>(ProgressProperty);

            RegisterUniform("progress", ProgressProperty);
            RegisterUniform("resolution", ResolutionProperty);
        }

        private static SKRuntimeShaderBuilder? s_shaderBuilder;
        private static SKRuntimeShaderBuilder CreateShaderBuilder()
        {
            if (s_shaderBuilder != null)
            {
                return s_shaderBuilder;
            }

            var sksl = @"
                uniform float2 resolution; 
                uniform shader src;
                uniform shader noise;
                uniform float2 noiseResolution; 
                uniform float  progress;

                float4 main(float2 coord) {
                    float val = noise.eval(fract(coord / resolution) * noiseResolution).x;

                    if(val < progress)
                    {
                        return src.eval(coord);
                    }
                    else
                    {
                        return float4(0,0,0,0);
                    }
                }
";
            var effect = SKRuntimeEffect.CreateShader(sksl, out var str);
            if (effect != null)
            {
                var noise = AssetLoader.Open(new Uri("avares://ControlCatalog/Assets/noise.png"));
                var noiseImage = SKImage.FromEncodedData(noise);
                var noiseImageShader = SKShader.CreateImage(noiseImage);
                var builder = new SKRuntimeShaderBuilder(effect);
                builder.Uniforms["noiseResolution"] = new SKSize(noiseImage.Width, noiseImage.Height);
                builder.Children["noise"] = noiseImageShader;
                s_shaderBuilder = builder;
                return s_shaderBuilder;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
