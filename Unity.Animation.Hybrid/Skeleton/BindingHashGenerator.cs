namespace Unity.Animation.Authoring
{
    /// <summary>
    /// Interface that represents a hash generator for animation bindings.
    /// This is used to convert authoring bindings to RigDefinition unique IDs.
    /// </summary>
    public interface IBindingHashGenerator
    {
        /// <summary>
        /// Converts a TransformBindingID to a unique uint hash.
        /// </summary>
        /// <param name="id">TransformBindingID value.</param>
        /// <returns>Unique uint hash.</returns>
        uint ToHash(TransformBindingID id);
        /// <summary>
        /// Converts a GenericBindingID to a unique uint hash.
        /// </summary>
        /// <param name="id">GenericBindingID value.</param>
        /// <returns>Unique uint hash.</returns>
        uint ToHash(GenericBindingID id);
    }

    /// <summary>
    /// Default Binding Hash Generator
    /// </summary>
    public class BindingHashGenerator : IBindingHashGenerator
    {
        /// <inheritdoc />
        public uint ToHash(TransformBindingID id) => StringHash.Hash(id.ID);
        /// <inheritdoc />
        public uint ToHash(GenericBindingID id) => StringHash.Hash(id.ID);
    }
}
