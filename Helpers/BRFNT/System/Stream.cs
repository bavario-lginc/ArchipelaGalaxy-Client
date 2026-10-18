using System;
using System.IO;
using System.Text;

namespace RevoFontEditor.System {
    public enum StreamEndian {
        Little = 0,
        Big = 1,
    }

    [Flags]
    public enum StreamConfig {
        None = 0,
        CanSeek = 1 << 0,
        CanRead = 1 << 1 | CanSeek,
        CanWrite = 1 << 2 | CanSeek,
        CanExpand = 1 << 3,

        Static = CanSeek | CanRead | CanWrite,
        All = Static | CanExpand,
    }

    public partial class UltraStream : Stream {
        private u8[] _buffer;
        private s32 _position;
        private s32 _length;
        private s32 _capacity;
        private s32 _allocBlockSize;
        private StreamEndian _endian;
        private StreamConfig _config;
        private Encoding _encoding;
        private bool _isOpen;

        /// <summary>
        /// Gets or sets the current position within the stream.
        /// </summary>
        public override s64 Position {
            get => _position;
            set => Seek(value, SeekOrigin.Begin);
        }

        /// <summary>
        /// The length of this stream in bytes.
        /// </summary>
        public override s64 Length {
            get => _length;
        }

        public s32 Capacity {
            get => _capacity;
            set {
                EnsureCanExpand();
                ArgumentOutOfRangeException.ThrowIfLessThan(value, _length, nameof(Capacity));

                if (_capacity == value)
                    return;

                if (_length > 0) {
                    Array.Resize(ref _buffer, value);
                }
                else {
                    _buffer = new u8[value];
                }
                
                _capacity = value;
            }
        }

        public s32 AllocBlockSize {
            get => _allocBlockSize;
            set {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, nameof(AllocBlockSize));

                if (!Util.IsPowerOfTwo(value)) {
                    throw new ArgumentException($"{nameof(AllocBlockSize)} must be a multiple of 2.");
                }

                _allocBlockSize = value;
            }
        }

        /// <summary>
        /// The byte order of data when reading/writting primitive types.
        /// </summary>
        public StreamEndian Endian {
            get => _endian;
            set => _endian = value;
        }

        /// <summary>
        /// If this stream supports reading.
        /// </summary>
        public override bool CanRead {
            get => _isOpen && (_config & StreamConfig.CanRead) != 0;
        }

        /// <summary>
        /// If this stream supports writting.
        /// </summary>
        public override bool CanWrite {
            get => _isOpen && (_config & StreamConfig.CanWrite) != 0;
        }

        /// <summary>
        /// If this stream supports seeking.
        /// </summary>
        public override bool CanSeek {
            get => _isOpen && (_config & StreamConfig.CanSeek) != 0;
        }

        /// <summary>
        /// If this stream's underlying buffer is expandable.
        /// </summary>
        public bool CanExpand {
            get => _isOpen && (_config & StreamConfig.CanExpand) != 0;
        }

        /// <summary>
        /// Default encoding to use when reading/writting strings.
        /// </summary>
        public Encoding DefaultEncoding {
            get => _encoding;
            set => _encoding = value;
        }

        /// <summary>
        /// Creates a new <see cref="UltraStream"/> with an expandable underlying buffer.
        /// </summary>
        /// <param name="capacity">The initial allocated size for this stream.</param>
        /// <param name="endian">The byte order of this stream's data.</param>
        /// <param name="config">This stream's configuration. Defines if this stream can be read from, written to, and more.</param>
        /// <param name="encoding">This stream's default text encoding. Used for string IO.</param>
        public UltraStream(s32 capacity = 0, StreamEndian endian = StreamEndian.Little, StreamConfig config = StreamConfig.All, Encoding? encoding = null)
            : this(Util.CreateArray<u8>(capacity), 0, 0, capacity, endian, config, encoding ?? Encoding.UTF8) { }

        /// <summary>
        /// Creates a new <see cref="UltraStream"/> with a user-provided underlying buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read/write data from/to.</param>
        /// <param name="endian">The byte order of this stream's data.</param>
        /// <param name="config">This stream's configuration. Defines if this stream can be read from, written to, and more.</param>
        /// <param name="encoding">This stream's default text encoding. Used for string IO.</param>
        public UltraStream(u8[] buffer, StreamEndian endian = StreamEndian.Little, StreamConfig config = StreamConfig.Static, Encoding? encoding = null)
            : this(buffer, 0, buffer.Length, buffer.Length, endian, config, encoding ?? Encoding.UTF8) { }

        private UltraStream(u8[] buffer, s32 position, s32 length, s32 capacity, StreamEndian endian, StreamConfig config, Encoding encoding) {
            ArgumentNullException.ThrowIfNull(buffer, nameof(buffer));

            _buffer = buffer;
            _position = position;
            _length = length;
            _capacity = capacity;
            _allocBlockSize = 256;
            _endian = endian;
            _config = config;
            _encoding = encoding;
            _isOpen = true;
        }

        /// <summary>
        /// Creates a new <see cref="UltraStream"/> with the contents of another stream.
        /// </summary>
        /// <param name="sourceStream">The stream to copy the contents from.</param>
        /// <param name="endian">The byte order of this stream's data.</param>
        /// <param name="config">This stream's configuration. Defines if this stream can be read from, written to, and more.</param>
        /// <param name="encoding">This stream's default text encoding. Used for string IO.</param>
        /// <returns>The created <see cref="UltraStream"/>, with the same contents as <paramref name="sourceStream"/>.</returns>
        public static UltraStream CreateFrom(Stream sourceStream, StreamEndian endian = StreamEndian.Little, StreamConfig config = StreamConfig.All, Encoding? encoding = null) {
            var stream = new UltraStream((s32)sourceStream.Length, endian, config, encoding);
            var oldPos = sourceStream.Position;

            sourceStream.Position = 0;
            sourceStream.CopyTo(stream);
            sourceStream.Position = oldPos;
            stream.Position = 0;

            return stream;
        }

        /// <summary>
        /// Sets <see cref="Endian"/> to the opposite byte order.
        /// </summary>
        public void FlipEndian() {
            _endian = _endian == StreamEndian.Little ? StreamEndian.Big : StreamEndian.Little;
        }

        /// <summary>
        /// Gets the underlying buffer of this stream.
        /// </summary>
        /// <returns>The underlying buffer.</returns>
        public u8[] GetBuffer() {
            return _buffer;
        }

        /// <summary>
        /// Gets a span from the stream's internal buffer.
        /// </summary>
        /// <remarks>
        /// No safety checking is performed in this method.
        /// </remarks>
        /// <param name="position">The position to slice the buffer from.</param>
        /// <param name="length">The byte size of the slice.</param>
        /// <returns>A span representing a section of buffer data.</returns>
        public Span<u8> GetSpan(s32 position, s32 length) {
            return _buffer.AsSpan().Slice(position, length);
        }

        public bool EnsureCapacity(s32 value) {
            if (value > _capacity) {
                EnsureCanExpand();
                EnsureNoPositionOverflow(value);

                var newCapacity = _capacity;

                while (newCapacity < value) {
                    newCapacity += _allocBlockSize;
                }

                if (newCapacity < 0 || newCapacity > s32.MaxValue) {
                    newCapacity = s32.MaxValue;
                }

                Capacity = (s32)newCapacity;
                return true;
            }

            return false;
        }

        public override void SetLength(s64 value) {
            if (value < _length) {
                throw new IOException("Input cannot be smaller than the current stream length.");
            }

            if (value > s32.MaxValue) {
                throw new IOException("Input cannot be greater than 2^31-1.");
            }

            if (_length > _capacity) {
                EnsureCapacity(_length);
            }

            _length = (s32)value;
        }

        public override void Flush() {

        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _isOpen = false;
            }
        }

        private void EnsureOpen() {
            ObjectDisposedException.ThrowIf(!_isOpen, this);
        }

        private void EnsureSeekable() {
            EnsureOpen();

            if (!CanSeek) {
                throw new InvalidOperationException("Stream is not seekable.");
            }
        }

        private void EnsureReadable() {
            EnsureOpen();

            if (!CanRead) {
                throw new InvalidOperationException("Stream is not readable.");
            }
        }

        private void EnsureWritable() {
            EnsureOpen();

            if (!CanWrite) {
                throw new InvalidOperationException("Stream is not writtable.");
            }
        }

        private void EnsureCanExpand() {
            EnsureOpen();

            if (!CanExpand) {
                throw new InvalidOperationException("This stream is not expandable.");
            }
        }

        private static void EnsureValidArgs(byte[] buffer, s32 offset, s32 count) {
            ArgumentNullException.ThrowIfNull(buffer, nameof(buffer));
            ArgumentOutOfRangeException.ThrowIfNegative(offset, nameof(offset));
            ArgumentOutOfRangeException.ThrowIfNegative(count, nameof(count));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - offset, nameof(count));
        }

        private static void EnsureNoPositionOverflow(s64 pos) {
            if (pos < 0) {
                throw new IOException($"Position overflow ({pos} is smaller than 0).");
            }

            if (pos > s32.MaxValue) {
                throw new IOException($"Position overflow ({pos} is greater than 2^31-1).");
            }
        }

        private void EnsurePositionInBounds(s64 pos) {
            EnsureNoPositionOverflow(pos);

            if (pos > _length) {
                throw new IOException($"Position is outside stream bounds ({pos} >= {_length}).");
            }
        }
    }
}
