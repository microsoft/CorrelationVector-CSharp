using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CorrelationVector
{
    /// <summary>
    /// This interface represents the Correlation Vector.
    /// The Correlation Vector is a format for tracing and correlating events in large systems.
    /// </summary>
    public interface ICorrelationVector
    {
        /// <summary>
        /// Gets the value of the correlation vector as a string.
        /// </summary>
        string Value
        {
            get;
        }

        /// <summary>
        /// This is the maximum vector length before it has to be reset or terminated.
        /// </summary>
        byte MaxVectorLength
        {
            get;
        }

        /// <summary>
        /// Increments the extension, the numerical value at the end of the vector, by one
        /// and returns the string representation.
        /// </summary>
        /// <returns>
        /// The new cV value as a string that you can add to the outbound message header.
        /// </returns>
        string Increment();

        /// <summary>
        /// Creates a new correlation vector when the total vector length (the <see cref="Value"/>) is longer than its maximum length.
        /// Not used in Correlation vectors earlier than V3.
        /// </summary>
        /// <param name="correlationVector">A correlationVector to be reset.</param>
        /// <returns>A pair of strings. The first string is the new correlation vector. The second string is the correlation vector it maps to, or continues from.</returns>
        Tuple<string, string> Reset();
    }
}
