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
    /// This is version 1 of the cV, which uses a shorter base length and can only be incremented.
    /// </summary>
    public sealed class CorrelationVectorV1 : CorrelationVector
    {
        internal new const byte MaxVectorLength = 63;
        internal const byte BaseLength = 16;

        internal readonly string BaseVector = null;

        private int extension = 0;

        private bool immutable = false;

        /// <summary>
        /// This is the termination sign which should be used when vector length exceeds 
        /// the <see cref="MaxVectorLength"/>
        /// </summary>
        public const string TerminationSign = "!";

        /// <summary>
        /// Creates a new correlation vector by extending an existing value. This should be
        /// done at the entry point of an operation.
        /// </summary>
        /// <param name="correlationVector">
        /// Taken from the message header indicated by <see cref="HeaderName"/>.
        /// </param>
        /// <returns>A new correlation vector extended from the current vector.</returns>
        public new static CorrelationVectorV1 Extend(string correlationVector)
        {
            if (CorrelationVectorV1.IsImmutable(correlationVector))
            {
                return CorrelationVectorV1.Parse(correlationVector);
            }

            if (CorrelationVectorV1.ValidateCorrelationVectorDuringCreation)
            {
                CorrelationVectorV1.Validate(correlationVector);
            }

            if (CorrelationVectorV1.IsOversized(correlationVector, 0))
            {
                return CorrelationVectorV1.Parse(correlationVector + CorrelationVectorV1.TerminationSign);
            }

            return new CorrelationVectorV1(correlationVector, 0, false);
        }

        /// <summary>
        /// Not supported in V1.
        /// </summary>
        /// <param name="correlationVector">
        /// Taken from the message header indicated by <see cref="HeaderName"/>.
        /// </param>
        /// <param name="parameters">
        /// The parameters to use when applying the Spin operator.
        /// </param>
        /// <returns>A new correlation vector extended from the current vector.</returns>
        public new static CorrelationVectorV1 Spin(string correlationVector, SpinParameters parameters)
        {
            throw new InvalidOperationException("Spin is not supported in Correlation Vector V1");
        }

        /// <summary>
        /// Creates a new correlation vector by parsing its string representation
        /// </summary>
        /// <param name="correlationVector">correlationVector</param>
        /// <returns>CorrelationVector</returns>
        public new static CorrelationVectorV1 Parse(string correlationVector)
        {
            if (!string.IsNullOrEmpty(correlationVector))
            {
                int p = correlationVector.LastIndexOf('.');
                bool immutable = CorrelationVectorV1.IsImmutable(correlationVector);
                if (p > 0)
                {
                    string extensionValue = immutable ? correlationVector.Substring(p + 1, correlationVector.Length - p - 1 - CorrelationVectorV1.TerminationSign.Length)
                        : correlationVector.Substring(p + 1);
                    int extension;
                    if (int.TryParse(extensionValue, out extension) && extension >= 0)
                    {
                        return new CorrelationVectorV1(correlationVector.Substring(0, p), extension, immutable);
                    }
                }
            }

            return new CorrelationVectorV1();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationVectorV1"/> class. 
        /// This should only be called when no correlation
        /// vector was found in the message header.
        /// </summary>
        public CorrelationVectorV1()
            : this(CorrelationVectorV1.GetUniqueValue(), 0, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationVectorV1"/> class of the
        /// V2 implemenation using the given <see cref="System.Guid"/> as the vector base.
        /// </summary>
        /// <param name="vectorBase">The <see cref="System.Guid"/> to use as a correlation
        /// vector base.</param>
        public CorrelationVectorV1(Guid vectorBase)
            : this(vectorBase.GetBaseFromGuid(BaseLength), 0, false)
        {
        }

        /// <summary>
        /// Gets the value of the correlation vector as a string.
        /// </summary>
        public override string Value
        {
            get
            {
                return string.Concat(this.BaseVector, ".", this.extension,
                    this.immutable ? CorrelationVectorV1.TerminationSign : string.Empty);
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
        public override string Increment()
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
                if (CorrelationVectorV1.IsOversized(this.BaseVector, next))
                {
                    this.immutable = true;
                    return this.Value;
                }
            }
            while (snapshot != Interlocked.CompareExchange(ref this.extension, next, snapshot));
            return string.Concat(this.BaseVector, ".", next);
        }

        public override string Base
        {
            get
            {
                int firstDotLocation = Value.IndexOf('.');
                return Value.Substring(0, firstDotLocation);
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
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return this.Value;
        }

        /// <summary>
        /// Determines whether two instances of the <see cref="CorrelationVectorV1"/> class
        /// are equal. 
        /// </summary>
        /// <param name="vector">
        /// The correlation vector you want to compare with the current correlation vector.
        /// </param>
        /// <returns>
        /// True if the specified correlation vector is equal to the current correlation
        /// vector; otherwise, false.
        /// </returns>
        public bool Equals(CorrelationVectorV1 vector)
        {
            return string.Equals(this.Value, vector.Value, StringComparison.Ordinal);
        }

        private CorrelationVectorV1(string baseVector, int extension, bool immutable)
        {
            this.BaseVector = baseVector;
            this.extension = extension;
            this.Version = CorrelationVectorVersion.V1;
            this.immutable = immutable || CorrelationVectorV1.IsOversized(baseVector, extension);
        }

        private static string GetUniqueValue()
        {
            byte[] bytes = Guid.NewGuid().ToByteArray();
            return Convert.ToBase64String(bytes, 0, 12);
        }

        private static bool IsImmutable(string correlationVector)
        {
            return !string.IsNullOrEmpty(correlationVector) && correlationVector.EndsWith(CorrelationVectorV1.TerminationSign);
        }

        /// <summary>
        /// Checks if the cV will be too big if an extension is added to the base vector.
        /// </summary>
        /// <param name="baseVector"></param>
        /// <param name="extension"></param>
        /// <returns>True if new vector will be too large. False if there is no vector or the vector is the appropriate size.</returns>
        private static bool IsOversized(string baseVector, int extension)
        {
            if (!string.IsNullOrEmpty(baseVector))
            {
                int size = baseVector.Length + 1 +
                    (extension > 0 ? (int)Math.Log10(extension) : 0) + 1;
                return size > MaxVectorLength;
            }
            return false;
        }

        private static void Validate(string correlationVector)
        {
            byte maxVectorLength;
            byte baseLength;
            maxVectorLength = CorrelationVectorV1.MaxVectorLength;
            baseLength = CorrelationVectorV1.BaseLength;

            if (string.IsNullOrWhiteSpace(correlationVector) || correlationVector.Length > maxVectorLength)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "The {0} correlation vector can not be null or bigger than {1} characters", CorrelationVectorVersion.V1, maxVectorLength));
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

        public override Tuple<string, string> Reset()
        {
            throw new InvalidOperationException("Reset is not supported in Correlation Vector V1");
        }
    }
}