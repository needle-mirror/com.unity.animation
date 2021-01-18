using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Unity.Animation.StateMachine
{
    internal struct StateMachineDefinition
    {
        public BlobArray<Hash128>                         States;
        public BlobArray<int>                             OutgoingTransitionsStartIndices;
        public BlobArray<EnterSelectorTransition>         OnEnterSelectors;
        public BlobArray<TransitionDefinition>            GlobalTransitions;
        public BlobArray<TransitionDefinition>            OutgoingTransitions;
        public BlobArray<TransitionConditionFragment>     ConditionFragments;
    }

    static internal class StateMachineBuilder
    {
        internal static BlobAssetReference<StateMachineDefinition> BuildStateMachineDefinitionFromGraph(BlobAssetReference<Graph> graph)
        {
            var enterStateSelectors = new NativeList<CreateTransitionCommand>(Allocator.Temp);
            var globalTransitions = new NativeList<CreateTransitionCommand>(Allocator.Temp);
            var stateToStateTransitions = new NativeList<CreateTransitionCommand>(Allocator.Temp);
            for (int i = 0; i < graph.Value.CreateTransitionCommands.Length; ++i)
            {
                var transition = graph.Value.CreateTransitionCommands[i];
                if (transition.Type == TransitionType.Global)
                    globalTransitions.Add(transition);
                else if (transition.Type == TransitionType.OnEnterSelector)
                    enterStateSelectors.Add(transition);
                else
                    stateToStateTransitions.Add(transition);
            }

            int maxNumberOfStateToStateTransitions = stateToStateTransitions.Length;
            int numberStates = graph.Value.CreateStateCommands.Length;

            BlobAssetReference<StateMachineDefinition> definitionAsset;
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            {
                ref var sm = ref blobBuilder.ConstructRoot<StateMachineDefinition>();

                var statesArray = blobBuilder.Allocate(ref sm.States, numberStates);
                var outgoingIndices = blobBuilder.Allocate(ref sm.OutgoingTransitionsStartIndices, numberStates);
                for (int i = 0; i < numberStates; ++i)
                {
                    statesArray[i] = graph.Value.CreateStateCommands[i].DefinitionHash;
                    outgoingIndices[i] = maxNumberOfStateToStateTransitions;
                }

                var conditionFragmentsArray = blobBuilder.Allocate(ref sm.ConditionFragments, graph.Value.CreateConditionFragmentCommands.Length);
                for (int i = 0; i < graph.Value.CreateConditionFragmentCommands.Length; ++i)
                {
                    conditionFragmentsArray[i] = BuildConditionFragment(graph.Value.CreateConditionFragmentCommands[i], i, ref conditionFragmentsArray);
                }

                var onEnterSelectorsArray = blobBuilder.Allocate(ref sm.OnEnterSelectors, enterStateSelectors.Length);
                var globalTransitionsArray = blobBuilder.Allocate(ref sm.GlobalTransitions, globalTransitions.Length);
                var outgoingTransitionsArray = blobBuilder.Allocate(ref sm.OutgoingTransitions, stateToStateTransitions.Length);

                int index = 0;
                for (index = 0; index < enterStateSelectors.Length; index++)
                {
                    var newEnterSelector = new EnterSelectorTransition();
                    FillTransitionDefinitionFromCommand(ref newEnterSelector.TransitionDefinition, enterStateSelectors[index]);
                    FillOverrideFromCommand(ref newEnterSelector.PropertiesOverride, enterStateSelectors[index]);
                    onEnterSelectorsArray[index] = newEnterSelector;
                }

                index = 0;
                for (index = 0; index < globalTransitions.Length; index++)
                {
                    var newGlobal = new TransitionDefinition();
                    FillTransitionDefinitionFromCommand(ref newGlobal, globalTransitions[index]);
                    globalTransitionsArray[index] = newGlobal;
                }

                index = 0;
                UnsafeHashMap<int, UnsafeList<TransitionDefinition>> sortedTransitions = new UnsafeHashMap<int, UnsafeList<TransitionDefinition>>(numberStates, Allocator.Temp);
                for (index = 0; index < stateToStateTransitions.Length; index++)
                {
                    var stateToState = stateToStateTransitions[index];
                    var newTransition = new TransitionDefinition();
                    FillTransitionDefinitionFromCommand(ref newTransition, stateToState);
                    UnsafeList<TransitionDefinition> currentList;
                    if (!sortedTransitions.TryGetValue(stateToState.SourceState, out currentList))
                    {
                        currentList = new UnsafeList<TransitionDefinition>(1, Allocator.Temp);
                        currentList.Add(newTransition);
                        sortedTransitions.Add(stateToState.SourceState, currentList);
                    }
                    else
                    {
                        currentList.Add(newTransition);
                        sortedTransitions[stateToState.SourceState] = currentList;
                    }
                }

                index = 0;
                var transitionsKeyValues = sortedTransitions.GetKeyValueArrays(Allocator.Temp);
                for (int i = 0; i < transitionsKeyValues.Length; ++i)
                {
                    for (int j = 0; j < transitionsKeyValues.Values[i].Length; ++j)
                    {
                        var newTransition = transitionsKeyValues.Values[i][j];
                        if (newTransition.SourceStateIndex != -1 && outgoingIndices[newTransition.SourceStateIndex] == maxNumberOfStateToStateTransitions)
                        {
                            outgoingIndices[newTransition.SourceStateIndex] = index;
                        }
                        outgoingTransitionsArray[index] = newTransition;
                        ++index;
                    }

                    transitionsKeyValues.Values[i].Dispose();
                }

                transitionsKeyValues.Dispose();

                sortedTransitions.Dispose();

                definitionAsset = blobBuilder.CreateBlobAssetReference<StateMachineDefinition>(Allocator.Persistent);
            }
            blobBuilder.Dispose();
            enterStateSelectors.Dispose();
            globalTransitions.Dispose();
            stateToStateTransitions.Dispose();

            return definitionAsset;
        }

        static void FillOverrideFromCommand(ref StateMachine.TransitionPropertiesOverride propertiesOverride, CreateTransitionCommand onEnterSelector)
        {
            propertiesOverride.OverrideDuration = onEnterSelector.OverrideDuration;
            propertiesOverride.OverrideSynchronizationMode = onEnterSelector.OverrideSyncType;
            propertiesOverride.OverrideSyncEntryPoint = onEnterSelector.OverrideSyncEntryPoint;
            propertiesOverride.OverrideSyncTagType = onEnterSelector.OverrideSyncTagType;
            propertiesOverride.OverrideSyncTargetRatio = onEnterSelector.OverrideSyncTargetRatio;
            propertiesOverride.OverrideAdvanceSourceDuringTransition = onEnterSelector.OverrideAdvanceSourceDuringTransition;
        }

        static void FillTransitionDefinitionFromCommand(ref StateMachine.TransitionDefinition transitionDefinition, CreateTransitionCommand transitionCommand)
        {
            transitionDefinition.Duration = transitionCommand.Duration;
            transitionDefinition.SynchronizationMode = ConvertSynchronizationMode(transitionCommand.SyncType);
            transitionDefinition.SyncEntryPoint = transitionCommand.SyncEntryPoint;
            //@TODO decide how sync tags are communicated
            //TODO@sonny SyncTagType should already be converted to string hash at conversion time
            //transitionDefinition.SyncTagType = new StringHash(transitionCommand.SyncTagType.ToString());
            transitionDefinition.SyncTargetRatio = transitionCommand.SyncTargetRatio;
            transitionDefinition.TargetStateIndex = transitionCommand.TargetState;
            transitionDefinition.SourceStateIndex = transitionCommand.SourceState;
            transitionDefinition.AdvanceSourceDuringTransition = transitionCommand.AdvanceSourceDuringTransition ? 1 : 0;
            transitionDefinition.RootConditionIndex = transitionCommand.TransitionFragmentIndex;
        }

        static TransitionSynchronization ConvertSynchronizationMode(TransitionSynchronizationType syncType)
        {
            if (syncType == TransitionSynchronizationType.Proportional)
                return TransitionSynchronization.Proportional;
            if (syncType == TransitionSynchronizationType.Ratio)
                return TransitionSynchronization.Ratio;
            if (syncType == TransitionSynchronizationType.Tag)
                return TransitionSynchronization.Tag;
            if (syncType == TransitionSynchronizationType.EntryPoint)
                return TransitionSynchronization.EntryPoint;
            if (syncType == TransitionSynchronizationType.InverseProportional)
                return TransitionSynchronization.InverseProportional;
            return TransitionSynchronization.None;
        }

        static TransitionConditionFragment BuildConditionFragment(CreateConditionFragmentCommand fragment, int index, ref BlobBuilderArray<TransitionConditionFragment> conditionFragmentsArray)
        {
            var newConditionFragment = new TransitionConditionFragment();
            newConditionFragment.Type = ConvertConditionFragmentType(fragment.Type);
            newConditionFragment.FirstChildConditionIndex = -1;
            newConditionFragment.NextSiblingConditionIndex = -1;

            if (fragment.Type == TransitionFragmentType.Markup)
            {
                newConditionFragment.MarkupHash = fragment.MarkupHash;
                newConditionFragment.IsSet = fragment.IsSet;
            }

            if (fragment.Type == TransitionFragmentType.StateTag)
            {
                newConditionFragment.ReferencedStateTagHash = fragment.StateTagHash;
            }

            if (fragment.Type == TransitionFragmentType.ElapsedTime || fragment.Type == TransitionFragmentType.EvaluationRatio || fragment.Type == TransitionFragmentType.EndOfDominantAnimation)
            {
                newConditionFragment.FloatValue = fragment.CompareVariant.Float;
            }

            if (fragment.Type == TransitionFragmentType.BlackboardValue)
            {
                newConditionFragment.Operation = ConvertCompareOperation(fragment.CompareOp);
                newConditionFragment.BlackboardValueComponentDataTypeIndex = TypeManager.GetTypeIndexFromStableTypeHash(fragment.BlackboardValueId.ComponentDataTypeHash);
                newConditionFragment.BlackboardValueOffset = fragment.BlackboardValueId.Offset;
                newConditionFragment.CompareValue = fragment.CompareVariant;
            }

            if (fragment.ParentIndex != -1)
            {
                if (conditionFragmentsArray[fragment.ParentIndex].FirstChildConditionIndex == -1)
                {
                    conditionFragmentsArray[fragment.ParentIndex].FirstChildConditionIndex = index;
                }
                else
                {
                    var currentSiblingIndex = conditionFragmentsArray[fragment.ParentIndex].FirstChildConditionIndex;
                    while (conditionFragmentsArray[currentSiblingIndex].NextSiblingConditionIndex != -1)
                    {
                        currentSiblingIndex = conditionFragmentsArray[currentSiblingIndex].NextSiblingConditionIndex;
                    }
                    conditionFragmentsArray[currentSiblingIndex].NextSiblingConditionIndex = index;
                }
            }

            return newConditionFragment;
        }

        static ComparisonOperation ConvertCompareOperation(CompareOp fragmentCompareOp)
        {
            if (fragmentCompareOp == CompareOp.Equal)
                return ComparisonOperation.Equal;
            if (fragmentCompareOp == CompareOp.Greater)
                return ComparisonOperation.GreaterThan;
            if (fragmentCompareOp == CompareOp.Less)
                return ComparisonOperation.LessThan;
            if (fragmentCompareOp == CompareOp.GreaterOrEqual)
                return ComparisonOperation.GreaterOrEqual;
            if (fragmentCompareOp == CompareOp.LessOrEqual)
                return ComparisonOperation.LessOrEqual;
            return ComparisonOperation.NotEqual;
        }

        static ConditionFragmentType ConvertConditionFragmentType(TransitionFragmentType fragmentType)
        {
            switch (fragmentType)
            {
                case TransitionFragmentType.Markup:
                    return ConditionFragmentType.Markup;
                case TransitionFragmentType.ElapsedTime:
                    return ConditionFragmentType.ElapsedTime;
                case TransitionFragmentType.BlackboardValue:
                    return ConditionFragmentType.BlackboardValue;
                case TransitionFragmentType.GroupAnd:
                    return ConditionFragmentType.GroupAnd;
                case TransitionFragmentType.GroupOr:
                    return ConditionFragmentType.GroupOr;
                case TransitionFragmentType.StateTag:
                    return ConditionFragmentType.StateTag;
                case TransitionFragmentType.EvaluationRatio:
                    return ConditionFragmentType.EvaluationRatio;
                case TransitionFragmentType.EndOfDominantAnimation:
                    return ConditionFragmentType.EndOfDominantAnimation;
            }

            return ConditionFragmentType.Empty;
        }
    }
}
