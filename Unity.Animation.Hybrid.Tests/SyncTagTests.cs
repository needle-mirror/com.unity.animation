using NUnit.Framework;

using Unity.Animation.Hybrid;

using UnityEngine;
using UnityEditor;


namespace Unity.Animation.Tests
{
    public class SyncTagTests : AnimationTestsFixture
    {
        internal class HumanoidGaitComponent : MonoBehaviour, ISynchronizationTag
        {
            public enum HumanoidGait
            {
                LeftFootContact = 1,
                RightFootPassover = 2,
                RightFootContact = 3,
                LeftFootPassover = 4
            }

            public HumanoidGait m_State;

            public StringHash Type => new StringHash(nameof(HumanoidGait));

            public int State
            {
                get { return (int)m_State; }
                set { m_State = (HumanoidGait)value; }
            }
        }

        internal class HorseTrotGaitComponent : MonoBehaviour, ISynchronizationTag
        {
            public enum HorseTrotGait
            {
                FrontLeft = 1,
                SuspensionRight = 2,
                FrontRight = 3,
                SuspensionLeft = 4
            }

            public HorseTrotGait m_State;

            public StringHash Type => new StringHash(nameof(HorseTrotGait));

            public int State
            {
                get { return (int)m_State; }
                set { m_State = (HorseTrotGait)value; }
            }
        }

        GameObject CreateSyncTag<SyncType>(int state)
            where SyncType : MonoBehaviour, ISynchronizationTag
        {
            var tag = CreateGameObject();
            var syncTag = tag.AddComponent<SyncType>() as ISynchronizationTag;

            syncTag.State = state;
            return tag;
        }

        [Test]
        public void EmptyClipDoesntHaveSyncTag()
        {
            var animationClip = CreateAnimationClip();
            var denseClip = animationClip.ToDenseClip();

            Assert.That(denseClip.Value.SynchronizationTags.Length, Is.EqualTo(0), "Unexpected number of synchronization tags");
        }

        [Test]
        public void AddingAnimationEventsDirtyAsset()
        {
            var animationClip = CreateAnimationClip();

            var dirtyCount = EditorUtility.GetDirtyCount(animationClip);

            var humanoidGaitBeat1 = CreateSyncTag<HumanoidGaitComponent>((int)HumanoidGaitComponent.HumanoidGait.LeftFootContact);

            var animationEvents = new AnimationEvent[]
            {
                new AnimationEvent { time = 0, objectReferenceParameter = humanoidGaitBeat1 }
            };

            AnimationUtility.SetAnimationEvents(animationClip, animationEvents);

            Assert.That(EditorUtility.GetDirtyCount(animationClip), Is.EqualTo(dirtyCount + 1), "Adding animation events should dirty the animation clip");
        }

        [Test]
        public void SyncTagsUseNormalizeTime()
        {
            var animationClip = CreateAnimationClip();

            var humanoidGaitBeat1 = CreateSyncTag<HumanoidGaitComponent>((int)HumanoidGaitComponent.HumanoidGait.LeftFootContact);

            var animationEvents = new AnimationEvent[]
            {
                new AnimationEvent { time = 0, objectReferenceParameter = humanoidGaitBeat1 },
                new AnimationEvent { time = 1.0f, objectReferenceParameter = humanoidGaitBeat1 },
                new AnimationEvent { time = 2.0f, objectReferenceParameter = humanoidGaitBeat1 }
            };

            AnimationUtility.SetAnimationEvents(animationClip, animationEvents);
            var denseClip = animationClip.ToDenseClip();

            Assert.That(denseClip.Value.SynchronizationTags.Length, Is.EqualTo(3), "Unexpected number of synchronization tags");

            Assert.That(denseClip.Value.SynchronizationTags[0].NormalizedTime, Is.EqualTo(0), "Unexpected NormalizeTime");
            Assert.That(denseClip.Value.SynchronizationTags[1].NormalizedTime, Is.EqualTo(0.5f), "Unexpected NormalizeTime");
            Assert.That(denseClip.Value.SynchronizationTags[2].NormalizedTime, Is.EqualTo(1.0f), "Unexpected NormalizeTime");
        }

        [Test]
        public void CanAddSyncTagToDenseClip()
        {
            var humanoidGaitBeat1 = CreateSyncTag<HumanoidGaitComponent>((int)HumanoidGaitComponent.HumanoidGait.LeftFootContact);
            var humanoidGaitBeat2 = CreateSyncTag<HumanoidGaitComponent>((int)HumanoidGaitComponent.HumanoidGait.RightFootPassover);
            var humanoidGaitBeat3 = CreateSyncTag<HumanoidGaitComponent>((int)HumanoidGaitComponent.HumanoidGait.RightFootContact);
            var humanoidGaitBeat4 = CreateSyncTag<HumanoidGaitComponent>((int)HumanoidGaitComponent.HumanoidGait.LeftFootPassover);

            var animationClip = CreateAnimationClip();

            var simpleClipAnimationEvents = new AnimationEvent[]
            {
                new AnimationEvent { time = 0, objectReferenceParameter = humanoidGaitBeat1 },
                new AnimationEvent { time = 0.25f, objectReferenceParameter = humanoidGaitBeat2 },
                new AnimationEvent { time = 0.5f, objectReferenceParameter = humanoidGaitBeat3 },
                new AnimationEvent { time = 0.75f, objectReferenceParameter = humanoidGaitBeat4 },
            };

            AnimationUtility.SetAnimationEvents(animationClip, simpleClipAnimationEvents);

            var denseClip = animationClip.ToDenseClip();
            var clipLength = animationClip.length;

            Assert.That(simpleClipAnimationEvents.Length, Is.EqualTo(4), "Unexpected number of synchronization tags");
            Assert.That(denseClip.Value.SynchronizationTags.Length, Is.EqualTo(4), "Unexpected number of synchronization tags");
            for (int i = 0; i < denseClip.Value.SynchronizationTags.Length; i++)
            {
                var go = simpleClipAnimationEvents[i].objectReferenceParameter as GameObject;
                var syncTag = go.GetComponent(typeof(ISynchronizationTag)) as ISynchronizationTag;
                Assert.That(denseClip.Value.SynchronizationTags[i].NormalizedTime, Is.EqualTo(simpleClipAnimationEvents[i].time / clipLength), "Sync tag NormalizeTime doesn't match");
                Assert.That(denseClip.Value.SynchronizationTags[i].Type, Is.EqualTo(syncTag.Type), "Sync tag Type doesn't match");
                Assert.That(denseClip.Value.SynchronizationTags[i].State, Is.EqualTo(syncTag.State), "Sync tag State doesn't match");
            }
        }

        [Test]
        public void CanAddMultipleSyncTagTypeToDenseClip()
        {
            var humanoidGaitBeat1 = CreateSyncTag<HumanoidGaitComponent>((int)HumanoidGaitComponent.HumanoidGait.LeftFootContact);
            var humanoidGaitBeat2 = CreateSyncTag<HumanoidGaitComponent>((int)HumanoidGaitComponent.HumanoidGait.RightFootPassover);
            var humanoidGaitBeat3 = CreateSyncTag<HumanoidGaitComponent>((int)HumanoidGaitComponent.HumanoidGait.RightFootContact);
            var humanoidGaitBeat4 = CreateSyncTag<HumanoidGaitComponent>((int)HumanoidGaitComponent.HumanoidGait.LeftFootPassover);

            var horseTrotBeat1 = CreateSyncTag<HorseTrotGaitComponent>((int)HorseTrotGaitComponent.HorseTrotGait.FrontLeft);
            var horseTrotBeat2 = CreateSyncTag<HorseTrotGaitComponent>((int)HorseTrotGaitComponent.HorseTrotGait.SuspensionRight);
            var horseTrotBeat3 = CreateSyncTag<HorseTrotGaitComponent>((int)HorseTrotGaitComponent.HorseTrotGait.FrontRight);
            var horseTrotBeat4 = CreateSyncTag<HorseTrotGaitComponent>((int)HorseTrotGaitComponent.HorseTrotGait.SuspensionLeft);

            var animationClip = CreateAnimationClip();

            var animationEvents = new AnimationEvent[]
            {
                new AnimationEvent { time = 0, objectReferenceParameter = humanoidGaitBeat1 },
                new AnimationEvent { time = 0.25f, objectReferenceParameter = humanoidGaitBeat2 },
                new AnimationEvent { time = 0.5f, objectReferenceParameter = humanoidGaitBeat3 },
                new AnimationEvent { time = 0.75f, objectReferenceParameter = humanoidGaitBeat4 },
                new AnimationEvent { time = 0, objectReferenceParameter = horseTrotBeat1 },
                new AnimationEvent { time = 0.25f, objectReferenceParameter = horseTrotBeat2 },
                new AnimationEvent { time = 0.5f, objectReferenceParameter = horseTrotBeat3 },
                new AnimationEvent { time = 0.75f, objectReferenceParameter = horseTrotBeat4 },
            };

            AnimationUtility.SetAnimationEvents(animationClip, animationEvents);
            animationEvents = AnimationUtility.GetAnimationEvents(animationClip);

            var denseClip = animationClip.ToDenseClip();
            var clipLength = animationClip.length;

            Assert.That(animationEvents.Length, Is.EqualTo(8), "Unexpected number of synchronization tags");
            Assert.That(denseClip.Value.SynchronizationTags.Length, Is.EqualTo(8), "Unexpected number of synchronization tags");
            for (int i = 0; i < denseClip.Value.SynchronizationTags.Length; i++)
            {
                var go = animationEvents[i].objectReferenceParameter as GameObject;
                var syncTag = go.GetComponent(typeof(ISynchronizationTag)) as ISynchronizationTag;

                Assert.That(denseClip.Value.SynchronizationTags[i].NormalizedTime, Is.EqualTo(animationEvents[i].time / clipLength), "Sync tag NormalizeTime doesn't match");
                Assert.That(denseClip.Value.SynchronizationTags[i].Type, Is.EqualTo(syncTag.Type), "Sync tag Type doesn't match");
                Assert.That(denseClip.Value.SynchronizationTags[i].State, Is.EqualTo(syncTag.State), "Sync tag State doesn't match");
            }
        }

        [Test]
        public void SyncTagShouldBeSortedByTime()
        {
            var humanoidGaitBeat1 = CreateSyncTag<HumanoidGaitComponent>((int)HumanoidGaitComponent.HumanoidGait.LeftFootContact);
            var humanoidGaitBeat2 = CreateSyncTag<HumanoidGaitComponent>((int)HumanoidGaitComponent.HumanoidGait.RightFootPassover);
            var humanoidGaitBeat3 = CreateSyncTag<HumanoidGaitComponent>((int)HumanoidGaitComponent.HumanoidGait.RightFootContact);
            var humanoidGaitBeat4 = CreateSyncTag<HumanoidGaitComponent>((int)HumanoidGaitComponent.HumanoidGait.LeftFootPassover);

            var horseTrotBeat1 = CreateSyncTag<HorseTrotGaitComponent>((int)HorseTrotGaitComponent.HorseTrotGait.FrontLeft);
            var horseTrotBeat2 = CreateSyncTag<HorseTrotGaitComponent>((int)HorseTrotGaitComponent.HorseTrotGait.SuspensionRight);
            var horseTrotBeat3 = CreateSyncTag<HorseTrotGaitComponent>((int)HorseTrotGaitComponent.HorseTrotGait.FrontRight);
            var horseTrotBeat4 = CreateSyncTag<HorseTrotGaitComponent>((int)HorseTrotGaitComponent.HorseTrotGait.SuspensionLeft);

            var animationClip = CreateAnimationClip();

            var animationEvents = new AnimationEvent[]
            {
                new AnimationEvent { time = 0.75f, objectReferenceParameter = humanoidGaitBeat4 },
                new AnimationEvent { time = 0.75f, objectReferenceParameter = horseTrotBeat4 },
                new AnimationEvent { time = 0.5f, objectReferenceParameter = humanoidGaitBeat3 },
                new AnimationEvent { time = 0, objectReferenceParameter = humanoidGaitBeat1 },
                new AnimationEvent { time = 0.25f, objectReferenceParameter = humanoidGaitBeat2 },
                new AnimationEvent { time = 0, objectReferenceParameter = horseTrotBeat1 },
                new AnimationEvent { time = 0.25f, objectReferenceParameter = horseTrotBeat2 },
                new AnimationEvent { time = 0.5f, objectReferenceParameter = horseTrotBeat3 },
            };

            AnimationUtility.SetAnimationEvents(animationClip, animationEvents);

            var denseClip = animationClip.ToDenseClip();
            var clipLength = animationClip.length;

            Assert.That(animationEvents.Length, Is.EqualTo(8), "Unexpected number of synchronization tags");
            Assert.That(denseClip.Value.SynchronizationTags.Length, Is.EqualTo(8), "Unexpected number of synchronization tags");
            for (int i = 1; i < denseClip.Value.SynchronizationTags.Length; i++)
            {
                Assert.That(denseClip.Value.SynchronizationTags[i - 1].NormalizedTime, Is.LessThanOrEqualTo(denseClip.Value.SynchronizationTags[i].NormalizedTime), "Sync tags is not sorted");
            }
        }
    }
}
