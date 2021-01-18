using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Unity.Animation.Editor.Tests
{
    class GraphVariantTests
    {
        public class GraphVariantScriptableObject : ScriptableObject
        {
            public GraphVariant Variant;
        }

        [Test]
        public void ToSerializedProperty_DoesNotThrow()
        {
            var scriptableObject = ScriptableObject.CreateInstance<GraphVariantScriptableObject>();
            var serializedObject = new SerializedObject(scriptableObject);
            SerializedProperty serializedPropertyVariant = serializedObject.FindProperty("Variant");
            var variant = new GraphVariant { Type = GraphVariant.ValueType.Float, Float = 4F };
            Assert.DoesNotThrow(() => variant.ToSerializedProperty(serializedPropertyVariant));
        }

        [Test]
        public void FromSerializedProperty_DoesNotThrow()
        {
            var scriptableObject = ScriptableObject.CreateInstance<GraphVariantScriptableObject>();
            var serializedObject = new SerializedObject(scriptableObject);
            SerializedProperty serializedPropertyVariant = serializedObject.FindProperty("Variant");
            var variant = new GraphVariant { Type = GraphVariant.ValueType.Float };
            variant.FromSerializedProperty(serializedPropertyVariant);
        }

        [Test]
        public void FromSerializedProperty_RetrievesValue()
        {
            var scriptableObject = ScriptableObject.CreateInstance<GraphVariantScriptableObject>();
            scriptableObject.Variant = new GraphVariant { Type = GraphVariant.ValueType.Float, Float = 4F };
            var serializedObject = new SerializedObject(scriptableObject);
            SerializedProperty serializedPropertyVariant = serializedObject.FindProperty("Variant");
            var variant = new GraphVariant { Type = GraphVariant.ValueType.Float };
            variant.FromSerializedProperty(serializedPropertyVariant);
            Assert.AreEqual(scriptableObject.Variant, variant);
        }

        [Test]
        public void ToSerializedProperty_StoresValue()
        {
            var scriptableObject = ScriptableObject.CreateInstance<GraphVariantScriptableObject>();
            var serializedObject = new SerializedObject(scriptableObject);
            SerializedProperty serializedPropertyVariant = serializedObject.FindProperty("Variant");

            var variant = new GraphVariant { Type = GraphVariant.ValueType.Float, Float = 4F };
            variant.ToSerializedProperty(serializedPropertyVariant);
            serializedObject.ApplyModifiedProperties();
            Assert.AreEqual(variant, scriptableObject.Variant);
        }
    }
}
