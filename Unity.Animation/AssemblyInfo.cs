using System.Runtime.CompilerServices;
using Unity.Animation;
using Unity.Entities;


[assembly: InternalsVisibleTo("Unity.Animation.Hybrid")]
[assembly: InternalsVisibleTo("Unity.Animation.Authoring")]
[assembly: InternalsVisibleTo("Unity.Animation.Hybrid.Tests")]
[assembly: InternalsVisibleTo("Unity.Animation.Tests")]
[assembly: InternalsVisibleTo("Unity.Animation.PerformanceTests")]
[assembly: InternalsVisibleTo("Unity.Animation.Graph")]
[assembly: InternalsVisibleTo("Unity.Animation.DefaultGraphPipeline")]

[assembly: RegisterGenericJobType(typeof(SortReadTransformComponentJob<NotSupportedTransformHandle>))]
[assembly: RegisterGenericJobType(typeof(ReadTransformComponentJob<NotSupportedTransformHandle>))]
[assembly: RegisterGenericJobType(typeof(ReadRootTransformJob<NotSupportedRootMotion>))]
[assembly: RegisterGenericJobType(typeof(UpdateRootRemapMatrixJob<NotSupportedRootMotion>))]
[assembly: RegisterGenericJobType(typeof(WriteRootTransformJob<NotSupportedRootMotion>))]
[assembly: RegisterGenericJobType(typeof(AccumulateRootTransformJob<NotSupportedRootMotion>))]
[assembly: RegisterGenericJobType(typeof(WriteTransformComponentJob<NotSupportedTransformHandle>))]
