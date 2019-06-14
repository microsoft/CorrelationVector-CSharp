// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CorrelationVector.UnitTests
{
    [TestClass]
    public class CorrelationVectorTests
    {
        [TestMethod]
        public void CreateV1CorrelationVectorTest()
        {
            var correlationVector = new CorrelationVectorV1();
            var splitVector = correlationVector.Value.Split('.');

            Assert.AreEqual(2, splitVector.Length, "Correlation Vector should be created with two components separated by a '.'");
            Assert.AreEqual(16, splitVector[0].Length, "Correlation Vector base should be 16 characters long");
            Assert.AreEqual("0", splitVector[1], "Correlation Vector extension should start with zero");
        }

        [TestMethod]
        public void CreateV2CorrelationVectorTest()
        {
            var correlationVector = new CorrelationVectorV2();
            var splitVector = correlationVector.Value.Split('.');

            Assert.AreEqual(2, splitVector.Length, "Correlation Vector should be created with two components separated by a '.'");
            Assert.AreEqual(22, splitVector[0].Length, "Correlation Vector base should be 22 characters long");
            Assert.AreEqual("0", splitVector[1], "Correlation Vector extension should start with zero");
        }

        [TestMethod]
        public void CreateV3CorrelationVectorTest()
        {
            var correlationVector = new CorrelationVectorV3();
            var splitVector = correlationVector.Value.Split('.');

            Assert.AreEqual(3, splitVector.Length, "Correlation Vector should be created with three components separated by a '.'");
            Assert.AreEqual("A", splitVector[0], "Correlation Vector v3 should start with \"A\".");
            Assert.AreEqual(22, splitVector[1].Length, "Correlation Vector base should be 22 characters long");
            Assert.AreEqual("0", splitVector[2], "Correlation Vector extension should start with zero");
        }

        [TestMethod]
        public void CreateCorrelationVectorFromGuidTest()
        {
            var guid = System.Guid.NewGuid();
            var correlationVector = new CorrelationVectorV1(guid);
            var splitVector = correlationVector.Value.Split('.');

            Assert.AreEqual(2, splitVector.Length, "Correlation Vector should be created with two components separated by a '.'");
            Assert.AreEqual(16, splitVector[0].Length, "Correlation Vector base should be 16 characters long");
            Assert.AreEqual("0", splitVector[1], "Correlation Vector extension should start with zero");
        }

        [TestMethod]
        public void CreateCorrelationVectorFromGuidTestV2()
        {
            var guid = System.Guid.NewGuid();
            var correlationVector = new CorrelationVectorV2(guid);
            var splitVector = correlationVector.Value.Split('.');

            Assert.AreEqual(2, splitVector.Length, "Correlation Vector should be created with two components separated by a '.'");
            Assert.AreEqual(22, splitVector[0].Length, "Correlation Vector base should be 22 characters long");
            Assert.AreEqual("0", splitVector[1], "Correlation Vector extension should start with zero");
        }

        [TestMethod]
        public void CreateCorrelationVectorFromGuidTestV3()
        {
            var guid = System.Guid.NewGuid();
            var correlationVector = new CorrelationVectorV3(guid);
            var splitVector = correlationVector.Value.Split('.');

            Assert.AreEqual(3, splitVector.Length, "Correlation Vector should be created with three components separated by a '.'");
            Assert.AreEqual("A", splitVector[0], "Correlation Vector v3 should start with \"A\".");
            Assert.AreEqual(22, splitVector[1].Length, "Correlation Vector base should be 22 characters long");
            Assert.AreEqual("0", splitVector[2], "Correlation Vector extension should start with zero");
        }

        [TestMethod]
        public void GetBaseAsGuidV1Test()
        {
            var correlationVector = new CorrelationVectorV1();

            Assert.ThrowsException<InvalidOperationException>(() => correlationVector.GetBaseAsGuid(),
                "V1 correlation vector base cannot be converted to a guid");
        }

        [TestMethod]
        public void GetBaseAsGuidV2Test()
        {
            var guid = System.Guid.NewGuid();
            var correlationVector = new CorrelationVectorV2(guid);
            Guid baseAsGuid = correlationVector.GetBaseAsGuid();

            Assert.AreEqual(guid, baseAsGuid, "Correlation Vector base as a guid should be the same as the initial guid");
        }

        [TestMethod]
        public void GetBaseAsGuidV3Test()
        {
            var guid = System.Guid.NewGuid();
            var correlationVector = new CorrelationVectorV3(guid);
            Guid baseAsGuid = correlationVector.GetBaseAsGuid();

            Assert.AreEqual(guid, baseAsGuid, "Correlation Vector base as a guid should be the same as the initial guid");
        }

        [TestMethod]
        public void GetBaseAsGuidForInvalidGuidVectorBaseTest()
        {
            // CV base which has four non-zero least significant bits meaning conversion to Guid will lose information.
            // CV Base -> Guid -> CV Base conversion results in:
            //   /////////////////////B -> ffffffff-ffff-ffff-ffff-fffffffffffc -> /////////////////////A
            string vectorBase = "/////////////////////B";
            Guid vectorBaseGuid = Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffffc");

            try
            {
                var correlationVector = CorrelationVector.Parse($"{vectorBase}.0");

                CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
                Guid baseAsGuid = correlationVector.GetBaseAsGuid();
                Assert.AreEqual(vectorBaseGuid, baseAsGuid, "Correlation Vector base as a guid should be the same as the expected guid");

                CorrelationVector.ValidateCorrelationVectorDuringCreation = true;
                Assert.ThrowsException<InvalidOperationException>(() => correlationVector.GetBaseAsGuid());
            }
            finally
            {
                CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            }
        }

        [TestMethod]
        public void ConvertFromVectorBaseToGuidBackToVectorBase()
        {
            // CV bases which have four zero least significant bits meaning a conversion to a Guid will retain all
            // information.
            // CV Base -> Guid -> CV Base conversions result in:
            //   /////////////////////A -> ffffffff-ffff-ffff-ffff-fffffffffffc -> /////////////////////A
            //   /////////////////////Q -> ffffffff-ffff-ffff-ffff-fffffffffffd -> /////////////////////Q
            //   /////////////////////g -> ffffffff-ffff-ffff-ffff-fffffffffffe -> /////////////////////g
            //   /////////////////////w -> ffffffff-ffff-ffff-ffff-ffffffffffff -> /////////////////////w
            string[] validGuidVectorBases = new string[]
            {
                "/////////////////////A",
                "/////////////////////Q",
                "/////////////////////g",
                "/////////////////////w",
            };

            foreach (string vectorBase in validGuidVectorBases)
            {
                var correlationVector = CorrelationVector.Parse($"{vectorBase}.0");
                Guid baseAsGuid = correlationVector.GetBaseAsGuid();
                var correlationVectorFromGuid = new CorrelationVectorV2(baseAsGuid);

                Assert.AreEqual(correlationVector.Value, correlationVectorFromGuid.Value,
                    $"Correlation vector base -> guid -> correlation vector base should result in the same vector base for {vectorBase}");
            }
        }

        [TestMethod]
        public void ParseCorrelationVectorV1Test()
        {
            var correlationVector = CorrelationVector.Parse("ifCuqpnwiUimg7Pk.1");
            var splitVector = correlationVector.Value.Split('.');

            Assert.AreEqual(correlationVector.Version, CorrelationVectorVersion.V1, "Correlation Vector version should be V1");
            Assert.AreEqual("ifCuqpnwiUimg7Pk", splitVector[0], "Correlation Vector base was not parsed properly");
            Assert.AreEqual("1", splitVector[1], "Correlation Vector extension was not parsed properly");
        }

        [TestMethod]
        public void ParseCorrelationVectorV2Test()
        {
            var correlationVector = CorrelationVector.Parse("Y58xO9ov0kmpPvkiuzMUVA.3.4.5");
            var splitVector = correlationVector.Value.Split('.');

            Assert.AreEqual(correlationVector.Version, CorrelationVectorVersion.V2, "Correlation Vector version should be V2");
            Assert.AreEqual(4, splitVector.Length, "Correlation Vector was not parsed properly");
            Assert.AreEqual("Y58xO9ov0kmpPvkiuzMUVA", splitVector[0], "Correlation Vector base was not parsed properly");
            Assert.AreEqual("3", splitVector[1], "Correlation Vector extension was not parsed properly");
            Assert.AreEqual("4", splitVector[2], "Correlation Vector extension was not parsed properly");
            Assert.AreEqual("5", splitVector[3], "Correlation Vector extension was not parsed properly");
        }

        [TestMethod]
        public void ParseCorrelationVectorV3Test()
        {
            var correlationVector = CorrelationVector.Parse("A.Y58xO9ov0kmpPvkiuzMUVA.3.4.A");
            var splitVector = correlationVector.Value.Split('.');

            Assert.AreEqual(CorrelationVectorVersion.V3, correlationVector.Version, "Correlation Vector version should be V3");
            Assert.AreEqual(5, splitVector.Length, "Correlation Vector was not parsed properly");
            Assert.AreEqual("Y58xO9ov0kmpPvkiuzMUVA", correlationVector.Base, "Correlation Vector base was not parsed properly");
            Assert.AreEqual("3", splitVector[2], "Correlation Vector extension was not parsed properly");
            Assert.AreEqual("4", splitVector[3], "Correlation Vector extension was not parsed properly");
            Assert.AreEqual(0xA, correlationVector.Extension, "Correlation Vector extension was not parsed properly");
        }

        [TestMethod]
        public void SimpleIncrementCorrelationVectorTest()
        {
            var correlationVector = new CorrelationVectorV1();
            correlationVector.Increment();
            var splitVector = correlationVector.Value.Split('.');
            Assert.AreEqual("1", splitVector[1], "Correlation Vector extension should have been incremented by one");
        }

        [TestMethod]
        public void SimpleExtendCorrelationVectorTest()
        {
            CorrelationVector correlationVector = new CorrelationVectorV1();
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
            var correlationVector = new CorrelationVectorV1();
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
        public void ResetWithTooBigCorrelationVectorValueV3()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = true;
            /* Bigger than 127 chars */
            var vector = CorrelationVector.Extend("A.KZY+dsX2jEaZesgCPjJ2Ng.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF");
            Assert.IsTrue(vector.Value.Contains("#"), "Reset vector must contain reset indicator");
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
            Assert.AreEqual("tul4NUsfs9Cl7mOf.2147483647.2147483647.2147483647.21474836479.1", vector.Value);

            for (int i = 0; i < 20; i++)
            {
                vector.Increment();
            }

            // We hit 63 chars so we silently stopped counting
            Assert.AreEqual("tul4NUsfs9Cl7mOf.2147483647.2147483647.2147483647.21474836479.9!", vector.Value);
        }

        [TestMethod]
        public void IncrementPastMaxWithNoErrorsV2()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            var vector = CorrelationVector.Extend("KZY+dsX2jEaZesgCPjJ2Ng.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.214");
            vector.Increment();
            Assert.AreEqual("KZY+dsX2jEaZesgCPjJ2Ng.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.214.1", vector.Value);

            for (int i = 0; i < 20; i++)
            {
                vector.Increment();
            }

            // We hit 127 chars so we silently stopped counting
            Assert.AreEqual("KZY+dsX2jEaZesgCPjJ2Ng.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.214.9!", vector.Value);
        }

        [TestMethod]
        public void SpinSortValidation()
        {
            var vector = new CorrelationVectorV2();
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
                CorrelationVector newVector = CorrelationVector.Spin(vector.Value, spinParameters);
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
        public void SpinSortValidationV3()
        {
            var vector = new CorrelationVectorV3();
            var spinParameters = new SpinParameters
            {
                Entropy = SpinEntropy.Four,
                Interval = SpinCounterInterval.Fine,
                Periodicity = SpinCounterPeriodicity.Long
            };

            ulong lastSpinValue = 0;
            var wrappedCounter = 0;
            for (int i = 0; i < 100; i++)
            {
                // The cV after a Spin will look like <cvBase>.0_<spinValue>.0, so the spinValue is at index = 2.
                CorrelationVector newVector = CorrelationVectorV3.Spin(vector.Value, spinParameters);
                string hexValue = newVector.Value.Split('.', '_')[3];
                var spinValue = ulong.Parse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

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
        public void TestResetV3()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            const string baseVector = "A.KZY+dsX2jEaZesgCPjJ2Ng.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.FFF";

            // we hit 127 chars limit, will reset vector
            Tuple<string, string> resetValues = CorrelationVector.Parse(baseVector).Reset();
            Assert.IsTrue(resetValues.Item1.Contains("#"), "Reset vector must contain reset indicator");
            Assert.AreEqual(baseVector, resetValues.Item2, "The stored vector is different from the base vector.");
        }

        [TestMethod]
        public void SpinPastMaxWithTerminationSignV2()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            const string baseVector = "KZY+dsX2jEaZesgCPjJ2Ng.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.214";

            // we hit 127 chars limit, will append "!" to vector
            var vector = CorrelationVector.Spin(baseVector);
            Assert.AreEqual(string.Concat(baseVector, CorrelationVectorV2.TerminationSign), vector.Value);
        }

        [TestMethod]
        public void SpinPastMaxWithResetV3()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            const string baseVector = "A.KZY+dsX2jEaZesgCPjJ2Ng.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF";

            // we hit 127 chars limit, will reset vector and show value
            var vector = CorrelationVector.Spin(baseVector);
            Assert.IsTrue(vector.Value.Contains("#"), "Reset vector must contain reset indicator");
        }

        [TestMethod]
        public void IncrementPastMaxWithResetV3()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            const string baseVector = "A.PmvzQKgYek6Sdk/T5sWaqw.1.FA.A1.23_B6A5E62FC38E9974.1_B6A6A13E588CF82F.2A.AB.213_B6A92D24A00C0F9B.47.8B.12.34.A123.2B.23.41.FF";

            var vectorToIncrement = CorrelationVector.Parse(baseVector);
            // we hit 127 chars limit, will reset vector and show value
            var incrementedString = vectorToIncrement.Increment();
            var newVector = CorrelationVector.Parse(incrementedString);
            Assert.IsTrue(incrementedString.Contains("#"), "Reset vector must contain reset indicator");
            Assert.AreEqual(0x100, newVector.Extension, "Vector with extension FF should increment to 100");
        }

        [TestMethod]
        public void ExtendPastMaxWithTerminationSign()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            const string baseVector = "tul4NUsfs9Cl7mOf.2147483647.2147483647.2147483647.214748364.23";

            // we hit 63 chars limit, will append "!" to vector
            var vector = CorrelationVector.Extend(baseVector);
            Assert.AreEqual(string.Concat(baseVector, CorrelationVectorV1.TerminationSign), vector.Value);
        }

        [TestMethod]
        public void ExtendPastMaxWithTerminationSignV2()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            const string baseVector = "KZY+dsX2jEaZesgCPjJ2Ng.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2141";

            // we hit 127 chars limit, will append "!" to vector
            var vector = CorrelationVector.Extend(baseVector);
            Assert.AreEqual(string.Concat(baseVector, CorrelationVectorV2.TerminationSign), vector.Value);
        }

        [TestMethod]
        public void ExtendPastMaxWithResetV3()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            const string baseVector = "A.KZY+dsX2jEaZesgCPjJ2Ng.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.7FFFFFFF.FFF";

            // we hit 127 chars limit, will append "!" to vector
            var vector = CorrelationVector.Extend(baseVector);
            Assert.IsTrue(vector.Value.Contains("#"), "Reset vector must contain reset indicator");
        }

        [TestMethod]
        public void ConvertTraceparentV3()
        {
            const string traceparent = "00-0af7651916cd43dd8448eb211c80319c-b9c7c989f97918e1-01";
            string[] traceSections = traceparent.Split('-');
            var vector = CorrelationVectorV3.Span(traceparent);

            var traceIDBytes = new byte[traceSections[1].Length / 2];
            for (var i = 0; i < traceIDBytes.Length; i++)
            {
                traceIDBytes[i] = Convert.ToByte(traceSections[1].Substring(i * 2, 2), 16);
            }
            string paddedBase = vector.Base.PadRight(24, '=');
            byte[] cvBaseBytes = Convert.FromBase64String(paddedBase);
            Assert.IsTrue(vector.Value.Contains("-"), "Span vector must contain span indicator");
            Assert.AreEqual(22, vector.Base.Length, "Correlation Vector base should be 22 characters long");
            CollectionAssert.AreEqual(traceIDBytes, cvBaseBytes, "Trace ID bytes and cV base bytes must be equal");
        }

        [TestMethod]
        public void ImmutableWithTerminationSign()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            const string cv = "tul4NUsfs9Cl7mOf.2147483647.2147483647.2147483647.21474836479.0!";

            var vector = CorrelationVector.Extend(cv);
            //extend do nothing
            Assert.AreEqual(cv, vector.Value);

            Assert.ThrowsException<InvalidOperationException>(() => CorrelationVector.Spin(cv));

            vector.Increment();
            // Increment does nothing since it has termination sign
            Assert.AreEqual(cv, vector.Value);
        }

        [TestMethod]
        public void ImmutableWithTerminationSignV2()
        {
            CorrelationVector.ValidateCorrelationVectorDuringCreation = false;
            const string cv = "KZY+dsX2jEaZesgCPjJ2Ng.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.214.0!";
            var vector = CorrelationVector.Extend(cv);
            //extend do nothing
            Assert.AreEqual(cv, vector.Value);

            vector = CorrelationVector.Spin(cv);
            //Spin do nothing
            Assert.AreEqual(cv, vector.Value);

            vector.Increment();
            // Increment do nothing since it has termination sign
            Assert.AreEqual(cv, vector.Value);
        }
    }
}
