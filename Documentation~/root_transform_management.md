# Root transform management

The root transform needs to be dealt differently than other transforms in the animation systems.

### Default behavior

By default, we always update the root (first bone specified in the `RigComponent`) entity transform component values using the following logic:
  1) Before evaluating the animation graph, root entity transform components are copied to the animation stream.
  2) Once the graph is evaluated, the new root transform values from the animation stream are copied back to the entity transform components and the root transform values in the stream are reset to identity.

The rig entity (the entity holding the `Rig`, `AnimatedData`, `AnimatedLocalToWorld`, etc. components) also holds an entity reference to the
root via the `RigRootEntity` component.

### Root motion

Root motion (or accumulation of delta root transforms) is a special case in animation. This requires an AnimationSystem specific `IAnimatedRootMotion` component on the rig entity.
When the `IAnimatedRootMotion` component is found, this disables default root transform handling as described above.
Any root displacement evaluated in an animation graph via the animation stream is accumulated on the root entity transform components.
Once the accumulation computed, the root transform values in the animation stream are reset to identity.
The major difference here is that the entity root transform component values are never copied to the animation stream prior to graph evaluation.
Any root values changed in the animation stream during graph evaluation is accumulated later in the entity transform components.
An optional user defined offset to root motion can be specified using `RootMotionOffset`.

### User defined behviour

If you would like to disable the default behaviour to roll your own solution in terms of root transform handling, you can simply add the `DisableRootTransformReadWriteTag` on the rig entity. 
When present, it prevents systems from doing any sort of work to manage the root. Put differently, the root transform values will remain in the animation stream and not be copied back to their entity transform components.
