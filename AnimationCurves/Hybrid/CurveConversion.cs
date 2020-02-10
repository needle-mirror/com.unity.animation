using Unity.Collections;
using Unity.Entities;

namespace Unity.Animation.Hybrid
{
    public static class CurveConversion
    {
        static (float, KeyframeData) KeyframeConversion(UnityEngine.Keyframe inKey)
        {
            var key = new KeyframeData
            {
                InTangent = inKey.inTangent,
                OutTangent = inKey.outTangent,
                Value = inKey.value,
            };

            return (inKey.time, key);
        }

        /// <summary>
        /// Converts a UnityEngine AnimationCurve to a DOTS AnimationCurve.
        /// </summary>
        /// <param name="curve">The UnityEngine.AnimationCurve to convert to DOTS format.</param>
        /// <returns>Returns a DOTS AnimationCurve.</returns>
        public static Animation.AnimationCurve ToDotsAnimationCurve(this UnityEngine.AnimationCurve curve)
        {
            var curveBlob = curve.ToAnimationCurveBlobAssetRef();
            var dotsCurve = new AnimationCurve();
            dotsCurve.SetAnimationCurveBlobAssetRef(curveBlob);

            return dotsCurve;
        }

        /// <summary>
        /// Converts a UnityEngine AnimationCurve to a BlobAssetReference of an AnimationCurveBlob.
        /// </summary>
        /// <param name="curve">The UnityEngine.AnimationCurve to convert to DOTS format.</param>
        /// <returns>Returns the BlobAssetReference of the AnimationCurveBlob.</returns>
        public static BlobAssetReference<AnimationCurveBlob> ToAnimationCurveBlobAssetRef(this UnityEngine.AnimationCurve curve)
        {
            if (curve.length == 0)
            {
                return BlobAssetReference<AnimationCurveBlob>.Null;
            }

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var curveBlob = ref blobBuilder.ConstructRoot<AnimationCurveBlob>();
            FillKeyframeCurveBlob(curve, ref blobBuilder, ref curveBlob);

            var outputClip = blobBuilder.CreateBlobAssetReference<AnimationCurveBlob>(Allocator.Persistent);

            blobBuilder.Dispose();

            return outputClip;
        }

        static void FillKeyframeCurveBlob(UnityEngine.AnimationCurve sourceCurve, ref BlobBuilder blobBuilder, ref AnimationCurveBlob curveBlob)
        {
            var times = blobBuilder.Allocate(ref curveBlob.KeyframesTime, sourceCurve.length);
            var keyframes = blobBuilder.Allocate(ref curveBlob.KeyframesData, sourceCurve.length);

            float length = sourceCurve.length;
            for (var i = 0; i < length; ++i)
            {
                (var time, var key) = KeyframeConversion(sourceCurve.keys[i]);
                times[i] = time;
                keyframes[i] = key;
            }
        }
    }
}

