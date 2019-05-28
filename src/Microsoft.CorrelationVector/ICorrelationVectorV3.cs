using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CorrelationVector
{
    /// <summary>
    /// This interface represents the Correlation Vector v3 and further.
    /// This implements W3C interoperability and reset functionality.
    /// </summary>
    interface ICorrelationVectorV3 : ICorrelationVector
    {
        /// <summary>
        /// Creates a new correlation vector when the total vector length (the <see cref="Value"/>) is longer than its maximum length.
        /// </summary>
        /// <param name="correlationVector">A correlationVector to be reset.</param>
        /// <returns>A Correlation Vector class.</returns>
        ICorrelationVectorV3 Reset(string correlationVector);
    }
}
