using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Microsoft.CorrelationVector
{
    public sealed class CorrelationVectorV3 : CorrelationVector
    {
        internal new const byte MaxVectorLength = 127;
        internal const byte BaseLength = 22;

        /// <summary>
        /// Version character for Correlation Vector v3.0
        /// </summary>
        public const char VersionChar = 'A';

        /// <summary>
        /// Standard delimiter used for suffix, version separation
        /// </summary>
        public const char StandardDelim = '.';

        /// <summary>
        /// Delimiter used for a cV that has had a reset operation
        /// </summary>
        public const char ResetDelim = '#';

        /// <summary>
        /// Delimiter used for a cV that is interoperable with a W3C traceparent
        /// </summary>
        public const char SpanDelim = '-';

        /// <summary>
        /// Delimiter used for a cV that uses a Spin operation
        /// </summary>
        public const char SpinDelim = '_';

        private static Random rng = new Random();

        /// <summary>
        /// Returns the full string representation of the Correlation Vector.
        /// </summary>
        public override string Value
        {
            get
            {
                // Convert extension to hex before returning
                string hexExtension = extension.ToString("X");
                return string.Concat(this.BaseVector, ".", hexExtension);
            }
        }

        /// <summary>
        /// The full correlation vector, excluding the suffix at the end.
        /// This includes the "A." at the beginning.
        /// </summary>
        internal readonly string BaseVector;

        /// <summary>
        /// The suffix at the end of the correlation vector, as an integer.
        /// </summary>
        private int extension = 0;

        /// <summary>
        /// Returns the cV base of the correlation vector.
        /// Example: cV with Value A.e8iECJiOvUGPvOVtchxG9g.F.A.23 returns e8iECJiOvUGPvOVtchxG9g
        /// </summary>
        public override string Base
        {
            get
            {
                // Search from first letter after "A." and stop at the first instance of a delimiter
                return this.Value.Substring(2, this.Value.IndexOfAny(new char[] { StandardDelim, ResetDelim, SpanDelim, SpinDelim }, 2) - 2);
            }
        }

        public override int Extension
        {
            get
            {
                return this.extension;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationVectorV3"/> class. 
        /// This should only be called if there is no correlation vector in the message header.
        /// </summary>
        public CorrelationVectorV3()
            : this(GetUniqueValue(), 0, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationVectorV3"/> class. 
        /// This should only be called if there is no correlation vector in the message header.
        /// </summary>
        public CorrelationVectorV3(Guid vectorBase)
            : this(vectorBase.GetBaseFromGuid(BaseLength), 0, false)
        {
        }

        private static string GetUniqueValue()
        {
            return Guid.NewGuid().GetBaseFromGuid(BaseLength);
        }

        private CorrelationVectorV3(string baseVector, int extension, bool immutable, bool appendVersion = true)
        {
            // first append the "A." unless it is not required to
            string baseVectorWithVersion;
            if (appendVersion)
            {
                baseVectorWithVersion = String.Concat(VersionChar, StandardDelim, baseVector);
            }
            else
            {
                baseVectorWithVersion = baseVector;
            }
            this.BaseVector = baseVectorWithVersion;
            this.Version = CorrelationVectorVersion.V3;
            this.extension = extension;
            // this.immutable = immutable || CorrelationVector.IsOversized(baseVector, extension, version);
        }

        /// <summary>
        /// Creates a new correlation vector by extending an existing value. This should be
        /// done at the entry point of an operation.
        /// </summary>
        /// <param name="correlationVector">
        /// Taken from the message header indicated by <see cref="HeaderName"/>.
        /// </param>
        /// <returns>A new correlation vector extended from the current vector.</returns>
        public new static CorrelationVectorV3 Extend(string correlationVector)
        {
            /*
            if (CorrelationVectorV3.IsImmutable(correlationVector))
            {
                return CorrelationVectorV3.Parse(correlationVector);
            }

            if (CorrelationVectorV3.ValidateCorrelationVectorDuringCreation)
            {
                CorrelationVectorV3.Validate(correlationVector);
            }

            if (CorrelationVectorV3.IsOversized(correlationVector, 0))
            {
            }
            */
            return new CorrelationVectorV3(correlationVector, 0, false, false);
        }

        /// <summary>
        /// Creates a new correlation vector by parsing its string representation.
        /// </summary>
        /// <param name="correlationVector">correlationVector. 
        /// Important: Make sure to include the "A." at the beginning!</param>
        /// <returns>CorrelationVector</returns>
        public new static CorrelationVectorV3 Parse(string correlationVector)
        {
            if (!string.IsNullOrEmpty(correlationVector))
            {
                int p = correlationVector.LastIndexOf('.');
                // bool oversized = CorrelationVectorV3.IsOversized(correlationVector, 0);
                if (p > 0)
                {
                    string extensionValue = correlationVector.Substring(p + 1);
                    int extension;
                    if (int.TryParse(extensionValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out extension) && extension >= 0)
                    {
                        return new CorrelationVectorV3(correlationVector.Substring(0, p), extension, false, false);
                    }
                }
            }

            return new CorrelationVectorV3();
        }

        /// <summary>
        /// Creates a new correlation vector with a W3C traceparent.
        /// </summary>
        /// <param name="traceparent"></param>
        /// <returns>CorrelationVector</returns>
        public static CorrelationVectorV3 Span(string traceparent)
        {
            return new CorrelationVectorV3(traceparent, 0, false, true);
        }

        /// <summary>
        /// Creates a new correlation vector by applying the Spin operator to an existing value.
        /// This should be done at the entry point of an operation.
        /// </summary>
        /// <param name="correlationVector">
        /// Taken from the message header indicated by <see cref="HeaderName"/>.
        /// </param>
        /// <returns>A new correlation vector extended from the current vector.</returns>
        public new static CorrelationVectorV3 Spin(string correlationVector)
        {
            SpinParameters defaultParameters = new SpinParameters
            {
                Interval = SpinCounterInterval.Coarse,
                Periodicity = SpinCounterPeriodicity.Short,
                Entropy = SpinEntropy.Two
            };

            return CorrelationVectorV3.Spin(correlationVector, defaultParameters);
        }

        /// <summary>
        /// Creates a new correlation vector by applying the Spin operator to an existing value.
        /// This should be done at the entry point of an operation.
        /// </summary>
        /// <param name="correlationVector">
        /// Taken from the message header indicated by <see cref="HeaderName"/>.
        /// </param>
        /// <param name="parameters">
        /// The parameters to use when applying the Spin operator.
        /// </param>
        /// <returns>A new correlation vector extended from the current vector.</returns>
        public new static CorrelationVectorV3 Spin(string correlationVector, SpinParameters parameters)
        {
            if (CorrelationVectorV3.ValidateCorrelationVectorDuringCreation)
            {
                // CorrelationVectorV3.Validate(correlationVector);
            }

            byte[] entropy = new byte[parameters.EntropyBytes];
            rng.NextBytes(entropy);

            ulong value = (ulong)(DateTime.UtcNow.Ticks >> parameters.TicksBitsToDrop);
            for (int i = 0; i < parameters.EntropyBytes; i++)
            {
                value = (value << 32) | Convert.ToUInt64(entropy[i]);
            }

            // Generate a bitmask and mask the lower TotalBits in the value.
            // The mask is generated by (1 << TotalBits) - 1. We need to handle the edge case
            // when shifting 64 bits, as it wraps around.
            value &= (parameters.TotalBits == 64 ? 0 : (ulong)1 << parameters.TotalBits) - 1;

            string s = unchecked((uint)value).ToString("X8");
            if (parameters.TotalBits > 32)
            {
                s = string.Concat((value >> 32).ToString("X8"), s);
            }

            string baseVector = string.Concat(correlationVector, SpinDelim, s);
            if (CorrelationVectorV3.IsOversized(baseVector, 0))
            {
                // Reset operation here
                return CorrelationVectorV3.Parse(correlationVector);
            }

            return new CorrelationVectorV3(baseVector, 0, false, false);
        }

        public override Tuple<string, string> Reset()
        {
            // First identify what the oversized value is for the cV.
            string oversizedValue = "adfkljdaslkfjadlksjfladsfjasdlfjasdljfdal";
            // Then get a sort/entropy value
            string resetValue = GetTickValue().ToString("X16");

            string newVector = String.Concat(VersionChar, StandardDelim, this.Base, ResetDelim, resetValue);

            return new Tuple<string, string>(newVector, oversizedValue);
        }

        private static ulong GetTickValue()
        {
            SpinParameters parameters = new SpinParameters
            {
                Interval = SpinCounterInterval.Coarse,
                Periodicity = SpinCounterPeriodicity.Short,
                Entropy = SpinEntropy.Four
            };
            byte[] entropy = new byte[parameters.EntropyBytes];
            rng.NextBytes(entropy);

            ulong value = (ulong)(DateTime.UtcNow.Ticks >> parameters.TicksBitsToDrop);
            for (int i = 0; i < parameters.EntropyBytes; i++)
            {
                value = (value << 32) | Convert.ToUInt64(entropy[i]);
            }

            // Generate a bitmask and mask the lower TotalBits in the value.
            // The mask is generated by (1 << TotalBits) - 1. We need to handle the edge case
            // when shifting 64 bits, as it wraps around.
            value &= (parameters.TotalBits == 64 ? 0 : (ulong)1 << parameters.TotalBits) - 1;

            return value;
        }

        public override string Increment()
        {
            /*
            if (this.immutable)
            {
                return this.Value;
            }
            */
            int snapshot = 0;
            int next = 0;
            do
            {
                snapshot = this.extension;
                if (snapshot == int.MaxValue) // 7FFFFFFF
                {
                    return this.Value;
                }
                next = snapshot + 1;
                if (CorrelationVectorV3.IsOversized(this.BaseVector, next))
                {
                    // Reset this stuff
                    return this.Value;
                }
            }
            while (snapshot != Interlocked.CompareExchange(ref this.extension, next, snapshot));
            return string.Concat(this.BaseVector, ".", next);
        }

        private static bool IsOversized(string baseVector, int extension)
        {
            if (!string.IsNullOrEmpty(baseVector))
            {
                int size = baseVector.Length + 1 +
                    (extension > 0 ? (int)Math.Log(extension, 16) : 0) + 1;
                return size > MaxVectorLength;
            }
            return false;
        }
    }
}
