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
                InWeight = inKey.weightedMode == UnityEngine.WeightedMode.In || inKey.weightedMode == UnityEngine.WeightedMode.Both ? inKey.inWeight : KeyframeData.DEFAULT_WEIGHT,
                OutWeight = inKey.weightedMode == UnityEngine.WeightedMode.Out || inKey.weightedMode == UnityEngine.WeightedMode.Both ? inKey.outWeight : KeyframeData.DEFAULT_WEIGHT,
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
            FillAnimationCurveBlob(curve, ref blobBuilder, ref curveBlob);

            var outputClip = blobBuilder.CreateBlobAssetReference<AnimationCurveBlob>(Allocator.Persistent);

            blobBuilder.Dispose();

            return outputClip;
        }

        /// <summary>
        /// Allocates memory in a blob and initializes it with the values of a UnityEngine animation curve.
        /// </summary>
        /// <param name="sourceCurve">The curve to be converted to a blob.</param>
        /// <param name="blobBuilder">The blob builder used to fill the blob.</param>
        /// <param name="curveBlob">The blob to which the curve is copied.</param>
        public static void FillAnimationCurveBlob(UnityEngine.AnimationCurve sourceCurve, ref BlobBuilder blobBuilder, ref AnimationCurveBlob curveBlob)
        {
            float length = sourceCurve.length;
            bool isHermitCurve = true;
            for (int i = 0; i < length; ++i)
            {
                isHermitCurve &= sourceCurve.keys[i].weightedMode == UnityEngine.WeightedMode.None;
            }

            var builder = curveBlob.CreateBuilder(blobBuilder, isHermitCurve ? AnimationCurveType.Hermite : AnimationCurveType.Bezier, sourceCurve.length);
            for (var i = 0; i < length; ++i)
            {
                var(time, key) = KeyframeConversion(sourceCurve.keys[i]);
                builder.SetKeyframe(i, time, key);
            }
        }
    }
}
