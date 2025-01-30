using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Diagnostics;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.PropertyStore
{
    internal class ValueStore : IBindingExpressionSink
    {
        private readonly List<ValueFrame> _frames = new();
        private Dictionary<int, IDisposable>? _localValueBindings;
        private AvaloniaPropertyDictionary<EffectiveValue> _effectiveValues;
        private int _inheritedValueCount;
        private int _isEvaluating;
        private int _frameGeneration;
        private int _styling;

        public ValueStore(AvaloniaObject owner) => Owner = owner;

        public AvaloniaObject Owner { get; }
        public ValueStore? InheritanceAncestor { get; private set; }
        public bool IsEvaluating => _isEvaluating > 0;
        public IReadOnlyList<ValueFrame> Frames => _frames;

        public void BeginStyling() => ++_styling;

        public void EndStyling()
        {
            if (--_styling == 0)
                ReevaluateEffectiveValues();
        }

        public void AddFrame(ValueFrame style)
        {
            InsertFrame(style);
            ReevaluateEffectiveValues();
        }

        public BindingExpressionBase AddBinding(
            AvaloniaProperty property,
            UntypedBindingExpressionBase source)
        {
            var priority = source.Priority;
            if (priority == BindingPriority.LocalValue || property.IsDirect)
            {
                DisposeExistingLocalValueBinding(property);
                _localValueBindings ??= new();
                _localValueBindings[property.Id] = source;
                source.AttachAndStart(this, Owner, property, BindingPriority.LocalValue);
                return source;
            }
            else
            {
                var effective = GetEffectiveValue(property);
                var frame = GetOrCreateImmediateValueFrame(property, priority, out _);

                source.Attach(this, frame, Owner, property, priority);
                frame.AddBinding(source);

                if (effective is null || priority <= effective.Priority)
                    source.Start();

                return source;
            }
        }

        public IDisposable AddBinding<T>(
            StyledProperty<T> property,
            IObservable<BindingValue<T>> source,
            BindingPriority priority)
        {
            if (priority == BindingPriority.LocalValue)
            {
                var observer = new LocalValueBindingObserver<T>(this, property);
                DisposeExistingLocalValueBinding(property);
                _localValueBindings ??= new();
                _localValueBindings[property.Id] = observer;
                observer.Start(source);
                return observer;
            }
            else
            {
                var effective = GetEffectiveValue(property);

                var frame = GetOrCreateImmediateValueFrame(property, priority, out _);
                var result = frame.AddBinding(property, source);

                if (effective is null || priority <= effective.Priority)
                    result.Start();

                return result;
            }
        }

        public IDisposable AddBinding<T>(
            StyledProperty<T> property,
            IObservable<T> source,
            BindingPriority priority)
        {
            if (priority == BindingPriority.LocalValue)
            {
                var observer = new LocalValueBindingObserver<T>(this, property);
                DisposeExistingLocalValueBinding(property);
                _localValueBindings ??= new();
                _localValueBindings[property.Id] = observer;
                observer.Start(source);
                return observer;
            }
            else
            {
                var effective = GetEffectiveValue(property);

                var frame = GetOrCreateImmediateValueFrame(property, priority, out _);
                var result = frame.AddBinding(property, source);

                if (effective is null || priority <= effective.Priority)
                    result.Start();

                return result;
            }
        }

        public IDisposable AddBinding<T>(
            StyledProperty<T> property,
            IObservable<object?> source,
            BindingPriority priority)
        {
            if (priority == BindingPriority.LocalValue)
            {
                var observer = new LocalValueBindingObserver<T>(this, property);
                DisposeExistingLocalValueBinding(property);
                _localValueBindings ??= new();
                _localValueBindings[property.Id] = observer;
                observer.Start(source);
                return observer;
            }
            else
            {
                var effective = GetEffectiveValue(property);

                var frame = GetOrCreateImmediateValueFrame(property, priority, out _);
                var result = frame.AddBinding(property, source);

                if (effective is null || priority <= effective.Priority)
                    result.Start();

                return result;
            }
        }

        public IDisposable AddBinding<T>(DirectPropertyBase<T> property, IObservable<BindingValue<T>> source)
        {
            var observer = new DirectBindingObserver<T>(this, property);
            DisposeExistingLocalValueBinding(property);
            _localValueBindings ??= new();
            _localValueBindings[property.Id] = observer;
            observer.Start(source);
            return observer;
        }

        public IDisposable AddBinding<T>(DirectPropertyBase<T> property, IObservable<T> source)
        {
            var observer = new DirectBindingObserver<T>(this, property);
            DisposeExistingLocalValueBinding(property);
            _localValueBindings ??= new();
            _localValueBindings[property.Id] = observer;
            observer.Start(source);
            return observer;
        }

        public IDisposable AddBinding<T>(DirectPropertyBase<T> property, IObservable<object?> source)
        {
            var observer = new DirectUntypedBindingObserver<T>(this, property);
            DisposeExistingLocalValueBinding(property);
            _localValueBindings ??= new();
            _localValueBindings[property.Id] = observer;
            observer.Start(source);
            return observer;
        }

        public void ClearValue(AvaloniaProperty property)
        {
            if (TryGetEffectiveValue(property, out var effective) &&
                (effective.Priority == BindingPriority.LocalValue || effective.IsOverridenCurrentValue))
            {
                effective.IsOverridenCurrentValue = false;
                ReevaluateEffectiveValue(property, effective, ignoreLocalValue: true);
            }
        }

        public IDisposable? SetValue<T>(StyledProperty<T> property, T value, BindingPriority priority)
        {
            if (property.ValidateValue?.Invoke(value) == false)
            {
                throw new ArgumentException($"{value} is not a valid value for '{property.Name}.");
            }

            if (priority != BindingPriority.LocalValue)
            {
                var frame = GetOrCreateImmediateValueFrame(property, priority, out _);
                var result = frame.AddValue(property, value);

                if (TryGetEffectiveValue(property, out var existing))
                {
                    var effective = (EffectiveValue<T>)existing;
                    effective.SetAndRaise(this, result, priority);
                }
                else
                {
                    var effectiveValue = CreateEffectiveValue(property);
                    AddEffectiveValue(property, effectiveValue);
                    effectiveValue.SetAndRaise(this, result, priority);
                }

                return result;
            }
            else
            {
                SetLocalValue(property, value);
                return null;
            }
        }

        public void SetCurrentValue<T>(StyledProperty<T> property, T value)
        {
            if (TryGetEffectiveValue(property, out var v))
            {
                ((EffectiveValue<T>)v).SetCurrentValueAndRaise(this, property, value);
            }
            else
            {
                var effectiveValue = CreateEffectiveValue(property);
                AddEffectiveValue(property, effectiveValue);
                effectiveValue.SetCurrentValueAndRaise(this, property, value);
            }
        }

        public void SetLocalValue(AvaloniaProperty property, object? value)
        {
            if (TryGetEffectiveValue(property, out var existing))
            {
                existing.SetLocalValueAndRaise(this, property, value);
            }
            else
            {
                var effectiveValue = property.CreateEffectiveValue(Owner);
                AddEffectiveValue(property, effectiveValue);
                effectiveValue.SetLocalValueAndRaise(this, property, value);
            }
        }

        public void SetLocalValue<T>(StyledProperty<T> property, T value)
        {
            if (TryGetEffectiveValue(property, out var existing))
            {
                var effective = (EffectiveValue<T>)existing;
                effective.SetLocalValueAndRaise(this, property, value);
            }
            else
            {
                var effectiveValue = CreateEffectiveValue(property);
                AddEffectiveValue(property, effectiveValue);
                effectiveValue.SetLocalValueAndRaise(this, property, value);
            }
        }

        public object? GetValue(AvaloniaProperty property)
        {
            if (_effectiveValues.TryGetValue(property, out var v))
                return v.Value;
            if (property.Inherits && TryGetInheritedValue(property, out v))
                return v.Value;

            return GetDefaultValue(property);
        }

        public T GetValue<T>(StyledProperty<T> property)
        {
            // <!> Performance critical method
            if (_effectiveValues.TryGetValue(property, out var v))
                return CastEffectiveValue<T>(v).Value;
            if (property.Inherits && TryGetInheritedValue(property, out v))
                return CastEffectiveValue<T>(v).Value;
            return property.GetDefaultValue(Owner);
        }

        public BindingExpressionBase? GetExpression(AvaloniaProperty property)
        {
            var evaluatedLocalValue = false;

            bool TryGetLocalValue(out BindingExpressionBase? result)
            {
                if (!evaluatedLocalValue)
                {
                    evaluatedLocalValue = true;

                    if (_localValueBindings?.TryGetValue(property.Id, out var o) == true)
                    {
                        result = o as BindingExpressionBase;
                        return true;
                    }
                }

                result = null;
                return false;
            }

            for (var i = _frames.Count - 1; i >= 0; --i)
            {
                var frame = _frames[i];

                if (frame.Priority > BindingPriority.LocalValue && TryGetLocalValue(out var localExpression))
                    return localExpression;

                if (frame.TryGetEntryIfActive(property, out var entry, out _))
                    return entry as BindingExpressionBase;
            }

            TryGetLocalValue(out var e);
            return e;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static EffectiveValue<T> CastEffectiveValue<T>(EffectiveValue value)
        {
#if DEBUG
            return (EffectiveValue<T>)value;
#else
            // Avoid casts in release mode for performance since GetValue is a very hot path.
            // We control every path:
            // it shouldn't be possible to have something else than an EffectiveValue<T> stored for a T property.
            return Unsafe.As<EffectiveValue<T>>(value);
#endif
        }

        public bool IsAnimating(AvaloniaProperty property)
        {
            if (_effectiveValues.TryGetValue(property, out var v))
                return v.Priority <= BindingPriority.Animation;
            return false;
        }

        public bool IsSet(AvaloniaProperty property) => _effectiveValues.TryGetValue(property, out _);

        public void CoerceValue(AvaloniaProperty property)
        {
            if (_effectiveValues.TryGetValue(property, out var v))
                v.CoerceValue(this, property);
            else
                property.RouteCoerceDefaultValue(Owner);
        }

        public void CoerceDefaultValue<T>(StyledProperty<T> property)
        {
            var metadata = property.GetMetadata(Owner);

            if (metadata.CoerceValue is null)
                return;

            var coercedDefaultValue = metadata.CoerceValue(Owner, metadata.DefaultValue);

            if (EqualityComparer<T>.Default.Equals(metadata.DefaultValue, coercedDefaultValue))
                return;

            // We have a situation where the default value isn't valid according to the coerce
            // function. In this case, we need to create an EffectiveValue entry.
            var effectiveValue = CreateEffectiveValue(property);
            AddEffectiveValue(property, effectiveValue);
            effectiveValue.SetCoercedDefaultValueAndRaise(this, property, coercedDefaultValue);
        }

        public Optional<T> GetBaseValue<T>(StyledProperty<T> property)
        {
            if (TryGetEffectiveValue(property, out var v) &&
                ((EffectiveValue<T>)v).TryGetBaseValue(out var baseValue))
            {
                return baseValue;
            }

            return default;
        }

        public bool TryGetInheritedValue(
            AvaloniaProperty property,
            [NotNullWhen(true)] out EffectiveValue? result)
        {
            Debug.Assert(property.Inherits);

            var i = InheritanceAncestor;

            while (i is not null)
            {
                if (i.TryGetEffectiveValue(property, out result))
                    return true;
                i = i.InheritanceAncestor;
            }

            result = null;
            return false;
        }

        public EffectiveValue<T> CreateEffectiveValue<T>(StyledProperty<T> property)
        {
            EffectiveValue<T>? inherited = null;

            if (property.Inherits && TryGetInheritedValue(property, out var v))
                inherited = (EffectiveValue<T>)v;

            return new EffectiveValue<T>(Owner, property, inherited);
        }

        public void SetInheritanceParent(AvaloniaObject? newParent)
        {
            var values = AvaloniaPropertyDictionaryPool<OldNewValue>.Get();
            var oldAncestor = InheritanceAncestor;
            var newAncestor = newParent?.GetValueStore();

            if (newAncestor?._inheritedValueCount == 0)
                newAncestor = newAncestor.InheritanceAncestor;

            // The old and new inheritance ancestors are the same, nothing to do here.
            if (oldAncestor == newAncestor)
                return;

            // First get the old values from the old inheritance ancestor.
            var f = oldAncestor;

            while (f is not null)
            {
                var count = f._effectiveValues.Count;

                for (var i = 0; i < count; ++i)
                {
                    var value = f._effectiveValues.GetValue(i);
                    if (value.Property.Inherits)
                        values.TryAdd(value.Property, new(value));
                }

                f = f.InheritanceAncestor;
            }

            f = newAncestor;

            // Get the new values from the new inheritance ancestor.
            while (f is not null)
            {
                var count = f._effectiveValues.Count;

                for (var i = 0; i < count; ++i)
                {
                    var value = f._effectiveValues.GetValue(i);
                    var property = value.Property;

                    if (!property.Inherits)
                        continue;

                    if (values.TryGetValue(property, out var existing))
                    {
                        if (existing.NewValue is null)
                            values[property] = existing.WithNewValue(value);
                    }
                    else
                    {
                        values.Add(property, new(null, value));
                    }
                }

                f = f.InheritanceAncestor;
            }

            OnInheritanceAncestorChanged(newAncestor);

            // Raise PropertyChanged events where necessary on this object and inheritance children.
            {
                var count = values.Count;
                for (var i = 0; i < count; ++i)
                {
                    var v = values.GetValue(i);
                    var oldValue = v.OldValue;
                    var newValue = v.NewValue;

                    if (oldValue != newValue)
                    {
                        var property = v.OldValue?.Property ?? v.NewValue!.Property;
                        InheritedValueChanged(property, oldValue, newValue);
                    }
                }
            }

            AvaloniaPropertyDictionaryPool<OldNewValue>.Release(values);
        }

        /// <summary>
        /// Called by non-LocalValue binding entries to re-evaluate the effective value when the
        /// binding produces a new value.
        /// </summary>
        /// <param name="entry">The binding entry.</param>
        /// <param name="priority">The priority of binding which produced a new value.</param>
        public void OnBindingValueChanged(
            IValueEntry entry,
            BindingPriority priority)
        {
            Debug.Assert(priority != BindingPriority.LocalValue);

            var property = entry.Property;

            if (TryGetEffectiveValue(property, out var existing))
            {
                if (priority <= existing.BasePriority)
                    ReevaluateEffectiveValue(property, existing, changedValueEntry: entry);
            }
            else
            {
                AddEffectiveValueAndRaise(property, entry, priority);
            }
        }

        /// <summary>
        /// Called by a <see cref="ValueFrame"/> when its <see cref="ValueFrame.IsActive"/>
        /// state changes.
        /// </summary>
        /// <param name="frame">The frame which produced the change.</param>
        public void OnFrameActivationChanged(ValueFrame frame)
        {
            if (frame.EntryCount == 0)
                return;
            else if (frame.EntryCount == 1)
            {
                var property = frame.GetEntry(0).Property;
                _effectiveValues.TryGetValue(property, out var current);
                ReevaluateEffectiveValue(property, current);
            }
            else
            {
                ReevaluateEffectiveValues();
            }
        }

        /// <summary>
        /// Called by the parent value store when its inheritance ancestor changes.
        /// </summary>
        /// <param name="ancestor">The new inheritance ancestor.</param>
        public void OnInheritanceAncestorChanged(ValueStore? ancestor)
        {
            if (ancestor != this)
            {
                InheritanceAncestor = ancestor;
                if (_inheritedValueCount > 0)
                    return;
            }

            var children = Owner.GetInheritanceChildren();

            if (children is null)
                return;

            var count = children.Count;

            for (var i = 0; i < count; ++i)
            {
                children[i].GetValueStore().OnInheritanceAncestorChanged(ancestor);
            }
        }

        /// <summary>
        /// Called by <see cref="EffectiveValue{T}"/> when an property with inheritance enabled
        /// changes its value on this value store.
        /// </summary>
        /// <param name="property">The property whose value changed.</param>
        /// <param name="oldValue">The old value of the property.</param>
        /// <param name="value">The effective value instance.</param>
        public void OnInheritedEffectiveValueChanged<T>(
            StyledProperty<T> property,
            T oldValue,
            EffectiveValue<T> value)
        {
            Debug.Assert(property.Inherits);

            var children = Owner.GetInheritanceChildren();

            if (children is null)
                return;

            var count = children.Count;

            for (var i = 0; i < count; ++i)
            {
                children[i].GetValueStore().OnAncestorInheritedValueChanged(property, oldValue, value.Value);
            }
        }

        /// <summary>
        /// Called by <see cref="EffectiveValue{T}"/> when an property with inheritance enabled
        /// is removed from the effective values.
        /// </summary>
        /// <param name="property">The property whose value changed.</param>
        /// <param name="oldValue">The old value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        public void OnInheritedEffectiveValueDisposed<T>(StyledProperty<T> property, T oldValue, T newValue)
        {
            Debug.Assert(property.Inherits);

            var children = Owner.GetInheritanceChildren();

            if (children is not null)
            {
                var count = children.Count;

                for (var i = 0; i < count; ++i)
                {
                    children[i].GetValueStore().OnAncestorInheritedValueChanged(property, oldValue, newValue);
                }
            }
        }

        /// <summary>
        /// Called when a <see cref="LocalValueBindingObserver{T}"/> or
        /// <see cref="DirectBindingObserver{T}"/> completes.
        /// </summary>
        /// <param name="property">The previously bound property.</param>
        /// <param name="observer">The observer.</param>
        public void OnLocalValueBindingCompleted(AvaloniaProperty property, IDisposable observer)
        {
            if (_localValueBindings is not null &&
                _localValueBindings.TryGetValue(property.Id, out var existing))
            {
                if (existing == observer)
                {
                    _localValueBindings?.Remove(property.Id);
                    ClearValue(property);
                }
            }
        }

        /// <summary>
        /// Called when an inherited property changes on the value store of the inheritance ancestor.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="oldValue">The old value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        public void OnAncestorInheritedValueChanged<T>(
            StyledProperty<T> property,
            T oldValue,
            T newValue)
        {
            Debug.Assert(property.Inherits);

            // If the inherited value is set locally, propagation stops here.
            if (_effectiveValues.ContainsKey(property))
            {
                // BUT, we also need to double-check whether the affected property is the Data-Context AND whether the Data-Context is bound. 
                // That is, a bound data-context needs re-evaluating even if it's bound locally 
                // Because a change in the data-context changes what it means for the data-context to be bound 
                if (property != StyledElement.DataContextProperty)
                {
                    return;
                }
                var bindingExpression = BindingOperations.GetBindingExpressionBase(Owner, property) as BindingExpression;
                if (bindingExpression is null)
                {
                    return;
                }
                // Both of these checks returning false means the affected property is the Data-Context, and it is bound. 
                // So, continue, because the data-context changing from an inherited context changes the meaning 
                // of what the property should be after binding. 

                // But now we need to handle cases where the DataContext is intending to change (that is, the path is not `.` )
                // easiest way is to just get the value of the binding expression and check whether it isn't the same as the newValue

                var bindingValue = bindingExpression.GetValue();
                if (bindingValue is T tInst && !EqualityComparer<T>.Default.Equals(tInst, newValue))
                {
                    return;
                }
            }

            using var notifying = PropertyNotifying.Start(Owner, property);

            Owner.RaisePropertyChanged(
                property,
                oldValue,
                newValue,
                BindingPriority.Inherited,
                true);

            var children = Owner.GetInheritanceChildren();

            if (children is null)
                return;

            var count = children.Count;

            for (var i = 0; i < count; ++i)
            {
                children[i].GetValueStore().OnAncestorInheritedValueChanged(property, oldValue, newValue);
            }
        }

        /// <summary>
        /// Called by a <see cref="ValueFrame"/> to re-evaluate the effective value when a value
        /// is removed.
        /// </summary>
        /// <param name="frame">The frame on which the change occurred.</param>
        /// <param name="property">The property whose value was removed.</param>
        public void OnValueEntryRemoved(ValueFrame frame, AvaloniaProperty property)
        {
            if (frame.EntryCount == 0)
                _frames.Remove(frame);

            if (TryGetEffectiveValue(property, out var existing))
            {
                if (frame.Priority <= existing.Priority)
                    ReevaluateEffectiveValue(property, existing);
            }
        }

        public bool RemoveFrame(ValueFrame frame)
        {
            if (_frames.Remove(frame))
            {
                frame.Dispose();
                ++_frameGeneration;
                ReevaluateEffectiveValues();
            }

            return false;
        }

        public void RemoveFrames(FrameType type)
        {
            var removed = false;

            for (var i = _frames.Count - 1; i >= 0; --i)
            {
                var frame = _frames[i];

                if (frame is not ImmediateValueFrame && frame.FramePriority.IsType(type))
                {
                    _frames.RemoveAt(i);
                    frame.Dispose();
                    removed = true;
                }
            }

            if (removed)
            {
                ++_frameGeneration;
                ReevaluateEffectiveValues();
            }
        }


        public void RemoveFrames(IReadOnlyList<IStyle> styles)
        {
            var removed = false;

            for (var i = _frames.Count - 1; i >= 0; --i)
            {
                var frame = _frames[i];

                if (frame is StyleInstance style && styles.Contains(style.Source))
                {
                    _frames.RemoveAt(i);
                    frame.Dispose();
                    removed = true;
                }
            }

            if (removed)
            {
                ++_frameGeneration;
                ReevaluateEffectiveValues();
            }
        }

        public AvaloniaPropertyValue GetDiagnostic(AvaloniaProperty property)
        {
            object? value;
            BindingPriority priority;
            bool overridden = false;

            if (_effectiveValues.TryGetValue(property, out var v))
            {
                value = v.Value;
                priority = v.Priority;
                overridden = v.IsOverridenCurrentValue;
            }
            else if (property.Inherits && TryGetInheritedValue(property, out v))
            {
                value = v.Value;
                priority = BindingPriority.Inherited;
            }
            else
            {
                value = GetDefaultValue(property);
                priority = BindingPriority.Unset;
            }

            return new AvaloniaPropertyValue(
                property,
                value,
                priority,
                null,
                overridden);
        }

        void IBindingExpressionSink.OnChanged(
            UntypedBindingExpressionBase instance,
            bool hasValueChanged,
            bool hasErrorChanged,
            object? value,
            BindingError? error)
        {
            Dispatcher.UIThread.VerifyAccess();
            Debug.Assert(instance.TargetProperty is not null);

            var property = instance.TargetProperty;

            if (property.IsDirect)
            {
                if (hasValueChanged)
                    property.RouteSetDirectValueUnchecked(Owner, value);
            }
            else
            {
                var priority = instance.Priority;

                if (hasValueChanged)
                {
                    if (priority == BindingPriority.LocalValue)
                    {
                        if (value != AvaloniaProperty.UnsetValue)
                            SetLocalValue(property, value);
                        else if (property == StyledElement.DataContextProperty)
                            SetLocalValue(property, null);
                        else
                            ClearValue(property);
                    }
                    else
                    {
                        if (TryGetEffectiveValue(property, out var existing))
                        {
                            if (priority <= existing.BasePriority)
                                ReevaluateEffectiveValue(property, existing, changedValueEntry: instance);
                        }
                        else
                        {
                            AddEffectiveValueAndRaise(property, instance, priority);
                        }
                    }
                }
            }

            if (hasErrorChanged && instance.IsDataValidationEnabled)
            {
                var e = error?.ErrorType.ToBindingValueType() ?? BindingValueType.Value;
                Owner.OnUpdateDataValidation(property, e, error?.Exception);
            }
        }

        /// <summary>
        /// Called by a binding expression when the binding produces completes.
        /// </summary>
        /// <param name="instance">The binding expression.</param>
        void IBindingExpressionSink.OnCompleted(UntypedBindingExpressionBase instance)
        {
            Dispatcher.UIThread.VerifyAccess();
            Debug.Assert(instance.TargetProperty is not null);

            var property = instance.TargetProperty;

            if (instance.IsDataValidationEnabled)
                Owner.OnUpdateDataValidation(property, BindingValueType.UnsetValue, null);

            if (instance.Priority == BindingPriority.LocalValue)
            {
                if (_localValueBindings is not null &&
                    _localValueBindings.TryGetValue(property.Id, out var existing))
                {
                    if (existing == instance)
                    {
                        _localValueBindings?.Remove(property.Id);
                        ClearValue(property);
                    }
                }
            }
        }

        public ValueStoreDiagnostic GetStoreDiagnostic()
        {
            var frames = new List<IValueFrameDiagnostic>();

            var effectiveLocalValues = new List<ValueEntryDiagnostic>(_effectiveValues.Count);
            for (var i = 0; i < _effectiveValues.Count; i++)
            {
                if (_effectiveValues.GetValue(i) is { } effectiveValue
                    && effectiveValue.Priority == BindingPriority.LocalValue)
                {
                    effectiveLocalValues.Add(new ValueEntryDiagnostic(effectiveValue.Property, effectiveValue.Value));
                }
            }

            if (effectiveLocalValues.Count > 0)
            {
                frames.Add(new LocalValueFrameDiagnostic(effectiveLocalValues));
            }

            foreach (var frame in Frames)
            {
                if (frame is StyleInstance { Source: StyleBase } styleInstance)
                {
                    frames.Add(new StyleValueFrameDiagnostic(styleInstance));
                }
                else
                {
                    frames.Add(new ValueFrameDiagnostic(frame));   
                }
            }

            return new ValueStoreDiagnostic(frames);
		}

        private int InsertFrame(ValueFrame frame)
        {
            Debug.Assert(!_frames.Contains(frame));

            var index = BinarySearchFrame(frame.FramePriority);
            _frames.Insert(index, frame);
            ++_frameGeneration;
            frame.SetOwner(this);
            return index;
        }

        private ImmediateValueFrame GetOrCreateImmediateValueFrame(
            AvaloniaProperty property,
            BindingPriority priority,
            out int frameIndex)
        {
            Debug.Assert(priority != BindingPriority.LocalValue);

            var index = BinarySearchFrame(priority.ToFramePriority());

            if (index > 0 && _frames[index - 1] is ImmediateValueFrame f &&
                f.Priority == priority &&
                !f.Contains(property))
            {
                frameIndex = index - 1;
                return f;
            }

            var result = new ImmediateValueFrame(priority);
            frameIndex = InsertFrame(result);
            return result;
        }

        private void AddEffectiveValue(AvaloniaProperty property, EffectiveValue effectiveValue)
        {
            _effectiveValues.Add(property, effectiveValue);

            if (property.Inherits && _inheritedValueCount++ == 0)
                OnInheritanceAncestorChanged(this);
        }

        /// <summary>
        /// Adds a new effective value, raises the initial <see cref="AvaloniaObject.PropertyChanged"/>
        /// event and notifies inheritance children if necessary .
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="entry">The value entry.</param>
        /// <param name="priority">The value priority.</param>
        private void AddEffectiveValueAndRaise(AvaloniaProperty property, IValueEntry entry, BindingPriority priority)
        {
            Debug.Assert(priority < BindingPriority.Inherited);
            var effectiveValue = property.CreateEffectiveValue(Owner);
            AddEffectiveValue(property, effectiveValue);
            effectiveValue.SetAndRaise(this, entry, priority);
        }

        private void RemoveEffectiveValue(AvaloniaProperty property, int index)
        {
            _effectiveValues.RemoveAt(index);
            if (property.Inherits && --_inheritedValueCount == 0)
                OnInheritanceAncestorChanged(InheritanceAncestor);
        }

        private bool RemoveEffectiveValue(AvaloniaProperty property)
        {
            if (_effectiveValues.Remove(property))
            {
                if (property.Inherits && --_inheritedValueCount == 0)
                    OnInheritanceAncestorChanged(InheritanceAncestor);
                return true;
            }

            return false;
        }

        private void InheritedValueChanged(
            AvaloniaProperty property,
            EffectiveValue? oldValue,
            EffectiveValue? newValue)
        {
            Debug.Assert(oldValue != newValue);
            Debug.Assert(oldValue is not null || newValue is not null);

            // If the value is set locally, propagation ends here.
            if (_effectiveValues.ContainsKey(property) == true)
                return;

            using var notifying = PropertyNotifying.Start(Owner, property);

            // Raise PropertyChanged on this object if necessary.
            (oldValue ?? newValue!).RaiseInheritedValueChanged(Owner, property, oldValue, newValue);

            var children = Owner.GetInheritanceChildren();

            if (children is null)
                return;

            var count = children.Count;

            for (var i = 0; i < count; ++i)
            {
                children[i].GetValueStore().InheritedValueChanged(property, oldValue, newValue);
            }
        }

        private void ReevaluateEffectiveValue(
            AvaloniaProperty property,
            EffectiveValue? current,
            IValueEntry? changedValueEntry = null,
            bool ignoreLocalValue = false)
        {
            ++_isEvaluating;

            try
            {
            restart:
                // Don't reevaluate if a styling pass is in effect, reevaluation will be done when
                // it has finished.
                if (_styling > 0)
                    return;

                var generation = _frameGeneration;

                // Notify the existing effective value that reevaluation is starting.
                current?.BeginReevaluation(ignoreLocalValue);

                // Iterate the frames to get the effective value.
                for (var i = _frames.Count - 1; i >= 0; --i)
                {
                    var frame = _frames[i];
                    var priority = frame.Priority;

                    // Exit early if the current EffectiveValue has higher priority than this frame.
                    if (current?.Priority < priority && current?.BasePriority < priority)
                        break;

                    // Try to get an entry from the frame for the property we're reevaluating.
                    var foundEntry = frame.TryGetEntryIfActive(property, out var entry, out var activeChanged);

                    // If the active state of the frame has changed since the last read, and
                    // the frame holds multiple values then we need to re-evaluate the
                    // effective values of all properties.
                    if (activeChanged && frame.EntryCount > 1)
                    {
                        ReevaluateEffectiveValues(changedValueEntry);
                        return;
                    }

                    // If the frame has an entry for this property with a higher priority than the
                    // current effective value (and that entry has a value), then we have a new 
                    // value for the property. Note that the check for entry.HasValue must be 
                    // evaluated last as it can cause bindings to be subscribed.
                    if (foundEntry &&
                        HasHigherPriority(entry!, priority, current, changedValueEntry) &&
                        entry!.HasValue())
                    {
                        if (current is not null)
                        {
                            current.SetAndRaise(this, entry, priority);
                        }
                        else
                        {
                            current = property.CreateEffectiveValue(Owner);
                            AddEffectiveValue(property, current);
                            current.SetAndRaise(this, entry, priority);
                        }
                    }

                    if (generation != _frameGeneration)
                        goto restart;
                }

                if (current is not null)
                {
                    current.EndReevaluation(this, property);

                    if (current.CanRemove())
                    {
                        if (current.BasePriority == BindingPriority.Unset)
                        {
                            RemoveEffectiveValue(property);
                            current.DisposeAndRaiseUnset(this, property);
                        }
                        else
                        {
                            current.RemoveAnimationAndRaise(this, property);
                        }
                    }

                    current.UnsubscribeIfNecessary();
                }
            }
            finally
            {
                --_isEvaluating;
            }
        }

        private void ReevaluateEffectiveValues(IValueEntry? changedValueEntry = null)
        {
            ++_isEvaluating;

            try
            {
            restart:
                // Don't reevaluate if a styling pass is in effect, reevaluation will be done when
                // it has finished.
                if (_styling > 0)
                    return;

                var generation = _frameGeneration;
                var count = _effectiveValues.Count;

                // Notify the existing effective values that reevaluation is starting.
                for (var i = 0; i < count; ++i)
                    _effectiveValues[i].BeginReevaluation();

                // Iterate the frames, setting and creating effective values.
                for (var i = _frames.Count - 1; i >= 0; --i)
                {
                    var frame = _frames[i];

                    if (!frame.IsActive())
                        continue;

                    var priority = frame.Priority;

                    count = frame.EntryCount;

                    for (var j = 0; j < count; ++j)
                    {
                        var entry = frame.GetEntry(j);
                        var property = entry.Property;
                        _effectiveValues.TryGetValue(property, out var effectiveValue);

                        if (!HasHigherPriority(entry, priority, effectiveValue, changedValueEntry))
                            continue;

                        if (!entry.HasValue())
                            continue;

                        if (effectiveValue is not null)
                        {
                            effectiveValue.SetAndRaise(this, entry, priority);
                        }
                        else
                        {
                            var v = property.CreateEffectiveValue(Owner);
                            AddEffectiveValue(property, v);
                            v.SetAndRaise(this, entry, priority);
                        }

                        if (generation != _frameGeneration)
                            goto restart;
                    }
                }

                // Remove all effective values that are still unset.
                for (var i = _effectiveValues.Count - 1; i >= 0; --i)
                {
                    var e = _effectiveValues.GetValue(i);
                    var property = e.Property;

                    e.EndReevaluation(this, property);

                    if (e.CanRemove())
                    {
                        RemoveEffectiveValue(property, i);
                        e.DisposeAndRaiseUnset(this, property);

                        if (i > _effectiveValues.Count)
                            break;
                    }

                    e.UnsubscribeIfNecessary();
                }
            }
            finally
            {
                --_isEvaluating;
            }
        }

        private static bool HasHigherPriority(
            IValueEntry entry,
            BindingPriority entryPriority,
            EffectiveValue? current,
            IValueEntry? changedValueEntry)
        {
            // Set the value if: there is no current effective value; or
            if (current is null)
                return true;

            // The value's priority is higher than the current effective value's priority; or
            if (entryPriority < current.Priority && entryPriority < current.BasePriority)
                return true;

            // - The value's priority is equal to the current effective value's priority
            // - But the effective value was set via SetCurrentValue
            // - As long as the SetCurrentValue wasn't overriding the value from the value entry under consideration
            // - Or if it was, the value entry under consideration has changed; or
            if (entryPriority == current.Priority &&
                current.IsOverridenCurrentValue &&
                (current.ValueEntry != entry || entry == changedValueEntry))
                return true;

            // The value is a non-animation value and its priority is higher than the current effective value's base
            // priority.
            if (entryPriority > BindingPriority.Animation && entryPriority < current.BasePriority)
                return true;

            return false;
        }

        private bool TryGetEffectiveValue(
            AvaloniaProperty property,
            [NotNullWhen(true)] out EffectiveValue? value)
        {
            return _effectiveValues.TryGetValue(property, out value);
        }

        private EffectiveValue? GetEffectiveValue(AvaloniaProperty property)
        {
            if (_effectiveValues.TryGetValue(property, out var value))
                return value;
            return null;
        }

        private object? GetDefaultValue(AvaloniaProperty property)
        {
            return ((IStyledPropertyAccessor)property).GetDefaultValue(Owner);
        }

        private void DisposeExistingLocalValueBinding(AvaloniaProperty property)
        {
            if (_localValueBindings is not null &&
                _localValueBindings.TryGetValue(property.Id, out var existing))
            {
                existing.Dispose();
            }
        }

        private int BinarySearchFrame(FramePriority priority)
        {
            var lo = 0;
            var hi = _frames.Count - 1;

            // Binary search insertion point.
            while (lo <= hi)
            {
                var i = lo + ((hi - lo) >> 1);
                var order = priority - _frames[i].FramePriority;

                if (order <= 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }

            return lo;
        }

        private readonly struct OldNewValue
        {
            public OldNewValue(EffectiveValue? oldValue)
            {
                OldValue = oldValue;
                NewValue = null;
            }

            public OldNewValue(EffectiveValue? oldValue, EffectiveValue? newValue)
            {
                OldValue = oldValue;
                NewValue = newValue;
            }

            public readonly EffectiveValue? OldValue;
            public readonly EffectiveValue? NewValue;

            public OldNewValue WithNewValue(EffectiveValue newValue) => new(OldValue, newValue);
        }
    }
}
