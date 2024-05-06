﻿using System.Threading;
using Avalonia.Metadata;

namespace Avalonia.Controls.Notifications
{
    /// <summary>
    /// Represents a notification manager that can be used to show notifications in a window or using
	/// the host operating system.
    /// </summary>
    [NotClientImplementable]
    public interface INotificationManager
    {
        /// <summary>
        /// Show a notification.
        /// </summary>
        /// <param name="notification">The notification to be displayed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void Show(INotification notification, CancellationToken? cancellationToken = null);

        /// <summary>
        /// Closes a notification.
        /// </summary>
        /// <param name="notification">The notification to be closed.</param>
        void Close(INotification notification);

        /// <summary>
        /// Clears all notifications.
        /// </summary>
        void ClearAll();
    }
}
