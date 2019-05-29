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
        /// Gets the base of the correlation vector as a string.
        /// </summary>
        string Base
        {
            get;
        }

        /// <summary>
        /// This is the header that should be used between services to pass the correlation
        /// vector.
        /// </summary>
        string HeaderName
        {
            get;
        }

        /// <summary>
        /// This is the maximum vector length before it has to be reset (before V3, terminated.)
        /// </summary>
        byte MaxVectorLength
        {
            get;
        }

        /// <summary>
        /// Increments the extension, the numerical value at the end of the vector, by one.
        /// </summary>
        /// <returns>
        /// The new value as a string that you can add to the outbound message header.
        /// </returns>
        ICorrelationVector Increment();

        /// <summary>
        /// Creates a new correlation vector by extending an existing value. This should be
        /// done at the entry point of an operation.
        /// </summary>
        /// <param name="correlationVector">
        /// Taken from the message header.
        /// </param>
        /// <returns>A new correlation vector extended from the current vector.</returns>
        ICorrelationVector Extend(string correlationVector);

        /// <summary>
        /// Creates a new correlation vector by applying the Spin operator to an existing value.
        /// This should be done at the entry point of an operation.
        /// </summary>
        /// <param name="correlationVector">
        /// Taken from the message header indicated by <see cref="HeaderName"/>.
        /// </param>
        /// <returns>A new correlation vector extended from the current vector.</returns>
        ICorrelationVector Spin(string correlationVector);

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
        ICorrelationVector Spin(string correlationVector, SpinParameters parameters);

        /// <summary>
        /// Creates a new correlation vector by parsing its string representation
        /// </summary>
        /// <param name="correlationVector">correlationVector</param>
        /// <returns>A Correlation Vector class.</returns>
        ICorrelationVector Parse(string correlationVector);

        /// <summary>
        /// Determines whether two instances of the <see cref="ICorrelationVector"/>
        /// are equal. 
        /// </summary>
        /// <param name="vector">
        /// The correlation vector you want to compare with the current correlation vector.
        /// </param>
        /// <returns>
        /// True if the specified correlation vector is equal to the current correlation
        /// vector; otherwise, false.
        /// </returns>
        bool Equals(ICorrelationVector vector);

        /// <summary>
        /// Determines if this vector is a valid <see cref="ICorrelationVector"/>.
        /// </summary>
        /// <param name="correlationVector">
        /// The string representation of a correlation vector.
        /// </param>
        /// <returns>
        /// No return value. Throws ArgumentException if invalid.
        /// </returns>
        void Validate(string correlationVector);

        /// <summary>
        /// Creates a new correlation vector when the total vector length (the <see cref="Value"/>) is longer than its maximum length.
        /// Not used in Correlation vectors earlier than V3.
        /// </summary>
        /// <param name="correlationVector">A correlationVector to be reset.</param>
        /// <returns>A Correlation Vector class.</returns>
        ICorrelationVector Reset(string correlationVector);
    }
}
