// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace Avalonia.Threading
{
    /// <summary>
    /// Generic implementation of object pooling pattern.
    /// Uses object default constructor to create new instances.
    /// </summary>
    /// <typeparam name="T">Object type.</typeparam>
    public class ThreadSafeObjectPool<T> where T : class, new()
    {
        private readonly Stack<T> _stack = new Stack<T>();
        private readonly object _lock = new object();

        /// <summary>
        /// Default object pool.
        /// </summary>
        public static ThreadSafeObjectPool<T> Default { get; } = new ThreadSafeObjectPool<T>();

        /// <summary>
        /// Gets new object from the pool. If there is none a new instance will be allocated.
        /// </summary>
        /// <returns>New object.</returns>
        public T Get()
        {
            lock (_lock)
            {
                if (_stack.Count == 0)
                    return new T();
                return _stack.Pop();
            }
        }

        /// <summary>
        /// Return object to the pool.
        /// </summary>
        /// <param name="obj">Object to return.</param>
        public void Return(T obj)
        {
            lock (_lock)
            {
                _stack.Push(obj);
            }
        }
    }
}
