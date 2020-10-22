namespace Unity.Animation.Hybrid
{
    /// <summary>
    /// Implement the IDeclareCustomRigChannel on a custom Monobehaviour part of a <see cref="RigComponent"/> hierarchy to add custom rig channel
    /// declarations at conversion time.
    /// </summary>
    public interface IDeclareCustomRigChannels
    {
        void DeclareRigChannels(RigChannelCollector collector);
    }
}
