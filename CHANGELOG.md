# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.9.0-preview.6] - 2021-01-18

### Added
- Compositor package is now merged in the Animation package.
    - All references to "Compositor" are now "Animation Graph" throughout.
- Added EndOfAnimation condition fragment that allow a state to change when reaching a certain threshold in the source dominant animation.

### Changed
- Added dependency on com.unity.graphtools.foundation at 0.7.0-preview.2.
- Upgraded com.unity.entities to 0.17.0-preview.41.
- Upgraded com.unity.dataflowgraph to 0.19.0-preview.7.
- `BlendTreeConversion` and `BakeOptions` have moved from `Unity.Animation.Editor` to `Unity.Animation.Hybrid` (they are still editor-only).

## [0.9.0-preview.5] - 2020-12-16

### Changed
- Upgraded com.unity.dataflowgraph to 0.19.0-preview.5
- Upgraded com.unity.entities to 0.17.0-preview.39
- Fixed ArgumentException thrown when setting a generic channel with an empty path in the RigComponent.

## [0.9.0-preview.4] - 2020-12-09

### Changed
- Upgraded com.unity.dataflowgraph to 0.19.0-preview.4
- Upgraded com.unity.entities to 0.17.0-preview.38
- Upgraded com.unity.jobs to 0.8.0-preview.23
- Fixed a typo in TransformBindingID.IsDescendantOf.

## [0.9.0-preview.3] - 2020-12-08

### Added
- Added the enum GenericChannelType used in GenericBindingID to accurately describe concrete channels in the Skeleton.
- Added a version of RigRemapUtils.CreateRemapTable that remaps using Skeletons.

### Changed
- Updated minimum Unity Editor version to 2020.1.9f1
- Upgraded com.unity.collections to 0.15.0-preview.21
- Upgraded com.unity.dataflowgraph to 0.19.0-preview.3
- Upgraded com.unity.entities to 0.17.0-preview.37
- Upgraded com.unity.jobs to 0.8.0-preview.22
- Upgraded com.unity.test-framework to 1.1.20
- Upgraded com.unity.test-framework.performance to 2.3.1-preview
- Updated RigAuthoring to map the skeleton channels unto a SkinnedMeshRenderer using automatic bone mapping with manual overrides.
- Added functions to add, remove, clear and query bone mapping overrides in RigAuthoring.

## [0.9.0-preview.2] - 2020-11-17

### Changed
- Upgraded com.unity.entities to 0.17.0-preview.34
- Upgraded com.unity.dataflowgraph to 0.19.0-preview.1
- Removed ActiveTransformChannels/InactiveTransformChannels from Skeleton and replaced them with GetAllTransforms. The original properties didn't take the skeleton root into account. GetAllTransforms uses an enum to determine what parts of the Skeleton to return
- Contains has been modified to also use the TransformChannelSearchMode enum to specify if you want to check if the id is part of the active or inactive part of the Skeleton.
- Added Skeleton.GetTransformChannelState that returns the state of a given channel, if it's active/inactive or does not exist in the Skeleton.
- Added TransformBindingID.IsDecendantOf which can be used to check descendancy of a TransformBindingID compared to another
- Added SkeletonBoneReference.ConvertToIndex / TransformBindingID.ConvertToIndex which return the active transform index of a bone relative to its Skeleton. It will return -1 when its not active. This index is the index that the bone will have at runtime.
- Added Skeleton.ActiveTransformChannelCount / Skeleton.InactiveTransformChannelCount which, respectively, return the active and inactive count of all the bones in the skeleton

## [0.9.0-preview.1] - 2020-11-06

### Changed
- With `com.unity.entities` v0.17.0, nested blob asset references are not supported anymore and generate a `ConstructBlobWithRefTypeViolation` error which affects how animation blend trees are stored as DOTS components. Until this becomes a supported feature by `com.unity.entities`, we had to do the following temporary changes:
  - Field `BlendTree2DMotionData.Motion` has changed type from `Motion` to `BlobAssetReference<Clip>`.
  - Field `BlendTree1DMotionData.Motion` has changed type from `Motion` to `BlobAssetReference<Clip>`.
  - Struct `Motion` was rewritten.
- Upgraded com.unity.entities to 0.17.0-preview.28
- Upgraded com.unity.collections to 0.15.0-preview.19
- Upgraded com.unity.jobs to 0.8.0-preview.20
- Upgraded com.unity.burst to 1.4.1

### Deprecated
- `BindingHashDelegate` and `BindingHashUtils` have been deprecated and replaced with `BindingHashGenerator` and `BindingHashGlobals`.

### Fixed
- Fixed missing Skeleton root channel when adding a child Transform channel in Skeleton.

## [0.8.0-preview.4] - 2020-10-30

### Added
- Added ClipBuilder as optional intermediate structure when converting UnityEngine AnimationClip to DOTS Clip, allowing to add procedurally created curves.

## [0.8.0-preview.3] - 2020-10-22

### Added
- Added PropertyDrawers for TransformBindingID and SkeletonBoneReference (which is now made public). Both make it possible to set bones from the Skeleton Asset using drag and drop or using the object picker. TransformBindingID is a path to a bone, and SkeletonBoneReference contains a reference to a Skeleton and a TransformBindingID, which represents a bone in the given Skeleton.
- Added SkeletonReferenceAttribute which can be used on TransformBindingID to associate it with a property returning, or a field containing a Skeleton. 
- Added ShowFullPathAttribute which can be used on TransformBindingID and SkeletonBoneReference to show bone names as full paths instead of just the name of the bone itself.
- Added a debugger view for `Rig` IComponentData. This view will display the `BlobAssetReference<RigDefinition>` internal data in the debugger. For more advanced debugging you can also add the DEBUG_STRINGHASH to your project scripting symbols which can be added under `Project Settings/Player`. This optionnal symbol will keep the original string for all hashes, and use this information when the debugger need to display a StringHash. There are currently a few limitations with this system:
  - Since it stores this information into a static class, every domain reload clears the dictionnary.
  - It currently only works in editor mode.

### Changed
- Upgraded com.unity.dataflowgraph to 0.18.0-preview.3
- Upgraded com.unity.entities to 0.16.0-preview.21
- Upgraded com.unity.collections to 0.14.0-preview.16
- Upgraded com.unity.jobs to 0.7.0-preview.17
- Upgraded com.unity.burst to 1.3.7

## [0.8.0-preview.2] - 2020-10-05

### Changed
- Added SkeletonAuthoring asset that isolates the animation bindings declaration and authoring from the rig instance in scene. This is the first version of SkeletonAuthoring and this asset will continue to evolve as more workflows are developed around it.
- Added RigAuthoring that uses a SkeletonAuthoring instance to generate a RigDefinition.
- Upgraded com.unity.entities to 0.16.0-preview.18
- Upgraded com.unity.collections to 0.14.0-preview.14
- Upgraded com.unity.jobs to 0.7.0-preview.15
- Upgraded com.unity.dataflowgraph to 0.17.1-preview.1
- Deprecated the property RigComponent in BoneRendererComponent. A reference to the RigComponent is not necessary anymore.

### Fixed
- Fixed bug when reading input port twist weights in the `TwistCorrectionNode`.

## [0.8.0-preview.1] - 2020-09-28

### Added
- Added `IDeclareCustomRigChannels` interface and `RigChannelCollector` helper to append custom rig channels at conversion time to a `RigDefinition`. In other words, any custom `GameObject` components implementing the `IDeclareCustomRigChannels` in a `RigComponent` hierarchy will be called during conversion to append custom channels.

### Changed
- Upgraded com.unity.burst to 1.3.6
- In 0.7.1-preview.1 we updated the DataFlowGraph package, but only disabled the warnings. We now have update our nodes with the new API:
    - `NodeDefinition` has been converted to `SimulationKernelNodeDefinition`, `SimulationNodeDefinition` or `KernelNodeDefinition` depending on the node;
    - Node Data and Kernel Data have been made private (previously public);
    - Kernel function is private (previously public);
    - The node itself does not handle the message to update the rig anymore (`void HandleMessage(in MessageContext ctx, in Rig rig)` has been removed);
    - `IRigContextHandler` has been updated : handling the message rig is now done in the node data, and the TaskPort is defined in the node.
- Renamed the following types:
    - `AnimationGraphSystemBase` -> `ProcessAnimationGraphBase`
    - `BeginFrameAnimationSystem` -> `InitializeAnimation`
    - `BeginFrameAnimationSystemBase` -> `InitializeAnimationBase`
    - `BoneRendererMatrixSystemBase` -> `ComputeBoneRenderingMatricesBase`
    - `BoneRendererRenderingSystemBase` -> `RenderBonesBase`
    - `ComputeDeformationDataSystemBase` -> `ComputeDeformationDataBase`
    - `ComputeSkinMatrixSystem` -> `ComputeDeformationData`
    - `PostAnimationGraphReadTransformHandle` -> `LateAnimationGraphReadTransformHandle`
    - `PostAnimationGraphWriteTransformHandle` -> `LateAnimationGraphWriteTransformHandle`
    - `PostAnimationGraphSystem` -> `ProcessLateAnimationGraph`
    - `PostAnimationSystemGroup` -> `LateAnimationSystemGroup`
    - `PreAnimationGraphReadTransformHandle` -> `DefaultAnimationGraphReadTransformHandle`
    - `PreAnimationGraphWriteTransformHandle` -> `DefaultAnimationGraphWriteTransformHandle`
    - `PreAnimationGraphSystem` -> `ProcessDefaultAnimationGraph`
    - `PreAnimationSystemGroup` -> `DefaultAnimationSystemGroup`
    - `RigComputeMatricesSystem` -> `ComputeRigMatrices`
    - `RigComputeMatricesSystemBase` -> `ComputeRigMatricesBase`

## [0.7.1-preview.2] - 2020-09-11

### Changed
- Added missing explicit dependency on com.unity.modules.animation
- Upgraded com.unity.dataflowgraph to 0.17.0-preview.4

## [0.7.1-preview.1] - 2020-09-09

### Changed
- Upgraded com.unity.collections to 0.12.0-preview.13
- Upgraded com.unity.entities to 0.14.0-preview.19
- Upgraded com.unity.jobs to 0.5.0-preview.14
- Upgraded com.unity.mathematics to 1.2.1
- Upgraded com.unity.dataflowgraph to 0.17.0-preview.3

### Deprecated
- `DeltaTimeNode` has been deprecated because it was using `UnityEngine.Time.DeltaTime` instead of `World.Time.DeltaTime`, but the latter is not accessible from inside a node. Instead, set the kernel time data of the nodes with `SetData` or use a `KernelPassThroughNodeFloat`.

### Fixed
- Fixed bug in `UberClipNode.Bake` to prevent out of bound reads from rotation data.

## [0.7.0-preview.1] - 2020-08-28

### Added
- Added a new section in `AnimationStream` for discrete channels:
    - Size can be accessed using `BindingSet.DiscreteDataChunkCount` or `AnimationStream.DiscreteDataChunkCount`
    - Pointer can be accessed using `AnimationStream.GetDiscreteDataChunkUnsafePtr()`
- Added `FillAnimationCurveBlob(UnityEngine.AnimationCurve sourceCurve, ref BlobBuilder blobBuilder, ref AnimationCurveBlob curveBlob)` to the public API.
- Added `AnimationCurveCache` to the public API. It is used to cache the curve interval accessed during evaluation.
- Added `Evaluate(float time, ref AnimationCurveBlob curveBlob, ref AnimationCurveCache cache)` that can be used to evaluate directly an AnimationCurveBlob.
- Added ref struct `ChannelMask` to the public API. This is used by the `AnimationStream` to reference the masks within a stream. 
- Added property `AnimationStream.PassMask` and `AnimationStream.FrameMask`.
- Added `UNITY_ENABLE_ANIMATION_PROFILING` preprocessor to turn ON animation profiler marker.
- Added bezier interpolation support for animation curves. Curves using bezier keyframes will be 66% bigger, but curve only using hermit keyframes remain the same size.
- Added the helper function `ToRigDefinition` to the `RigComponent`.
- Added `RigComponent.ExtractRigBuilderData` to build the data used to create the rig definition.
- Added `RigBuilder.CreateRigDefinition(Unity.Animation.RigBuilderData rigBuilderData)` that takes an intermediate data structure to create the rig definition.
- Added `RigBuilder.RunCreateRigDefinitionJob` that creates a rig definition from a job. Note that this cannot be called from another job (jobs cannot be nested).

### Changed
- Improved support for int channels:
    - `AnimationStream` uses a separate section for int channels.
    - All core blending methods blend int channels using `math.select` instead of `math.lerp`.
- Renamed some members of `BindingSet`:
    - `RotationChunkCount` to `RotationDataChunkCount`
    - `DataChunkCount` to `InterpolatedDataChunkCount`
- Renamed some members of `AnimationStream`:
    - `RotationChunkCount` to `RotationDataChunkCount`
    - `DataChunkCount` to `InterpolatedDataChunkCount`
    - `GetDataChunkUnsafePtr()` to `GetInterpolatedDataChunkUnsafePtr()`
    - `GetRotationChunkUnsafePtr()` to `GetRotationDataChunkUnsafePtr()`
- All validation checks can now be turned on/off by using Burst menu options Jobs/Burst/Safety Checks. Since we now rely on `ENABLE_UNITY_COLLECTIONS_CHECKS`, validation checks are only available in the editor build.
- Changed `AnimationStream` masking from one mask to two masks: 
    - `AnimationStream.PassMask` is always cleared at the beginning of an `AnimationGraphSystemBase` update, see `PreAnimationGraphSystem` and `PostAnimationGraphSystem`, and contains all the channels that were modified within this system update.
    - `AnimationStream.FrameMask` is always cleared at the beginning of a new frame in the `InitializationSystemGroup` update, and contains all the channels that were modified within the complete frame.
    - Renamed `AnimationStream.AndChannelMasks()` to `AnimationStream.AndMasks()`, this method operates on both Pass and Frame masks.
    - Renamed `AnimationStream.OrChannelMasks()` to `AnimationStream.OrMasks()`, this method operates on both Pass and Frame masks.
    - Renamed `AnimationStream.ClearChannelMasks()` to `AnimationStream.ClearMasks()`, this method operates on both Pass and Frame masks.
    - Renamed `AnimationStream.SetChannelMasks()` to `AnimationStream.SetMasks()`, this method operates on both Pass and Frame masks.
    - Renamed `AnimationStream.CopyChannelMasksFrom()` to `AnimationStream.CopyMasksFrom()`, this method operates on both Pass and Frame masks.
- Changed the way to build new `AnimationCurve`s. Instead of allocating the time and keyframe buffers, one can now call the `AnimationCurve.CreateBuilder` and use the builder to set the keyframes and times. See the implementation of `CurveConversion.FillAnimationCurveBlob` for an example.

### Deprecated
- Deprecated `UNITY_DISABLE_ANIMATION_CHECKS`. To be Burst compatible in standalone builds all asserts and exceptions have to be guarded by `ENABLE_UNITY_COLLECTIONS_CHECKS`
- Deprecated animation system tags. This is not required anymore in order to evaluate an animation system graph. Evaluation is solely based on the graph nodes that read/write `AnimatedData` from/to entities.
    - `IAnimationSystemTag` is deprecated and will be removed.
    - `IAnimationSystem` is deprecated and replaced with `IAnimationGraphSystem`.
    - `AnimationSystemBase` is deprecated and replaced with `AnimationGraphSystemBase`
    - `Tag` struct and `TagComponent` property from `PreAnimationGraphSystem` and `PostAnimationGraphSystem` are deprecated and will be removed.
- Deprecated `UNITY_DISABLE_ANIMATION_PROFILING` since now all systems and DFG nodes provide profiler marker service.
- Deprecated `RigGenerator.ExtractAnimationChannelFromRigComponent`. Use `RigComponent.ExtractRigBuilderData` instead.
- Deprecated `RigGenerator.ExtractSkeletonNodesFromRigComponent`. Use `RigComponent.ExtractRigBuilderData` instead.

### Removed
- Removed the following methods since we now have two different masks in the `AnimationStream`, you first need to retrieve the mask that you want to query with either `AnimationStream.PassMask` or `AnimationStream.FrameMask` and then call the equivalent methods on `ChannelMask`:
    - `AnimationStream.GetChannelMaskBitCount()` -> `ChannelMask.CountChannels()`.
    - `AnimationStream.GetFloatChannelMask()` -> `ChannelMask.IsFloatSet()`.
    - `AnimationStream.GetIntChannelMask()` -> `ChannelMask.IsIntSet()`.
    - `AnimationStream.GetTranslationChannelMask()` -> `ChannelMask.IsTranslationSet()`.
    - `AnimationStream.GetRotationChannelMask()` -> `ChannelMask.IsRotationSet()`.
    - `AnimationStream.GetScaleChannelMask()` -> `ChannelMask.IsScaleSet()`.
    - `AnimationStream.HasAllChannelMasks()` -> `ChannelMask.HasAll()`.    
    - `AnimationStream.HasAnyChannelMasks()` -> `ChannelMask.HasAny()`.
    - `AnimationStream.HasNoChannelMasks()` -> `ChannelMask.HasNone()`.
- Removed all animation specific profiler marker from DFG nodes and ECS systems since now both package provide profiler marker: 
    - For DFG you need to add `DFG_PER_NODE_PROFILING` in your project settings to enable DFG node profiling.
- Removed the `AnimationCurve.KeyframesData` array. To access keyframes use `AnimationCurve.this[int]`.

### Fixed
- Fixed a bug where the bones in the RigComponent Bones list would not be editable when a skeleton root bone was set when its parent was deselected
- Fixed a bug where sampling int channels close to keyframes would interpolate.
- Fixed a bug where when evaluating a curve, if the cache was reused the wrong times would be used for evaluation.

## [0.6.0-preview.2] - 2020-07-30

### Added
- Added an experimental node `WorldToRootNode` that remaps a transform matrix from world space to root space.

### Changed
- Changed the `RigRootEntity` component to store the `RemapToRootMatrix`, a matrix used to compute the remapping from world space to root space. Note: This is not the complete remapping matrix, it needs to be multiplied with the value of index 0 of the stream and the result of this multiplication must be inverted.

## [0.6.0-preview.1] - 2020-07-24

### Added
- Added new methods to `AnimationStream`:
    - `AnimationStream.CopyFrom(ref AnimationStream other)`
    - `AnimationStream.ResetToZero()`
    - `AnimationStream.ResetToDefaultValues()`
- Added two core functions for inertial motion blending:
    - `Core.InertialBlend`
    - `Core.ComputeInertialBlendingCoefficients`

### Changed
- Updated minimum Unity Editor version to 2020.1.0b15.
- Deformations only work with com.unity.rendering.hybrid 0.7.0-preview.24 and above.
- `AnimationStream` validation checks are now enabled by `ENABLE_UNITY_COLLECTIONS_CHECKS` rather than disabled by `UNITY_DISABLE_ANIMATION_CHECKS`
- Changed exception thrown by `AnimationStream` from `System.ArithmeticException` to `System.NotFiniteNumberException` for invalid input data.
- RigComponent now shows the used bones in its inspector as a tree structure, which can be edited in the inspector. 
A number of safety checks have been added that make it impossible to add transforms that are not children of the RigComponent, 
or the transform of the RigComponent itself. Every transform is checked to always have an unbroken chain of transforms 
from it to the skeleton tree root. Besides the tree structure there are several other UX improvements, such as a search 
filter that filters the bones in the treeview. In addition to this the bones show their index and are draggable. 
Finally, when editing the RigComponent bones, it's possible to set the Skeleton Tree Root, which represents the 
top most bone in the animation skeleton.
- Upgraded com.unity.dataflowgraph to 0.16.0-preview.2
- Upgraded com.unity.entities to 0.13.0-preview.24
- Upgraded com.unity.collections to 0.11.0-preview.17
- Upgraded com.unity.jobs to 0.4.0-preview.18
- Upgraded com.unity.burst to 1.3.2

### Upgrade Guide
- The first time you modify a RigComponent after upgrading, its serialized data are automatically updated to ensure it is valid. 
If you have any prefab instances or variants with overrides to the Bones property, you should verify these overrides for correctness.

### Deprecated
- Deprecated `AnimationStreamUtils`:
    - `AnimationStreamUtils.MemCpy(ref AnimationStream dst, ref AnimationStream src)` is replaced with `AnimationStream.CopyFrom(ref AnimationStream other)`
    - `AnimationStreamUtils.SetDefaultValues(ref AnimationStream stream)` is replaced with `AnimationStream.ResetToDefaultValues()`
    - `AnimationStreamUtils.MemClear(ref AnimationStream stream)` is replaced with `AnimationStream.ResetToZero()`

### Known Issues
- HDRP 8 is not compatible with the deformation pipeline. Use HDRP 9+ instead.

## [0.5.1-preview.3] - 2020-07-08

### Fixed
- Fixed regression when computing skin matrices causing an unwanted offset when root transform management is enabled.

## [0.5.1-preview.2] - 2020-06-30

### Added
- Added new methods to compute rig matrices in LocalToRoot space with an extra offset parameter:
    - `Core.ComputeLocalToRoot(ref AnimationStream stream, float4x4 offset, NativeArray<float4x4> outMatrices)`
    - `Core.ComputeLocalToRoot(ref AnimationStream stream, float4x4 offset1, NativeArray<float4x4> outMatrices1, float4x4 offset2, NativeArray<float4x4> outMatrices2)`

### Changed
- Changed function signature of `Core.ComputeLocalToWorld()`.

### Deprecated
- Deprecated `Core.ComputeLocalToWorld()` and `Core.ComputeLocalToWorldAndRoot()` since these can be expressed using `Core.ComputeLocalToRoot()` with a LocalToWorld offset.

### Fixed
- Fixed computation of `AnimatedLocalToRoot` when root transform management is enabled.

## [0.5.1-preview.1] - 2020-06-18

### Added
- Added an experimental conversion from `Animator` to `Rig`. The `UNITY_ENABLE_ANIMATION_ANIMATOR_CONVERSION` flag needs to be defined to enable the conversion.
- Improved root transform handling and root motion. See documentation [here](Documentation~/root_transform_management.md).
    - Added `RigRootEntity` IComponentData which holds the entity reference to the root transform (first bone defined in the `RigComponent`). **NOTE: Custom rig conversion systems, in other words projects not using the `RigComponent`, should make sure to populate this new required component.**
- Added the `PreAnimationGraphWriteTransformHandle`.
- Added `AnimationLocalToParentOverride` and `AnimationLocalToWorldOverride`.
- Improved support for affine transforms in the `AnimationStream` enabling us to properly support non-uniform scale and shearing. Following new methods were added:
    - `GetLocalToParentTR(int index, out float3 t, out quaternion r)`
    - `GetLocalToParentInverseMatrix(int index)`
    - `GetLocalToRootInverseMatrix(int index)`
    - `GetLocalToRootScale(int index)`
    - `GetLocalToRootTRS(int index, out float3 t, out quaternion r, out float3 s)`
    - `SetLocalToParentTR(int index, float3 t, quaternion r)`
    - `SetLocalToRootScale(int index, float3 s)`
    - `SetLocalToRootTRS(int index, float3 t, quaternion r, float3 s)`
- Added `Scale` ports to `GetAnimationStreamLocalToRootNode` and `SetAnimationStreamLocalToRootNode` nodes.
- Added an experimental Rig conversion clean up system `RigConversionCleanup`: 
    - All entities that are not exposed and only have Transform component type are deleted by this system to remove unused entities.
    - By default the system is not enabled, you must add the following preprocessor `UNITY_ENABLE_ANIMATION_RIG_CONVERSION_CLEANUP` in your project's scripting define symbols list to enable the system.
- Added `IReadExposeTransform` and `IWriteExposeTransform` interfaces in order to filter queries based on Read/Write type.
- Added channel masks in the `AnimationStream` enabling us to provide information about which channels were modified. New methods were added:
    - `AnimationStream.ClearChannelMasks()`
    - `AnimationStream.SetChannelMasks(bool value)`
    - `AnimationStream.CopyChannelMasksFrom(ref AnimationStream src)`
    - `AnimationStream.OrChannelMasks(ref AnimationStream other)`
    - `AnimationStream.OrChannelMasks(ref AnimationStream lhs, ref AnimationStream rhs)`
    - `AnimationStream.AndChannelMasks(ref AnimationStream other)`
    - `AnimationStream.AndChannelMasks(ref AnimationStream lhs, ref AnimationStream rhs)`
    - `AnimationStream.GetTranslationChannelMask(int index)`
    - `AnimationStream.GetRotationChannelMask(int index)`
    - `AnimationStream.GetScaleChannelMask(int index)`
    - `AnimationStream.GetFloatChannelMask(int index)`
    - `AnimationStream.GetIntChannelMask(int index)`
    - `AnimationStream.GetChannelMaskBitCount()`
    - `AnimationStream.HasAnyChannelMasks()`
    - `AnimationStream.HasAllChannelMasks()`
    - `AnimationStream.HasNoChannelMasks()`
- Added masking operation on all Core methods used for sampling and blending.
- Added support for `SkinnedMeshRenderer` blendshapes. This uses a new and experimental compute shader deformation pipeline only available in Unity 2020.1.0b9+. To enable it in your project make sure to add the following player scripting define `ENABLE_COMPUTE_DEFORMATIONS` and update your shader graphs to use the `Compute Deformation` node.

### Changed
- With changes to root transform handling, the `RootMotionNode` has been deprecated. This node was previously accumulating the
the delta root transform via component nodes connected on the root entity transform components. The new preferred setup is to use
the `IAnimatedRootMotion` component instead. The `AnimationSystemBase` will take care of performing the delta accumulation on the entity transform components directly.
See documentation [here](Documentation~/root_transform_management.md) for more details.
- Removed `PrepareSkinMatrixToRendererSystemBase` and `FinalizePushMatrixToRendererSystemBase`. Both systems are now owned by `com.unity.rendering.hybrid`. **Note that you'll need to upgrade any previous material graphs using the `LinearBlendSkinning` node to remove the `BoneIndexOffset` input altogether when moving to Unity 2020.1.0b9+**
- `ComputeSkinMatrixSystemBase` has been deprecated use `ComputeDeformationDataSystemBase` instead.
- `SkinnedMeshRigEntity` has been deprecated use `Unity.Animation.RigEntity` IComponentData instead.
- `Unity.Animation.BoneRenderer.RigEntity` has been deprecated use `Unity.Animation.RigEntity` IComponentData instead.
- `Unity.Animation.SkinMatrix` has been deprecated use `Unity.Deformations.SkinMatrix` found in com.unity.entities instead.
- Upgraded com.unity.entities to 0.11.1-preview.4
- Upgraded com.unity.burst to 1.3.0
- Upgraded com.unity.jobs to 0.2.10-preview.12
- Upgraded com.unity.collections to 0.9.0-preview.6
- Upgraded com.unity.dataflowgraph to 0.15.0-preview.5
- `AddWriteTransformHandle` does not add the component `AnimationTransformOverride` to the entity anymore. It adds the two components `AnimationLocalToParentOverride` and `AnimationLocalToWorldOverride`.
- `WriteTransformHandleJob` was writing only in the `LocalToWorld` component of the exposed entity. Now it also writes in the `LocalToParent`, `Translation`, `Rotation`, `Scale` and `NonUniformScale` components.
- A warning is logged when a SkinnedMeshRenderer is referencing a RootBone that has not been exposed. This leads to errors when computing the render bounds.

### Deprecated
- Deprecated `AnimationLocalToWorldOverride`. If a query defines `LocalToParent` as ReadOnly and `LocalToWorld` as ReadWrite, then `AnimationTransformOverride` will be in the Any field of the query and not the None field, so the entity will still be selected. It is replaced by `AnimationLocalToParentOverride` and `AnimationLocalToWorldOverride`.

## [0.4.0-preview.3] - 2020-05-14

### Fixed
- Reverted changes for Hybrid.Editor assemblies because it was a breaking change for Dots Timeline.
- Moved back `BlobAssetStoreExtensions` from `Unity.Animation.Hybrid.Editor` to `Unity.Animation.Hybrid`. Be aware that the `BlobAssetStoreExtensions` will only work in the editor and will throw a `NotImplementedException` if used in the standalone player.

## [0.4.0-preview.2] - 2020-05-13

### Fixed
- Fixed standalone player builds and added Hybrid.Editor assemblies for codepaths depending on UnityEditor.

## [0.4.0-preview.1] - 2020-05-06

### Added
- Added 3 new assemblies: `Unity.Animation.Graph`, `Unity.Animation.DefaultGraphPipeline`, and `Unity.Animation.DefaultGraphPipeline.Hybrid`
- Added Euler curve support for rotation bindings in `ClipBuilder`'s AnimationClip to DenseClip conversion.
- Added velocity calculation functions to Core namespace:
    - `Core.ComputeLocalToParentLinearVelocities`
    - `Core.ComputeLocalToParentAngularVelocities`
    - `Core.ComputeLocalToWorldLinearVelocities`
    - `Core.ComputeLocalToWorldAngularVelocities`
- Added experimental API to create and manage a set of sub-nodes associated with a GraphHandle. These nodes are either automatically released when disposing the animation system NodeSet or when you explicitly dispose the GraphHandle. Relevant functions added are:
    - `AnimationSystemBase.CreateGraph()`
    - `AnimationSystemBase.CreateNode<T>(GraphHandle)`
    - `AnimationSystemBase.CreateNode(GraphHandle, Entity)`
    - `AnimationSystemBase.Dispose(GraphHandle)`
- Added BlobAssetStore extension methods to support `AnimationClip`, `AnimationCurve` and `RigComponent`:
    - `BlobAssetStore.GetClip(AnimationClip)`
    - `BlobAssetStore.GetAnimationCurve(AnimationCurve)`
    - `BlobAssetStore.GetRigDefinition(RigComponent)`
- Added `SynchronizationTag` to `Clip`: To use them you need to create Animation Events on your clips defined with an ObjectReferenceParameter that implements the `ISynchronizationTag` interface. Their main use are for continuous blending between motion of the same type but that aren't authored like Synchronize Motions. 
- Added `ISynchronizationTag` to the `Unity.Animation.Hybrid` assembly. 
- Added a twist bone solver `Core.SolveTwistCorrection` and relevant DFG node `TwistCorrectionNode` to redistribute a percentage of the source rotation over a leaf bone in order to correct *candy-wrapper* mesh deformation artifacts.

### Changed
- Moved following files from `Unity.Animation` to `Unity.Animation.Graph`:
    - `AnimationSystemBase`
    - All DFG nodes
    - `NodeDefinitionAttribute`
    - `IRigContextHandler`
    - `DFGUtils` class
    - NodeSet extension methods
- Moved following files from `Unity.Animation` to `Unity.Animation.DefaultGraphPipeline`:
    - `PreAnimationSystemGroup` and `PostAnimationSystemGroup`
    - `PreAnimationGraphSystem` and `PostAnimationGraphSystem`
    - `RigComputeMatricesSystem`
    - `ComputeSkinMatrixSystem`
    - `PrepareSkinMatrixToRendererSystem`
    - `FinalizePushSkinMatrixToRendererSystem`
- Moved following files from `Unity.Animation` to `Unity.Animation.DefaultGraphPipeline.Hybrid`:
    - `PostAnimationGraphReadTransformHandle`
    - `PostAnimationGraphWriteTransformHandle`
    - `PreAnimationGraphReadTransformHandle`
- Changed assembly `Unity.AnimationCurves.asmdef` to `Unity.Animation.Curves.asmdef`
- Changed assembly `Unity.AnimationCurves.Hybdrid.asmdef` to `Unity.Animation.Curves.Hybrid.asmdef`
- Changed assembly `Unity.AnimationCurves.Tests.asmdef` to `Unity.Animation.Curves.Tests.asmdef`
- Removed dependency on `com.unity.rendering.hybrid` since common rendering structures are defined in `com.unity.entities`. **This means that all projects currently using `com.unity.animation` and HDRP for skinning should include `com.unity.rendering.hybrid` to their package manifests**.
- Removed nested `BlobAssetReference<RigDefinition>` found in `ClipInstance`. Storing `RigHashCode` and `ClipHashCode` to know what data was used to generate the specific `ClipInstance`.
- Changed `Unity.Animation.RigRemapUtils.CreateRemapTable` and `Unity.Animation.Hybrid.RigRemapUtils.CreateRemapTable` to take an extra optional parameter in order to remap specific channel types. By default all rig channels are used for matching.
- Interface `IAnimationChannel` does not inherit from `System.IEquatable` anymore.
- `LocalTranslationChannel` now implements `IEquatable<LocalTranslationChannel>`. The `Equals` function was changed to take an argument of type `LocalTranslationChannel` instead of the generic `IAnimationChannel`. Same for `LocalRotationChannel`, `LocalScaleChannel`, `FloatChannel` and `IntChannel`.
- Deprecated `ClipBuilder.AnimationClipToDenseClip` which is replaced by extension method `AnimationClip.ToDenseClip` implemented in `Unity.Animation.Hybrid`.
- Deprecated `ClipInstance.Create` which is replaced by `ClipInstanceBuilder.Create`.
- `AnimationSystemBase`, `RigComputeMatricesSystemBase`, `ComputeSkinMatrixSystemBase`, `BoneRendererMatrixSystemBase` and `BoneRendererRenderingSystemBase` have been upgraded to `SystemBase`.
- Upgraded com.unity.entities to 0.9.1-preview.15
- Upgraded com.unity.jobs to 0.2.8-preview.3
- Upgraded com.unity.collections to 0.7.1-preview.3
- Upgraded com.unity.dataflowgraph to 0.14.0-preview.2
- Upgraded com.unity.test-framework to 1.1.13
- Upgraded com.unity.test-framework.performance to 2.0.8-preview

### Fixed
- Fixed StringHash so that both null and empty strings give the same default hash value (id = 0)
- Fixed `SolveAimConstraint` to prevent rolling effects
- Fixed copy of integer curves values to incorrect AnimationStream indices in `Core.EvaluateClip`
- Fixed RigRemapperNode to output destination rig default values (or overridden default values) when the RemapTable, SourceRigDefintion or node input stream is invalid.

## [0.3.0-preview.9] - 2020-03-26

### Added
- Added `SkinnedMeshRendererConversion` system that automatically convert any `SkinnedMeshRenderer` component if a `RigComponent` exists on this GameObject or on any of his parents up to the root GameObject.
- Added `NotSupportedTransformHandle` for animation systems that do not support reading from or writing to exposed transforms like the default `PreAnimationGraphSystem`
- Added a `BindingHashDelegate` to override hashing strategy of bindings when converting clips and rig definitions. Notably the following functions have changed signature:
    - `Unity.Animation.ClipBuilder.AnimationClipToDenseClip(AnimationClip sourceClip, BindingHashDelegate bindingHash = null)`
    - `Unity.Animation.Hybrid.RigGenerator.ExtractSkeletonNodesFromTransforms(Transform root, Transform[] transforms, BindingHashDelegate bindingHash = null)`
    - `Unity.Animation.Hybrid.RigGenerator.ExtractSkeletonNodesFromGameObject(GameObject root, BindingHashDelegate bindingHash = null)`
    - `Unity.Animation.Hybrid.RigGenerator.ExtractSkeletonNodesFromRigComponent(RigComponent rigComponent, BindingHashDelegate bindingHash = null)`
    - `Unity.Animation.Hybrid.RigGenerator.ExtractAnimationChannelFromRigComponent(RigComponent rigComponent, BindingHashDelegate bindingHash = null)`
    - When no `BindingHashDelegate` is specified, the `Unity.Animation.BindingHashUtils.DefaultBindingHash` is used. The `Unity.Animation.BindingHashUtils.DefaultBindingHash` defaults to `Unity.Animation.BindingHashUtils.HashFullPath`. If your pipeline wants to create bindings based on names rather than the full path, `Unity.Animation.BindingHashUtils.HashName` could be employed.
- Added support for LocalToRoot space remapping and two new helpers to streamline the process of creating a `RigRemapTable`:
    - `Unity.Animation.RigRemapUtils.CreateRemapTable(BlobAssetReference<RigDefinition> src, BlobAssetReference<RigDefinition> dst, OffsetOverrides offsetOverides = null)`: Given a source and destination `RigDefinition` this function creates a remap table based on matching bindings. By default all matches are mapped in LocalToParent space but this can be overriden using the `Unity.Animation.RigRemapUtils.OffsetOverrides`.
    - `Unity.Animation.Hybrid.RigRemapUtils.CreateRemapTable(RigComponent src, RigComponent dst, OffsetOverrides offsetOverrides = null, BindingHashDelegate bindingHash = null)`: Given a source and destination `RigComponent` this function creates a remap table based on matching ids. A `BindingHashDelegate` can be specified in order to match using either the transform path [`BindingHashUtils.HashFullPath`], the transform name [`BindingHashUtils.HashName`] or a custom delegate. When no binding hash deletegate is specified, the system wide `BindingHashUtils.DefaultBindingHash` is used. By default, LocalToParent mapping is performed, however `Unity.Animation.RigRemapUtils.OffsetOverrides` can be specified to remap in LocalToRoot space and/or add translation/rotation offsets.
    - Note that remapping in LocalToRoot space has a performance cost and should be used with care.
    - LocalToRoot space in the `RigRemapTable` changes format. **All previous `RigRemapTable` BlobAssetReferences or usage of these in SubScenes need to be regenerated**.

### Changed
- Removed `PreAnimationGraphWriteTransformHandle` MonoBehaviour since default system definition does not support this operation.
- Upgraded com.unity.dataflowgraph to 0.13.0-preview.2

### Fixed
- Undeprecated `RigComputeMatricesSystem`, it's still necessary for pipelines to have a well defined sync point in order for all rig buffers to be up-to-date.
- Fixed AnimationSystemBase to be reactive to component data instead of previous `AnimationSystemOperation` flags. The `WriteTransformComponentJob` computes `AnimatedLocalToWorld` only for the required rig.
- Fixed NullReferenceException when using Menu option `Animation/Rig/Setup Rig Transforms` for the first setup.

### Deprecated
- Deprecated `SkinnedMesh` and `SkinnedMeshConversion` system to reduce setup complexity. You no longer need to add the `SkinnedMesh` component on the same GameObject than your `SkinnedMeshRenderer` to convert it. see `SkinnedMeshRendererConversion`

## [0.3.0-preview.8] - 2020-03-18

### Added
- Added scripting define symbols for package which can be added under `Project Settings/Player`
    * `UNITY_DISABLE_ANIMATION_CHECKS`: disables all validation checks in animation functions
    * `UNITY_DISABLE_ANIMATION_PROFILING`: disables all animation profiling markers
- Added `public interface IReadTransformHandle : IBufferElementData` to allow users to define a read transform handle for custom animation systems.
- Added `public interface IWriteTransformHandle : IBufferElementData` to allow users to define a write transform handle for custom animation systems.
- Added `public struct AnimationTransformOverride : IComponentData` to allow the animation system to override the transform system. This component defines WriteGroups for `LocalToParent` and `LocalToWorld`. The animation system will add this component data on all exposed transform entities with write access.
- Added `PostAnimationGraphReadTransformHandle`, `PostAnimationGraphWriteTransformHandle`, `PreAnimationGraphReadTransformHandle`, and `PreAnimationGraphWriteTransformHandle` MonoBehaviour. You need to use them on your GameObject transform hierarchy to define which transforms are exposed as read/write but also which animation graph should use this information to read from/write to entity transform components.
- Added `RigEntityBuilder.AddReadTransformHandle<T>(EntityManager entityManager, Entity rig, Entity transform, int index) where T : struct, IReadTransformHandle` to add a new expose transform that you want to read from.
- Added `RigEntityBuilder.AddWriteTransformHandle<T>(EntityManager entityManager, Entity rig, Entity transform, int index) where T : struct, IWriteTransformHandle` to add a new expose transform that you want to write to.

### Changed
- Deprecated `RigComputeMatricesSystem`. The system jobs are still executed but are now folded into `AnimationSystemBase`. They are scheduled only if the system needs to write back into transform entities or if the system has to update the skin matrices.
- Removed deprecated `MixerBeginNode`, `MixerAddNode` and `MixerEndNode`.
- Removed deprecated `IGraphOutput` and `AnimationGraphSystem`.
- Removed cached `LimbLengths` in `TwoBoneIKConstraint` which streamlines setup and lets users modify bone lengths dynamically. No significant performance loss was detected.
- `Core.MixerAdd` and `Core.MixerEnd` have changed function signatures to perform implace work on output stream directly:
    - `MixerAdd(ref AnimationStream output, ref AnimationStream input, ref AnimationStream add, float weight, float sumWeight)` changed to `MixerAdd(ref AnimationStream output, ref AnimationStream add, float weight, float sumWeight)`
    - `MixerEnd(ref AnimationStream output, ref AnimationStream input, ref AnimationStream defaultPose, float sumWeight)` changed to `MixerEnd(ref AnimationStream output, ref AnimationStream defaultPose, float sumWeight)`
- Upgraded com.unity.jobs to 0.2.7-preview.11
- Upgraded com.unity.rendering.hybrid to 0.4.0-preview.8
- Upgraded com.unity.burst to 1.3.0-preview.7
- Upgraded com.unity.entities to 0.8.0-preview.8
- Upgraded com.unity.collections to 0.7.0-preview.2
- Upgraded com.unity.dataflowgraph to 0.13.0-preview.1
- Moved simulation ports to kernel ports:
  * Changed LayerMixerNode.SimulationPorts.BlendingModes for LayerMixerNode.KernelPorts.BlendingModes
- Renamed  `IGraphTag` to `IAnimationSystemTag`

### Removed
- Removed `PreAnimationGraphTag`. Instead the animation system tag is now defined in `PreAnimationGraphSystem`. You can use `PreAnimationGraphSystem.TagComponent` to retrieve the tag.
- Removed `PostAnimationGraphTag`. Instead the animation system tag is now defined in `PostAnimationGraphSystem`. You can use `PostAnimationGraphSystem.TagComponent` to retrieve the tag.

### Fixed
- Fixed ComputeMatrixBuffer queries to include necessary `LocalToWorld` IComponentData to prevent errors from being thrown. Updated `RigEntityBuilder` creation functions to add the `LocalToWorld` IComponentData if it's not current on the entity.
- Fixed race condition crash when deleting a LayerMixer caused by internal allocation jobs for kernel memory.

### Known Issues
- Global scale is unsupported while reading from Expose transform.

## [0.3.0-preview.7] - 2020-02-10

### Changed
- Upgraded com.unity.test-framework to 1.1.11

## [0.3.0-preview.6] - 2020-02-07

### Changed
- Add missing Compositor node markup on `RootMotionNode` and `DeltaRootMotionNode`

## [0.3.0-preview.5] - 2020-02-07

### Changed
- Moved `BindingSet.Create` functions to be extension methods part of Clip and RigDefinition struct (i.e. `Clip.CreateBindingSet` and `RigDefinition.CreateBindingSet`).
- Propagated Additive port on all our clip nodes : `ClipPlayerNode`, `ConfigurableClipNode` and `UberClipNode`.
- `ClipNode` additive message input port is now a `bool` instead of an `int`.
- Updated animation core nodes to have better representative categories and documentation in Compositor UI. 

## [0.3.0-preview.4] - 2020-02-03

### Changed
- Changed how we allocate all DFG nodes' output buffers for AnimatedData to take into account optimized data format. 
  Please update all your custom DFG node from `Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output,
                Buffer<AnimatedData>.SizeRequest(rigDefinition.IsCreated ? rigDefinition.Value.Bindings.CurveCount : 0)
                );` to
                `Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output,
                Buffer<AnimatedData>.SizeRequest(rigDefinition.IsCreated ? rigDefinition.Value.Bindings.StreamSize : 0)
                );`
- AnimationStream is not anymore a generic type since now both ECS and DFG use the same data layout, all methods using an AnimationStream have been updated to reflect this change.
- Renamed ChannelWeightMixerNode.ChannelWeightBuffer to ChannelWeightMixerNode.WeightMasks and also changed the buffer type from `buffer<float>` to `buffer<WeightData>`.
- Changed RigDefinition.DefaultValues from a `struct BindingDefaultValues` to a `BlobArray<float> DefaultValues` for the new optimized data format, if you need to read some values from this buffer
please use `AnimationStream.FromDefaultValues(RigDefinition rig);` and use the stream accessor.
- The `AnimationStreamProvider` has been deprecated and all create functions have been moved to `AnimationStream` struct.
- All `BlobAssetReference<RigDefinition>` simulation ports on animation nodes have been changed to use the `Rig` IComponentData instead.
- Renamed `BufferElementToPortNode` to `BufferElementValueNode`
- Added shared DataFlowGraph `NodeDefinition` and `PortDefinition` attributes for better UI representation in Compositor
- Added shared `IRigContextHandler` in order to automate Rig port messaging on DataFlowGraphs in Compositor
- Upgraded com.unity.dataflowgraph to 0.12.0-preview.6
- Upgraded com.unity.jobs to 0.2.4-preview.11
- Upgraded com.unity.rendering.hybrid to 0.3.3-preview.11
- Upgraded com.unity.burst to 1.2.1
- Upgraded com.unity.test-framework.performance to 1.3.3-preview
- Downgraded com.unity.entities to 0.5.1-preview.11
- Downgraded com.unity.collections to 0.5.1-preview.11

### Added

- Added BindingSet.RotationChunkCount to iterate in chunk of 4 quaternions in blending operator for rotation data.
- Added BindingSet.DataChunkCount to iterate in block of float4 in blending operator for all other float datas (Translation, Scale, float, int).
- Added BindingSet.StreamSize to allocate enough memory for all stream operations.
- Added `public struct WeightData : IBufferElementData` which needs to be used on all nodes supporting weighting of channels, since now we have an optimized blending operator we need to splat weights from channels (i.e. translation) to sub component (t.x, t.y, t.z) for best performance.
- Added `var defaultStream = AnimationStream.FromDefaultValues(outputStream.Rig);` to create an animation stream from the rig default values.
- Added new utility class ClipTransformations which support a few data operation on Clip: `Clone()`, `CreatePose()`, `Reverse()`, and `FilterBindings()`.

## [0.3.0-preview.3] - 2020-01-20

### Changed

- Changed `FeatherBlendQuery` to `ChannelWeightQuery`.
- Changed `FeatherBlendTable` to `ChannelWeightTable`.
- Changed `FeatherBlendNode` to `ChannelWeightMixerNode`.
- Changed `DeltaNode` to `DeltaPoseNode`.
- Changed `AddNode` to `AddPoseNode`.
- Changed `InverseNode` to `InversePoseNode`.
- Added GetHashCode to dense clip to create better hash key for global clip instance cache. **[This change requires rebuilding all scene dependency caches of projects]**
- Added `PreAnimationGraphSystem` and `PostAnimationGraphSystem` which execute graph evaluations that run before and after the `TransformSystemGroup`. Any entities with component tags such as `PreAnimationGraphTag` or `PostAnimationGraphTag` will update the respective DataFlowGraph NodeSet in the systems.
- Upgraded com.unity.dataflowgraph to 0.12.0-preview.5
- Upgraded com.unity.entities to 0.6.0-preview.0
- Upgraded com.unity.collections to 0.6.0-preview.0
- Upgraded com.unity.jobs to 0.2.4-preview.0
- Upgraded com.unity.rendering.hybrid to 0.3.3-preview.0
- Upgraded com.unity.burst to 1.2.0-preview.12
- Upgraded com.unity.test-framework to 1.1.10
- Fixed typo in struct name `BlendTree2DSimpleDirectionnal` and rename it to `BlendTree2DSimpleDirectional`.
- Fixed typo in method name `Core.ComputeBlendTree2DSimpleDirectionnalWeights` and rename it to `Core.ComputeBlendTree2DSimpleDirectionalWeights`.
- Fixed typo in method name `BlendTreeBuilder.CreateBlendTree2DSimpleDirectionnal` and rename it to `BlendTreeBuilder.CreateBlendTree2DSimpleDirectional`.
- Changed `WeightBuilderNode` to take a message of the BlobAssetReference of the RigDefinition instead of an integer, to set the size of the output weight buffer.
- Changed method `unsafe public static AnimationStream<AnimationStreamOffsetPtrDescriptor> Create(
            BlobAssetReference<RigDefinition> rig,
            NativeArray<AnimatedLocalTranslation> localTranslations,
            NativeArray<AnimatedLocalRotation> localRotations,
            NativeArray<AnimatedLocalScale> localScales,
            NativeArray<AnimatedFloat> floats,
            NativeArray<AnimatedInt> ints)` to 
              `unsafe public static AnimationStream<AnimationStreamOffsetPtrDescriptor> Create(
            BlobAssetReference<RigDefinition> rig,
            NativeArray<AnimatedData> buffer)`.
- Animation nodes have been refactored to use `Buffer<AnimatedData>` instead of `Buffer<float>` as animation stream kernel ports. Since the data type is now the same between ECS and DataFlowGraph it removes the need for any conversion nodes and extra connections. **[Custom animation nodes will need to change their animation stream kernel ports to `Buffer<AnimatedData>`]**
- Renamed `AnimationStreamOffsetPtrDescriptor` to `AnimationStreamPtrDescriptor`

### Fixed
- Fixed InvalidOperationExceptions being thrown in UberClipNode when a Clip has only one of translation and rotation channels.

### Deprecated

- Deprecated `MixerBeginNode`, `MixerAddNode` and `MixerEndNode`. Use `NMixerNode` instead.
- Deprecated `GraphOutput` component. Use DataFlowGraph ComponentNodes instead with `OutputRigBuffersNode`
- Deprecated `AnimationGraphSystemBase`. Use either the `PreAnimationGraphSystem` or `PostAnimationGraphSystem` which run before and after the `TransformSystemGroup`.

### Added

- New `DefaultValuesNode` which fills the animation stream with default values from the RigDefinition.
- Utility DataFlowGraph nodes to set and extract information from the AnimationStream
  - `GetAnimationStreamLocalToParentNode`, `GetAnimationStreamLocalToRootNode`, `GetAnimationStreamFloatNode` and `GetAnimationStreamIntNode`.
  - `SetAnimationStreamLocalToParentNode`, `SetAnimationStreamLocalToRootNode`, `SetAnimationStreamFloatNode` and `SetAnimationStreamIntNode`
- Utility conversion DataFlowGraph nodes to simplify communication between ECS component types and animation nodes
  - `ConvertLocalToWorldComponentToFloat4x4Node`, `ConvertFloat4x4ToLocalToWorldComponentNode`, `ConvertLocalToParentComponentToFloat4x4Node` and `ConvertFloat4x4ToLocalToParentComponentNode`
- Added two new methods `Core.ComputeBlendTree1DDuration` and `Core.ComputeBlendTree2DSimpleDirectionalDuration`.
- Added overload for UberClipNode.Bake that takes an existing NodeSet.
- Added a kernel input data port to the `WeightBuilderNode` to initialize the default value of the weights in the ouput buffer (so that it can be different from 0).
- Added `AnimatedData` IBufferElement that represent any kind of animation data, to read/write typed values you need an `AnimationStream` and a `RigDefinition`.

### Removed
- Removed `AnimatedLocalTranslation`, to access the same data you need to create an `AnimationStream` with `RigDefinition` and `AnimatedData` IBufferElement and call either `SetLocalToParentTranslation` or `GetLocalToParentTranslation`.
- Removed `AnimatedLocalRotation`, to access the same data you need to create an `AnimationStream` with `RigDefinition` and `AnimatedData` IBufferElement and call either `SetLocalToParentRotation` or `GetLocalToParentRotation`.
- Removed `AnimatedLocalScale`, to access the same data you need to create an `AnimationStream` with `RigDefinition` and `AnimatedData` IBufferElement and call 
either `SetLocalToParentScale` or `GetLocalToParentScale`.
- Removed `AnimatedFloat`, to access the same data you need to create an `AnimationStream` with `RigDefinition` and `AnimatedData` IBufferElement and call 
either `SetFloat` or `GetFloat`.
- Removed `AnimatedInt`, to access the same data you need to create an `AnimationStream` with `RigDefinition` and `AnimatedData` IBufferElement and call 
either `SetInt` or `GetInt`.

## [0.3.0-preview.2] - 2019-12-10

### Changed
- Upgraded com.unity.dataflowgraph to 0.12.0-preview.4

## [0.3.0-preview.1] - 2019-12-06

### Changed
- We now have [Pre/Post]AnimationSystemGroups that executes before/after the TransformSystemGroup.
- ClipInstance refactor in DFG node: 
  * Removing ClipInstance from ClipNode and all node that use the ClipNode to generalize the animation graph to any type of Rig.  
The ClipNode now take a RigDefinition and a Clip, when changing either the RigDefinition or the Clip, the ClipNode will generate the ClipInstance for the pair <RigDefinition,Clip>.  
With the ClipManager you can pre generate those clip to avoid a CPU spike when changing the rig definition for a ClipNode.
  * Removing ClipInstance from ClipPlayerNode.
  * Removing ClipInstance from ConfigurableClipNode.
  * Removing ClipInstance from UberClipNode.
- Changed method `Core.EvaluateClip<T>(ref ClipInstance clipInstance, float time, ref AnimationStream<T> stream, int additive)` to `Core.EvaluateClip<T>(BlobAssetReference<ClipInstance> clipInstance, float time, ref AnimationStream<T> stream, int additive)`
- SharedRigDefinition has been replaced by Rig (IComponentData) and when memory chunking is needed you can use the SharedRigHash (SharedComponentData).
- Upgraded com.unity.entities to 0.3.0-preview.4
- Upgraded com.unity.collections to 0.3.0-preview.0
- Upgraded com.unity.jobs to 0.2.1-preview.3
- Upgraded com.unity.burst to 1.2.0-preview.9
- Upgraded com.unity.dataflowgraph to 0.12.0-preview.3
- Moved simulation ports to kernel ports:
  * Changed MixerNode.SimulationPorts.Blend for MixerNode.KernelPorts.Weight
  * Changed LayerMixerNode.SimulationPorts.WeightInputN to port array and renamed to LayerMixerNode.KernelPorts.Weights.
  * Changed ClipPlayerNode.SimulationPorts.Speed to ClipPlayerNode.KernelPorts.Speed.
  * Changed TimeCounterNode.SimulationPorts.Speed to TimeCounterNode.KernelPorts.Speed.
  * Changed BlendTree1DNode.SimulationPorts.Parameter to BlendTree1DNode.KernelPorts.BlendParameter.
  * Changed BlendTree1DNode.SimulationPorts.Duration to BlendTree1DNode.KernelPorts.Duration.
  * Changed BlendTree2DNode.SimulationPorts.Parameter to BlendTree2DNode.KernelPorts.BlendParameterX and BlendTree2DNode.KernelPorts.BlendParameterY.
  * Changed BlendTree2DNode.SimulationPorts.Duration to BlendTree2DNode.KernelPorts.Duration.
  * Changed LayerMixerNode.SimulationPorts.MaskInput0 to port array and renamed to LayerMixerNode.KernelPorts.WeightsMask. You can now either specify a weight per bindings if WeightsMask is connected to something or no masking at all if it's not connected. The weight mask is modulated by the layer weight.
- Changed LayerMixerNode.KernelPorts.Input0 to port array and renamed to LayerMixerNode.KernelPorts.Inputs.
- Changed LayerMixerNode.SimulationPorts.BlendModeInput0 to port array and renamed to LayerMixerNode.SimulationPorts.BlendingModes.
- Changed `AnimationStream.GetLocalToRigTranslation` to `AnimationStream.GetLocalToRootTranslation`.
- Changed `AnimationStream.SetLocalToRigTranslation` to `AnimationStream.SetLocalToRootTranslation`.
- Changed `AnimationStream.GetLocalToRigRotation` to `AnimationStream.GetLocalToRootRotation`.
- Changed `AnimationStream.SetLocalToRigRotation` to `AnimationStream.SetLocalToRootRotation`.
- Changed `AnimationStream.GetLocalToRigMatrix` to `AnimationStream.GetLocalToRootMatrix`.
- Changed `AnimationStream.GetLocalToRigTR` to `AnimationStream.GetLocalToRootTR`.
- Changed `AnimationStream.SetLocalToRigTR` to `AnimationStream.SetLocalToRootTR`.
- Changed `AnimatedLocalToRig` IBufferElementData to `AnimatedLocalToRoot`.
- Renamed WeightNode to WeightPoseNode.

### Added
- Dependency on com.unity.rendering.hybrid 0.3.0-preview.4 for GPU skinning
- Added `ComputeBlendTree1DWeightsNode`, `ComputeBlendTree2DWeightsNode`, and `BufferElementToPortNode` to let you compute in DFG render phase the blend tree weights and duration based on the blend parameter.
- Added LayerMixerNode.SimulationPorts.LayerCount to let you define how many layer the node can handle.
- Added TwoBoneIK solver `Core.SolveTwoBoneIK` and DFG node `TwoBoneIKNode`
- Added PositionConstraint solver `Core.SolvePositionConstraint` and DFG node `PositionConstraintNode`
- Added RotationConstraint solver `Core.SolveRotationConstraint` and DFG node `RotationConstraintNode`
- Added ParentConstraint solver `Core.SolveParentConstraint` and DFG node `ParentConstraintNode`
- Added AimConstraint solver `Core.SolveAimConstraint` and DFG node `AimConstraintNode`
- Added feather blending overload of `Core.Blend` and a DFG node `FeatherBlendNode` that blends two clips according to a weight buffer.
- Added a `WeightBuilderNode` to build a buffer of weights of the rig size from a potentially smaller set of input weights.

### Removed
- Removed built-in support for nested blend tree. Rather than nesting everything under the same blob asset we will provide a set of blend tree nodes that will provide port connection for any kind of input (ClipNode, ConfigurableClipNode, UberClipNode, BlendTree1DNode, BlendTree2DNode, etc.).
- Removed blendParameter from `BlobAssetReference<BlendTree1D> CreateBlendTree(BlendTree1DMotionData[] motionData, StringHash blendParameter)`. BlendParameter is now a kernel port on BlendTree1DNode.
- Removed blendParameterX and blendParameterY from `BlobAssetReference<BlendTree2DSimpleDirectionnal> CreateBlendTree2DSimpleDirectionnal(BlendTree2DMotionData[] motionData, StringHash blendParameterX, StringHash blendParameterY)`. BlendParameter is now a kernel port on BlendTree2DNode.
- Removed IMotion, INormalizeTime, and IBlendTree interfaces as we don't need them anymore for the nested blend tree.
- Removed Parameter struct used by blend tree for setting blend parameter. Each blend tree node has a kernel port for the blend parameter.
- Removed setup constraint on blend tree, you no longer need to set the rig definition before the blend tree asset. Assets can now be setup in any order.
	


## [0.2.16-preview.1] - 2019-10-31

### Changed
- Upgraded com.unity.entities to 0.2.0-preview.12
- Upgraded com.unity.collections to 0.2.0-preview.7
- Upgraded com.unity.jobs to 0.2.0-preview.7

## [0.2.15-preview.1] - 2019-10-24

### Added
- Baking of a UberClipNode. UberClipNode let you Loop Transform, In Place, Compute Root Motion, etc. on the fly at runtime. 
  However those computation are cpu intensive and can be baked down to a simple clip evaluation.
  
### Fixed
- Reviewed animation clip resampling. More precisely about what happens around evaluation at stop time (Duration). 
  Clips with duration not at a multiple of sample rate were not evaluated correctly around last frame.

## [0.2.15] - 2019-10-23

### Changed
- Upgraded com.unity.dataflowgraph to 0.11.8-preview

## [0.2.14] - 2019-10-18

### Added
- Added RigComponent and RigConversion System for conversion workflow.

### Changed
- BlendTree1DNode changed FileNotFoundException for InvalidOperationException since we are not loading anymore the motion from the file system.
- BlendTree2DNode changed FileNotFoundException for InvalidOperationException since we are not loading anymore the motion from the file system.
- Upgraded com.unity.burst to 1.2.0-preview.6
- Upgraded com.unity.dataflowgraph to 0.11.7-preview
- Upgraded com.unity.entities to 0.2.0-preview.1
- Upgraded com.unity.collections to 0.1.2-preview.1
- Upgraded com.unity.jobs to 0.1.2-preview.1
- Upgraded com.unity.test-framework to 1.1.2
- Upgraded com.unity.test-framework.performance to 1.3.0-preview

### Fixed
- Fixed BlendTree1DNode throwing error when changing rig definition.

### Deprecated
- Deprecating Skeleton, please use a RigComponent instead.

### Removed
- Removed WeakAssetReference, Asset serialization is now handled by DOTS conversion workflow. All animation asset should now be stored in DOTS entities file format.

### Know Isssues
- Conversion of nested Blend Tree is disabled by default. We can't nest blob asset reference into a blob asset reference. Nested Blend tree created at runtime work.
- BlobAssetReference on ISharedComponent is not currectly supported by DOTS conversion workflow. Since we are using a Shared component to batch togheter similar rig
  for performance reason, you need to manually setup the rig entity at Initialization with a call to: 
  ```
    if (EntityManager.HasComponent<Unity.Animation.RigDefinitionComponent>(rigEntity))
    {
        var rigDefinition = EntityManager.GetComponentData<Unity.Animation.RigDefinitionComponent>(rigEntity);
        if(!rigDefinition.Value.IsCreated)
            throw new System.ObjectDisposedException("RigDefinition is not Created");
        RigEntityBuilder.SetupRigEntity(rigEntity, EntityManager, rigDefinition.Value);
    }
  ```

## [0.2.13] - 2019-09-13

### Added
- Added support for additionnal curve from the ModelImporter. You can now use the Additionnal curve from the ModelImporter to create custom float curve like IK weight.

### Changed
- Renamed RigComputeGlobalSystem to RigComputeMatricesSystem and added extra AnimatedLocalToRig buffer
- Upgraded com.unity.dataflowgraph to 0.11.5-preview

## [0.2.12] - 2019-09-05

### Changed
- Upgraded com.unity.dataflowgraph to 0.11.2-preview

## [0.2.11] - 2019-09-04

### Changed
- Fixed bug with AnimationClip converter converting the preview clip.
- Deprecating AdditiveClips converter. Rather than relying on the old tech(animator) to bake clips we will use the DeltaNode at runtime to generate an additive animation stream.
- Updated UNodeSystemBase as a generic class that filters on input/output components part of an entity. This simplifies declaring multiple UNodeSystems within one animation pipeline.
- Removed UNodeOutputToRigBuffersSystemBase. The UNodeSystemBase now handles preparing input data coming from ECS to UNode and output data going from UNode to ECS.
- Upgraded com.unity.unode from 0.9.3-preview to com.unity.dataflowgraph 0.11.1-preview. Package was renamed.
- Fixed bug with Source control which wasn't openning for edit the requested Animation clip asset.
- Bursted job now use fast-math

## [0.2.10] - 2019-08-26

### Added
- Added debbuger view proxy for AnimationStream
- Added DeltaNode to generate additive animation at runtime.

### Changed
- Upgraded com.unity.unode to 0.9.3-preview

## [0.2.9] - 2019-08-16

### Changed
- Upgraded com.unity.unode to 0.9.2-preview
- Upgraded com.unity.entities to 0.1.1-preview
- Upgraded com.unity.collections to 0.1.1-preview
- Upgraded com.unity.jobs to 0.1.1-preview

## [0.2.8] - 2019-08-07

### Added
- Added unodes needed to implement Absolute Root Motion: InPlaceMotionNode, InPlaceClipNode, CycleRootMotionNode and CycleRootClipNode.
- Added support for 2d Simple Directionnal Blend Tree.
- Bone Rendering:
	- Added a BoneRendererAuthoring script (MonoBehaviour) to author a bone renderer.
	- Added the BoneRendererAuthoringConversionSystem to convert the BoneRendererAuthoring components into bone renderer entities.
	- Added the system BoneRendererMatrixSystemBase to compute the bones' matrices.
	- Added the system BoneRendererRenderingSystemBase to render the bones.
	- Added various BoneRendererComponents: 
		- BoneColor, BoneRendererEntity, BoneSize.
		- BoneWorldMatrix, RigIndex and RigParentIndex that are buffer elements.
		- RigEntity that is a shared component that references the original rig.
		- BoneShape that is another shared component used for the bones' draw instancing.
	- Added a BoneRendererSystemGroup that executes inside the AnimationSystemGroup, after the RigComputeGlobalSystem.
- Curves:
	- Added the KeyframeCurve struct that represents a curve with a NativeArray of keyframes.
	- Added the KeyframeCurveBlob struct that represents a curve with a BlobArray of keyframes.
	- Added the KeyframeCurveAccessor: a struct with a pointer used to unify the evaluation of the Native/Blob arrays.
	- Added the KeyframeCurveEvaluator: evaluates any type of KeyframeCurve (native array, blob, or accessor) by doing an Hermite interpolation between the keyframes. No cache is used.
	- Added tests for the conversion between several types of curves.
	- Added tests for the relative precision of the evaluation.

### Changed
- Changed TimeLoopNode to output curent cycle and normalized time.
- Removed FutureSampledAnimation IComponentData
- Upgraded com.unity.unode to 0.8.1-preview
- Upgraded com.unity.entities to 0.1.0-preview
- Upgraded com.unity.collections to 0.1.0-preview
- Upgraded com.unity.jobs to 0.1.0-preview
- Upgraded com.unity.burst to 1.1.2
- Upgraded com.unity.mathematics to 1.1.0

## [0.2.7] - 2019-06-21

### Changed
- Upgraded uNode to 0.7.2-preview.animation
- Upgraded Burst to 1.1.0-preview.2

## [0.2.6] - 2019-06-21

### Added
- Added support for nested 1d Blend Tree.
- Added support for multi input mixer with MixerBeginNode -> MixerAddNode -> MixerAddNode -> MixerEndNode. Add as many MixerAddNode than the number of input you need.
- Time management nodes and al. Major revamp where all timing related code was removed from specific nodes to instead be made explicit in unode graph.
- Renamed LayerMixerNode input kernel port: Input1->Input0, Input2->Input1, Input3->Input2, Input4->Input3.
- Renamed LayerMixerNode input simulation port: BlendModeInput1->BlendModeInput0, BlendModeInput2->BlendModeInput1, BlendModeInput3->BlendModeInput2, BlendModeInput4->BlendModeInput3.
- Renamed LayerMixerNode input simulation port: WeightInput1->WeightInput0, WeightInput2->WeightInput1, WeightInput3->WeightInput2, WeightInput4->WeightInput3.
- Renamed LayerMixerNode input simulation port: MaskInput1->MaskInput0, MaskInput2->MaskInput1, MaskInput3->MaskInput2, MaskInput4->MaskInput3.

### Changed
- Upgraded uNode to 0.7.1-preview.animation

## [0.2.5] - 2019-06-06

### Fixed
- Implement IEquatable for SkinRenderer since it has some managed fields and this is now a requirement for Entities.0.0.12-preview.33.

## [0.2.4] - 2019-06-04

### Added
- Added support for generic animation retargeting 
- Added support for 1d Blend Tree
- Animation menu contains two new actions: Create Blend Tree and Convert Blend Tree
- Added BlendTreeBuilder class to create DOTs Blend Tree asset from script
- Added BlendTree1DNode for UNode

### Changed
- Upgraded Entities to 0.0.12-preview.33
- Upgraded test-framework to 1.0.13
- Upgraded test-framework.performance to 1.2.0
- Upgraded uNode to 0.6.1-preview.animation
- Upgraded Burst to 1.0.4
- Removed Root Motion implementation for better rewrite. All Blob Assets need to be regenerated.
- All data structures using UnityEngine.PropertyName have been changed to use a new StringHash implementation. All Blob Assets need to be regenerated.
- Renamed MixerNode input port: Input1->Input0, Input2->Input1.
- Changed behaviour for unconnected mixer node input:
	- If no input is connected at all it returns the bind pose.
	- If there is only one connected input it returns a mix between the bind pose and the connected input.
	- If both inputs are connected it returns a mix between both inputs.

## [0.2.3] - 2019-05-01

### Fixed
- [case 1149350] Fixed mixer node crashing when no inputs are connected, should throw an InvalidOperationException now but only when burst is disabled

### Added
- New tools to setup a batch of animation clip as Additive animation clip and convert them to DOTs animation clip(only works with generic rig animation clip)
- New animation stream helper that works in ECS/UNode.
- Added WeakAssetReference to blobify animation data for runtime. Animation menu contains two new actions: Import Animation Clips and Import Rig.
- Added RigRemapperNode to map a source rig definition on a destination rig definition.
- Added RigRemapQuery to create a remap table between two rig.

### Changed
- Animation Systems are now grouped into AnimationSystemGroup, each system in this group derive from a base system to allow user to reconfigure the animation pipeline
- Changed occurence of UnityEngine.Debug.Assert to UnityEngine.Assertions.Assert
- Upgraded burst to 1.0.8 (Fix needed to run playmode test in standalone)
- Upgraded UNode to 0.5.4-animationpreview.1
- Upgraded Entities to 0.0.12-preview.30
- Removed SkeletonBindingBufferSystem and related structs. Used the AnimationStream instead to do the mapping between ECS and uNode.
- Renamed UNodeWorldSystem to UNodeSystem.
- Renamed UNodeSystem.WorldSet to UNodeSystem.Set
- Renamed GraphOutputToLocalsSystem to UNodeOutputToRigBuffersSystem
- Renamed SkeletonComputeGlobalSystem to RigComputeGlobalSystem
- Renamed SkinnedMeshComputeSkinMatrixSystem to ComputeSkinMatrixSystem
- Renamed SkinnedMeshPushToRendererSystem to PushSkinMatrixToRendererSystem

## [0.2.2] - 2019-03-28

### Changed
- All jobs now use async compilation mode
- Renamed everything called "position" into "translation"
- Renamed "SkinnedMeshComponentData.Skeleton" to "SkinnedMeshComponentData.RigEntity"
- Upgrade to com.unity.unode@0.5.1-animationpreview.1

### Fixes
- Fix position and rotation glitch when looping on a clip with root motion
- Fix performance test not working anymore with com.unity.entity 0.0.12-preview.28
- Fix SkinnedMeshComputeSkinMatrixSystem throwing assert when running on Entity without Rig buffer

## [0.2.1] - 2019-03-26

### Fixes
- Upgrade to Burst v1.0.0-preview.6; fixes issue with BlobAssetHeader
- Added dependency to UNode v0.5.0-animationpreview.1

## [0.2.0] - 2019-03-25

### Added
- New RigGenerator class (imported from Unity project)
- New wrapper around Debug.Log\* methods
- Add tests for the root motion

### Changed
- Use full path for the bindings
- Throw warnings if the ref count is wrong in UNodeWorldSystem instead of silently fail
- Blob files for performance tests are now generated before running the tests
- Merge changes from A2

### Fixed
- Fix uninitialized values in Blob assets
- Fix profiler markers in perf tests
- Fix memory leaks in tests
- Fix the root motion computation

## [0.1.0] - 2019-03-04

### This is the first release of *Unity Package Animation*.

This package adds a brand new animation system, leveraged with DOTS
(Data-Oriented Tech Stack) and completely written in HPC# (High Performance
C#).
