﻿// ------------------------------------------------------------
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace ITA.Common.RestHelp.SwaggerHelpers
{
    /// <summary>
    /// Extension methods for <see cref="Type"/>.
    /// </summary>
    public static class TypeExtensions
    {
        private static readonly Dictionary<Type, System.Func<OpenApiSchema>> _simpleTypeToOpenApiSchema =
            new Dictionary<Type, System.Func<OpenApiSchema>>
            {
                {typeof(bool), () => new OpenApiSchema {Type = "boolean"}},
                {typeof(byte), () => new OpenApiSchema {Type = "string", Format = "byte"}},
                {typeof(int), () => new OpenApiSchema {Type = "integer", Format = "int32"}},
                {typeof(uint), () => new OpenApiSchema {Type = "integer", Format = "int32"}},
                {typeof(long), () => new OpenApiSchema {Type = "integer", Format = "int64"}},
                {typeof(ulong), () => new OpenApiSchema {Type = "integer", Format = "int64"}},
                {typeof(float), () => new OpenApiSchema {Type = "number", Format = "float"}},
                {typeof(double), () => new OpenApiSchema {Type = "number", Format = "double"}},
                {typeof(decimal), () => new OpenApiSchema {Type = "number", Format = "double"}},
                {typeof(DateTime), () => new OpenApiSchema {Type = "string", Format = "date-time"}},
                {typeof(DateTimeOffset), () => new OpenApiSchema {Type = "string", Format = "date-time"}},
                {typeof(Guid), () => new OpenApiSchema {Type = "string", Format = "uuid"}},
                {typeof(char), () => new OpenApiSchema {Type = "string"}},
                {typeof(TimeSpan), () => new OpenApiSchema {Type = "string"}},

                {typeof(bool?), () => new OpenApiSchema {Type = "boolean", Nullable = true}},
                {typeof(byte?), () => new OpenApiSchema {Type = "string", Format = "byte", Nullable = true}},
                {typeof(int?), () => new OpenApiSchema {Type = "integer", Format = "int32", Nullable = true}},
                {typeof(uint?), () => new OpenApiSchema {Type = "integer", Format = "int32", Nullable = true}},
                {typeof(long?), () => new OpenApiSchema {Type = "integer", Format = "int64", Nullable = true}},
                {typeof(ulong?), () => new OpenApiSchema {Type = "integer", Format = "int64", Nullable = true}},
                {typeof(float?), () => new OpenApiSchema {Type = "number", Format = "float", Nullable = true}},
                {typeof(double?), () => new OpenApiSchema {Type = "number", Format = "double", Nullable = true}},
                {typeof(decimal?), () => new OpenApiSchema {Type = "number", Format = "double", Nullable = true}},
                {typeof(DateTime?), () => new OpenApiSchema {Type = "string", Format = "date-time", Nullable = true}},
                {typeof(DateTimeOffset?), () =>
                    new OpenApiSchema {Type = "string", Format = "date-time", Nullable = true}},
                {typeof(Guid?), () => new OpenApiSchema {Type = "string", Format = "uuid", Nullable = true}},
                {typeof(char?), () => new OpenApiSchema {Type = "string", Nullable = true}},
                {typeof(TimeSpan?), () => new OpenApiSchema {Type = "string", Nullable = true}},

                // Uri is treated as simple string.
                {typeof(Uri), () => new OpenApiSchema {Type = "string", Nullable = true}},

                {typeof(string), () => new OpenApiSchema {Type = "string", Nullable = true}},

                {typeof(object), () => new OpenApiSchema {Type = "object", Nullable = true}}
            };

        /// <summary>
        /// Gets the item type in an array or an IEnumerable.
        /// </summary>
        /// <param name="type">An array or IEnumerable type to get the item type from.</param>
        /// <returns>The type of the item in the array or IEnumerable.</returns>
        public static Type GetEnumerableItemType(this Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (typeof(IEnumerable).IsAssignableFrom(type) && type.GetGenericArguments().Any())
            {
                return type.GetGenericArguments().First();
            }

            return null;
        }

        /// <summary>
        /// Determines whether the given type implements the given interface type.
        /// </summary>
        public static bool ImplementInterface(this Type type, Type interfaceType)
        {
            for (Type currentType = type; currentType != null; currentType = currentType.BaseType)
            {
                IEnumerable<Type> interfaces = currentType.GetInterfaces();
                foreach (Type i in interfaces)
                {
                    if (i == interfaceType || (i != null && i.ImplementInterface(interfaceType)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the given type is a dictionary.
        /// </summary>
        public static bool IsDictionary(this Type type)
        {
            return type.IsGenericType &&
            (typeof(IDictionary<,>).IsAssignableFrom(type.GetGenericTypeDefinition()) ||
                typeof(Dictionary<,>).IsAssignableFrom(type.GetGenericTypeDefinition()));
        }

        /// <summary>
        /// Determines whether the given type is enumerable.
        /// </summary>
        /// <remarks>
        /// Even though string is technically an IEnumerable of char, this method will
        /// return false for string since it is generally expected to behave like a simple type.
        /// </remarks>
        public static bool IsEnumerable(this Type type)
        {
            return type != typeof(string) && (type.IsArray || typeof(IEnumerable).IsAssignableFrom(type));
        }

        /// <summary>
        /// Determines whether the given type is a "simple" type.
        /// </summary>
        /// <remarks>
        /// A simple type is defined to match with what OpenAPI generally recognizes.
        /// This includes the so-called C# primitive types and a few other types such as string, DateTime, etc.
        /// </remarks>
        public static bool IsSimple(this Type type)
        {
            return _simpleTypeToOpenApiSchema.ContainsKey(type);
        }

        /// <summary>
        /// Maps a simple type to an OpenAPI data type and format.
        /// </summary>
        /// <param name="type">Simple type.</param>
        /// <remarks>
        /// All the following types from http://swagger.io/specification/#data-types-12 are supported.
        /// Other types including nullables and URL are also supported.
        /// Common Name      type    format      Comments
        /// ===========      ======= ======      =========================================
        /// integer          integer int32       signed 32 bits
        /// long             integer int64       signed 64 bits
        /// float            number  float
        /// double           number  double
        /// string           string  [empty]
        /// byte             string  byte        base64 encoded characters
        /// binary           string  binary      any sequence of octets
        /// boolean          boolean [empty]
        /// date             string  date        As defined by full-date - RFC3339
        /// dateTime         string  date-time   As defined by date-time - RFC3339
        /// password         string  password    Used to hint UIs the input needs to be obscured.
        /// If the type is not recognized as "simple", System.String will be returned.
        /// </remarks>
        public static OpenApiSchema MapToOpenApiSchema(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            System.Func<OpenApiSchema> result;
            return _simpleTypeToOpenApiSchema.TryGetValue(type, out result)
                ? result()
                : new OpenApiSchema {Type = "string"};
        }

        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }
}