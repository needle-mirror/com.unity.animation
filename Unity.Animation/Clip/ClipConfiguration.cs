using System;

namespace Unity.Animation
{
    public enum ClipConfigurationMask : int
    {
        NormalizedTime         = 1 << 0,
        LoopTime               = 1 << 1,
        LoopValues             = 1 << 2,
        CycleRootMotion        = 1 << 3,
        DeltaRootMotion        = 1 << 4,
        RootMotionFromVelocity = 1 << 5,
        BankPivot              = 1 << 6
    }

    [Serializable]
    public struct ClipConfiguration
    {
        public ClipConfigurationMask Mask;
        public StringHash MotionID;
    }
}
