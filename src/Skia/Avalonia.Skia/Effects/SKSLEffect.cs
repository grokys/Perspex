using System.Collections.Generic;
using Avalonia.Logging;
using Avalonia.Media;
using SkiaSharp;

namespace Avalonia.Skia.Effects
{
    public class SKSLEffect : Effect, ISKSLEffect, IMutableEffect
    {
        public SKRuntimeShaderBuilder ShaderBuilder { get; set; }

        public float MaxSampleRadius { get; set; } = 0f;

        public string[] ChildShaderNames { get; set; } = [];

        public SKImageFilter?[] Inputs { get; set; } = [];

        private readonly Dictionary<string, AvaloniaProperty> _uniformProperties = new Dictionary<string, AvaloniaProperty>();

        public SKSLEffect(SKRuntimeShaderBuilder builder)
        {
            ShaderBuilder = builder;
        }

        public void RegisterUniform(string name, AvaloniaProperty<int> property) => _uniformProperties.Add(name, property);

        public void RegisterUniform(string name, AvaloniaProperty<float> property) =>  _uniformProperties.Add(name, property);

        public void RegisterUniform(string name, AvaloniaProperty<Size> property) => _uniformProperties.Add(name, property);

        public IImmutableEffect ToImmutable()
        {
            SKRuntimeShaderBuilder builder = new SKRuntimeShaderBuilder(ShaderBuilder);
            foreach (var property in _uniformProperties)
            {
                var value = GetValue(property.Value);
                if (value is int intVal)
                {
                    builder.Uniforms[property.Key] = intVal;
                }
                else if (value is float floatVal)
                {
                    builder.Uniforms[property.Key] = floatVal;
                }
                else if (value is Size sizeVal)
                {
                    float[] val = [(float)sizeVal.Width, (float)sizeVal.Height];
                    builder.Uniforms[property.Key] = val;
                }
                else
                {
                    Logger.TryGet(LogEventLevel.Error, "Effect")?.Log(this, $"Unsupported uniform type: {value?.GetType() ?? null}");
                }
            }

            return new ImmutableSKSLEffect(builder, MaxSampleRadius, ChildShaderNames, Inputs);
        }
    }

    public class ImmutableSKSLEffect : ISKSLEffect, IImmutableEffect
    {
        public SKRuntimeShaderBuilder ShaderBuilder { get; }

        public float MaxSampleRadius { get; }

        public string[] ChildShaderNames { get; }

        public SKImageFilter?[] Inputs { get; }

        public ImmutableSKSLEffect(SKRuntimeShaderBuilder builder, float maxSampleRadius, string[] childShaderNames, SKImageFilter?[] inputs)
        {
            ShaderBuilder = builder;
            MaxSampleRadius = maxSampleRadius;
            ChildShaderNames = childShaderNames;
            Inputs = inputs;
        }

        public bool Equals(IEffect? other)
        {
            return false;
        }
    }

    public interface ISKSLEffect : IShaderEffect
    {
        SKRuntimeShaderBuilder ShaderBuilder { get; }

        float MaxSampleRadius { get; }

        string[] ChildShaderNames { get; }

        SKImageFilter?[] Inputs { get; }
    }
}
