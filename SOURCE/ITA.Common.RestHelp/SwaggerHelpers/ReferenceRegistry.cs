// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.

// MIT License

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// ------------------------------------------------------------

using System.Collections.Generic;
using ITA.Common.RestHelp.Data;
using Microsoft.OpenApi.Interfaces;

namespace ITA.Common.RestHelp.SwaggerHelpers
{
    /// <summary>
    /// Reference Registry for an <see cref="IOpenApiReferenceable"/> class.
    /// </summary>
    public abstract class ReferenceRegistry<TInput, TOutput>
        where TOutput : IOpenApiReferenceable
    {
        /// <summary>
        /// The dictionary containing all references of the given type.
        /// </summary>
        public abstract IDictionary<string, TOutput> References { get; }

        /// <summary>
        /// Finds the existing reference object based on the key from the input or creates a new one.
        /// </summary>
        /// <returns>The existing or created reference object.</returns>
        internal abstract TOutput FindOrAddReference(TInput input, TypeDocumentation typeDoc);

        /// <summary>
        /// Gets the key from the input object to use as reference string.
        /// </summary>
        /// <remarks>This must match the regular expression ^[a-zA-Z0-9\.\-_]+$</remarks>
        internal abstract string GetKey(TInput input);
    }
}