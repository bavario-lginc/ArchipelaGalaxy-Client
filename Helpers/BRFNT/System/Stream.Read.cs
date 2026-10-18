using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace RevoFontEditor.System {
    public partial class UltraStream : Stream {
        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">The buffer to store the data in.</param>
        /// <param name="offset">The byte offset in buffer at which to begin storing the data read from the stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested.</returns>
        public override s32 Read(u8[] buffer, s32 offset, s32 count) {
            EnsureReadable();
            EnsureValidArgs(buffer, offset, count);

            var n = Math.Min(_length - _position, buffer.Length);
            if (n <= 0) {
                return 0;
            }

            return ReadCore(buffer.AsSpan().Slice(offset, n), n);
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">The span to store the data in.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested.</returns>
        public override s32 Read(Span<u8> buffer) {
            EnsureReadable();

            var n = Math.Min(_length - _position, buffer.Length);
            if (n <= 0) {
                return 0;
            }

            return ReadCore(buffer, n);
        }

        /// <summary>
        /// Reads an unmanaged type from the current stream with the specified endianness and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <typeparam name="T">Unmanaged data type.</typeparam>
        /// <param name="endian">An optional byte ordering specification. If no endian is specified, <see cref="Endian"/> will be used.</param>
        /// <returns>The data that was read from the stream.</returns>
        public T ReadValue<T>(StreamEndian? endian = null) where T : unmanaged {
            var val = new T();
            ReadValue(ref val, endian);
            return val;
        }

        /// <summary>
        /// Reads an unmanaged type from the current stream with the specified endianness, but does not advance the position within the stream.
        /// </summary>
        /// <typeparam name="T">Unmanaged data type.</typeparam>
        /// <param name="endian">An optional byte ordering specification. If no endian is specified, <see cref="Endian"/> will be used.</param>
        /// <returns>The data that was read from the stream.</returns>
        public T PeekValue<T>(StreamEndian? endian = null) where T : unmanaged {
            var val = new T();
            PeekValue(ref val, endian);
            return val;
        }

        /// <summary>
        /// Reads an unmanaged type from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <typeparam name="T">Unmanaged data type.</typeparam>
        /// <param name="val">A reference to the value to write the data to.</param>
        /// <param name="endian">An optional byte ordering specification. If no endian is specified, <see cref="Endian"/> will be used.</param>
        public void ReadValue<T>(ref T val, StreamEndian? endian = null) where T : unmanaged {
            EnsureReadable();

            var i = _position + Unsafe.SizeOf<T>();
            EnsurePositionInBounds(i);

            var span = Util.SpanFromRef(ref val);
            ReadCore(span, span.Length);

            if (!Util.IsNativeEndian(endian ?? _endian)) {
                span.Reverse();
            }
        }

        /// <summary>
        /// Reads an unmanaged type from the current stream, but does not advance the position within the stream.
        /// </summary>
        /// <typeparam name="T">Unmanaged data type.</typeparam>
        /// <param name="val">A reference to the value to write the data to.</param>
        /// <param name="endian">An optional byte ordering specification. If no endian is specified, <see cref="Endian"/> will be used.</param>
        public void PeekValue<T>(ref T val, StreamEndian? endian = null) where T : unmanaged {
            EnsureReadable();

            var oldPosition = _position;
            var i = oldPosition + Unsafe.SizeOf<T>();
            EnsurePositionInBounds(i);

            var span = Util.SpanFromRef(ref val);
            ReadCore(span, span.Length);

            if (!Util.IsNativeEndian(endian ?? _endian)) {
                span.Reverse();
            }

            _position = oldPosition;
        }

        /// <summary>
        /// fuck microsoft
        /// </summary>
        /// <returns>an actual fucking byte</returns>
        public new u8 ReadByte() {
            EnsureReadable();
            return _buffer[_position++];
        }

        /// <summary>
        /// Reads a string with a fixed size.
        /// </summary>
        /// <param name="length">The size of the string.</param>
        /// <param name="enc">An encoding to use. If no encoding is specified, <see cref="DefaultEncoding"/> will be used.</param>
        /// <returns>The string that has been read.</returns>
        public string ReadString(s32 length, Encoding? enc = null) {
            EnsureReadable();
            enc ??= _encoding;

            var i = _position + length;
            EnsurePositionInBounds(i);

            var span = GetSpan(_position, length);
            _position = i;

            return enc.GetString(span);
        }

        /// <summary>
        /// Reads a null-terminated string in a specified encoding.
        /// </summary>
        /// <param name="enc">An encoding to use. If no encoding is specified, <see cref="DefaultEncoding"/> will be used.</param>
        /// <returns>The string that has been read.</returns>
        public string ReadNTString(Encoding? enc = null) {
            EnsureReadable();
            enc ??= _encoding;

            var nullCharSize = enc.GetByteCount("\0");
            var zeroNum = 0;
            var basePos = _position;

            while (zeroNum < nullCharSize) {
                if (ReadValue<u8>() == 0) {
                    zeroNum++;
                }
                else {
                    zeroNum = 0;
                }
            }

            if (basePos + nullCharSize == _position) {
                return string.Empty;
            }

            var span = GetSpan(basePos, _position - basePos);
            return enc.GetString(span);
        }

        private s32 ReadCore(Span<u8> buffer, s32 count) {
            if (count < 8) {
                var byteCount = count;

                while (--byteCount >= 0) {
                    buffer[byteCount] = _buffer[_position + byteCount];
                }
            }
            else {
                GetSpan(_position, count).CopyTo(buffer);
            }
            
            _position += count;
            return count;
        }
    }
}
