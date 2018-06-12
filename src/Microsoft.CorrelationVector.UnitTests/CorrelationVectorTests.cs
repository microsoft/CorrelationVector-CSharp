// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
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

        [TestMethod]
        public void ValidateCreationTest()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = true;
            var correlationVector = new CorrelationVector();
            correlationVector.Increment();
            var splitVector = correlationVector.Value.Split('.');
            Assert.AreEqual("1", splitVector[1], "Correlation Vector extension should have been incremented by one");
        }

        [TestMethod]
        public void ExtendNullCorrelationVector()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            // This shouldn't throw since we skip validation
            var vector = CorrelationVector.Extend(null);
            Assert.IsTrue(vector.ToString() == ".0");
            Assert.ThrowsException<ArgumentException>(() =>
            {
                CorrelationVector.ValidateCorrelationVectorDuringCreation = true;
                vector = CorrelationVector.Extend(null);
                Assert.IsTrue(vector.ToString() == ".0");
            }
            );
        }

        [TestMethod]
        public void ThrowWithInsufficientCharsCorrelationVectorValue()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            // This shouldn't throw since we skip validation
            var vector = CorrelationVector.Extend("tul4NUsfs9Cl7mO.1");
            Assert.ThrowsException<ArgumentException>(() =>
            {
                CorrelationVector.ValidateCorrelationVectorDuringCreation = true;
                vector = CorrelationVector.Extend("tul4NUsfs9Cl7mO.1");
            });
        }

        [TestMethod]
        public void ThrowWithTooManyCharsCorrelationVectorValue()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            // This shouldn't throw since we skip validation
            var vector = CorrelationVector.Extend("tul4NUsfs9Cl7mOfN/dupsl.1");

            Assert.ThrowsException<ArgumentException>(() =>
            {
                CorrelationVector.ValidateCorrelationVectorDuringCreation = true;
                vector = CorrelationVector.Extend("tul4NUsfs9Cl7mOfN/dupsl.1");
            });
        }

        [TestMethod]
        public void ThrowWithTooBigCorrelationVectorValue()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                CorrelationVector.ValidateCorrelationVectorDuringCreation = true;
                /* Bigger than 63 chars */
                var vector = CorrelationVector.Extend("tul4NUsfs9Cl7mOf.2147483647.2147483647.2147483647.2147483647.2147483647");
            });
        }

        [TestMethod]
        public void ThrowWithTooBigCorrelationVectorValueV2()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                CorrelationVector.ValidateCorrelationVectorDuringCreation = true;
                /* Bigger than 127 chars */
                var vector = CorrelationVector.Extend("KZY+dsX2jEaZesgCPjJ2Ng.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647");
            });
        }

        [TestMethod]
        public void ThrowWithTooBigExtensionCorrelationVectorValue()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                CorrelationVector.ValidateCorrelationVectorDuringCreation = true;
                /* Bigger than INT32 */
                var vector = CorrelationVector.Extend("tul4NUsfs9Cl7mOf.11111111111111111111111111111");
            });
        }

        [TestMethod]
        public void IncrementPastMaxWithNoErrors()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            var vector = CorrelationVector.Extend("tul4NUsfs9Cl7mOf.2147483647.2147483647.2147483647.21474836479");
            vector.Increment();
            Assert.IsTrue(vector.Value == "tul4NUsfs9Cl7mOf.2147483647.2147483647.2147483647.21474836479.1");

            for (int i = 0; i < 20; i++)
            {
                vector.Increment();
            }

            // We hit 63 chars so we silently stopped counting
            Assert.IsTrue(vector.Value == "tul4NUsfs9Cl7mOf.2147483647.2147483647.2147483647.21474836479.9!");
        }

        [TestMethod]
        public void IncrementPastMaxWithNoErrorsV2()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            var vector = CorrelationVector.Extend("KZY+dsX2jEaZesgCPjJ2Ng.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.214");
            vector.Increment();
            Assert.IsTrue(vector.Value == "KZY+dsX2jEaZesgCPjJ2Ng.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.214.1");

            for (int i = 0; i < 20; i++)
            {
                vector.Increment();
            }

            // We hit 127 chars so we silently stopped counting
            Assert.IsTrue(vector.Value == "KZY+dsX2jEaZesgCPjJ2Ng.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.214.9!");
        }

        [TestMethod]
        public void SpinSortValidation()
        {
            var vector = new CorrelationVector();
            var spinParameters = new SpinParameters
            {
                Entropy = SpinEntropy.Two,
                Interval = SpinCounterInterval.Fine,
                Periodicity = SpinCounterPeriodicity.Short
            };

            uint lastSpinValue = 0;
            var wrappedCounter = 0;
            for (int i = 0; i < 100; i++)
            {
                // The cV after a Spin will look like <cvBase>.0.<spinValue>.0, so the spinValue is at index = 2.
                var spinValue = uint.Parse(CorrelationVector.Spin(vector.Value, spinParameters).Value.Split('.')[2]);

                // Count the number of times the counter wraps.
                if (spinValue <= lastSpinValue)
                {
                    wrappedCounter++;
                }

                lastSpinValue = spinValue;

                // Wait for 10ms.
                Task.Delay(10).Wait();
            }

            // The counter should wrap at most 1 time.
            Assert.IsTrue(wrappedCounter <= 1);
        }

        [TestMethod]
        public void SpinPastMaxWithTerminationSign()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            var vector = CorrelationVector.Spin("tul4NUsfs9Cl7mOf.2147483647.2147483647.2147483647.21474836479");
            Assert.IsTrue(vector.Value.EndsWith(CorrelationVector.TerminationSign));
        }

        [TestMethod]
        public void SpinPastMaxWithTerminationSignV2()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            var vector = CorrelationVector.Spin("KZY+dsX2jEaZesgCPjJ2Ng.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.214");
            Assert.IsTrue(vector.Value.EndsWith(CorrelationVector.TerminationSign));
        }

        [TestMethod]
        public void ExtendPastMaxWithTerminationSign()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            var vector = CorrelationVector.Extend("tul4NUsfs9Cl7mOf.2147483647.2147483647.2147483647.21474836479.1");
            Assert.IsTrue(vector.Value.EndsWith(CorrelationVector.TerminationSign));
        }

        [TestMethod]
        public void ExtendPastMaxWithTerminationSignV2()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            var vector = CorrelationVector.Extend("KZY+dsX2jEaZesgCPjJ2Ng.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.214.1");
            Assert.IsTrue(vector.Value.EndsWith(CorrelationVector.TerminationSign));
        }

        [TestMethod]
        public void ImmutableWithTerminationSign()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            const string cv = "tul4NUsfs9Cl7mOf.2147483647.2147483647.2147483647.21474836479.0!";

            var vector = CorrelationVector.Extend(cv);
            //extend do nothing
            Assert.IsTrue(vector.Value == cv);

            vector = CorrelationVector.Spin(cv);
            //Spin do nothing
            Assert.IsTrue(vector.Value == cv);

            vector.Increment();
            // Increment do nothing since it has termination sign
            Assert.IsTrue(vector.Value == cv);
        }

        [TestMethod]
        public void ImmutableWithTerminationSignV2()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            const string cv = "KZY+dsX2jEaZesgCPjJ2Ng.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.214.0!";
            var vector = CorrelationVector.Extend(cv);
            //extend do nothing
            Assert.IsTrue(vector.Value == cv);


            vector = CorrelationVector.Spin(cv);
            //Spin do nothing
            Assert.IsTrue(vector.Value == cv);

            vector.Increment();
            // Increment do nothing since it has termination sign
            Assert.IsTrue(vector.Value == cv);
        }
    }
}
