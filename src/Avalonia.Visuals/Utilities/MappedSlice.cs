// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/

using System;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities
{
    /// <summary>
    /// Provides a mapped view of an underlying slice, selecting arbitrary indices
    /// from the source array.
    /// </summary>
    /// <typeparam name="T">The type of item contained in the underlying array.</typeparam>
    internal readonly struct MappedSlice<T>
        where T : struct
    {
        private readonly Slice<T> _data;
        private readonly Slice<int> _map;

        /// <summary>
        /// Initializes a new instance of the <see cref="MappedSlice{T}"/> struct.
        /// </summary>
        /// <param name="data">The data slice.</param>
        /// <param name="map">The map slice.</param>
        public MappedSlice(in Slice<T> data, in Slice<int> map)
        {
            if (map.Length < data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof (map));
            }

            _data = data;
            _map = map;
        }

        /// <summary>
        /// Gets the number of items in the map.
        /// </summary>
        public int Length => _map.Length;

        /// <summary>
        /// Returns a reference to specified element of the slice.
        /// </summary>
        /// <param name="index">The index of the element to return.</param>
        /// <returns>The <typeparamref name="T"/>.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to <see cref="Length"/>.
        /// </exception>
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _data[_map[index]];
        }
    }
}
