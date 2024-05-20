using System.Collections.Generic;
using SharpGen.Runtime;
using Vortice;
using Vortice.DirectWrite;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// Resource FontFileEnumerator.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="DWriteResourceFontFileEnumerator"/> class.
    /// </remarks>
    internal class DWriteResourceFontFileEnumerator : CallbackBase, IDWriteFontFileEnumerator
    {
        private readonly IDWriteFactory _factory;
        private readonly IDWriteFontFileLoader _loader;
        private readonly DataStream _keyStream;
        private IDWriteFontFile _currentFontFile;
        private static readonly HashSet<object> s_instances = new HashSet<object>();

        /// <param name="factory">The factory.</param>
        /// <param name="loader">The loader.</param>
        /// <param name="buffer">The key buffer.</param>
        /// <param name="size">The key size.</param>
        public DWriteResourceFontFileEnumerator(IDWriteFactory factory, IDWriteFontFileLoader loader, nint buffer, long size)
        {
            s_instances.Add(this);
            _factory = factory;
            _loader = loader;
            _keyStream = new DataStream(buffer, size, true, false);
        }

        /// <summary>
        /// Advances to the next font file in the collection. When it is first created, the enumerator is positioned before the first element of the collection and the first call to MoveNext advances to the first file.
        /// </summary>
        /// <returns>
        /// the value TRUE if the enumerator advances to a file; otherwise, FALSE if the enumerator advances past the last file in the collection.
        /// </returns>
        /// <unmanaged>HRESULT IDWriteFontFileEnumerator::MoveNext([Out] BOOL* hasCurrentFile)</unmanaged>
        RawBool IDWriteFontFileEnumerator.MoveNext()
        {
            bool moveNext = _keyStream.RemainingLength != 0;

            if (!moveNext)
                return false;

            _currentFontFile?.Dispose();

            _currentFontFile = _factory.CreateCustomFontFileReference(_keyStream.PositionPointer, 4, _loader);

            _keyStream.Position += 4;

            return true;
        }

        /// <summary>
        /// Gets a reference to the current font file.
        /// </summary>
        /// <value></value>
        /// <returns>a reference to the newly created <see cref="IDWriteFontFile"/> object.</returns>
        /// <unmanaged>HRESULT IDWriteFontFileEnumerator::GetCurrentFontFile([Out] IDWriteFontFile** fontFile)</unmanaged>
        IDWriteFontFile IDWriteFontFileEnumerator.GetCurrentFontFile()
        {
            _currentFontFile.AddRef();

            return _currentFontFile;
        }

        protected override void DisposeCore(bool disposing)
        {
            if (disposing)
            {
                s_instances.Remove(this);
            }
        }
    }
}
