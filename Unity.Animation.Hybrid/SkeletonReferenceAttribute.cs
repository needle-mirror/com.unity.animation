using System;

namespace Unity.Animation.Authoring
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class SkeletonReferenceAttribute : Attribute
    {
        /// <summary>Creates a SkeletonSource Attribute</summary>
        /// <param name="relativePropertyPathToSkeleton">Specifies the path to a Skeleton field or property. The path needs to point to a sibling, or a child of a sibling, of the field this attribute is set on.</param>
        /// <example>
        /// public class SomeClass
        /// {
        ///     public Skeleton mySkeleton;
        ///     [SkeletonSource(nameof(mySkeleton))]
        ///     public TransformBindingID myBone;
        /// }
        /// </example>
        public SkeletonReferenceAttribute(string relativePropertyPathToSkeleton)
        {
            RelativeSkeletonPath = relativePropertyPathToSkeleton;
        }

        public readonly string RelativeSkeletonPath;
    }
}
