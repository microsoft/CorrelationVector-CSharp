// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Globalization;

namespace Microsoft.CorrelationVector
{
    /// <summary>
    /// This class represents a lightweight vector for identifying and measuring
    /// causality.
    /// </summary>
    public sealed partial class CorrelationVector : MarshalByRefObject
    {
        private const byte MaxVectorLength = 63;
        private const byte MaxVectorLengthV2 = 127;
        private const byte BaseLength = 16;
        private const byte BaseLengthV2 = 22;

        private readonly string baseVector = null;

        private int extension = 0;

        private bool immutable = false;

        private static Random rng = new Random();

        /// <summary>
        /// This is the header that should be used between services to pass the correlation
        /// vector.
        /// </summary>
        public const string HeaderName = "MS-CV";

        /// <summary>
        /// This is termination sign should be used when vector lenght exceeds 
        /// max allowed length
        /// </summary>
        public const string TerminationSign = "!";

        /// <summary>
        /// Gets or sets a value indicating whether or not to validate the correlation
        /// vector on creation.
        /// </summary>
        public static bool ValidateCorrelationVectorDuringCreation { get; set; }

        /// <summary>
        /// Creates a new correlation vector by extending an existing value. This should be
        /// done at the entry point of an operation.
        /// </summary>
        /// <param name="correlationVector">
        /// Taken from the message header indicated by <see cref="HeaderName"/>.
        /// </param>
        /// <returns>A new correlation vector extended from the current vector.</returns>
        public static CorrelationVector Extend(string correlationVector)
        {
            if (CorrelationVector.IsImmutable(correlationVector))
            {
                return CorrelationVector.Parse(correlationVector);
            }

            CorrelationVectorVersion version = CorrelationVector.InferVersion(
                correlationVector, CorrelationVector.ValidateCorrelationVectorDuringCreation);

            if (CorrelationVector.ValidateCorrelationVectorDuringCreation)
            {
                CorrelationVector.Validate(correlationVector, version);
            }

            return new CorrelationVector(correlationVector, 0, version, false);
        }

        /// <summary>
        /// Creates a new correlation vector by applying the Spin operator to an existing value.
        /// This should be done at the entry point of an operation.
        /// </summary>
        /// <param name="correlationVector">
        /// Taken from the message header indicated by <see cref="HeaderName"/>.
        /// </param>
        /// <returns>A new correlation vector extended from the current vector.</returns>
        public static CorrelationVector Spin(string correlationVector)
        {
            SpinParameters defaultParameters = new SpinParameters
            {
                Interval = SpinCounterInterval.Coarse,
                Periodicity = SpinCounterPeriodicity.Short,
                Entropy = SpinEntropy.Two
            };

            return CorrelationVector.Spin(correlationVector, defaultParameters);
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
        public static CorrelationVector Spin(string correlationVector, SpinParameters parameters)
        {
            if (CorrelationVector.IsImmutable(correlationVector))
            {
                return CorrelationVector.Parse(correlationVector);
            }

            CorrelationVectorVersion version = CorrelationVector.InferVersion(
                correlationVector, CorrelationVector.ValidateCorrelationVectorDuringCreation);

            if (CorrelationVector.ValidateCorrelationVectorDuringCreation)
            {
                CorrelationVector.Validate(correlationVector, version);
            }

            byte[] entropy = new byte[parameters.EntropyBytes];
            rng.NextBytes(entropy);

            ulong value = (ulong)(DateTime.UtcNow.Ticks >> parameters.TicksBitsToDrop);
            for (int i = 0; i < parameters.EntropyBytes; i++)
            {
                value = (value << 8) | Convert.ToUInt64(entropy[i]);
            }

            // Generate a bitmask and mask the lower TotalBits in the value.
            // The mask is generated by (1 << TotalBits) - 1. We need to handle the edge case
            // when shifting 64 bits, as it wraps around.
            value &= (parameters.TotalBits == 64 ? 0 : (ulong)1 << parameters.TotalBits) - 1;

            string s = unchecked((uint)value).ToString();
            if (parameters.TotalBits > 32)
            {
                s = string.Concat((value >> 32).ToString(), ".", s);
            }

            return new CorrelationVector(string.Concat(correlationVector, ".", s), 0, version, false);
        }

        /// <summary>
        /// Creates a new correlation vector by parsing its string representation
        /// </summary>
        /// <param name="correlationVector">correlationVector</param>
        /// <returns>CorrelationVector</returns>
        public static CorrelationVector Parse(string correlationVector)
        {
            if (!string.IsNullOrEmpty(correlationVector))
            {
                int p = correlationVector.LastIndexOf('.');
                bool immutable = CorrelationVector.IsImmutable(correlationVector);
                if (p > 0)
                {
                    string extensionValue = immutable ? correlationVector.Substring(p + 1, correlationVector.Length - p - 1 - CorrelationVector.TerminationSign.Length)
                        : correlationVector.Substring(p + 1);
                    int extension;
                    if (int.TryParse(extensionValue, out extension) && extension >= 0)
                    {
                        return new CorrelationVector(correlationVector.Substring(0, p), extension, CorrelationVector.InferVersion(correlationVector, false), immutable);
                    }
                }
            }

            return new CorrelationVector();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationVector"/> class. This
        /// should only be called when no correlation vector was found in the message
        /// header.
        /// </summary>
        public CorrelationVector()
            : this(CorrelationVectorVersion.V1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationVector"/> class of the
        /// given implemenation version. This should only be called when no correlation
        /// vector was found in the message header.
        /// </summary>
        /// <param name="version">The correlation vector implemenation version.</param>
        public CorrelationVector(CorrelationVectorVersion version)
            : this(CorrelationVector.GetUniqueValue(version), 0, version, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationVector"/> class of the
        /// V2 implemenation using the given <see cref="System.Guid"/> as the vector base.
        /// </summary>
        /// <param name="vectorBase">The <see cref="System.Guid"/> to use as a correlation
        /// vector base.</param>
        public CorrelationVector(Guid vectorBase)
            : this(CorrelationVector.GetBaseFromGuid(vectorBase), 0, CorrelationVectorVersion.V2, false)
        {
        }

        /// <summary>
        /// Gets the value of the correlation vector as a string.
        /// </summary>
        public string Value
        {
            get
            {
                return string.Concat(this.baseVector, ".", this.extension,
                    this.immutable ? CorrelationVector.TerminationSign : string.Empty);
            }
        }

        /// <summary>
        /// Increments the current extension by one. Do this before passing the value to an
        /// outbound message header.
        /// </summary>
        /// <returns>
        /// The new value as a string that you can add to the outbound message header
        /// indicated by <see cref="HeaderName"/>.
        /// </returns>
        public string Increment()
        {
            if (this.immutable)
            {
                return this.Value;
            }
            int snapshot = 0;
            int next = 0;
            do
            {
                snapshot = this.extension;
                if (snapshot == int.MaxValue)
                {
                    return this.Value;
                }
                next = snapshot + 1;
                if (CorrelationVector.IsOversized(this.baseVector, next, this.Version))
                {
                    this.immutable = true;
                    return this.Value;
                }
            }
            while (snapshot != Interlocked.CompareExchange(ref this.extension, next, snapshot));
            return string.Concat(this.baseVector, ".", next);
        }

        /// <summary>
        /// Gets the version of the correlation vector implementation.
        /// </summary>
        public CorrelationVectorVersion Version
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return this.Value;
        }

        /// <summary>
        /// Determines whether two instances of the <see cref="CorrelationVector"/> class
        /// are equal. 
        /// </summary>
        /// <param name="vector">
        /// The correlation vector you want to compare with the current correlation vector.
        /// </param>
        /// <returns>
        /// True if the specified correlation vector is equal to the current correlation
        /// vector; otherwise, false.
        /// </returns>
        public bool Equals(CorrelationVector vector)
        {
            return string.Equals(this.Value, vector.Value, StringComparison.Ordinal);
        }

        private CorrelationVector(string baseVector, int extension, CorrelationVectorVersion version, bool immutable)
        {
            this.baseVector = baseVector;
            this.extension = extension;
            this.Version = version;
            this.immutable = immutable || CorrelationVector.IsOversized(baseVector, extension, version);
        }

        private static string GetBaseFromGuid(Guid guid)
        {
            byte[] bytes = guid.ToByteArray();

            // Removes the base64 padding
            return Convert.ToBase64String(bytes).Substring(0, CorrelationVector.BaseLengthV2);
        }

        private static string GetUniqueValue(CorrelationVectorVersion version)
        {
            if (CorrelationVectorVersion.V1 == version)
            {
                byte[] bytes = Guid.NewGuid().ToByteArray();
                return Convert.ToBase64String(bytes, 0, 12);
            }
            else if (CorrelationVectorVersion.V2 == version)
            {
                return CorrelationVector.GetBaseFromGuid(Guid.NewGuid());
            }
            else
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Unsupported correlation vector version: {0}", version));
            }
        }

        private static CorrelationVectorVersion InferVersion(string correlationVector, bool reportErrors)
        {
            int index = correlationVector == null ? -1 : correlationVector.IndexOf('.');

            if (CorrelationVector.BaseLength == index)
            {
                return CorrelationVectorVersion.V1;
            }
            else if (CorrelationVector.BaseLengthV2 == index)
            {
                return CorrelationVectorVersion.V2;
            }
            else
            {
                //By default not reporting error, just return V1
                return CorrelationVectorVersion.V1;
            }
        }

        private static bool IsImmutable(string correlationVector)
        {
            return !string.IsNullOrEmpty(correlationVector) && correlationVector.EndsWith(CorrelationVector.TerminationSign);
        }

        private static bool IsOversized(string baseVector, int extension, CorrelationVectorVersion version)
        {
            if (!string.IsNullOrEmpty(baseVector))
            {
                int size = baseVector.Length + 1 +
                    (extension > 0 ? (int)Math.Log10(extension) : 0) + 1;
                return ((version == CorrelationVectorVersion.V1 &&
                      size > CorrelationVector.MaxVectorLength) ||
                     (version == CorrelationVectorVersion.V2 &&
                      size > CorrelationVector.MaxVectorLengthV2));
            }
            return false;
        }

        private static void Validate(string correlationVector, CorrelationVectorVersion version)
        {
            byte maxVectorLength;
            byte baseLength;

            if (CorrelationVectorVersion.V1 == version)
            {
                maxVectorLength = CorrelationVector.MaxVectorLength;
                baseLength = CorrelationVector.BaseLength;
            }
            else if (CorrelationVectorVersion.V2 == version)
            {
                maxVectorLength = CorrelationVector.MaxVectorLengthV2;
                baseLength = CorrelationVector.BaseLengthV2;
            }
            else
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Unsupported correlation vector version: {0}", version));
            }

            if (string.IsNullOrWhiteSpace(correlationVector) || correlationVector.Length > maxVectorLength)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "The {0} correlation vector can not be null or bigger than {1} characters", version, maxVectorLength));
            }

            string[] parts = correlationVector.Split('.');

            if (parts.Length < 2 || parts[0].Length != baseLength)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid correlation vector {0}. Invalid base value {1}", correlationVector, parts[0]));
            }

            for (int i = 1; i < parts.Length; i++)
            {
                int result;
                if (int.TryParse(parts[i], out result) == false || result < 0)
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid correlation vector {0}. Invalid extension value {1}", correlationVector, parts[i]));
                }
            }
        }
    }
}