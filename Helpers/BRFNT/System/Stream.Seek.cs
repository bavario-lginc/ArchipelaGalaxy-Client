using SixLabors.ImageSharp.Memory;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace RevoFontEditor.System {
    public partial class UltraStream : Stream {
        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the specified <paramref name="origin"/>.</param>
        /// <param name="origin">The reference point used to obtain the new position.</param>
        /// <returns>The new position within the stream.</returns>
        public override s64 Seek(s64 offset, SeekOrigin origin) {
            EnsureSeekable();
            s64 tempPosition = 0;

            switch (origin) {
                case SeekOrigin.Begin:
                    tempPosition = offset;
                    break;
                case SeekOrigin.Current:
                    tempPosition = _position + offset;
                    break;
                case SeekOrigin.End:
                    tempPosition = _length + offset;
                    break;
            }

            EnsureNoPositionOverflow(tempPosition);
            return _position = (s32)tempPosition;
        }

        /// <summary>
        /// Skips over a specified number of bytes.
        /// </summary>
        /// <param name="offset">The number of bytes to skip. Can be negative.</param>
        /// <returns>The new position within the stream.</returns>
        public s64 Skip(s32 offset) {
            return Seek(offset, SeekOrigin.Current);
        }

        /// <summary>
        /// Writes zeros to align the current stream position to a power of 2.
        /// </summary>
        /// <param name="v">The target alignment.</param>
        public s64 Pad(s32 v) {
            var padded = (_position + v - 1) & ~(v - 1);
            var diff = padded - _position;

            if (diff > 0) {
                EnsureWritable();

                var i = _position + diff;
                EnsureNoPositionOverflow(i);

                if (i > _length) {
                    EnsureCapacity(i);
                    _length = i;
                }

                _position = i;
            }

            return padded;
        }

        /// <summary>
        /// Creates a <see cref="StreamSeek{T}"/> object that holds this stream's position.
        /// </summary>
        /// <returns>The newly created <see cref="StreamSeek{T}"/>. Should be disposed of later.</returns>
        public StreamSeek<UltraStream> TemporarySeek() {
            return new(this);
        }

        /// <summary>
        /// Creates a <see cref="StreamSeek{T}"/> object that holds this stream's position and sets its current position to a new offset.
        /// </summary>
        /// <returns>The newly created <see cref="StreamSeek{T}"/>. Should be disposed of later.</returns>
        public StreamSeek<UltraStream> TemporarySeek(s64 offset, SeekOrigin origin = SeekOrigin.Begin) {
            return new(this, offset, origin);
        }

        /// <summary>
        /// Creates a reference to the current position in the stream.
        /// </summary>
        /// <typeparam name="T">The data type of the reference.</typeparam>
        /// <param name="skip">If the data should be skipped over.</param>
        /// <returns>A reference to the current position in the stream.</returns>
        public StreamReference<T> CreateReference<T>(bool skip = true) where T : unmanaged {
            return new StreamReference<T>(this, _position, skip);
        }

        /// <summary>
        /// Creates a reference to the current position in the stream.
        /// </summary>
        /// <typeparam name="T">The data type of the reference.</typeparam>
        /// <param name="position">The position of this reference.</param>
        /// <param name="skip">If the data should be skipped over.</param>
        /// <returns>A reference to the current position in the stream.</returns>
        public StreamReference<T> CreateReference<T>(s64 position, bool skip = true) where T : unmanaged {
            return new StreamReference<T>(this, position, skip);
        }
    }

    /// <summary>
    /// Holds a stream and a position within said stream. Once disposed of, the stream held by this class will return to this position.
    /// </summary>
    /// <typeparam name="T">Any <see cref="Stream"/> derived type.</typeparam>
    public class StreamSeek<T> : IDisposable where T : Stream {
        private readonly T _stream;
        private readonly s64 _position;

        /// <summary>
        /// The position that the stream was in when this object was created. Once disposed of, the stream held by this class will return to this position.
        /// </summary>
        public s64 Position {
            get => _position;
        }

        /// <summary>
        /// Creates a <see cref="StreamSeek{T}"/> object that holds this stream's position.
        /// </summary>
        /// <param name="stream">The stream to hold the position of.</param>
        public StreamSeek(T stream) {
            _stream = stream;
            _position = stream.Position;
        }

        /// <summary>
        /// Creates a <see cref="StreamSeek{T}"/> object that holds this stream's position and sets its current position to a new offset.
        /// </summary>
        /// <param name="stream">The stream to hold the position of.</param>
        /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
        /// <param name="origin">A reference point used to obtain the new position.</param>
        public StreamSeek(T stream, s64 offset, SeekOrigin origin = SeekOrigin.Begin) {
            _stream = stream;
            _position = stream.Position;

            stream.Seek(offset, origin);
        }

        /// <summary>
        /// Restores the stream's position to its original position.
        /// </summary>
        public void Restore() {
            _stream.Seek(_position, SeekOrigin.Begin);
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
            Restore();
        }
    }

    /// <summary>
    /// A reference to a position in an <see cref="UltraStream{T}"/>.
    /// </summary>
    /// <typeparam name="T">The data type of the reference.</typeparam>
    public class StreamReference<T> where T : unmanaged {
        private readonly UltraStream _stream;
        private readonly s64 _position;

        /// <summary>
        /// The position of this data reference.
        /// </summary>
        public s64 Position {
            get => _position;
        }

        public StreamReference(UltraStream stream, s64 position, bool skip) {
            _stream = stream;
            _position = position;

            if (skip) {
                _stream.Skip(Unsafe.SizeOf<T>());
            }
        }

        public T Read() {
            using (_stream.TemporarySeek(_position)) {
                return _stream.ReadValue<T>();
            }
        }

        public void Write(T value) {
            using (_stream.TemporarySeek(_position)) {
                _stream.WriteValue<T>(value);
            }
        }
    }
}
