using Unity.Animation;
using Unity.Entities;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Animation.Tests")]

[assembly: RegisterGenericJobType(typeof(SortReadTransformComponentJob<ProcessDefaultAnimationGraph.ReadTransformHandle>))]
[assembly: RegisterGenericJobType(typeof(ReadTransformComponentJob<ProcessDefaultAnimationGraph.ReadTransformHandle>))]
[assembly: RegisterGenericJobType(typeof(ReadRootTransformJob<ProcessDefaultAnimationGraph.AnimatedRootMotion>))]
[assembly: RegisterGenericJobType(typeof(UpdateRootRemapMatrixJob<ProcessDefaultAnimationGraph.AnimatedRootMotion>))]
[assembly: RegisterGenericJobType(typeof(WriteRootTransformJob<ProcessDefaultAnimationGraph.AnimatedRootMotion>))]
[assembly: RegisterGenericJobType(typeof(AccumulateRootTransformJob<ProcessDefaultAnimationGraph.AnimatedRootMotion>))]
[assembly: RegisterGenericJobType(typeof(WriteTransformComponentJob<ProcessDefaultAnimationGraph.WriteTransformHandle>))]

[assembly: RegisterGenericJobType(typeof(SortReadTransformComponentJob<ProcessLateAnimationGraph.ReadTransformHandle>))]
[assembly: RegisterGenericJobType(typeof(ReadTransformComponentJob<ProcessLateAnimationGraph.ReadTransformHandle>))]
[assembly: RegisterGenericJobType(typeof(WriteTransformComponentJob<ProcessLateAnimationGraph.WriteTransformHandle>))]
