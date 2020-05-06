using NUnit.Framework;

namespace Unity.Animation.Tests
{
    public class StringHashTests
    {
        [TestCase(null, "0")]
        [TestCase("test0", "test1")]
        [TestCase("a/path/test", "b/path/test")]
        [TestCase("a/path/test", "a/path/testb")]
        [TestCase("123456789", "0123456789")]
        [TestCase("numbers/in/path/1234", "numbers/in/path/01234")]
        public void DifferentStringsGiveDifferentStringHashValues(string str1, string str2)
        {
            var strHash1 = new StringHash(str1);
            var strHash2 = new StringHash(str2);
            Assert.AreNotEqual(strHash1, strHash2);
        }

        [TestCase("", "")]
        [TestCase(null, null)]
        [TestCase(null, "")] // Special case null and empty string should give same default hash value
        [TestCase("test", "test")]
        [TestCase("a/path/test", "a/path/test")]
        [TestCase("123456789", "123456789")]
        [TestCase("numbers/in/path/1234", "numbers/in/path/1234")]
        public void SameStringsGiveSameStringHashValues(string str1, string str2)
        {
            var strHash1 = new StringHash(str1);
            var strHash2 = new StringHash(str2);
            Assert.AreEqual(strHash1, strHash2);
        }

        [Test]
        public void EmptyOrNullStringsGiveDefaultStringHashValue()
        {
            StringHash defaultValue = default;
            var testNull = new StringHash(null);
            var testEmpty = new StringHash("");

            Assert.AreEqual(testNull, defaultValue);
            Assert.AreEqual(testEmpty, defaultValue);
            Assert.IsTrue(StringHash.IsNullOrEmpty(testNull));
            Assert.IsTrue(StringHash.IsNullOrEmpty(testEmpty));
        }
    }
}
