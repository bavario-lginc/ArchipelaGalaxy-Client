using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace RevoFontEditor.System {
    public static class Util {
        private static readonly string _configPath = Path.Join(AppContext.BaseDirectory, "Config");
        private static readonly string _dbgPath = Path.GetFullPath("../../../dbg/");
        private static Encoding _defaultEncoding = Encoding.UTF8;

        /// <summary>
        /// The default encoding used for hash calculations.
        /// </summary>
        public static Encoding DefaultEncoding {
            get => _defaultEncoding;
            set => _defaultEncoding = value;
        }

        /// <summary>
        /// The default byte ordering of the user's device.
        /// </summary>
        public readonly static StreamEndian NativeEndian = BitConverter.IsLittleEndian ? StreamEndian.Little : StreamEndian.Big;

        /// <summary>
        /// Checks if the specified byte order is native to the user's device. 
        /// </summary>
        /// <param name="endian">The endianness to check.</param>
        /// <returns>If the specified endianness matches the current device's native byte ordering.</returns>
        public static bool IsNativeEndian(StreamEndian endian) {
            return endian == NativeEndian;
        }

        /// <summary>
        /// Allocates a new array with the specified element count. If <paramref name="num"/> is smaller than or equal to 0, an empty array will be created.
        /// </summary>
        /// <typeparam name="T">The array data type.</typeparam>
        /// <param name="num">The number of objects to allocate.</param>
        /// <returns>The allocated array.</returns>
        public static T[] CreateArray<T>(s32 num) {
            return num <= 0 ? [] : new T[num];
        }

        /// <summary>
        /// Reads an unmanaged type from an array.
        /// </summary>
        /// <typeparam name="T">The type to be read.</typeparam>
        /// <param name="buffer">The buffer to read the value from.</param>
        /// <param name="pos">The position in the buffer to read from.</param>
        /// <param name="endian">The byte order to read the data in.</param>
        /// <returns>The unmanaged value.</returns>
        public static T ReadFromArray<T>(u8[] buffer, s32 pos, StreamEndian endian = StreamEndian.Little) where T : unmanaged {
            var value = new T();
            var valueSpan = SpanFromRef(ref value);

            buffer.AsSpan()
                .Slice(pos, Unsafe.SizeOf<T>())
                .CopyTo(valueSpan);

            if (!IsNativeEndian(endian)) {
                valueSpan.Reverse();
            }

            return value;
        }

        /// <summary>
        /// Creates a span of bytes from a reference to an unmanaged type.
        /// </summary>
        /// <typeparam name="T">The unmanaged type of the provided value.</typeparam>
        /// <param name="val">The value reference.</param>
        /// <returns>A span of bytes referencing the byte contents of this reference.</returns>
        public static Span<u8> SpanFromRef<T>(scoped ref T val) where T : unmanaged {
            return MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref val, 1));
        }

        /// <summary>
        /// Gets file information from a file inside the application's 'Config/' directory.
        /// </summary>
        /// <param name="name">The name of the file, relative to the 'Config/' directory.</param>
        /// <param name="ensureExists">Should this method throw an exception if the file does not exist?</param>
        /// <returns>The <see cref="FileInfo"/> of the path.</returns>
        /// <exception cref="FileNotFoundException">Thrown if <paramref name="ensureExists"/> is <see cref="true"/> and the file does not exist.</exception>
        public static FileInfo GetConfigFile(string name, bool ensureExists = false) {
            var fi = new FileInfo(Path.Join(_configPath, name));

            if (ensureExists && !fi.Exists) {
                throw new FileNotFoundException("The specified config file was not found.", fi.FullName);
            }

            return fi;
        }

        /// <summary>
        /// Gets file information from a file inside the source code's 'dbg/' directory.
        /// </summary>
        /// <param name="name">The name of the file, relative to the 'dbg/' directory.</param>
        /// <param name="ensureExists">If this method throw an exception if the file does not exist.</param>
        /// <returns>The <see cref="FileInfo"/> of the path.</returns>
        /// <exception cref="FileNotFoundException">Thrown if <paramref name="ensureExists"/> is <see cref="true"/> and the file does not exist.</exception>
        public static FileInfo GetDebugFile(string name, bool ensureExists = false) {
            var fi = new FileInfo(Path.Join(_dbgPath, name));

            if (ensureExists && !fi.Exists) {
                throw new FileNotFoundException("The specified debug file was not found.", fi.FullName);
            }

            return fi;
        }

        /// <summary>
        /// Gets directory information from a directory inside the source code's 'dbg/' directory.
        /// </summary>
        /// <param name="name">The name of the directory, relative to the 'dbg/' directory.</param>
        /// <param name="ensureExists">If this method throw an exception if the directory does not exist.</param>
        /// <returns>The <see cref="FileInfo"/> of the path.</returns>
        /// <exception cref="FileNotFoundException">Thrown if <paramref name="ensureExists"/> is <see cref="true"/> and the file does not exist.</exception>
        public static DirectoryInfo GetDebugDirectory(string name, bool ensureExists = false) {
            var fi = new DirectoryInfo(Path.Join(_dbgPath, name));

            if (ensureExists && !fi.Exists) {
                throw new FileNotFoundException("The specified debug directory was not found.", fi.FullName);
            }

            return fi;
        }

        /// <summary>
        /// Formats a path to a fatal exception log file.
        /// </summary>
        /// <param name="time">The time of the exception.</param>
        /// <returns>The file path.</returns>
        public static string GetFatalDumpPath(DateTime time) {
            return Path.Join(AppContext.BaseDirectory, $"Fatal_{time:dd.MM.yyyy_HH.mm.ss}.txt");
        }

        /// <summary>
        /// Gets the <see cref="SingletonHolder{T}"/>'s instance of a type.
        /// </summary>
        /// <typeparam name="T">The singleton type.</typeparam>
        /// <returns>The singleton instance, or null.</returns>
        public static T? GetSingleton<T>() {
            return SingletonHolder<T>.Instance;
        }

        /// <summary>
        /// Sets the <see cref="SingletonHolder{T}"/>'s instance of a type.
        /// </summary>
        /// <typeparam name="T">The singleton type.</typeparam>
        /// <param name="val">The new singleton instance.</param>
        public static void SetSingleton<T>(T? val) {
            SingletonHolder<T>.Instance = val;
        }

        /// <summary>
        /// Calculates a JGadget hash code.
        /// </summary>
        /// <param name="s">The string to calculate the hash from.</param>
        /// <param name="enc">The encoding to get string bytes from. If no encoding is specified, <see cref="DefaultEncoding"/> will be used.</param>
        /// <returns>The hash code.</returns>
        public static u32 CalcHashCode(string s, Encoding? enc = null) {
            enc ??= DefaultEncoding;
            u32 v = 0;

            foreach (var b in enc.GetBytes(s)) {
                v = (u32)((s8)b + (v * 31));
            }

            return v;
        }

        /// <summary>
        /// Checks if an integer is a power of two.
        /// </summary>
        /// <remarks>A value of zero will return true.</remarks>
        /// <param name="val">The value to check.</param>
        /// <returns>If this value is a power of 2.</returns>
        public static bool IsPowerOfTwo(s64 val) {
            return (val & (val - 1u)) == 0;
        }

        /// <summary>
        /// Aligns a number to another value.
        /// </summary>
        /// <typeparam name="T">Number type.</typeparam>
        /// <param name="val">The value to align.</param>
        /// <param name="align">Target alignment. Must be a multiple of 32.</param>
        /// <returns>The aligned value.</returns>
        public static T Align<T>(T val, T align) where T : INumber<T>, IBitwiseOperators<T, T, T> {
            align--;
            return (val + align) & ~align;
        }
    }
}
