# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

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
