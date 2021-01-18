using Unity.Mathematics;

namespace Unity.Animation
{
    sealed class RigDebugView
    {
        Rig m_Rig;

        public RigDebugView(Rig rig)
        {
            m_Rig = rig;
        }

        public StringHash[] SkeletonIds
        {
            get
            {
                return m_Rig.Value.Value.Skeleton.Ids.ToArray();
            }
        }

        public int[] SkeletonParentIndices
        {
            get
            {
                return m_Rig.Value.Value.Skeleton.ParentIndexes.ToArray();
            }
        }


        public StringHash[] TranslationBindings
        {
            get
            {
                return m_Rig.Value.Value.Bindings.TranslationBindings.ToArray();
            }
        }

        public StringHash[] RotationBindings
        {
            get
            {
                return m_Rig.Value.Value.Bindings.RotationBindings.ToArray();
            }
        }

        public StringHash[] ScaleBindings
        {
            get
            {
                return m_Rig.Value.Value.Bindings.ScaleBindings.ToArray();
            }
        }

        public StringHash[] FloatBindings
        {
            get
            {
                return m_Rig.Value.Value.Bindings.FloatBindings.ToArray();
            }
        }

        public StringHash[] IntBindings
        {
            get
            {
                return m_Rig.Value.Value.Bindings.IntBindings.ToArray();
            }
        }

        public unsafe float3[] TranslationDefaultValues
        {
            get
            {
                var count = m_Rig.Value.Value.Bindings.TranslationCurveCount / BindingSet.TranslationKeyFloatCount;

                float* floatPtr = (float*)m_Rig.Value.Value.DefaultValues.GetUnsafePtr();
                var data = new Ptr<float3>((float3*)(floatPtr + m_Rig.Value.Value.Bindings.TranslationSamplesOffset));

                float3[] values = new float3[count];
                for (int i = 0; i < count; i++)
                    values[i] = data.Get(i);

                return values;
            }
        }

        public unsafe quaternion[] RotationDefaultValues
        {
            get
            {
                var count = m_Rig.Value.Value.Bindings.RotationCurveCount / BindingSet.RotationKeyFloatCount;

                float* floatPtr = (float*)m_Rig.Value.Value.DefaultValues.GetUnsafePtr();
                var data = new Ptr<quaternion4>((quaternion4*)(floatPtr + m_Rig.Value.Value.Bindings.RotationSamplesOffset));

                quaternion[] values = new quaternion[count];
                for (int i = 0; i < count; i++)
                {
                    int idx = i & 0x3; // equivalent to % 4
                    ref quaternion4 q4 = ref data.Get(i >> 2);
                    values[i] = math.quaternion(q4.x[idx], q4.y[idx], q4.z[idx], q4.w[idx]);
                }

                return values;
            }
        }

        public unsafe float3[] ScaleDefaultValues
        {
            get
            {
                var count = m_Rig.Value.Value.Bindings.ScaleCurveCount / BindingSet.ScaleKeyFloatCount;

                float* floatPtr = (float*)m_Rig.Value.Value.DefaultValues.GetUnsafePtr();
                var data = new Ptr<float3>((float3*)(floatPtr + m_Rig.Value.Value.Bindings.ScaleSamplesOffset));

                float3[] values = new float3[count];
                for (int i = 0; i < count; i++)
                    values[i] = data.Get(i);

                return values;
            }
        }

        public unsafe float[] FloatDefaultValues
        {
            get
            {
                var count = m_Rig.Value.Value.Bindings.FloatCurveCount / BindingSet.FloatKeyFloatCount;

                float* floatPtr = (float*)m_Rig.Value.Value.DefaultValues.GetUnsafePtr();
                var data = new Ptr<float>((float*)(floatPtr + m_Rig.Value.Value.Bindings.FloatSamplesOffset));

                float[] values = new float[count];
                for (int i = 0; i < count; i++)
                    values[i] = data.Get(i);

                return values;
            }
        }

        public unsafe int[] IntDefaultValues
        {
            get
            {
                var count = m_Rig.Value.Value.Bindings.IntCurveCount / BindingSet.IntKeyFloatCount;

                float* floatPtr = (float*)m_Rig.Value.Value.DefaultValues.GetUnsafePtr();
                var data = new Ptr<int>((int*)(floatPtr + m_Rig.Value.Value.Bindings.IntSamplesOffset));

                int[] values = new int[count];
                for (int i = 0; i < count; i++)
                    values[i] = data.Get(i);

                return values;
            }
        }

        public int HashCode
        {
            get
            {
                return m_Rig.Value.Value.m_HashCode;
            }
        }
    }
}
