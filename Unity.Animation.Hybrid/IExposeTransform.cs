using Unity.Entities;
using UnityEngine;

namespace Unity.Animation.Hybrid
{
    public interface IExposeTransform
    {
        void AddExposeTransform(EntityManager entityManager, Entity rig, Entity transform, int index);
    }

    public interface IReadExposeTransform : IExposeTransform {}

    public interface IWriteExposeTransform : IExposeTransform {}

    public class ReadExposeTransform<TTransformHandle> : MonoBehaviour, IReadExposeTransform
        where TTransformHandle : struct, IReadTransformHandle
    {
        public void AddExposeTransform(EntityManager entityManager, Entity rig, Entity transform, int index)
        {
            RigEntityBuilder.AddReadTransformHandle<TTransformHandle>(
                entityManager, rig, transform, index
            );
        }
    }

    public class WriteExposeTransform<TTransformHandle> : MonoBehaviour, IWriteExposeTransform
        where TTransformHandle : struct, IWriteTransformHandle
    {
        public void AddExposeTransform(EntityManager entityManager, Entity rig, Entity transform, int index)
        {
            RigEntityBuilder.AddWriteTransformHandle<TTransformHandle>(
                entityManager, rig, transform, index
            );
        }
    }
}
