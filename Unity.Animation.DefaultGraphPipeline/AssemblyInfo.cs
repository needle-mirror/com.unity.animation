using Unity.Animation;
using Unity.Entities;

[assembly: RegisterGenericJobType(typeof(SortReadTransformComponentJob<PreAnimationGraphSystem.ReadTransformHandle>))]
[assembly: RegisterGenericJobType(typeof(ReadTransformComponentJob<PreAnimationGraphSystem.ReadTransformHandle>))]
[assembly: RegisterGenericJobType(typeof(ReadRootTransformJob<PreAnimationGraphSystem.AnimatedRootMotion>))]
[assembly: RegisterGenericJobType(typeof(UpdateRootRemapMatrixJob<PreAnimationGraphSystem.AnimatedRootMotion>))]
[assembly: RegisterGenericJobType(typeof(WriteRootTransformJob<PreAnimationGraphSystem.AnimatedRootMotion>))]
[assembly: RegisterGenericJobType(typeof(AccumulateRootTransformJob<PreAnimationGraphSystem.AnimatedRootMotion>))]
[assembly: RegisterGenericJobType(typeof(WriteTransformComponentJob<PreAnimationGraphSystem.WriteTransformHandle>))]

[assembly: RegisterGenericJobType(typeof(SortReadTransformComponentJob<PostAnimationGraphSystem.ReadTransformHandle>))]
[assembly: RegisterGenericJobType(typeof(ReadTransformComponentJob<PostAnimationGraphSystem.ReadTransformHandle>))]
[assembly: RegisterGenericJobType(typeof(WriteTransformComponentJob<PostAnimationGraphSystem.WriteTransformHandle>))]
