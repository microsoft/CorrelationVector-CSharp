// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.CorrelationVector
{
    /// <summary>
    /// Extensions methods providing additional functionality for correlation vectors.
    /// </summary>
    public static class CorrelationVectorExtensions
    {
        /// <summary>
        /// Gets an encoded vector base value from the <see cref="Guid"/>.
        /// </summary>
        /// <param name="guid">The <see cref="Guid"/> to encode as a vector base.</param>
        /// <returns>The encoded vector base value.</returns>
        public static string GetBaseFromGuid(this Guid guid)
        {
            byte[] bytes = guid.ToByteArray();

            // Removes the base64 padding
            return Convert.ToBase64String(bytes).Substring(0, CorrelationVectorV2.BaseLength);
        }

        /// <summary>
        /// Gets the value of the correlation vector base encoded as a <see cref="Guid"/>.
        /// </summary>
        /// <returns>The <see cref="Guid"/> value of the encoded vector base.</returns>
        public static Guid GetBaseAsGuid(this CorrelationVector correlationVector)
        {
            if (correlationVector.Version == CorrelationVectorVersion.V1)
            {
                throw new InvalidOperationException("Cannot convert a V1 correlation vector base to a guid.");
            }

            if (CorrelationVectorV2.ValidateCorrelationVectorDuringCreation)
            {
                // In order to reliably convert a V2 vector base to a guid, the four least significant bits of the last
                // base64 content-bearing 6-bit block must be zeros.
                // There are four such base64 characters so we can easily detect whether this condition is true.
                // A - 00 0000
                // Q - 01 0000
                // g - 10 0000
                // w - 11 0000
                char lastChar = correlationVector.Base[CorrelationVectorV2.BaseLength - 1];

                if (lastChar != 'A' && lastChar != 'Q' && lastChar != 'g' && lastChar != 'w')
                {
                    throw new InvalidOperationException(
                        "The four least significant bits of the base64 encoded vector base must be zeros to reliably convert to a guid.");
                }
            }

            return new Guid(Convert.FromBase64String(correlationVector.Base + "=="));
        }
    }
}
