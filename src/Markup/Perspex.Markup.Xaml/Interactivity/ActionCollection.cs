﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Perspex.Xaml.Interactivity
{
    using System;
    using System.Collections.Specialized;
    using Perspex.Collections;

    /// <summary>
    /// Represents a collection of IActions.
    /// </summary>
    public sealed class ActionCollection : PerspexList<PerspexObject>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActionCollection"/> class.
        /// </summary>
        public ActionCollection()
        {
            this.CollectionChanged += ActionCollection_CollectionChanged;
        }

        private void ActionCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs eventArgs)
        {
            NotifyCollectionChangedAction collectionChange = eventArgs.Action;

            if (collectionChange == NotifyCollectionChangedAction.Reset)
            {
                foreach (PerspexObject item in this)
                {
                    VerifyType(item);
                }
            }
            else if (collectionChange == NotifyCollectionChangedAction.Add || collectionChange == NotifyCollectionChangedAction.Replace)
            {
                PerspexObject changedItem = this[eventArgs.NewStartingIndex];
                VerifyType(changedItem);
            }
        }

        private static void VerifyType(PerspexObject item)
        {
            if (!(item is IAction))
            {
                // TODO: Replace string from original resources
                throw new InvalidOperationException("NonActionAddedToActionCollectionExceptionMessage");
            }
        }
    }
}
