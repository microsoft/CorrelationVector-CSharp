using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CorrelationVector
{
    /// <summary>
    /// This interface represents the abstract class from which Correlation Vectors are derived.
    /// Not to be confused with the cV Base, which is the string that represents the base of the vector.
    /// </summary>
    public abstract class CorrelationVector : ICorrelationVector
    {
        protected CorrelationVector()
        {

        }

        public abstract string Value { get; }

        public abstract string Base { get; }

        public abstract int Extension { get; }

        protected const string HeaderName = "MS-CV";

        public CorrelationVectorVersion Version
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to validate the correlation
        /// vector on creation.
        /// </summary>
        public static bool ValidateCorrelationVectorDuringCreation { get; set; }

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
        public bool Equals(ICorrelationVector vector)
        {
            return string.Equals(this.Value, vector.Value, StringComparison.Ordinal);
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

            if (CorrelationVectorV1.BaseLength == index)
            {
                return CorrelationVectorVersion.V1;
            }
            else if (CorrelationVectorV2.BaseLength == index)
            {
                return CorrelationVectorVersion.V2;
            }
            else if (index == 1)
            {
                // cV version indicated by starting single letter for V3 and after
                if (correlationVector.Substring(0, index) == "A")
                {
                    return CorrelationVectorVersion.V3;
                }
                else
                {
                    //By default not reporting error, just return V1
                    return CorrelationVectorVersion.V1;
                }
            }
            else
            {
                //By default not reporting error, just return V1
                return CorrelationVectorVersion.V1;
            }
        }

        /// <summary>
        /// Converts a string representation of a Correlation Vector into this class.
        /// </summary>
        /// <param name="correlationVector">String representation</param>
        /// <returns>The Correlation Vector based on its version.</returns>
        public static CorrelationVector Parse(string correlationVector)
        {
            CorrelationVectorVersion version = InferVersion(correlationVector);
            return RunStaticMethod(correlationVector, version, CorrelationVectorV1.Parse, CorrelationVectorV2.Parse, CorrelationVectorV3.Parse);

        }

        public static CorrelationVector Extend(string correlationVector)
        {
            CorrelationVectorVersion version = InferVersion(correlationVector);
            return RunStaticMethod(correlationVector, version, CorrelationVectorV1.Extend, CorrelationVectorV2.Extend); 
        }

        public static CorrelationVector Spin(string correlationVector)
        {
            SpinParameters defaultParameters = new SpinParameters
            {
                Interval = SpinCounterInterval.Coarse,
                Periodicity = SpinCounterPeriodicity.Short,
                Entropy = SpinEntropy.Two
            };
            return Spin(correlationVector, defaultParameters);
        }

        public static CorrelationVector Spin(string correlationVector, SpinParameters parameters)
        {
            CorrelationVectorVersion version = InferVersion(correlationVector);
            switch (version)
            {
                case CorrelationVectorVersion.V1:
                    return CorrelationVectorV1.Spin(correlationVector, parameters);
                case CorrelationVectorVersion.V2:
                    return CorrelationVectorV2.Spin(correlationVector, parameters);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Helper method to run method based on version.
        /// </summary>
        /// <param name="correlationVector">String representation of your cV</param>
        /// <param name="v">Correlation Vector version.</param>
        /// <param name="functions">The parameters for each cV version. Versions are in order from V1, V2, V3, etc.</param>
        /// <returns>cV based on cV version.</returns>
        private static CorrelationVector RunStaticMethod(string correlationVector, CorrelationVectorVersion v, params Func<string, CorrelationVector>[] functions)
        {
            if ((int)v < functions.Length)
            {
                return functions[(int)v](correlationVector);
            }
            else
            {
                throw new ArgumentException("No function indicated for this version");
            }
        }

        public abstract Tuple<string, string> Reset();

        public abstract string Increment();
    }
}
