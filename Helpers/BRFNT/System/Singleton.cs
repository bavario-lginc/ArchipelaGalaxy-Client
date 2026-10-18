namespace RevoFontEditor.System {
    /// <summary>
    /// Holds a static instance of any type.
    /// </summary>
    /// <typeparam name="T">The singleton type.</typeparam>
    public static class SingletonHolder<T> {
        private static T? _instance;

        /// <summary>
        /// The instance held by <see cref="SingletonHolder{T}"/>.
        /// </summary>
        public static T? Instance {
            get => _instance;
            set => _instance = value;
        }
    }
}
