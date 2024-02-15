using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Data;

namespace Avalonia.Controls;

public abstract class ItemSorter : ItemsSourceViewLayer, IComparer
{
    public abstract int Compare(object? x, object? y);
}
