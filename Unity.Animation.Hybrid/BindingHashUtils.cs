namespace Unity.Animation.Hybrid
{
    /// <summary>
    /// Delegate method used to override binding hash strategy
    /// </summary>
    /// <param name="path">Property path</param>
    /// <returns>Hash value</returns>
    public delegate uint BindingHashDelegate(string path);

    public static class BindingHashUtils
    {
        /// <summary>
        /// System wide default binding hash strategy. By default, the full property paths are hashed.
        /// </summary>
        public static BindingHashDelegate DefaultBindingHash = HashFullPath;

        /// <summary>
        /// Hashes the full property path
        /// </summary>
        /// <param name="path">Property path</param>
        /// <returns>Hash value</returns>
        public static uint HashFullPath(string path) =>
            StringHash.Hash(path);

        /// <summary>
        /// Hashes only the filename in the property path
        /// </summary>
        /// <param name="path">Property path</param>
        /// <returns>Hash value</returns>
        public static uint HashName(string path) =>
            StringHash.Hash(System.IO.Path.GetFileName(path));

        /// <summary>
        /// Builds binding property path
        /// </summary>
        /// <param name="path">Relative path from RigComponent</param>
        /// <param name="property">Property name</param>
        /// <returns></returns>
        internal static string BuildPath(string path, string property)
        {
            bool nullPath = string.IsNullOrEmpty(path);
            bool nullProperty = string.IsNullOrEmpty(property);

            if (nullPath && nullProperty)
                return string.Empty;
            if (nullPath)
                return property;
            if (nullProperty)
                return path;

            return path + "/" + property;
        }
    }
}
