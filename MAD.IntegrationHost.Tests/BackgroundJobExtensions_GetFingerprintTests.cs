using MAD.Integration.Common.Jobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MAD.Integration.Common.Tests
{
    [TestClass]
    public class BackgroundJobExtensions_GetFingerprintTests
    {
        private class FingerprintClass
        {
            public static string GetFingerprint(List<object> args)
            {
                return BackgroundJobExtensions.GetFingerprint(typeof(FingerprintClass), typeof(FingerprintClass).GetMethod(nameof(FingerprintClass.FingerprintMethod)), args);
            }

            public string FingerprintMethod()
            {
                return string.Empty;
            }
        }

        [TestMethod]
        public void GetFingerprint_NoArguments_HashReproduced()
        {
            var fp = FingerprintClass.GetFingerprint(null);
            var fp2 = FingerprintClass.GetFingerprint(null);

            Assert.AreEqual(fp, fp2);
        }

        [TestMethod]
        public void GetFingerprint_IntegerList_HashReproduced()
        {
            var intList = new[] { 9, 10, 348, 5444, 1111, 3243, 656756, 8 }.Cast<object>().ToList();
            var fp = FingerprintClass.GetFingerprint(intList);
            var fp2 = FingerprintClass.GetFingerprint(intList);

            Assert.AreEqual(fp, fp2);
        }

        [TestMethod]
        public void GetFingerprint_SeparateIntegerList_HashNotReproduced()
        {
            var intList = new[] { 1, 2, 3, 4, 5 }.Cast<object>().ToList();
            var intList2 = new[] { 9,10,348,5444,1111,3243,656756,8 }.Cast<object>().ToList();
            var fp = FingerprintClass.GetFingerprint(intList);
            var fp2 = FingerprintClass.GetFingerprint(intList2);

            Assert.AreNotEqual(fp, fp2);
        }
    }
}
