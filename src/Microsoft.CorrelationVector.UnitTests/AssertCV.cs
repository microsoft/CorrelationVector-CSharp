// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CorrelationVector.UnitTests
{
    [ExcludeFromCodeCoverage]
    public static class AssertCV
    {
        private class CV : IEquatable<CV>
        {
            public CV(string value)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(value), "Correlation vector cannot be empty.");
                string[] parts = value.Split('.');
                Assert.AreNotEqual(1, parts.Length, "Correlation vector has no vector part. Value: {0}", value);
                byte[] correlation = null;
                try
                {
                    correlation = Convert.FromBase64String(parts[0]);
                }
                catch
                {
                    Assert.Fail("Correlation vector does not have a Base64 string correlation part. Value: {0}", value);
                }
                Assert.AreEqual(12, correlation.Length, "Correlation vector has a correlation part of incorrect binary length. Value: {0}", value);
                this.CorrelationHigh = BitConverter.ToInt64(correlation, 0);
                this.CorrelationLow = BitConverter.ToInt32(correlation, 8);
                if (parts.Length == 1)
                {
                    return;
                }
                this.Vector = new uint[parts.Length - 1];
                for (int i = 0; i < this.Vector.Length; i++)
                {
                    uint parsed = 0;
                    if (uint.TryParse(parts[i + 1], out parsed))
                    {
                        this.Vector[i] = parsed;
                        continue;
                    }
                    Assert.Fail("Correlation vector has a vector part that is not an unsigned integer. Value: {0}, Vector index: {1}", value, i);
                }
                this.Value = value;
            }

            public long CorrelationHigh { get; private set; }

            public int CorrelationLow { get; private set; }

            public uint[] Vector { get; private set; }

            public string Value { get; private set; }

            public static bool operator ==(CV left, CV right)
            {
                return CV.AreEqual(left, right);
            }

            public static bool operator !=(CV left, CV right)
            {
                return !CV.AreEqual(left, right);
            }

            public bool Equals(CV other)
            {
                return CV.AreEqual(this, other);
            }

            public override bool Equals(object obj)
            {
                return CV.AreEqual(this, obj as CV);
            }

            public override int GetHashCode()
            {
                return StringComparer.Ordinal.GetHashCode(this.Value);
            }

            public override string ToString()
            {
                return this.Value;
            }

            private static bool AreEqual(CV left, CV right)
            {
                CV self = left;
                CV other = right;
                if (object.ReferenceEquals(self, null))
                {
                    self = right;
                    other = left;
                }
                if (object.ReferenceEquals(self, null))
                {
                    return true;
                }
                if (object.ReferenceEquals(other, null))
                {
                    return false;
                }
                return string.Equals(self.Value, other.Value, StringComparison.Ordinal);
            }
        }

        public static void AreEqual(string expected, string actual)
        {
            Assert.AreEqual(new CV(expected), new CV(actual), "Correlation vectors do not match.");
        }

        public static void AtVirtualTime(int expectedTick, int index, string actual)
        {
            CV cv = new CV(actual);
            Assert.IsTrue(index >= 0, "Correlation vector has no clock at this index. Actual: {0}, Index: {1}", actual, index);
            Assert.IsTrue(index < cv.Vector.Length, "Correlation vector has no clock at this index. Actual: {0}, Index: {1}", actual, index);
            Assert.AreEqual((uint)expectedTick, cv.Vector[index], "Correlation vector clock does not match. Actual: {0}, Index: {1}", actual, index);
        }

        public static void CausedBy(string expectedCause, string actualEffect)
        {
            CV cause = new CV(expectedCause);
            CV effect = new CV(actualEffect);
            Assert.AreNotEqual(cause, effect, "Events happened simultaneously.");
            Assert.AreEqual(cause.CorrelationHigh, effect.CorrelationHigh, "Events did not happen on the same transaction. Expected Cause: {0}, Actual Effect: {1}", expectedCause, actualEffect);
            Assert.AreEqual(cause.CorrelationLow, effect.CorrelationLow, "Events did not happen on the same transaction. Expected Cause: {0}, Actual Effect: {1}", expectedCause, actualEffect);
            int last = cause.Vector.Length - 1;
            for (int i = 0; i < cause.Vector.Length; i++)
            {
                Assert.IsTrue(i < effect.Vector.Length, "Actual event caused by expected event. Expected Cause: {0}, Actual Effect: {1}, Index: {2}", expectedCause, actualEffect, i);
                Assert.IsFalse(cause.Vector[i] < effect.Vector[i], "Actual event happened later than expected event in an unrelated branch. Expected Cause: {0}, Actual Effect: {1}, Index: {2}", expectedCause, actualEffect, i);
                if (i == last)
                {
                    return;
                }
                Assert.IsFalse(cause.Vector[i] > effect.Vector[i], "Actual event happened earlier than expected event in an unrelated branch. Expected Cause: {0}, Actual Effect: {1}, Index: {2}", expectedCause, actualEffect, i);
            }
        }

        public static void HappensAfter(string expectedBefore, string actualAfter)
        {
            CV before = new CV(expectedBefore);
            CV after = new CV(actualAfter);
            Assert.AreNotEqual(before, after, "Events happened simultaneously.");
            Assert.AreEqual(before.CorrelationHigh, after.CorrelationHigh, "Events did not happen on the same transaction. Expected Before: {0}, Actual After: {1}", expectedBefore, actualAfter);
            Assert.AreEqual(before.CorrelationLow, after.CorrelationLow, "Events did not happen on the same transaction. Expected Before: {0}, Actual After: {1}", expectedBefore, actualAfter);
            for (int i = 0; i < before.Vector.Length; i++)
            {
                Assert.IsTrue(i < after.Vector.Length, "Actual event happened earlier than expected event. Expected Before: {0}, Actual After: {1}, Index: {2}", expectedBefore, actualAfter, i);
                if (before.Vector[i] < after.Vector[i])
                {
                    return;
                }
                Assert.IsFalse(before.Vector[i] > after.Vector[i], "Actual event happened earlier than expected event. Expected Before: {0}, Actual After: {1}, Index: {2}", expectedBefore, actualAfter, i);
            }
        }

        public static void IsRoot(string actual)
        {
            CV cv = new CV(actual);
            Assert.AreEqual(1, cv.Vector.Length, "Correlation vector is not the source of this transaction. Actual: {0}", actual);
        }

        public static void IsValid(string actual)
        {
            CV cv = new CV(actual);
            Assert.IsNotNull(cv);
        }
    }
}
