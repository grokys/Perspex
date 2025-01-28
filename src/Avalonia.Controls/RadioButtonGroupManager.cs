using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Collections.Pooled;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Rendering;

namespace Avalonia.Controls;

internal interface IRadioButton : ILogical
{
    string? GroupName { get; }
    MenuItemToggleType ToggleType { get; }
    bool IsChecked { get; set; }
}

internal class RadioButtonGroupManager
{
    private static readonly ConditionalWeakTable<IRenderRoot, RadioButtonGroupManager> s_registeredVisualRoots = new();

    private readonly Dictionary<string, List<WeakReference<IRadioButton>>> _registeredGroups = new();
    private bool _ignoreCheckedChanges;

    public static RadioButtonGroupManager GetOrCreateForRoot(IRenderRoot root)
    {
        return s_registeredVisualRoots.GetValue(root, key => new RadioButtonGroupManager());
    }

    public void Add(IRadioButton radioButton)
    {
        var groupName = radioButton.GroupName;
        if (groupName is not null && radioButton.ToggleType == MenuItemToggleType.Radio)
        {
            if (!_registeredGroups.TryGetValue(groupName, out var group))
            {
                group = new List<WeakReference<IRadioButton>>();
                _registeredGroups.Add(groupName, group);
            }

            foreach(var item in group)
            {
                if (!item.TryGetTarget(out var button))
                {
                    continue;
                }

                //Return if the button is already in the group
                if (button == radioButton)
                {
                    return;
                }
            }
            group.Add(new WeakReference<IRadioButton>(radioButton));
        }
    }

    public void Remove(IRadioButton radioButton, string? oldGroupName)
    {
        if (!string.IsNullOrEmpty(oldGroupName) && _registeredGroups.TryGetValue(oldGroupName, out var group))
        {
            int i = 0;
            while (i < group.Count)
            {
                if (!group[i].TryGetTarget(out var button) || button == radioButton)
                {
                    group.RemoveAt(i);
                    continue;
                }

                i++;
            }

            if (group.Count == 0)
            {
                _registeredGroups.Remove(oldGroupName);
            }
        }
    }

    public void OnCheckedChanged(IRadioButton radioButton)
    {
        if (_ignoreCheckedChanges || radioButton.ToggleType != MenuItemToggleType.Radio)
        {
            return;
        }

        _ignoreCheckedChanges = true;
        try
        {
            var groupName = radioButton.GroupName;
            if (!string.IsNullOrEmpty(groupName))
            {
                if (_registeredGroups.TryGetValue(groupName, out var group))
                {
                    var i = 0;
                    while (i < group.Count)
                    {
                        if (!group[i].TryGetTarget(out var current))
                        {
                            group.RemoveAt(i);
                            continue;
                        }

                        if (current != radioButton && current.IsChecked)
                            current.IsChecked = false;
                        i++;
                    }

                    if (group.Count == 0)
                    {
                        _registeredGroups.Remove(groupName);
                    }

                    var parent = radioButton.LogicalParent as IRadioButton;
                    while (parent is not null && parent.GroupName == groupName)
                    {
                        parent.IsChecked = true;
                        parent = parent.LogicalParent as IRadioButton;
                    }
                }
            }
            else
            {
                if (radioButton.LogicalParent is { } parent)
                {
                    foreach (var sibling in parent.LogicalChildren)
                    {
                        if (sibling != radioButton
                            && sibling is IRadioButton { ToggleType: MenuItemToggleType.Radio } button
                            && string.IsNullOrEmpty(button.GroupName)
                            && button.IsChecked)
                        {
                            button.IsChecked = false;
                        }
                    }
                }
            }
        }
        finally
        {
            _ignoreCheckedChanges = false;
        }
    }
}
