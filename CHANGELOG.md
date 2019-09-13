# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.2.14] - unreleased

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
