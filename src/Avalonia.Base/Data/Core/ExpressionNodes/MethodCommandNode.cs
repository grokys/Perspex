﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using Avalonia.Data.Converters;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in an <see cref="BindingExpression"/> which converts methods to an
/// <see cref="ICommand"/>.
/// </summary>
internal sealed class MethodCommandNode : ExpressionNode
{
    private readonly string _methodName;
    private readonly Action<object, object?> _execute;
    private readonly Func<object, object?, bool>? _canExecute;
    private readonly ISet<string> _dependsOnProperties;
    private Command? _command;
    private MethodToCommandConverter.WeakPropertyChangedProxy? _weakPropertyChanged;

    public MethodCommandNode(
        string methodName,
        Action<object, object?> execute,
        Func<object, object?, bool>? canExecute,
        ISet<string> dependsOnProperties)
    {
        _methodName = methodName;
        _execute = execute;
        _canExecute = canExecute;
        _dependsOnProperties = dependsOnProperties;
    }

    public override void BuildString(StringBuilder builder)
    {
        if (builder.Length > 0 && builder[builder.Length - 1] != '!')
            builder.Append('.');
        builder.Append(_methodName);
        builder.Append("()");
    }

    protected override void OnSourceChanged(object source, Exception? dataValidationError)
    {
        if (source is INotifyPropertyChanged newInpc)
        {
            _weakPropertyChanged = new MethodToCommandConverter.WeakPropertyChangedProxy(newInpc, OnPropertyChanged);
        }

        _command = new Command(source, _execute, _canExecute);
        SetValue(_command);
    }

    protected override void Unsubscribe(object oldSource)
    {
        if (oldSource is INotifyPropertyChanged)
        {
            _weakPropertyChanged?.Unsubscribe();
            _weakPropertyChanged = null;
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || _dependsOnProperties.Contains(e.PropertyName))
        {
            _command?.RaiseCanExecuteChanged();
        }
    }

    private sealed class Command : ICommand
    {
        private readonly WeakReference<object?> _target;
        private readonly Action<object, object?> _execute;
        private readonly Func<object, object?, bool>? _canExecute;

        public event EventHandler? CanExecuteChanged;

        public Command(object? target, Action<object, object?> execute, Func<object, object?, bool>? canExecute)
        {
            _target = new(target);
            _execute = execute;
            _canExecute = canExecute;
        }

        public void RaiseCanExecuteChanged()
        {
            Threading.Dispatcher.UIThread.Post(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty)
               , Threading.DispatcherPriority.Input);
        }

        public bool CanExecute(object? parameter)
        {
            if (_target.TryGetTarget(out var target))
            {
                if (_canExecute == null)
                {
                    return true;
                }
                return _canExecute(target, parameter);
            }
            return false;
        }

        public void Execute(object? parameter)
        {
            if (_target.TryGetTarget(out var target))
            {
                _execute(target, parameter);
            }
        }
    }

}
