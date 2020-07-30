# DataFlowGraph Nodex Index

This is a list of the available nodes, with a brief description.

## Constraints

- **AimConstraintNode**: Aim constraint based on multiple sources.
- **ParentConstraintNode**: Parent constraint based on multiple sources.
- **PositionConstraintNode**: Position constraint based on multiple sources.
- **RotationConstraintNode**: Rotation constraint based on multiple sources.
- **TwistCorrectionNode**: Twist correction is mainly used to redistribute a percentage of the source rotation over a leaf bone in order to correct mesh deformation artifacts.
- **TwoBoneIkNode**: Two bone IK solver.

## Conversion

- **ConvertLocalToWorldComponentToFloat4x4Node** (in ConvertFloat4x4.cs): Converts LocalToWorld component data to float4x4.
- **ConvertFloat4x4ToLocalToWorldComponentNode** (in ConvertFloat4x4.cs): Converts float4x4 to LocalToWorld component data.
- **ConvertLocalToParentComponentToFloat4x4Node** (in ConvertFloat4x4.cs): Converts LocalToParent component data to float4x4.
- **ConvertFloat4x4ToLocalToParentComponentNode** (in ConvertFloat4x4.cs): Convert float4x4 to LocalToParent component data.

## Float

- **FloatAddNode**
- **FloatMulNode**
- **FloatRcpNode**
- **FloatRcpSimNode**
- **FloatSubNode**

## Pass Through

- **SimPassThroughNode** (in PassThroughNodex.cs)
- **KernelPassThroughNodeFloat** (in PassThroughNodex.cs)
- **KernelPassThroughNodeBufferFloat** (in PassThroughNodex.cs)

## Root Motion

- **CycleRootMotionNode**: Computes and sets the total root motion offset amount based on the number of cycles for a given clip. This node is internally used by the UberClipNode.
- **DeltaRootMotionNode**: Computes the delta root motion from a previous and current animation stream. This node is internally used by the UberClipNode.
- **InPlaceRootMotionNode**: Extracts motion from a specified transform and projects it's values on the root transform. This node is internally used by the UberClipNode.
- **RootMotionFromVelocityNode**: Computes root motion values from a baked clip. Used internally by the UberClipNode.

## Time

- **DeltatimeNode**: Computes delta time.
- **NormalizedTimeNode**: Computes normalized time [0, 1] given an input time and duration.
- **TimeCounterNode**: Accumulates and outputs current time based on scale and delta time.
- **TimeLoopNode**: Computes looping time and cycle count given a duration and unbound time.

## Uber

- **BlendTree1DNode**: Evaluates a 1D BlendTree based on a blend parameter.
- **BlendTree2DNode**: Evaluates a 2D BlendTree based on X and Y blend parameters.
- **ClipPlayerNode**: Evaluates an animation clip given a clip configuration and time value.
- **ConfigurableClipNode**: Evaluates a clip based on the clip configuration mask.
- **DeltaPoseNode**: Computes the delta animation stream given two input streams.
- **LoopNode**
- **UberClipNode**: Clip node that can perform different actions based on clip configuration data and supports root motion.

## Miscellaneous

- **AddPoseNode**: Adds two animation streams.
- **GetAnimationStreamFloatNode** (in AnimationStreamFloatNode.cs): Gets a float value from the AnimationStream.
- **SetAnimationStreamFloatNode** (in AnimationStreamFloatNode.cs): Sets a float value in the AnimationStream.
- **GetAnimationStreamIntNode** (in AnimationStreamIntNode.cs): Gets an integer value from the AnimationStream.
- **SetAnimationStreamIntNode** (in AnimationStreamIntNode.cs): Sets an integer value in the AnimationStream.
- **GetAnimationStreamLocalToParentNode** (in AnimationStreamLocalToParent.cs): Gets the local to parent information of a bone in the AnimationStream.
- **SetAnimationStreamLocalToParentNode** (in AnimationStreamLocalToParent.cs): Sets the local to parent information of a bone in the AnimationStream.
- **GetAnimationStreamLocalToRootNode** (in AnimationStreamLocalToRoot.cs): Gets the local to root information of a bone in the AnimationStream.
- **SetAnimationStreamLocalToRootNode** (in AnimationStreamLocalToRoot.cs): Sets the local to root information of a bone in the AnimationStream.
- **GetBufferElementValueNode** (in BufferElementValueNode.cs): Gets a value given an index from a buffer.
- **ChannelWeightMixerNode**: Blends two animation streams given per channel weight values. Weight masks can be built using the WeightBuilderNode.
- **ClipNode**: Base clip sampling node.
- **ComputeBlendTree1DWeightsNode**: Computes 1D BlendTree weights based on parameter input.
- **ComputeBlendTree2DWeightsNode**: Computes 2D BlendTree weights based on parameter input.
- **DefaultValuesNode**: Outputs the default values of a RigDefinition as an animation stream (i.e. the bind pose).
- **EvaluateCurveNode**: Samples an AnimationCurve at a given time.
- **InversePoseNode**: Computes the inverse animation stream.
- **LayerMixerNode**: Blends animation streams based on an ordered layer approach. Each layer can blend in either override or additive mode. Weight masks can be built using the WeightBuilderNode.
- **MixerNode**: Blends two animation streams given an input weight value.
- **NMixerNode**: Blends N animation streams together given an N buffer of weights where the value at index *i* is the weight of the *i*eth stream.
- **RigRemapperNode**: Remaps one animation stream to another given a known remapping table.
- **WeightBuilderNode**: Creates weight masks based on passed channel indices and weights.
- **WeightPoseNode**: Applies a set of weights to an animation stream.
- **WorldToRootNode**: Given a LocalToWorld component, outputs its value in rig space.
