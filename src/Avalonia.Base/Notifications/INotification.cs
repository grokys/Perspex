﻿// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Avalonia.Notifications
{
    /// <summary>
    /// Represents a notification that can be shown in a window or by the host operating system.
    /// </summary>
    [PublicAPI]
    public interface INotification
    {
        /// <summary>
        /// The id for <see cref="INotificationManager"/> control
        /// </summary>
        uint? Id { get; }

        /// <summary>
        /// Gets the Title of the notification.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Gets the notification message.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets the <see cref="NotificationType"/> of the notification.
        /// </summary>
        NotificationType Type { get; }

        /// <summary>
        /// Gets the expiration time of the notification after which it will automatically close.
        /// If the value is <see cref="TimeSpan.Zero"/> then the notification will remain open until the user closes it.
        /// </summary>
        TimeSpan Expiration { get; }

        /// <summary>
        /// Gets an Action to be run when the notification is clicked.
        /// </summary>
        Action OnClick { get; }

        /// <summary>
        /// Gets an Action to be run when the notification is closed.
        /// </summary>
        Action OnClose { get; }

        /// <summary>
        /// Closes this notification
        /// </summary>
        Task CloseAsync();

        /// <inheritdoc cref="CloseAsync"/>
        void Close();

        /// <summary>
        /// Clones this notification
        /// </summary>
        INotification Clone();

        /// <summary>
        /// Sets the id of the notification
        /// </summary>
        void SetId(uint id, INotificationManager notificationManager);
    }
}
