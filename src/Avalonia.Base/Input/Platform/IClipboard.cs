using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Input.Platform
{
    [NotClientImplementable]
    public interface IClipboard
    {
        /// <summary>
        /// Returns a string containing the text data on the Clipboard.
        /// </summary>
        /// <returns>A string containing text data in the specified data format, or an empty string if no corresponding text data is available.</returns>
        Task<string?> GetTextAsync();

        /// <summary>
        /// Stores text data on the Clipboard. The text data to store is specified as a string.
        /// </summary>
        /// <param name="text">A string that contains the UnicodeText data to store on the Clipboard.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="text"/> is null.</exception>
        Task SetTextAsync(string? text);

        /// <summary>
        /// Clears any data from the system Clipboard.
        /// </summary>
        Task ClearAsync();

        /// <summary>
        /// Places a specified non-persistent data object on the system Clipboard.
        /// </summary>
        /// <param name="data">A data object (an object that implements <see cref="IDataObject"/>) to place on the system Clipboard.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="data"/> is null.</exception>
        Task SetDataObjectAsync(IDataObject data);

        /// <summary>
        /// Permanently adds the data that is on the Clipboard so that it is available after the data's original application closes.
        /// </summary>
        /// <returns></returns>
        Task FlushAsync();

        /// <summary>
        /// Get list of available Clipboard format.
        /// </summary>
        Task<string[]> GetFormatsAsync();

        /// <summary>
        /// Retrieves data in a specified format from the Clipboard.
        /// </summary>
        /// <param name="format">A string that specifies the format of the data to retrieve. For a set of predefined data formats, see the <see cref="DataFormats"/> class.</param>
        /// <returns></returns>
        Task<object?> GetDataAsync(string format);
    }
}
