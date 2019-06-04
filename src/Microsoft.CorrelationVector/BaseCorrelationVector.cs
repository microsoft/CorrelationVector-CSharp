using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CorrelationVector
{
    /// <summary>
    /// This interface represents the abstract class from which Correlation Vectors are derived.
    /// Not to be confused with the cV Base, which is the string that represents the base of the vector.
    /// </summary>
    public abstract class BaseCorrelationVector : ICorrelationVector
    {
        protected BaseCorrelationVector()
        {

        }
        public abstract string Value { get; }
        public abstract string Base { get; }
        public abstract int Extension { get; }
        protected const string HeaderName = "MS-CV";
        /// <summary>
        /// This is the maximum vector length before it has to be reset or terminated.
        /// </summary>
        internal const byte MaxVectorLength = 127;
        /// <summary>
        /// Determines whether two instances of the <see cref="ICorrelationVector"/> class
        /// are equal. 
        /// </summary>
        /// <param name="vector">
        /// The correlation vector you want to compare with the current correlation vector.
        /// </param>
        /// <returns>
        /// True if the specified correlation vector is equal to the current correlation
        /// vector; otherwise, false.
        /// </returns>
        public bool Equals(ICorrelationVector vector) {
            return string.Equals(this.Value, vector.Value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Converts a string representation of a Correlation Vector into this class.
        /// </summary>
        /// <param name="correlationVector">String representation</param>
        /// <returns>The Correlation Vector based on its version.</returns>
        public static BaseCorrelationVector Parse(string correlationVector)
        {
            CorrelationVectorVersion version = BaseCorrelationVector.InferVersion(correlationVector);
            switch (version)
            {
                case CorrelationVectorVersion.V1:
                    return CorrelationVector.Parse(correlationVector);
                case CorrelationVectorVersion.V2:
                    return CorrelationVector.Parse(correlationVector);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Identifies which version of the Correlation Vector is being used.
        /// </summary>
        /// <returns>
        /// An enumerator indicating correlation vector version.
        /// </returns>
        private static CorrelationVectorVersion InferVersion(string correlationVector)
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

        public delegate ICorrelationVector Extend();
        public delegate ICorrelationVector Spin(SpinParameters parameters);
        protected delegate void Validate();
        public abstract Tuple<string, string> Reset();
        public abstract string Increment();
    }
}
