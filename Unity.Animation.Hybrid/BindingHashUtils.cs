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
    }
}
