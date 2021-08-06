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

using System;
using System.Collections.Generic;
using System.Linq;
using ITA.Common.RestHelp.Data;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace ITA.Common.RestHelp.SwaggerHelpers
{
    /// <summary>
    /// Reference Registry for <see cref="OpenApiSchema"/>
    /// </summary>
    public class SchemaReferenceRegistry : ReferenceRegistry<Type, OpenApiSchema>
    {
        /// <summary>
        /// Creates an instance of <see cref="SchemaReferenceRegistry"/>.
        /// </summary>
        /// <param name="schemaGenerationSettings">The schema generation settings.</param>
        public SchemaReferenceRegistry()
        {
            
        }

        Dictionary<string, OpenApiSchema> _references = new Dictionary<string, OpenApiSchema>();
        /// <summary>
        /// The dictionary containing all references of the given type.
        /// </summary>
        public override IDictionary<string, OpenApiSchema> References
        {
            get { return _references; }
        }

        private OpenApiSchema GetSimpleTypeSchema(Type input)
        {
            OpenApiSchema schema = input.MapToOpenApiSchema();

            // Certain simple types yield more specific information.
            if (input == typeof(char))
            {
                schema.MinLength = 1;
                schema.MaxLength = 1;
            }
            else if (input == typeof(Guid))
            {
                schema.Example = new OpenApiString(Guid.Empty.ToString());
            }

            return schema;
        }

        private OpenApiSchema GetEnumSchema(Type input)
        {
            var schema = new OpenApiSchema();
            schema.Type = "integer";
            List<string> enumValueDescriptions = new List<string>();
            var title = input.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0
                ? Resources.HelpPageBimaskEnumeration
                : Resources.HelpPageEnumeration;
            enumValueDescriptions.Add(string.Format("### {0}", title));
            foreach (var name in Enum.GetNames(input))
            {
                var convertedObj = Convert.ChangeType(Enum.Parse(input, name), Enum.GetUnderlyingType(input));
                int intVal = Convert.ToInt32(convertedObj);
                //schema.Enum.Add(new OpenApiInteger(intVal));
                enumValueDescriptions.Add(string.Format("- {0}={1}", name, intVal));
            }

            schema.Description = string.Join("\n", enumValueDescriptions);
            return schema;
        }

        private OpenApiSchema GetNullableEnumSchema(Type input)
        {
            var schema = GetEnumSchema(input.GenericTypeArguments[0]);
            
            schema.Nullable = true;

            return schema;
        }

        private OpenApiSchema GetArraySchema(Type input, TypeDocumentation typeDoc)
        {
            var schema = new OpenApiSchema();

            var arrayType = input.GetEnumerableItemType();
            if (arrayType != null && arrayType.FullName != null && arrayType.FullName.Equals(typeof(byte).FullName))
            {
                schema.Type = "string";
                schema.Format = "byte";
                schema.Nullable = true;

                return schema;
            }
            schema.Type = "array";
            schema.Nullable = true;

            schema.Items = FindOrAddReference(input.GetEnumerableItemType(), typeDoc);

            return schema;
        }

        /// <summary>
        /// Finds the existing reference object based on the key from the input or creates a new one.
        /// </summary>
        /// <returns>The existing or created reference object.</returns>
        internal override OpenApiSchema FindOrAddReference(Type input, TypeDocumentation typeDoc)
        {
            // Return empty schema when the type does not have a name. 
            // This can occur, for example, when a generic type without the generic argument specified
            // is passed in.
            if (input == null || input.FullName == null)
            {
                return new OpenApiSchema();
            }

            var key = GetKey(input);

            // If the schema already exists in the References, simply return.
            if (References.ContainsKey(key))
            {
                return new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Id = key,
                        Type = ReferenceType.Schema
                    }
                };
            }

            try
            {
                // There are multiple cases for input types that should be handled differently to match the OpenAPI spec.
                //
                // 1. Simple Type
                // 2. Enum Type
                // 3. Dictionary Type
                // 4. Enumerable Type
                // 5. Object Type
                var schema = new OpenApiSchema();
                
                if (input.IsSimple())
                {
                    return GetSimpleTypeSchema(input);
                }

                if (input.IsEnum)
                {
                    return GetEnumSchema(input);
                }

                if (input.IsNullable() && input.GenericTypeArguments.Length > 0 && input.GenericTypeArguments[0].IsEnum)
                {
                    return GetNullableEnumSchema(input);
                }

                if (input.IsDictionary())
                {
                    schema.Type = "object";
                    schema.Nullable = true;
                    schema.AdditionalProperties = FindOrAddReference(input.GetGenericArguments()[1], typeDoc);

                    return schema;
                }

                if (input.IsEnumerable())
                {
                    return GetArraySchema(input, typeDoc);
                }

                schema.Type = "object";
                schema.Nullable = true;
                schema.Description = typeDoc.Summary;
                
                // Note this assignment is necessary to allow self-referencing type to finish
                // without causing stack overflow.
                // We can also assume that the schema is an object type at this point.
                References[key] = schema;

                HashSet<string> requiredProperties = new HashSet<string>();
                var propertyNameDeclaringTypeMap = new Dictionary<string, Type>();
                foreach (var propertyInfo in input.GetProperties())
                {
                    var ignoreProperty = false;
                    
                    var propertyName = propertyInfo.Name; 
                    var foundPropertyDoc = typeDoc is TypeDictDocumentation 
                        ? typeDoc.Properties.FirstOrDefault(p => p.DataMemberName.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase))
                        : typeDoc.Properties.FirstOrDefault(p => p.Property != null && p.Property.Name.Equals(propertyName));
                    var serializedPropertyName = foundPropertyDoc != null && foundPropertyDoc.IsDataMemeber &&
                                                 !string.IsNullOrEmpty(foundPropertyDoc.DataMemberName)
                        ? foundPropertyDoc.DataMemberName
                        : propertyName;
                    var innerSchema = FindOrAddReference(propertyInfo.PropertyType, foundPropertyDoc != null ? foundPropertyDoc.TypeDocumentation : null);
                    var attributes = propertyInfo.GetCustomAttributes(false);

                    foreach (var attribute in attributes)
                    {
                        if (attribute is JsonPropertyAttribute)
                        {
                            var type = attribute.GetType();
                            var requiredPropertyInfo = type.GetProperty("Required");

                            if (requiredPropertyInfo != null)
                            {
                                var requiredValue = Enum.GetName(
                                    requiredPropertyInfo.PropertyType,
                                    requiredPropertyInfo.GetValue(attribute, null));

                                if (requiredValue == "Always")
                                {
                                    schema.Required.Add(serializedPropertyName);
                                }
                            }
                        }

                        if (attribute is JsonIgnoreAttribute)
                        {
                            ignoreProperty = true;
                        }
                    }

                    if (ignoreProperty || (foundPropertyDoc != null && !foundPropertyDoc.IsDataMemeber))
                    {
                        continue;
                    }

                    var propertyDeclaringType = propertyInfo.DeclaringType;

                    if (propertyNameDeclaringTypeMap.ContainsKey(serializedPropertyName))
                    {
                        var existingPropertyDeclaringType = propertyNameDeclaringTypeMap[serializedPropertyName];
                        var duplicateProperty = true;

                        if (existingPropertyDeclaringType != null && propertyDeclaringType != null)
                        {
                            if (propertyDeclaringType.IsSubclassOf(existingPropertyDeclaringType)
                                || (existingPropertyDeclaringType.IsInterface
                                && propertyDeclaringType.ImplementInterface(existingPropertyDeclaringType)))
                            {
                                // Current property is on a derived class and hides the existing
                                schema.Properties[serializedPropertyName] = innerSchema;
                                duplicateProperty = false;
                            }

                            if (existingPropertyDeclaringType.IsSubclassOf(propertyDeclaringType)
                                || (propertyDeclaringType.IsInterface
                                && existingPropertyDeclaringType.ImplementInterface(propertyDeclaringType)))
                            {
                                // current property is hidden by the existing so don't add it
                                continue;
                            }
                        }

                        if (duplicateProperty)
                        {
                            throw new Exception(string.Format("Duplicate property found '{0}'", serializedPropertyName));
                        }
                    }

                    if (innerSchema.Type != null && innerSchema.Type.Equals("object", StringComparison.InvariantCultureIgnoreCase) ||
                        innerSchema.Reference != null)
                    {
                        schema.Properties[serializedPropertyName] = new OpenApiSchema()
                        {
                            Nullable = true,
                            OneOf = new List<OpenApiSchema>(new OpenApiSchema[]{innerSchema})
                        };
                    }
                    else
                    {
                        schema.Properties[serializedPropertyName] = innerSchema;
                    }
                    if (foundPropertyDoc != null)
                    {
                        schema.Properties[serializedPropertyName].Title = foundPropertyDoc.Summary;
                        if (foundPropertyDoc.IsRequired)
                        {
                            requiredProperties.Add(serializedPropertyName);
                        }
                    }

                    propertyNameDeclaringTypeMap.Add(serializedPropertyName, propertyDeclaringType);
                }

                schema.Required = requiredProperties;
                References[key] = schema;

                return new OpenApiSchema
                {
                    Type = schema.Type,
                    Reference = new OpenApiReference
                    {
                        Id = key,
                        Type = ReferenceType.Schema
                    }
                };
            }
            catch (Exception e)
            {
                // Something went wrong while fetching schema, so remove the key if exists from the references.
                if (References.ContainsKey(key))
                {
                    References.Remove(key);
                }

                throw new Exception(string.Format("Adding reference failed. Key='{0}', Message='{1}'", key, e.Message));
            }
        }

        /// <summary>
        /// Gets the key from the input object to use as reference string.
        /// </summary>
        /// <remarks>
        /// This must match the regular expression ^[a-zA-Z0-9\.\-_]+$ due to OpenAPI V3 spec.
        /// </remarks>
        internal override string GetKey(Type input)
        {
            // Type.ToString() returns full name for non-generic types and
            // returns a full name without unnecessary assembly information for generic types.
            var typeName = input.ToString();

            return typeName.SanitizeClassName();
        }
    }
}