using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Animation.Authoring
{
    /// <summary>
    /// Interface to cover all channel bindings.
    /// </summary>
    public interface IBindingID
    {
        string ID { get; }
    }

    /// <summary>
    /// This enum represents the current state of a <see cref="TransformChannel"/>
    /// </summary>
    enum TransformChannelState
    {
        DoesNotExist,
        Active,
        Inactive
    }

    /// <summary>
    /// Binding for transform channels.
    /// Translation, Rotation and Scale channels only need a path
    /// as unique identifier.
    /// Q. Should we instead commit to use a generic binding like other channels?
    /// See also <seealso cref="SkeletonBoneReference"/>, <seealso cref="SkeletonReferenceAttribute"/> and <seealso cref="ShowFullPathAttribute"/>
    /// </summary>
    [Serializable]
    [DebuggerDisplay("id = {ID}")]
    public struct TransformBindingID : IBindingID, IEquatable<TransformBindingID>, IComparable<TransformBindingID>
    {
        /// <summary>
        /// Path as it's set in the hierarchy.
        /// </summary>
        public string Path;
        /// <summary>
        /// Retrieves a unique ID for the transform channel.
        /// </summary>
        public string ID { get => Path; }

        /// <summary>
        /// Parent TransformBindingID if it can extracted from path. Invalid TransformBindingID otherwise.
        /// </summary>
        public TransformBindingID GetParent()
        {
            if (this.Equals(Root))
                return Invalid;

            int index = Path.LastIndexOf(Skeleton.k_PathSeparator);
            if (index == -1)
                return Root;

            return new TransformBindingID
            {
                Path = Path.Substring(0, index)
            };
        }

        /// <summary>
        /// Invalid TransformBindingID.
        /// </summary>
        public static TransformBindingID Invalid
        {
            get => new TransformBindingID {Path = null};
        }

        /// <summary>
        /// TransformBindingID describing the root node of the animated hierarchy.
        /// </summary>
        public static TransformBindingID Root
        {
            get => new TransformBindingID {Path = ""};
        }

        /// <summary>
        /// Name of the node described by the TransformBindingID.
        /// </summary>
        public string Name
        {
            get => System.IO.Path.GetFileName(Path);
        }

        /// <inheritdoc/>
        public bool Equals(TransformBindingID other)
        {
            return Path == other.Path;
        }

        /// <inheritdoc/>
        public int CompareTo(TransformBindingID other)
        {
            return ID.CompareTo(other.ID);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is TransformBindingID other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (Path != null ? Path.GetHashCode() : 0);
        }

        public override string ToString() => Path;

        public static bool operator==(TransformBindingID left, TransformBindingID right) => left.Equals(right);
        public static bool operator!=(TransformBindingID left, TransformBindingID right) => !left.Equals(right);
    }

    /// <summary>
    /// Binding for generic channels.
    /// </summary>
    [Serializable]
    public struct GenericBindingID : IBindingID, IEquatable<GenericBindingID>, IComparable<GenericBindingID>
    {
        /// <summary>
        /// Path as it's set in the hierarchy.
        /// </summary>
        public string Path;
        /// <summary>
        /// Name of the attribute.
        /// </summary>
        public string AttributeName;
        /// <summary>
        /// Type of value.
        /// </summary>
        public GenericPropertyType ValueType;

        /// <summary>
        /// Type of component.
        /// </summary>
        [SerializeField]
        private string m_ComponentName;

        private static readonly string[] k_Suffixes = new[] {"x", "y", "z", "w"};

        /// <summary>
        /// Type of component.
        /// </summary>
        public Type ComponentType
        {
            get => m_ComponentName != null ? Type.GetType(m_ComponentName) : null;
            set => m_ComponentName = value != null ? value.AssemblyQualifiedName : null;
        }

        /// <summary>
        /// Retrieves a sub-id in a GenericBindingID that contain multiple channels.
        /// </summary>
        /// <param name="index">Index of the sub-channel.</param>
        /// <exception cref="ArgumentException">Index must be between [0...3]</exception>
        public GenericBindingID this[int index]
        {
            get
            {
                if ((uint)index >= 4)
                    throw new System.ArgumentException("index must be between[0...3]");

                return new GenericBindingID
                {
                    Path = Path,
                    AttributeName = $"{AttributeName}.{k_Suffixes[index]}",
                    ComponentType = ComponentType,
                    ValueType = ValueType.GetGenericChannelType()
                };
            }
        }

        /// <summary>
        /// Retrieves x channel sub-id.
        /// </summary>
        public GenericBindingID x { get => this[0]; }
        /// <summary>
        /// Retrieves y channel sub-id.
        /// </summary>
        public GenericBindingID y { get => this[1]; }
        /// <summary>
        /// Retrieves z channel sub-id.
        /// </summary>
        public GenericBindingID z { get => this[2]; }
        /// <summary>
        /// Retrieves w channel sub-id.
        /// </summary>
        public GenericBindingID w { get => this[3]; }

        /// <summary>
        /// Retrieves r channel sub-id. This is the same as <see cref="x"/>.
        /// </summary>
        public GenericBindingID r { get => this[0]; }
        /// <summary>
        /// Retrieves r channel sub-id. This is the same as <see cref="y"/>.
        /// </summary>
        public GenericBindingID g { get => this[1]; }
        /// <summary>
        /// Retrieves r channel sub-id. This is the same as <see cref="z"/>.
        /// </summary>
        public GenericBindingID b { get => this[2]; }
        /// <summary>
        /// Retrieves r channel sub-id. This is the same as <see cref="w"/>.
        /// </summary>
        public GenericBindingID a { get => this[3]; }

        /// <summary>
        /// Invalid GenericBindingID.
        /// </summary>
        public static GenericBindingID Invalid
        {
            get => new GenericBindingID {AttributeName = null, Path = null, ComponentType = null};
        }

        /// <summary>
        /// Retrieves the unique ID for the generic property channel.
        /// </summary>
        public string ID
        {
            get => this.Equals(Invalid) ? null : $"{Path}:{AttributeName}:{m_ComponentName}";
        }

        /// <inheritdoc/>
        public bool Equals(GenericBindingID other)
        {
            return Path == other.Path && AttributeName == other.AttributeName && Equals(m_ComponentName, other.m_ComponentName);
        }

        /// <inheritdoc/>
        public int CompareTo(GenericBindingID other)
        {
            var result = Path.CompareTo(other.Path);
            if (result == 0)
            {
                return AttributeName.CompareTo(other.AttributeName);
            }

            return result;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is GenericBindingID other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Path != null ? Path.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (AttributeName != null ? AttributeName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_ComponentName != null ? m_ComponentName.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    /// <summary>
    /// Interface that describes an animated channel on the Skeleton asset.
    /// </summary>
    public interface IChannel
    {
        /// <summary>
        /// Unique ID.
        /// </summary>
        IBindingID ID { get; }
    }

    /// <summary>
    /// Interface that describes an animated generic channel on the Skeleton asset.
    /// </summary>
    public interface IGenericChannel : IChannel
    {
        /// <summary>
        /// Default Value for this channel.
        /// </summary>
        GenericPropertyVariant DefaultValue { get; }
    }

    /// <summary>
    /// Properties of a transform channel defined in a <see cref="Skeleton"/>.
    /// </summary>
    [Serializable]
    public struct TransformChannelProperties
    {
        /// <summary>
        /// Default transform channel properties.
        /// </summary>
        public static readonly TransformChannelProperties Default = new TransformChannelProperties
        {
            DefaultTranslationValue = float3.zero,
            DefaultRotationValue = quaternion.identity,
            DefaultScaleValue = new float3(1f)
        };

        /// <summary>
        /// Default translation value.
        /// </summary>
        public float3 DefaultTranslationValue;

        /// <summary>
        /// Default rotation value.
        /// </summary>
        public quaternion DefaultRotationValue;

        /// <summary>
        /// Default scale value.
        /// </summary>
        public float3 DefaultScaleValue;

        public override string ToString() =>
            $"{{ TRS=({DefaultTranslationValue}, {DefaultRotationValue}, {DefaultScaleValue}) }}";
    }

    /// <summary>
    /// A key-value pair of a <see cref="TransformBindingID"/> and <see cref="TransformChannelProperties"/>.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("id = {ID.ID}, properties = {Properties}")]
    public struct TransformChannel : IChannel, IComparable<TransformChannel>
    {
        /// <summary>
        /// Unique ID for the Transform channel.
        /// </summary>
        public TransformBindingID ID;

        /// <summary>
        /// Properties of the transform channel.
        /// </summary>
        public TransformChannelProperties Properties;

        /// <inheritdoc/>
        IBindingID IChannel.ID
        {
            get => ID;
        }

        /// <summary>
        /// Invalid Transform channel.
        /// </summary>
        public static TransformChannel Invalid
        {
            get => new TransformChannel { ID = TransformBindingID.Invalid };
        }

        /// <inheritdoc/>
        public int CompareTo(TransformChannel other)
        {
            return ID.CompareTo(other.ID);
        }

        public override string ToString() => $"{{ {nameof(ID)}={ID}, {nameof(Properties)}={Properties} }}";
    }

    /// <summary>
    /// Animated Generic channel.
    /// This channel describes generic properties that are not part of the Transform component.
    /// </summary>
    /// Q. The GenericChannel also covers standalone Quaternion properties in generic
    /// component. This allows to greatly simplify the TransformChannel logic, but is still up
    /// for debate.
    [Serializable]
    public struct GenericChannel<T> : IGenericChannel, IComparable<GenericChannel<T>>
        where T : struct
    {
        /// <summary>
        /// Unique ID for the generic property channel.
        /// </summary>
        public GenericBindingID ID;
        /// <summary>
        /// Default value.
        /// </summary>
        public T DefaultValue;

        /// <inheritdoc/>
        IBindingID IChannel.ID { get => ID; }

        /// <inheritdoc/>
        GenericPropertyVariant IGenericChannel.DefaultValue
        {
            get => new GenericPropertyVariant {Object = DefaultValue};
        }

        /// <summary>
        /// Invalid generic channel.
        /// </summary>
        public static GenericChannel<T> Invalid
        {
            get => new GenericChannel<T> {ID = GenericBindingID.Invalid};
        }

        /// <inheritdoc/>
        public int CompareTo(GenericChannel<T> other)
        {
            return ID.CompareTo(other.ID);
        }
    }

    /// <summary>
    /// This is the base interface that represents label reference.
    /// A label reference is the association of a PropertyLabel to a
    /// specific channel binding.
    /// </summary>
    interface ILabelReference
    {
        /// <summary>
        /// Label.
        /// </summary>
        PropertyLabelBase Label { get; }
        /// <summary>
        /// Unique ID.
        /// </summary>
        IBindingID ID { get; }
    }

    /// <summary>
    /// Property label reference for Transform channels.
    /// </summary>
    [Serializable]
    struct TransformLabelReference : ILabelReference
    {
        /// <summary>
        /// Transform label.
        /// </summary>
        public TransformLabel Label;
        /// <summary>
        /// Unique ID.
        /// </summary>
        public TransformBindingID ID;

        /// <inheritdoc/>
        PropertyLabelBase ILabelReference.Label { get => Label; }
        /// <inheritdoc/>
        IBindingID ILabelReference.ID { get => ID; }
    }

    /// <summary>
    /// Property label reference for generic channels.
    /// </summary>
    [Serializable]
    struct GenericPropertyLabelReference : ILabelReference
    {
        /// <summary>
        /// Generic property label.
        /// </summary>
        public GenericPropertyLabel Label;
        /// <summary>
        /// Unique ID.
        /// </summary>
        public GenericBindingID ID;

        /// <inheritdoc/>
        PropertyLabelBase ILabelReference.Label { get => Label; }
        /// <inheritdoc/>
        IBindingID ILabelReference.ID { get => ID; }
    }


    /// <summary>
    /// A reference to a specific bone in a skeleton
    /// See also <seealso cref="TransformBindingID"/>, <seealso cref="SkeletonReferenceAttribute"/> and <seealso cref="ShowFullPathAttribute"/>
    /// </summary>
    [Serializable]
    public struct SkeletonBoneReference
    {
        internal const string nameOfID = nameof(m_ID);
        internal const string nameOfSkeleton = nameof(m_Skeleton);

        [SerializeField] Skeleton m_Skeleton;
        public Skeleton Skeleton { get => m_Skeleton; internal set => m_Skeleton = value; }
        [SerializeField] TransformBindingID m_ID;
        public TransformBindingID ID { get => m_ID; }

        public SkeletonBoneReference(Skeleton skeleton, TransformBindingID id)
        {
            m_Skeleton = skeleton;
            m_ID = id;
        }

        public bool IsValid()
        {
            if (m_Skeleton == null)
                return false;
            return m_Skeleton.Contains(ID);
        }
    }


    /// <summary>
    /// Skeleton Authoring Asset.
    /// </summary>
    //[CreateAssetMenu(fileName = "Skeleton", menuName = "DOTS/Animation/Skeleton", order = 1)]
    public class Skeleton : ScriptableObject
    {
        internal const char k_PathSeparator = '/';

        [SerializeField] private List<TransformLabelReference> m_TransformLabels = new List<TransformLabelReference>();
        [SerializeField] private List<GenericPropertyLabelReference> m_GenericPropertyLabels = new List<GenericPropertyLabelReference>();

        // Channels are kept separately in similar data structures to RigComponent
        // to ensure a tight conversion loop.
        // Quaternion needs to remain as interpolation is done separately.
        [SerializeField] private List<TransformChannel> m_TransformChannels = new List<TransformChannel>();
        [SerializeField] private List<TransformChannel> m_InactiveTransformChannels = new List<TransformChannel>();
        [SerializeField] private List<GenericChannel<int>> m_IntChannels = new List<GenericChannel<int>>();
        [SerializeField] private List<GenericChannel<float>> m_FloatChannels = new List<GenericChannel<float>>();
        [SerializeField] private List<GenericChannel<quaternion>> m_QuaternionChannels = new List<GenericChannel<quaternion>>();

        [SerializeField] private TransformBindingID m_Root = TransformBindingID.Invalid;

        /// <summary>
        /// Delegate function that is called whenever a new transform channel is added.
        /// </summary>
        internal event Action<TransformBindingID> BoneAdded;
        /// <summary>
        /// Delegate function that is called whenever a transform channel values are changed.
        /// </summary>
        internal event Action<TransformBindingID> BoneModified;
        /// <summary>
        /// Delegate function that is called whenever a transform channel is removed.
        /// </summary>
        internal event Action<TransformBindingID> BoneRemoved;
        /// <summary>
        /// Delegate function that is called whenever a new generic channel is added.
        /// </summary>
        internal event Action<GenericBindingID> GenericPropertyAdded;
        /// <summary>
        /// Delegate function that is called whenever a generic channel value is changed.
        /// </summary>
        internal event Action<GenericBindingID> GenericPropertyModified;
        /// <summary>
        /// Delegate function that is called whenever a generic channel is removed.
        /// </summary>
        internal event Action<GenericBindingID> GenericPropertyRemoved;

        /// <summary>
        /// The root node of this skeleton.
        /// </summary>
        public TransformBindingID Root { get { return m_Root; } set { m_Root = value; } }

        /// <summary>
        /// Retrieves Transform Labels.
        /// </summary>
        internal IReadOnlyList<TransformLabelReference> TransformLabels => m_TransformLabels;

        /// <summary>
        /// Retrieves Generic Property Labels.
        /// </summary>
        internal IReadOnlyList<GenericPropertyLabelReference> GenericPropertyLabels => m_GenericPropertyLabels;

        /// <summary>
        /// Retrieves the Transform channels that are active.
        /// </summary>
        public IReadOnlyList<TransformChannel> ActiveTransformChannels => m_TransformChannels;

        /// <summary>
        /// Retrieves the Transform channels that are currently inactive.
        /// </summary>
        public IReadOnlyList<TransformChannel> InactiveTransformChannels => m_InactiveTransformChannels;

        /// <summary>
        /// Retrieves the Integer channels.
        /// </summary>
        public IReadOnlyList<GenericChannel<int>> IntChannels => m_IntChannels;

        /// <summary>
        /// Retrieves the Float channels.
        /// </summary>
        public IReadOnlyList<GenericChannel<float>> FloatChannels => m_FloatChannels;

        /// <summary>
        /// Retrieves the Quaternion channels.
        /// </summary>
        public IReadOnlyList<GenericChannel<quaternion>> QuaternionChannels => m_QuaternionChannels;


        /// <summary>
        /// Retrieves all generic channels.
        /// Generic channels are comprised of float, integer and quaternion channels.
        /// </summary>
        public void GetGenericChannels(List<IGenericChannel> channels)
        {
            var totalSize = m_FloatChannels.Count + m_IntChannels.Count + m_QuaternionChannels.Count;
            channels.Capacity = totalSize;

            for (int i = 0; i < m_FloatChannels.Count; ++i)
                channels.Add(m_FloatChannels[i]);

            for (int i = 0; i < m_IntChannels.Count; ++i)
                channels.Add(m_IntChannels[i]);

            for (int i = 0; i < m_QuaternionChannels.Count; ++i)
                channels.Add(m_QuaternionChannels[i]);
        }

        protected Skeleton()
        {
        }

        /// <summary>
        /// Clears all channels and labels.
        /// </summary>
        public void Clear()
        {
            m_TransformChannels.Clear();
            m_InactiveTransformChannels.Clear();
            m_IntChannels.Clear();
            m_FloatChannels.Clear();
            m_QuaternionChannels.Clear();

            m_TransformLabels.Clear();
            m_GenericPropertyLabels.Clear();
        }

        /// <summary>
        /// Adds or updates a transform channel with the specified ID.
        /// If its ancestors do not yet exist, they are also added with default <see cref="TransformChannelProperties"/>.
        /// </summary>
        /// <param name="id">A transform channel identifier.</param>
        public TransformChannelProperties this[TransformBindingID id]
        {
            get
            {
                var index = m_TransformChannels.FindIndex(c => c.ID.Equals(id));
                if (index >= 0)
                    return m_TransformChannels[index].Properties;
                index = m_InactiveTransformChannels.FindIndex(c => c.ID.Equals(id));
                if (index >= 0)
                    return m_InactiveTransformChannels[index].Properties;
                throw new KeyNotFoundException($"No transform channel defined for {id}");
            }
            set => AddOrSetTransformChannel(new TransformChannel { ID = id, Properties = value });
        }

        readonly List<TransformBindingID> m_ModifiedChannels = new List<TransformBindingID>(16);

        /// <summary>
        /// Adds or sets Transform bone channel in the current skeleton definition.
        /// Updates the current channel values if it already exist in skeleton definition.
        /// </summary>
        /// <param name="channel">Transform channel</param>
        /// <returns>Returns True if bone was added to channels.</returns>
        /// <exception cref="ArgumentException">channel.ID is invalid</exception>
        internal void AddOrSetTransformChannel(TransformChannel channel)
        {
            if (channel.ID.Equals(TransformBindingID.Invalid))
                throw new ArgumentException($"The Argument {nameof(channel.ID)} is not valid");

            int activeIndex = m_TransformChannels.FindIndex(c => c.ID.Equals(channel.ID));
            if (activeIndex == -1)
            {
                int inactiveIndex = m_InactiveTransformChannels.FindIndex(c => c.ID.Equals(channel.ID));
                if (inactiveIndex == -1)
                {
                    m_ModifiedChannels.Clear();

                    TransformBindingID parentID;
                    do
                    {
                        parentID = channel.ID.GetParent();
                        var comparableParentChannel = new TransformChannel { ID = parentID };

                        int insertIndex = -1;
                        List<TransformChannel> listToInsertInto = m_TransformChannels;

                        // if parent exists in active list, insert as normal
                        if (m_TransformChannels.BinarySearch(comparableParentChannel) >= 0)
                            insertIndex = m_TransformChannels.BinarySearch(channel);

                        // otherwise, see if parent exists in inactive list
                        else if (m_InactiveTransformChannels.BinarySearch(comparableParentChannel) >= 0)
                        {
                            insertIndex = m_InactiveTransformChannels.BinarySearch(channel);
                            listToInsertInto = m_InactiveTransformChannels;
                        }

                        if (insertIndex < 0)
                            insertIndex = ~insertIndex;

                        listToInsertInto.Insert(insertIndex, channel);

                        m_ModifiedChannels.Add(channel.ID);

                        channel.ID = parentID;
                        channel.Properties = TransformChannelProperties.Default;
                    }
                    while (parentID != TransformBindingID.Invalid && parentID != TransformBindingID.Root && !Contains(parentID));

                    foreach (var ch in m_ModifiedChannels)
                        BoneAdded?.Invoke(ch);
                }
                else
                {
                    m_InactiveTransformChannels[activeIndex] = channel;

                    BoneModified?.Invoke(channel.ID);
                }
            }
            else
            {
                m_TransformChannels[activeIndex] = channel;

                BoneModified?.Invoke(channel.ID);
            }
        }

        bool RemoveTransformChannelAndDescendants(List<TransformChannel> channels, TransformBindingID id, List<TransformBindingID> modifiedChannels)
        {
            // Remove transform channel associated to binding id.
            int index = channels.FindIndex(channel => channel.ID.Path.StartsWith(id.Path));
            var removedChannel = index != -1 && channels[index].ID.Equals(id);

            if (index == -1)
                return removedChannel;

            do
            {
                modifiedChannels.Add(channels[index].ID);
                channels.RemoveAt(index);
            }
            while (index < channels.Count && channels[index].ID.Path.StartsWith(id.Path));

            return removedChannel;
        }

        /// <summary>
        /// Remove a transform channel and all its descendants from the current skeleton definition.
        /// </summary>
        /// <param name="id">The Transform bone binding ID.</param>
        /// <returns>Returns True if bone with binding ID was removed from channels.</returns>
        /// <exception cref="ArgumentException">channel.ID is invalid</exception>
        public bool RemoveTransformChannelAndDescendants(TransformBindingID id)
        {
            if (id.Equals(TransformBindingID.Invalid))
                throw new ArgumentException($"The Argument {nameof(id)} is not valid");

            // Remove transform channel associated with binding id and descendants
            m_ModifiedChannels.Clear();
            bool hasRemovedBone = RemoveTransformChannelAndDescendants(m_TransformChannels, id, m_ModifiedChannels);
            hasRemovedBone |= RemoveTransformChannelAndDescendants(m_InactiveTransformChannels, id, m_ModifiedChannels);

            // Remove all transform labels associated with binding ids
            foreach (var ch in m_ModifiedChannels)
                for (int index = m_TransformLabels.Count - 1; index >= 0; --index)
                {
                    if (m_TransformLabels[index].ID.Equals(ch))
                    {
                        m_TransformLabels.RemoveAt(index);
                    }
                }

            foreach (var ch in m_ModifiedChannels)
                BoneRemoved?.Invoke(ch);

            return hasRemovedBone;
        }

        /// <summary>
        /// Appends the Transform label unto a channel identified by specified transform binding.
        /// </summary>
        /// <param name="id">The Transform binding ID.</param>
        /// <param name="label">The Transform property label.</param>
        /// <returns>Returns True if label has been added to channel. False otherwise.</returns>
        /// <exception cref="ArgumentNullException">label is null.</exception>
        internal bool AddTransformLabel(TransformBindingID id, TransformLabel label)
        {
            if (label == null)
                throw new ArgumentNullException($"The Argument {nameof(label)} cannot be null");

            // TransformLabel is already associated to a transform. Cannot add the same transform label twice.
            if (m_TransformLabels.FindIndex(labelReference => labelReference.Label == label) != -1)
                return false;

            // Shouldn't be able to add a label if property doesn't exist in bindings.
            if (m_TransformChannels.FindIndex(channel => channel.ID.Equals(id)) == -1)
            {
                if (m_InactiveTransformChannels.FindIndex(channel => channel.ID.Equals(id)) == -1)
                    return false;
            }

            m_TransformLabels.Add(new TransformLabelReference
            {
                ID = id,
                Label = label
            });

            return true;
        }

        /// <summary>
        /// Removes the Transform label from channel it has been appended to.
        /// </summary>
        /// <param name="label">The Transform property label.</param>
        /// <returns>Returns True if label has been removed from channel. False otherwise.</returns>
        /// <exception cref="ArgumentNullException">label is null.</exception>
        internal bool RemoveTransformLabel(TransformLabel label)
        {
            if (label == null)
                throw new ArgumentNullException($"The Argument {nameof(label)} cannot be null");

            int index = m_TransformLabels.FindIndex(labelReference => labelReference.Label == label);
            if (index != -1)
            {
                m_TransformLabels.RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds or sets a generic property channel in the current skeleton definition.
        /// </summary>
        /// <param name="id">The unique binding ID describing the generic property.</param>
        /// <param name="defaultValue">The default value for the generic property.</param>
        /// <returns>Returns True if property has been added to channels. False otherwise.</returns>
        /// <exception cref="InvalidOperationException">Type must be a recognized animatable type.</exception>
        /// <exception cref="ArgumentException">id is invalid</exception>
        public void AddOrSetGenericProperty(GenericBindingID id, GenericPropertyVariant defaultValue)
        {
            if (id.Equals(GenericBindingID.Invalid))
                throw new ArgumentException($"The Argument {nameof(id)} is not valid");

            switch (defaultValue.Type)
            {
                case GenericPropertyType.Float:
                    AddOrSetGenericProperty(id, m_FloatChannels, defaultValue.Float);
                    break;
                case GenericPropertyType.Float2:
                    AddOrSetGenericTupleProperty(id, m_FloatChannels, new[] {defaultValue.Float2.x, defaultValue.Float2.y});
                    break;
                case GenericPropertyType.Float3:
                    AddOrSetGenericTupleProperty(id, m_FloatChannels, new[] {defaultValue.Float3.x, defaultValue.Float3.y, defaultValue.Float3.z});
                    break;
                case GenericPropertyType.Int:
                    AddOrSetGenericProperty(id, m_IntChannels, defaultValue.Int);
                    break;
                case GenericPropertyType.Quaternion:
                    AddOrSetGenericProperty(id, m_QuaternionChannels, defaultValue.Quaternion);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid Type {defaultValue.Type}");
            }
        }

        private void AddOrSetGenericTupleProperty<T>(GenericBindingID id, List<GenericChannel<T>> channels, T[] defaultValues)
            where T : struct
        {
            var genericPropertyLabel = GenericPropertyLabel.Create(id.AttributeName);

            m_GenericPropertyLabels.Add(new GenericPropertyLabelReference
            {
                ID = id,
                Label = genericPropertyLabel
            });

            for (int i = 0; i < defaultValues.Length; ++i)
            {
                AddOrSetGenericProperty(id[i], channels, defaultValues[i]);
            }
        }

        private void AddOrSetGenericProperty<T>(GenericBindingID id, List<GenericChannel<T>> channels, T defaultValue)
            where T : struct
        {
            var newChannel = new GenericChannel<T>
            {
                ID = id,
                DefaultValue = defaultValue
            };

            int index = channels.FindIndex(channel => channel.ID.Equals(id));
            if (index == -1)
            {
                var insertIndex = channels.BinarySearch(newChannel);
                if (insertIndex < 0) insertIndex = ~insertIndex;

                channels.Insert(insertIndex, newChannel);

                GenericPropertyAdded?.Invoke(newChannel.ID);
            }
            else
            {
                channels[index] = newChannel;

                GenericPropertyModified?.Invoke(newChannel.ID);
            }
        }

        /// <summary>
        /// Removes a generic property channel from the current skeleton definition.
        /// </summary>
        /// <param name="id">The generic property binding ID.</param>
        /// <returns>Returns True if property with binding ID was removed from channels.</returns>
        /// <exception cref="InvalidOperationException">Type must be a recognized animatable type.</exception>
        /// <exception cref="ArgumentException">id is invalid</exception>
        public bool RemoveGenericProperty(GenericBindingID id)
        {
            if (id.Equals(GenericBindingID.Invalid))
                throw new ArgumentException($"The Argument {nameof(id)} is not valid");

            switch (id.ValueType)
            {
                case GenericPropertyType.Float:
                    return RemoveGenericProperty(id, m_FloatChannels);
                case GenericPropertyType.Float2:
                    return RemoveGenericTupleProperty(id, m_FloatChannels);
                case GenericPropertyType.Float3:
                    return RemoveGenericTupleProperty(id, m_FloatChannels);
                case GenericPropertyType.Int:
                    return RemoveGenericProperty(id, m_IntChannels);
                case GenericPropertyType.Quaternion:
                    return RemoveGenericProperty(id, m_QuaternionChannels);
                default:
                    throw new InvalidOperationException($"Invalid Type {id.ValueType}");
            }
        }

        private bool RemoveGenericTupleProperty<T>(GenericBindingID id, List<GenericChannel<T>> channels)
            where T : struct
        {
            // Remove all generic labels associated to binding id.
            for (int index = m_GenericPropertyLabels.Count - 1; index >= 0; --index)
            {
                if (m_GenericPropertyLabels[index].ID.Equals(id))
                {
                    m_GenericPropertyLabels.RemoveAt(index);
                }
            }

            bool hasRemovedProperty = false;
            for (int i = 0; i < id.ValueType.GetNumberOfChannels(); ++i)
            {
                hasRemovedProperty |= RemoveGenericProperty(id[i], channels);
            }

            return hasRemovedProperty;
        }

        private bool RemoveGenericProperty<T>(GenericBindingID id, List<GenericChannel<T>> channels)
            where T : struct
        {
            bool hasRemovedProperty = false;

            int index = channels.FindIndex(channel => channel.ID.Equals(id));
            if (index != -1)
            {
                channels.RemoveAt(index);
                GenericPropertyRemoved?.Invoke(id);

                hasRemovedProperty = true;
            }

            // Remove all generic labels associated to binding id.
            for (index = m_GenericPropertyLabels.Count - 1; index >= 0; --index)
            {
                if (m_GenericPropertyLabels[index].ID.Equals(id))
                {
                    m_GenericPropertyLabels.RemoveAt(index);
                }
            }

            return hasRemovedProperty;
        }

        /// <summary>
        /// Appends the property label unto a channel identified by specified generic binding.
        /// </summary>
        /// <param name="id">The generic binding ID.</param>
        /// <param name="label">The generic property label.</param>
        /// <returns>Returns True if label was successfully appended to channel. False otherwise.</returns>
        /// <exception cref="ArgumentNullException">label is null.</exception>
        internal bool AddGenericPropertyLabel(GenericBindingID id, GenericPropertyLabel label)
        {
            if (label == null)
                throw new ArgumentNullException($"The Argument {nameof(label)} cannot be null");

            switch (id.ValueType.GetGenericChannelType())
            {
                case GenericPropertyType.Float:
                    return AddGenericPropertyLabel(id, m_FloatChannels, label);
                case GenericPropertyType.Int:
                    return AddGenericPropertyLabel(id, m_IntChannels, label);
                case GenericPropertyType.Quaternion:
                    return AddGenericPropertyLabel(id, m_QuaternionChannels, label);
            }

            return false;
        }

        private bool AddGenericPropertyLabel<T>(GenericBindingID id, List<GenericChannel<T>> channels, GenericPropertyLabel label)
            where T : struct
        {
            // GenericLabel is already associated to a property. Cannot add the same property label twice.
            if (m_GenericPropertyLabels.FindIndex(labelReference => labelReference.Label == label) != -1)
                return false;

            // Shouldn't be able to add a label if property doesn't exist in bindings.
            if (id.ValueType.GetNumberOfChannels() > 1)
            {
                // To define a label on a tuple property, all channels must be defined for property.
                for (int i = 0; i < id.ValueType.GetNumberOfChannels(); ++i)
                {
                    if (channels.FindIndex(channel => channel.ID.Equals(id[i])) == -1)
                        return false;
                }
            }
            else
            {
                if (channels.FindIndex(channel => channel.ID.Equals(id)) == -1)
                    return false;
            }

            // Label ValueType must match associated ID
            if (id.ValueType != label.ValueType)
                return false;

            m_GenericPropertyLabels.Add(new GenericPropertyLabelReference
            {
                ID = id,
                Label = label
            });

            return true;
        }

        /// <summary>
        /// Removes the property label from channel it has been appended to.
        /// </summary>
        /// <param name="label">The generic property label.</param>
        /// <returns>Returns True if label has been removed from channel. False otherwise.</returns>
        /// <exception cref="ArgumentNullException">label is null.</exception>
        internal bool RemoveGenericPropertyLabel(GenericPropertyLabel label)
        {
            if (label == null)
                throw new ArgumentNullException($"The Argument {nameof(label)} cannot be null");

            int index = m_GenericPropertyLabels.FindIndex(labelReference => labelReference.Label == label);
            if (index != -1)
            {
                m_GenericPropertyLabels.RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Queries whether the skeleton definition implements all the labels of a label set.
        /// </summary>
        /// <param name="labelSet">The label set.</param>
        /// <returns>Returns True if all labels are referenced in the skeleton definition. False otherwise.</returns>
        /// <exception cref="ArgumentNullException">labelSet is null.</exception>
        internal bool ImplementsLabelSet(SkeletonLabelSet labelSet)
        {
            if (labelSet == null)
                throw new ArgumentNullException($"The Argument {nameof(labelSet)} cannot be null");

            bool allLabelsAreSet = labelSet.TransformLabels.Find(label => QueryTransformIndex(label) == -1);
            if (!allLabelsAreSet)
                return false;

            allLabelsAreSet = labelSet.GenericPropertyLabels.Find(label => QueryGenericPropertyIndex(label) == -1);
            if (!allLabelsAreSet)
                return false;

            return true;
        }

        /// <summary>
        /// Retrieves the range in the active transform channels based on the skeleton root node if set.
        /// </summary>
        /// <returns>Returns the range based on the skeleton root node.</returns>
        RangeInt GetActiveTransformChannelsRange()
        {
            if (m_Root.Equals(TransformBindingID.Invalid))
                return new RangeInt(0, m_TransformChannels.Count);

            int index = m_TransformChannels.FindIndex((channel) => channel.ID.Equals(m_Root));
            if (index == -1)
                return new RangeInt(0, m_TransformChannels.Count);

            int lastIndex = m_TransformChannels.FindLastIndex((channel) => channel.ID.Path.StartsWith(m_Root.Path));

            return new RangeInt(index, lastIndex - index + 1);
        }

        /// <summary>
        /// Queries the channel index that is identified by the specified label.
        /// Optionally, continuous channels can be queried by specifying a size.
        /// </summary>
        /// <param name="label">The transform property label.</param>
        /// <param name="size">The continuous size of channels to query.</param>
        /// <returns>Returns the index if channels matching the query were found. -1 otherwise.</returns>
        /// <exception cref="ArgumentNullException">label is null.</exception>
        internal int QueryTransformIndex(TransformLabel label, uint size = 1)
        {
            if (label == null)
                throw new ArgumentNullException($"The Argument {nameof(label)} cannot be null");

            var index = m_TransformLabels.FindIndex(labelReference => labelReference.Label == label);
            if (index == -1)
                return -1;

            var id = m_TransformLabels[index].ID;
            return QueryTransformIndex(id, size);
        }

        /// <summary>
        /// Queries the channel index that is identified by the specified id.
        /// Optionally, continuous channels can be queried by specifying a size.
        /// </summary>
        /// <param name="id">The transform binding id.</param>
        /// <param name="size">The continuous size of channels to query.</param>
        /// <returns>Returns the index if channels matching the query were found. -1 otherwise.</returns>
        /// <exception cref="ArgumentException">id is invalid</exception>
        internal int QueryTransformIndex(TransformBindingID id, uint size = 1)
        {
            if (id.Equals(TransformBindingID.Invalid))
                throw new ArgumentException($"The Argument {nameof(id)} is not valid");

            var range = GetActiveTransformChannelsRange();
            var index = m_TransformChannels.FindIndex(range.start, range.length, (channel) => channel.ID.Equals(id));
            if (index != -1)
                return ((index + size) <= range.end) ? index - range.start : -1;

            return -1;
        }

        /// <summary>
        /// Determines if the channel that is identified by the specified id is part of this skeleton.
        /// </summary>
        /// <param name="id">The transform binding id.</param>
        /// <returns>Returns the true if the id exists in this skeleton, false otherwise.</returns>
        /// <exception cref="ArgumentException">id is invalid</exception>
        public bool Contains(TransformBindingID id)
        {
            if (id.Equals(TransformBindingID.Invalid))
                throw new ArgumentException("Invalid identifier", nameof(id));

            return m_TransformChannels.FindIndex(channel => channel.ID.Equals(id)) != -1
                || m_InactiveTransformChannels.FindIndex(channel => channel.ID.Equals(id)) != -1;
        }

        /// <summary>
        /// Queries the transform labels that are associated with a channel ID.
        /// </summary>
        /// <param name="id">Channel ID</param>
        /// <param name="labels">List of transform labels.</param>
        /// <exception cref="ArgumentException">id is invalid</exception>
        internal void QueryTransformLabels(TransformBindingID id, List<TransformLabel> labels)
        {
            if (id.Equals(TransformBindingID.Invalid))
                throw new ArgumentException($"The Argument {nameof(id)} is not valid");

            for (int i = 0; i < m_TransformLabels.Count; ++i)
            {
                if (m_TransformLabels[i].ID.Equals(id))
                    labels.Add(m_TransformLabels[i].Label);
            }
        }

        /// <summary>
        /// Queries the channel index that is identified by the specified label.
        /// Optionally, continuous channels can be queried by specifying a size.
        /// </summary>
        /// <param name="label">The generic property label.</param>
        /// <param name="size">The continuous size of channels to query.</param>
        /// <returns>Returns the index if channels matching the query were found. -1 otherwise.</returns>
        /// <exception cref="ArgumentNullException">label is null.</exception>
        internal int QueryGenericPropertyIndex(GenericPropertyLabel label, uint size)
        {
            if (label == null)
                throw new ArgumentNullException($"The Argument {nameof(label)} cannot be null");

            var index = m_GenericPropertyLabels.FindIndex(labelReference => labelReference.Label == label);
            if (index == -1)
                return -1;

            var id = m_GenericPropertyLabels[index].ID;

            // Label does not match binding ID.
            if (label.ValueType != id.ValueType)
                return -1;

            return QueryGenericPropertyIndex(id, size);
        }

        /// <summary>
        /// Queries the channel index that is identified by the specified label.
        /// Optionally, continuous channels can be queried by specifying a size.
        /// </summary>
        /// <param name="label">The generic property label.</param>
        /// <returns>Returns the index if channels matching the query were found. -1 otherwise.</returns>
        /// <exception cref="ArgumentNullException">label is null.</exception>
        internal int QueryGenericPropertyIndex(GenericPropertyLabel label)
        {
            if (label == null)
                throw new ArgumentNullException($"The Argument {nameof(label)} cannot be null");

            var index = m_GenericPropertyLabels.FindIndex(labelReference => labelReference.Label == label);
            if (index == -1)
                return -1;

            var id = m_GenericPropertyLabels[index].ID;

            // Label does not match binding ID.
            if (label.ValueType != id.ValueType)
                return -1;

            return QueryGenericPropertyIndex(id, id.ValueType.GetNumberOfChannels());
        }

        /// <summary>
        /// Queries the channel index that is identified by the specified binding id.
        /// </summary>
        /// <param name="id">The generic binding id.</param>
        /// <returns>Returns the index if channels matching the query were found. -1 otherwise.</returns>
        /// <exception cref="ArgumentException">id is invalid</exception>
        /// <exception cref="InvalidOperationException">Type must be a recognized animatable type.</exception>
        internal int QueryGenericPropertyIndex(GenericBindingID id)
        {
            return QueryGenericPropertyIndex(id, id.ValueType.GetNumberOfChannels());
        }

        /// <summary>
        /// Queries the channel index that is identified by the specified binding id.
        /// Optionally, continuous channels can be queried by specifying a size.
        /// </summary>
        /// <param name="id">The generic binding id.</param>
        /// <param name="size">The continuous size of channels to query.</param>
        /// <returns>Returns the index if channels matching the query were found. -1 otherwise.</returns>
        /// <exception cref="ArgumentException">id is invalid</exception>
        /// <exception cref="InvalidOperationException">Type must be a recognized animatable type.</exception>
        internal int QueryGenericPropertyIndex(GenericBindingID id, uint size = 1)
        {
            if (id.Equals(GenericBindingID.Invalid))
                throw new ArgumentException($"The Argument {nameof(id)} is not valid");

            switch (id.ValueType)
            {
                case GenericPropertyType.Float:
                    return QueryGenericPropertyIndex(m_FloatChannels, id, size);
                case GenericPropertyType.Float2:
                    return QueryGenericPropertyIndex(m_FloatChannels, id.x, size);
                case GenericPropertyType.Float3:
                    return QueryGenericPropertyIndex(m_FloatChannels, id.x, size);
                case GenericPropertyType.Int:
                    return QueryGenericPropertyIndex(m_IntChannels, id, size);
                case GenericPropertyType.Quaternion:
                    // Quaternion indices need to be offset as they are appended to transform channels.
                    return QueryGenericPropertyIndex(m_QuaternionChannels, id, size);
                default:
                    throw new InvalidOperationException($"Invalid Type {id.ValueType}");
            }
        }

        private int QueryGenericPropertyIndex<T>(List<GenericChannel<T>> channels, GenericBindingID id, uint size)
            where T : struct
        {
            var index = channels.FindIndex(channel => channel.ID.Equals(id));
            if (index != -1)
                return ((index + size) <= channels.Count) ? index : -1;

            return -1;
        }

        /// <summary>
        /// Queries the channels that are identified by the specified id.
        /// </summary>
        /// <param name="id">Channel ID</param>
        /// <param name="channels">Generic channels matching the binding id</param>
        /// <exception cref="ArgumentException">id is invalid</exception>
        /// <exception cref="InvalidOperationException">Type must be a recognized animatable type.</exception>
        public void QueryGenericPropertyChannels(GenericBindingID id, List<IGenericChannel> channels)
        {
            if (id.Equals(GenericBindingID.Invalid))
                throw new ArgumentException($"The Argument {nameof(id)} is not valid");

            var index = QueryGenericPropertyIndex(id);
            if (index != -1)
            {
                uint numberOfChannels = id.ValueType.GetNumberOfChannels();

                switch (id.ValueType.GetGenericChannelType())
                {
                    case GenericPropertyType.Float:
                        for (int i = 0; i < numberOfChannels; ++i)
                        {
                            channels.Add(m_FloatChannels[index + i]);
                        }
                        break;
                    case GenericPropertyType.Int:
                        for (int i = 0; i < numberOfChannels; ++i)
                        {
                            channels.Add(m_IntChannels[index + i]);
                        }
                        break;
                    case GenericPropertyType.Quaternion:
                        for (int i = 0; i < numberOfChannels; ++i)
                        {
                            channels.Add(m_QuaternionChannels[index + i]);
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid Type {id.ValueType}");
                }
            }
        }

        /// <summary>
        /// Queries Property Labels associated to a specific channel.
        /// </summary>
        /// <param name="id">Channel ID</param>
        /// <param name="labels">List of property labels.</param>
        /// <exception cref="ArgumentException">id is invalid</exception>
        internal void QueryGenericPropertyLabels(GenericBindingID id, List<GenericPropertyLabel> labels)
        {
            if (id.Equals(GenericBindingID.Invalid))
                throw new ArgumentException($"The Argument {nameof(id)} is not valid");

            for (int i = 0; i < m_GenericPropertyLabels.Count; ++i)
            {
                if (m_GenericPropertyLabels[i].ID.Equals(id))
                {
                    labels.Add(m_GenericPropertyLabels[i].Label);
                }
            }

            if (id.ValueType.GetNumberOfChannels() > 1)
            {
                for (int i = 0; i < id.ValueType.GetNumberOfChannels(); ++i)
                {
                    for (int j = 0; j < m_GenericPropertyLabels.Count; ++j)
                    {
                        if (m_GenericPropertyLabels[j].ID.Equals(id[i]))
                        {
                            labels.Add(m_GenericPropertyLabels[j].Label);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates the DOTS representation of a rig.
        /// </summary>
        /// <param name="hasher">
        /// The hash function used to generate the ID of the rig channels.
        /// </param>
        /// <returns>The blob asset reference of a RigDefinition.</returns>
        public BlobAssetReference<RigDefinition> ToRigDefinition(IBindingHashGenerator hasher = null)
        {
            BlobAssetReference<RigDefinition> rigDefinition;
            using (var rigBuilderData = ToRigBuilderData(hasher, Allocator.Temp))
            {
                rigDefinition = RigBuilder.CreateRigDefinition(rigBuilderData);
            }

            return rigDefinition;
        }

        /// <summary>
        /// Fills the lists of the RigBuilderData from the bones and custom channels of the RigComponent.
        /// </summary>
        /// <param name="hasher">
        /// The hash function used to generate the ID of the rig channels.
        /// </param>
        /// <param name="allocator">
        /// A member of the [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html)
        /// enumeration.
        /// It is used to allocate all the NativeLists inside the RigBuilderData.
        /// </param>
        /// <returns>
        /// The RigBuilderData with all its lists filled with the corresponding rig channels.
        /// </returns>
        /// <remarks>
        /// If you have your own rig representation, you just need to create a function like this one that fills
        /// a <see cref="RigBuilderData"/> and use it with <see cref="RigBuilder.CreateRigDefinition"/>.
        /// </remarks>
        public RigBuilderData ToRigBuilderData(IBindingHashGenerator hasher = null, Allocator allocator = Allocator.Persistent)
        {
            hasher = hasher ?? new BindingHashGenerator();

            var transformChannelsRange = GetActiveTransformChannelsRange();

            var skeletonNodesCount = transformChannelsRange.length;
            var floatChannelsCount = m_FloatChannels.Count;
            var intChannelsCount = m_IntChannels.Count;
            var quaternionChannelsCount = m_QuaternionChannels.Count;

            var rigBuilderData = new RigBuilderData(allocator);
            rigBuilderData.SkeletonNodes.Capacity = skeletonNodesCount;
            rigBuilderData.RotationChannels.Capacity = quaternionChannelsCount;
            rigBuilderData.FloatChannels.Capacity = floatChannelsCount;
            rigBuilderData.IntChannels.Capacity = intChannelsCount;

            for (int i = transformChannelsRange.start; i < transformChannelsRange.end; i++)
            {
                rigBuilderData.SkeletonNodes.Add(new SkeletonNode
                {
                    Id = hasher.ToHash(m_TransformChannels[i].ID),
                    AxisIndex = -1,
                    LocalTranslationDefaultValue = m_TransformChannels[i].Properties.DefaultTranslationValue,
                    LocalRotationDefaultValue = m_TransformChannels[i].Properties.DefaultRotationValue,
                    LocalScaleDefaultValue = m_TransformChannels[i].Properties.DefaultScaleValue,
                    ParentIndex = m_TransformChannels.FindIndex(channel => channel.ID.Equals(m_TransformChannels[i].ID.GetParent()))
                });
            }

            for (int i = 0; i < m_QuaternionChannels.Count; i++)
            {
                rigBuilderData.RotationChannels.Add(new LocalRotationChannel
                {
                    Id = hasher.ToHash(m_QuaternionChannels[i].ID),
                    DefaultValue = m_QuaternionChannels[i].DefaultValue
                });
            }

            for (int i = 0; i < m_FloatChannels.Count; i++)
            {
                rigBuilderData.FloatChannels.Add(new Unity.Animation.FloatChannel
                {
                    Id = hasher.ToHash(m_FloatChannels[i].ID),
                    DefaultValue = m_FloatChannels[i].DefaultValue
                });
            }

            for (int i = 0; i < m_IntChannels.Count; i++)
            {
                rigBuilderData.IntChannels.Add(new Unity.Animation.IntChannel
                {
                    Id = hasher.ToHash(m_IntChannels[i].ID),
                    DefaultValue = m_IntChannels[i].DefaultValue
                });
            }

            return rigBuilderData;
        }

        /// <summary>
        /// Sets all the transform channels that are inactive. This is used internally to being able to set all active/inactive bones in a single method.
        /// Beware that this method can be dangerous! It doesn't ensure that all bones form an unbroken chain from the root to every leaf.
        /// </summary>
        /// <param name="ids">A list of ids of bones, which are included in this skeleton, that need to be inactive. All remaining bones in this skeleton will be set as active.</param>
        /// <exception cref="ArgumentException">If any id is invalid or does not point to a bone in this skeleton an ArgumentException will be thrown. The skeleton will not be modified.</exception>
        internal void SetInactiveTransformChannels(IEnumerable<TransformBindingID> ids)
        {
            if (ids == null)
                throw new NullReferenceException(nameof(ids));
            m_TransformChannels.AddRange(m_InactiveTransformChannels);
            m_InactiveTransformChannels.Clear();
            var channelIndices = new List<int>();
            foreach (var id in ids)
            {
                int index = m_TransformChannels.FindIndex(channel => channel.ID.Equals(id));
                if (index == -1)
                    throw new ArgumentException($"{nameof(ids)} contains a {nameof(TransformBindingID)} that is not part of this {nameof(Skeleton)}", nameof(ids));
                channelIndices.Add(index);
            }
            channelIndices.Sort();
            for (int i = channelIndices.Count - 1; i >= 0; i--)
            {
                m_InactiveTransformChannels.Add(m_TransformChannels[i]);
                m_TransformChannels.RemoveAt(i);
            }
            m_TransformChannels.Sort();
            m_InactiveTransformChannels.Sort();
        }

        [Flags]
        enum BoneRelationship
        {
            None = 0,
            Self = 1,
            Ancestor = 2,
            Decendant = 4
        }

        static BoneRelationship GetPathRelationship(string path, string otherPath)
        {
            // We check if paths are subsets of each other, in which case they're an Ancestor or Descentant
            // Note: If the length of our path ends with a "/" in the path
            // of the other channel then it could potentially be a subset
            // (this is to prevent finding "Root/Left" in "Root/LeftFoot/")

            if (otherPath.Length > path.Length)
            {
                return (path.Length != 0 && // root is never a descendant
                    otherPath[path.Length] == k_PathSeparator && otherPath.StartsWith(path))
                    ? BoneRelationship.Decendant : BoneRelationship.None;
            }
            else if (otherPath.Length < path.Length)
            {
                return (otherPath.Length == 0 || // root is always an ancestor
                    (path[otherPath.Length] == k_PathSeparator && path.StartsWith(otherPath))) ?
                    BoneRelationship.Ancestor : BoneRelationship.None;
            }

            // Finally, check if the path is identical, in which case it's itself
            return (otherPath == path) ? BoneRelationship.Self : BoneRelationship.None;
        }

        // returns true if the given ids contain the root id
        static bool GetPathsFromIDs(Skeleton skeleton, IEnumerable<TransformBindingID> ids, List<string> foundPaths)
        {
            bool containsRoot = false;
            // Loop through all ids, check them & find their respective channels & paths
            foreach (var id in ids)
            {
                if (id.Equals(TransformBindingID.Invalid))
                    throw new ArgumentException($"The Argument {nameof(ids)} contains an id that is not valid.", nameof(ids));

                // We need to check if our id exists in our skeleton
                if (!skeleton.Contains(id))
                    throw new ArgumentException($"The Argument {nameof(ids)} contains an id that is not part of this {nameof(Skeleton)}.", nameof(ids));

                var path = id.Path;
                // If our current channel is the root, then ALL channels need to be modified
                if (string.IsNullOrEmpty(path))
                    containsRoot = true;

                // Store our path, we'll use it to find all our descendants
                foundPaths.Add(path);
            }
            return containsRoot;
        }

        // Moves transform channels from one list to another, depending on if the relationship between their paths (an enum), is included in the compareRelationship flags
        static void MoveTransformChannelsBasedOnPathRelationship(List<string> comparePaths, BoneRelationship compareRelationship, List<TransformChannel> fromChannel, List<TransformChannel> toChannel)
        {
            // Since we move channels from one list to another, we don't actually need to check both lists.
            // So we go through all the channels in fromChannel, and see what it's relationship is to our channel.
            for (int i = fromChannel.Count - 1; i >= 0; i--)
            {
                var otherPath = fromChannel[i].ID.Path;
                foreach (var path in comparePaths)
                {
                    // This returns the relationship between the paths as an enum value
                    // (each relationship option has its own bit)
                    var relationship = GetPathRelationship(path, otherPath);

                    // If any of the bits in compareRelationship match ...
                    if ((relationship & compareRelationship) == BoneRelationship.None)
                        continue;

                    // ... we move the channel from fromChannel to toChannel
                    toChannel.Add(fromChannel[i]);
                    fromChannel.RemoveAt(i);

                    // since this value isn't part of fromChannel anymore,
                    // we can break out the inner loop.
                    break;
                }
            }
            toChannel.Sort();
            fromChannel.Sort();
        }

        // Temporaries to avoid GC allocations at runtime/editor time
        static readonly List<string> s_FoundPaths = new List<string>();
        static TransformBindingID[] s_BindingIDArrayOf1 = new TransformBindingID[1];

        /// <summary>
        /// Sets all the descendants and ancestors of all the given bones to active, including the bones themselves.
        /// It'll skip all siblings and siblings of our ancestors.
        /// This method ensures an unbroken chain from the root to all the descendants of the given bones in <paramref name="ids"/>.
        /// </summary>
        /// <param name="ids">The ids of the bones for which we set the descendants/ancestors to active. All the ids must point to bones in this skeleton or an exception will be thrown.</param>
        /// <exception cref="ArgumentException">If an id is invalid or does not point to a bone in this skeleton an ArgumentException will be thrown.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="ids"/> is null, an ArgumentNullException will be thrown.</exception>
        public void SetTransformChannelDescendantsAndAncestorsToActive(IEnumerable<TransformBindingID> ids)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            // Find all the paths in the given ids, throws exception when we have invalid ids
            s_FoundPaths.Clear();
            bool containsRoot = GetPathsFromIDs(this, ids, s_FoundPaths);

            // Nothing to do ...
            if (s_FoundPaths.Count == 0)
                return;

            // If the array contains the root, then ALL channels need to be set to active
            if (containsRoot) { m_TransformChannels.AddRange(m_InactiveTransformChannels); m_InactiveTransformChannels.Clear(); m_TransformChannels.Sort(); return; }

            const BoneRelationship pathRelationship = BoneRelationship.Ancestor | BoneRelationship.Decendant | BoneRelationship.Self;

            // We want to move active transform channels to inactive transform channels, based on their path relationship
            MoveTransformChannelsBasedOnPathRelationship(s_FoundPaths, pathRelationship, m_InactiveTransformChannels, m_TransformChannels);
        }

        /// <summary>
        /// Sets all the descendants and the ancestors of a bone to active, including the bone itself. This method ensures an unbroken chain from the root to all the descendants of the given bone <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The <paramref name="id"/> of the bone for which set the descendants/ancestors to active. This id must point to a bone in this skeleton or an exception will be thrown.</param>
        /// <exception cref="ArgumentException">If <paramref name="id"/> is invalid or does not point to a bone in this skeleton an ArgumentException will be thrown.</exception>
        public void SetTransformChannelDescendantsAndAncestorsToActive(TransformBindingID id)
        {
            s_BindingIDArrayOf1[0] = id;
            SetTransformChannelDescendantsAndAncestorsToActive(s_BindingIDArrayOf1);
        }

        /// <summary>
        /// Sets all the descendants of all the given bones to inactive, optionally including the given bones themselves.
        /// </summary>
        /// <param name="ids">The ids of all the given bones for which we set the descendants to inactive. All the ids must point to bones in this skeleton or an exception will be thrown.</param>
        /// <exception cref="ArgumentException">If an id in <paramref name="ids"/> is invalid or does not point to a bone in this skeleton an ArgumentException will be thrown.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="ids"/> is null, an ArgumentNullException will be thrown.</exception>
        public void SetTransformChannelDescendantsToInactive(IEnumerable<TransformBindingID> ids, bool includeSelf = true)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            // Find all the paths in the given ids, throws exception when we have invalid ids
            s_FoundPaths.Clear();
            bool containsRoot = GetPathsFromIDs(this, ids, s_FoundPaths);
            if (s_FoundPaths.Count == 0)
                return;

            // If the array contains the root, then ALL channels need to be set to inactive
            if (containsRoot) { m_InactiveTransformChannels.AddRange(m_TransformChannels); m_TransformChannels.Clear(); m_InactiveTransformChannels.Sort(); return; }

            var pathRelationship = includeSelf ? BoneRelationship.Decendant | BoneRelationship.Self : BoneRelationship.Decendant;

            // We want to move active transform channels to inactive transform channels, based on their path relationship
            MoveTransformChannelsBasedOnPathRelationship(s_FoundPaths, pathRelationship, m_TransformChannels, m_InactiveTransformChannels);
        }

        /// <summary>
        /// Sets all the descendants of a bone to inactive, optionally including the bone itself.
        /// </summary>
        /// <param name="id">The id of the bone for which we set the descendants to inactive. This id must point to a bone in this skeleton or an exception will be thrown.</param>
        /// <param name="includeSelf">True if <paramref name="id"/> needs to set to inactive, false if it needs to be ignored. Default value is true.</param>
        /// <exception cref="ArgumentException">If the <paramref name="id"/> is invalid or does not point to a bone in this skeleton an ArgumentException will be thrown.</exception>
        public void SetTransformChannelDescendantsToInactive(TransformBindingID id, bool includeSelf = true)
        {
            s_BindingIDArrayOf1[0] = id;
            SetTransformChannelDescendantsToInactive(s_BindingIDArrayOf1, includeSelf);
        }

        /// <summary>
        /// Sets all the ancestors of the given bones to active.
        /// </summary>
        /// <param name="ids">The ids of all the given bones for which we set the ancestors to active. All the ids must point to bones in this skeleton or an exception will be thrown.</param>
        /// <param name="includeSelf">True if the bones included in <paramref name="ids"/> needs to be set to active, false if it needs to be ignored. Default value is true.</param>
        /// <exception cref="ArgumentException">If an id in <paramref name="ids"/> is invalid or does not point to a bone in this skeleton an ArgumentException will be thrown.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="ids"/> is null, an ArgumentNullException will be thrown.</exception>
        public void SetTransformChannelAncestorsToActive(IEnumerable<TransformBindingID> ids, bool includeSelf = true)
        {
            if (ids == null)
                throw new ArgumentNullException(nameof(ids));

            // Find all the paths in the given ids, throws exception when we have invalid ids
            s_FoundPaths.Clear();
            GetPathsFromIDs(this, ids, s_FoundPaths);
            if (s_FoundPaths.Count == 0)
                return;

            var pathRelationship = includeSelf ? (BoneRelationship.Ancestor | BoneRelationship.Self) : BoneRelationship.Ancestor;

            // We want to move active transform channels to inactive transform channels, based on their path relationship
            MoveTransformChannelsBasedOnPathRelationship(s_FoundPaths, pathRelationship, m_InactiveTransformChannels, m_TransformChannels);
        }

        /// <summary>
        /// Sets all the ancestors of a bone to active, optionally including the bone itself.
        /// </summary>
        /// <param name="id">The id of the bone for which set the ancestors to active. This id must point to a bone in this skeleton or an exception will be thrown.</param>
        /// <param name="includeSelf">True if <paramref name="id"/> needs to set to inactive, false if it needs to be ignored. Default value is true.</param>
        /// <exception cref="ArgumentException">If an id in <paramref name="id"/> is invalid or does not point to a bone in this skeleton an ArgumentException will be thrown.</exception>
        public void SetTransformChannelAncestorsToActive(TransformBindingID id, bool includeSelf = true)
        {
            s_BindingIDArrayOf1[0] = id;
            SetTransformChannelAncestorsToActive(s_BindingIDArrayOf1, includeSelf);
        }

        /// <summary>
        /// Queries a given transform channel/bone to see if it's active, inactive or does not exist in this skeleton
        /// </summary>
        internal TransformChannelState GetTransformChannelState(TransformBindingID id)
        {
            if (id.Equals(TransformBindingID.Invalid))
                throw new ArgumentException($"The Argument {nameof(id)} is not valid");
            var channelIndex = m_TransformChannels.FindIndex(channel => channel.ID.Equals(id));
            if (channelIndex != -1)
                return TransformChannelState.Active;
            channelIndex = m_InactiveTransformChannels.FindIndex(channel => channel.ID.Equals(id));
            if (channelIndex != -1)
                return TransformChannelState.Inactive;
            return TransformChannelState.DoesNotExist;
        }
    }
}
