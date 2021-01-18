using Unity.Collections;

namespace Unity.Animation
{
    /// <summary>
    /// SynchronizationTag provide a way to annotate a clip with meta data that can be used by various systems to synchronize similar type of motions together.
    /// </summary>
    /// <remarks>Users must define their own SynchronizationTag type, such as the HumanoidGait or HorseTrot examples below, with all possible sub states for a given type.
    /// <code>
    /// public enum HumanoidGait
    /// {
    ///     LeftFootContact = 1,
    ///     RightFootPassover = 2,
    ///     RightFootContact = 3,
    ///     LeftFootPassover = 4
    /// }
    ///
    /// public enum HorseTrotGait
    /// {
    ///     FrontLeft = 1,
    ///     SuspensionRight = 2,
    ///     FrontRight = 3,
    ///     SuspensionLeft = 4
    /// }
    /// </code>
    /// </remarks>
    [BurstCompatible]
    public struct SynchronizationTag
    {
        /// <summary>
        /// The normalized time of the SynchronizationTag.
        /// </summary>
        /// <value>SynchronizationTag occur at a specific time in a clip. The normalized time is a ratio based on the overall clip duration.</value>
        public float       NormalizedTime;

        /// <summary>
        /// The unique type id of the SynchronizationTag.
        /// </summary>
        public StringHash  Type;

        /// <summary>
        /// The state value of this SynchronizationTag.
        /// </summary>
        public int         State;
    }
}
