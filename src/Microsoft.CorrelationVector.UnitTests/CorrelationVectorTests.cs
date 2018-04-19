// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CorrelationVector;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CorrelationVector.UnitTests
{
    [TestClass]
    public class CorrelationVectorTests
    {
        [TestMethod]
        public void SimpleCreateCorrelationVectorTest()
        {
            var correlationVector = new CorrelationVector();
            var splitVector = correlationVector.Value.Split('.');
            
            Assert.AreEqual(2, splitVector.Length, "Correlation Vector should be created with two components separated by a '.'");
            Assert.AreEqual(16, splitVector[0].Length, "Correlation Vector base should be 16 character long");
            Assert.AreEqual("0", splitVector[1], "Correlation Vector extension should start with zero");
        }

        [TestMethod]
        public void CreateV1CorrelationVectorTest()
        {
            var correlationVector = new CorrelationVector(CorrelationVectorVersion.V1);
            var splitVector = correlationVector.Value.Split('.');

            Assert.AreEqual(2, splitVector.Length, "Correlation Vector should be created with two components separated by a '.'");
            Assert.AreEqual(16, splitVector[0].Length, "Correlation Vector base should be 16 character long");
            Assert.AreEqual("0", splitVector[1], "Correlation Vector extension should start with zero");
        }

        [TestMethod]
        public void CreateV2CorrelationVectorTest()
        {
            var correlationVector = new CorrelationVector(CorrelationVectorVersion.V2);
            var splitVector = correlationVector.Value.Split('.');

            Assert.AreEqual(2, splitVector.Length, "Correlation Vector should be created with two components separated by a '.'");
            Assert.AreEqual(22, splitVector[0].Length, "Correlation Vector base should be 22 character long");
            Assert.AreEqual("0", splitVector[1], "Correlation Vector extension should start with zero");
        }

        [TestMethod]
        public void CreateCorrelationVectorFromGuidTest()
        {
            var guid = System.Guid.NewGuid();
            var correlationVector = new CorrelationVector(guid);
            var splitVector = correlationVector.Value.Split('.');

            Assert.AreEqual(2, splitVector.Length, "Correlation Vector should be created with two components separated by a '.'");
            Assert.AreEqual(22, splitVector[0].Length, "Correlation Vector base should be 22 character long");
            Assert.AreEqual("0", splitVector[1], "Correlation Vector extension should start with zero");
        }

        [TestMethod]
        public void ParseCorrelationVectorV1Test()
        {
            var correlationVector = CorrelationVector.Parse("ifCuqpnwiUimg7Pk.1");
            var splitVector = correlationVector.Value.Split('.');

            Assert.AreEqual("ifCuqpnwiUimg7Pk", splitVector[0], "Correlation Vector base was not parsed properly");
            Assert.AreEqual("1", splitVector[1], "Correlation Vector extension was not parsed properly");
        }

        [TestMethod]
        public void ParseCorrelationVectorV2Test()
        {
            var correlationVector = CorrelationVector.Parse("Y58xO9ov0kmpPvkiuzMUVA.3.4.5");
            var splitVector = correlationVector.Value.Split('.');

            Assert.AreEqual(4, splitVector.Length, "Correlation Vector was not parsed properly");
            Assert.AreEqual("Y58xO9ov0kmpPvkiuzMUVA", splitVector[0], "Correlation Vector base was not parsed properly");
            Assert.AreEqual("3", splitVector[1], "Correlation Vector extension was not parsed properly");
            Assert.AreEqual("4", splitVector[2], "Correlation Vector extension was not parsed properly");
            Assert.AreEqual("5", splitVector[3], "Correlation Vector extension was not parsed properly");
        }

        [TestMethod]
        public void SimpleIncrementCorrelationVectorTest()
        {
            var correlationVector = new CorrelationVector();
            correlationVector.Increment();
            var splitVector = correlationVector.Value.Split('.');
            Assert.AreEqual("1", splitVector[1], "Correlation Vector extension should have been incremented by one");
        }

        [TestMethod]
        public void SimpleExtendCorrelationVectorTest()
        {
            var correlationVector = new CorrelationVector();
            var splitVector = correlationVector.Value.Split('.');
            var vectorBase = splitVector[0];
            var extension = splitVector[1];

            correlationVector = CorrelationVector.Extend(correlationVector.Value);
            splitVector = correlationVector.Value.Split('.');

            Assert.AreEqual(3, splitVector.Length, "Correlation Vector should contain 3 components separated by a '.' after extension");
            Assert.AreEqual(vectorBase, splitVector[0], "Correlation Vector base should contain the same base after extension");
            Assert.AreEqual(extension, splitVector[1], "Correlation Vector should preserve original ");
            Assert.AreEqual("0", splitVector[2], "Correlation Vector new extension should start with zero");
        }

        public void ValidateCreationTest()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = true;
            var correlationVector = new CorrelationVector();
            correlationVector.Increment();
            var splitVector = correlationVector.Value.Split('.');
            Assert.AreEqual("1", splitVector[1], "Correlation Vector extension should have been incremented by one");
        }
    }
}
