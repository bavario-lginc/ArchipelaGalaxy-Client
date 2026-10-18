using System;
using System.IO;

namespace RevoFontEditor.System {
    public partial class UltraStream : Stream {
        /// <summary>
        /// Writes an unmanaged value to the stream and advances the position within the stream by the number of bytes written.
        /// </summary>
        /// <typeparam name="T">Unmanaged data type.</typeparam>
        /// <param name="val">The value to write.</param>
        /// <param name="endian">An optional byte ordering specification. If no endian is specified, <see cref="Endian"/> will be used.</param>
        public void WriteValue<T>(T val, StreamEndian? endian = null) where T : unmanaged {
            EnsureWritable();

            var span = Util.SpanFromRef(ref val);

            if (!Util.IsNativeEndian(endian ?? _endian)) {
                span.Reverse();
            }

            Write(span);
        }

        /// <summary>
        /// Writes a sequence of bytes from to current stream and advances the position within the stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">The buffer of the data to store.</param>
        /// <param name="offset">The byte offset in buffer at which to begin writting the data to the stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(u8[] buffer, s32 offset, s32 count) {
            EnsureWritable();
            EnsureValidArgs(buffer, offset, count);

            var i = _position + count;
            EnsureNoPositionOverflow(i);

            if (i > _length) {
                EnsureCapacity(i);
                _length = i;
            }

            if ((count <= 8) && (buffer != _buffer)) {
                var bytesLeft = count;

                while (--bytesLeft >= 0) {
                    _buffer[_position + bytesLeft] = buffer[offset + bytesLeft];
                }
            }
            else {
                Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
            }

            _position = i;
        }

        /// <summary>
        /// Writes a sequence of bytes from to current stream and advances the position within the stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">The span to store the data to.</param>
        public override void Write(ReadOnlySpan<u8> buffer) {
            EnsureWritable();

            var i = _position + buffer.Length;
            EnsureNoPositionOverflow(i);

            if (i > _length) {
                EnsureCapacity(i);
                _length = i;
            }

            buffer.CopyTo(GetSpan(_position, buffer.Length));
            _position = i;
        }
    }
}
