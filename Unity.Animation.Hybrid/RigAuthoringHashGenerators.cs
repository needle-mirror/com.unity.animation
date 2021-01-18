using System.Text.RegularExpressions;

namespace Unity.Animation.Hybrid
{
    /// <summary>
    /// Hash Generator used to create a Bone mapping table for skeletons.
    /// </summary>
    struct MapByPathHashGenerator
    {
        public TransformBindingID RootID;
        public string BonePrefix;

        private string FormatPath(string path)
        {
            if (!RootID.Equals(TransformBindingID.Invalid))
            {
                path = path.Substring(RootID.Path.Length);
                path = path.TrimStart('/');
            }

            if (!string.IsNullOrEmpty(BonePrefix))
            {
                path = Regex.Replace(path, $"^{BonePrefix}", "");
                path = path.Replace($"/{BonePrefix}", "/");
            }

            return path;
        }

        public uint TransformBindingHashFullPath(TransformBindingID id)
        {
            id.Path = FormatPath(id.Path);
            return BindingHashGlobals.TransformBindingHashFullPath(id);
        }

        public uint GenericBindingHashFullPath(GenericBindingID id)
        {
            id.Path = FormatPath(id.Path);
            return BindingHashGlobals.GenericBindingHashFullPath(id);
        }

        public static implicit operator BindingHashGenerator(MapByPathHashGenerator hasher)
        {
            return new BindingHashGenerator
            {
                TransformBindingHashFunction = hasher.TransformBindingHashFullPath,
                GenericBindingHashFunction = hasher.GenericBindingHashFullPath
            };
        }
    }
}
