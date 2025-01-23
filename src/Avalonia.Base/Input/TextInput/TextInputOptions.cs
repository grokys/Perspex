namespace Avalonia.Input.TextInput;

public class TextInputOptions
{
    public static TextInputOptions FromStyledElement(StyledElement avaloniaObject)
    {
        var result = new TextInputOptions
        {
            ContentType = GetContentType(avaloniaObject),
            ReturnKeyType = GetReturnKeyType(avaloniaObject),
            Multiline = GetMultiline(avaloniaObject),
            AutoCapitalization = GetAutoCapitalization(avaloniaObject),
            IsSensitive = GetIsSensitive(avaloniaObject),
            Lowercase = GetLowercase(avaloniaObject),
            Uppercase = GetUppercase(avaloniaObject),
            ShowSuggestions = GetShowSuggestions(avaloniaObject),
        };

        return result;
    }

    public static readonly TextInputOptions Default = new();
    
    /// <summary>
    /// Defines the <see cref="ContentType"/> property.
    /// </summary>
    public static readonly AttachedProperty<TextInputContentType> ContentTypeProperty =
        AvaloniaProperty.RegisterAttached<TextInputOptions, StyledElement, TextInputContentType>(
            "ContentType",
            defaultValue: TextInputContentType.Normal,
            inherits: true);
    
    /// <summary>
    /// Sets the value of the attached <see cref="ContentTypeProperty"/> on a control.
    /// </summary>
    /// <param name="avaloniaObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetContentType(StyledElement avaloniaObject, TextInputContentType value)
    {
        avaloniaObject.SetValue(ContentTypeProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached <see cref="ContentTypeProperty"/>.
    /// </summary>
    /// <param name="avaloniaObject">The target.</param>
    /// <returns>TextInputContentType</returns>
    public static TextInputContentType GetContentType(StyledElement avaloniaObject)
    {
        return avaloniaObject.GetValue(ContentTypeProperty);
    }
    
    /// <summary>
    /// The content type (mostly for determining the shape of the virtual keyboard)
    /// </summary>
    public TextInputContentType ContentType { get; set; }
    
    
    /// <summary>
    /// Defines the <see cref="ReturnKeyType"/> property.
    /// </summary>
    public static readonly AttachedProperty<TextInputReturnKeyType> ReturnKeyTypeProperty =
        AvaloniaProperty.RegisterAttached<TextInputOptions, StyledElement, TextInputReturnKeyType>(
            "ReturnKeyType",
            defaultValue: TextInputReturnKeyType.Default,
            inherits: true);
    
    /// <summary>
    /// Sets the value of the attached <see cref="ReturnKeyTypeProperty"/> on a control.
    /// </summary>
    /// <param name="avaloniaObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetReturnKeyType(StyledElement avaloniaObject, TextInputReturnKeyType value)
    {
        avaloniaObject.SetValue(ReturnKeyTypeProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached <see cref="ReturnKeyTypeProperty"/>.
    /// </summary>
    /// <param name="avaloniaObject">The target.</param>
    /// <returns>TextInputReturnKeyType</returns>
    public static TextInputReturnKeyType GetReturnKeyType(StyledElement avaloniaObject)
    {
        return avaloniaObject.GetValue(ReturnKeyTypeProperty);
    }
    
    /// <summary>
    /// Determines what the Return key says and how it behaves.
    /// </summary>
    public TextInputReturnKeyType ReturnKeyType { get; set; }
    
    /// <summary>
    /// Defines the <see cref="Multiline"/> property.
    /// </summary>
    public static readonly AttachedProperty<bool> MultilineProperty =
        AvaloniaProperty.RegisterAttached<TextInputOptions, StyledElement, bool>(
            "Multiline",
            inherits: true);
    
    /// <summary>
    /// Sets the value of the attached <see cref="MultilineProperty"/> on a control.
    /// </summary>
    /// <param name="avaloniaObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetMultiline(StyledElement avaloniaObject, bool value)
    {
        avaloniaObject.SetValue(MultilineProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached <see cref="MultilineProperty"/>.
    /// </summary>
    /// <param name="avaloniaObject">The target.</param>
    /// <returns>true if multiline</returns>
    public static bool GetMultiline(StyledElement avaloniaObject)
    {
        return avaloniaObject.GetValue(MultilineProperty);
    }
        
    /// <summary>
    /// Text is multiline
    /// </summary>
    public bool Multiline { get; set; }
    
    /// <summary>
    /// Defines the <see cref="Lowercase"/> property.
    /// </summary>
    public static readonly AttachedProperty<bool> LowercaseProperty =
        AvaloniaProperty.RegisterAttached<TextInputOptions, StyledElement, bool>(
            "Lowercase",
            inherits: true);
    
    /// <summary>
    /// Sets the value of the attached <see cref="LowercaseProperty"/> on a control.
    /// </summary>
    /// <param name="avaloniaObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetLowercase(StyledElement avaloniaObject, bool value)
    {
        avaloniaObject.SetValue(LowercaseProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached <see cref="LowercaseProperty"/>.
    /// </summary>
    /// <param name="avaloniaObject">The target.</param>
    /// <returns>true if Lowercase</returns>
    public static bool GetLowercase(StyledElement avaloniaObject)
    {
        return avaloniaObject.GetValue(LowercaseProperty);
    }
        
    /// <summary>
    /// Text is in lower case
    /// </summary>
    public bool Lowercase { get; set; }
    
    /// <summary>
    /// Defines the <see cref="Uppercase"/> property.
    /// </summary>
    public static readonly AttachedProperty<bool> UppercaseProperty =
        AvaloniaProperty.RegisterAttached<TextInputOptions, StyledElement, bool>(
            "Uppercase",
            inherits: true);
    
    /// <summary>
    /// Sets the value of the attached <see cref="UppercaseProperty"/> on a control.
    /// </summary>
    /// <param name="avaloniaObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetUppercase(StyledElement avaloniaObject, bool value)
    {
        avaloniaObject.SetValue(UppercaseProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached <see cref="UppercaseProperty"/>.
    /// </summary>
    /// <param name="avaloniaObject">The target.</param>
    /// <returns>true if Uppercase</returns>
    public static bool GetUppercase(StyledElement avaloniaObject)
    {
        return avaloniaObject.GetValue(UppercaseProperty);
    }
        
    /// <summary>
    /// Text is in upper case
    /// </summary>
    public bool Uppercase { get; set; }
        
    /// <summary>
    /// Defines the <see cref="AutoCapitalization"/> property.
    /// </summary>
    public static readonly AttachedProperty<bool> AutoCapitalizationProperty =
        AvaloniaProperty.RegisterAttached<TextInputOptions, StyledElement, bool>(
            "AutoCapitalization",
            inherits: true);
    
    /// <summary>
    /// Sets the value of the attached <see cref="AutoCapitalizationProperty"/> on a control.
    /// </summary>
    /// <param name="avaloniaObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetAutoCapitalization(StyledElement avaloniaObject, bool value)
    {
        avaloniaObject.SetValue(AutoCapitalizationProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached <see cref="AutoCapitalizationProperty"/>.
    /// </summary>
    /// <param name="avaloniaObject">The target.</param>
    /// <returns>true if AutoCapitalization</returns>
    public static bool GetAutoCapitalization(StyledElement avaloniaObject)
    {
        return avaloniaObject.GetValue(AutoCapitalizationProperty);
    }
    
    /// <summary>
    /// Automatically capitalize letters at the start of the sentence
    /// </summary>
    public bool AutoCapitalization { get; set; }
        
    /// <summary>
    /// Defines the <see cref="IsSensitive"/> property.
    /// </summary>
    public static readonly AttachedProperty<bool> IsSensitiveProperty =
        AvaloniaProperty.RegisterAttached<TextInputOptions, StyledElement, bool>(
            "IsSensitive",
            inherits: true);
    
    /// <summary>
    /// Sets the value of the attached <see cref="IsSensitiveProperty"/> on a control.
    /// </summary>
    /// <param name="avaloniaObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetIsSensitive(StyledElement avaloniaObject, bool value)
    {
        avaloniaObject.SetValue(IsSensitiveProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached <see cref="IsSensitiveProperty"/>.
    /// </summary>
    /// <param name="avaloniaObject">The target.</param>
    /// <returns>true if IsSensitive</returns>
    public static bool GetIsSensitive(StyledElement avaloniaObject)
    {
        return avaloniaObject.GetValue(IsSensitiveProperty);
    }
    
    /// <summary>
    /// Text contains sensitive data like card numbers and should not be stored  
    /// </summary>
    public bool IsSensitive { get; set; }
    
    /// <summary>
    /// Defines the <see cref="ShowSuggestions"/> property.
    /// </summary>
    public static readonly AttachedProperty<bool?> ShowSuggestionsProperty =
        AvaloniaProperty.RegisterAttached<TextInputOptions, StyledElement, bool?>(
            "ShowSuggestions",
            inherits: true);
    
    /// <summary>
    /// Sets the value of the attached <see cref="ShowSuggestionsProperty"/> on a control.
    /// </summary>
    /// <param name="avaloniaObject">The control.</param>
    /// <param name="value">The property value to set.</param>
    public static void SetShowSuggestions(StyledElement avaloniaObject, bool? value)
    {
        avaloniaObject.SetValue(ShowSuggestionsProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached <see cref="ShowSuggestionsProperty"/>.
    /// </summary>
    /// <param name="avaloniaObject">The target.</param>
    /// <returns>true if ShowSuggestions</returns>
    public static bool? GetShowSuggestions(StyledElement avaloniaObject)
    {
        return avaloniaObject.GetValue(ShowSuggestionsProperty);
    }
    
    /// <summary>
    /// Show virtual keyboard suggestions
    /// </summary>
    public bool? ShowSuggestions { get; set; }
}
