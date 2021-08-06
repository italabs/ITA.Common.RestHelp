using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using ITA.Common.RestHelp.Interfaces;
using Newtonsoft.Json;

namespace ITA.Common.RestHelp.Data
{
    [DataContract]
    internal sealed class ContractInformation : ICommentProvider, IResponseProvider
    {
        private static readonly ConcurrentDictionary<Type, TypeDocumentation> TypeCache = new ConcurrentDictionary<Type, TypeDocumentation>();

        private readonly List<AssemblyDocumentation> _assemblies;
        private readonly IHelpExampleProvider _exampleProvider;

        public ContractInformation(ContractDescription contractDescription, IHelpExampleProvider exampleProvider)
        {
            _exampleProvider = exampleProvider;

            _assemblies = FindReferencedAssemblies(contractDescription)
                .Distinct(new AssemblyComparer())
                .Select(x => new AssemblyDocumentation(x))
                .Where(x => !string.IsNullOrWhiteSpace(x.AssemblyName))
                .ToList();

            ApiInfo = GetApiInformation(contractDescription.ContractType);
            ContractDocumentation = GetDocumentation(contractDescription.ContractType);
            Operations = new List<OperationInformation>();
            foreach (var operation in contractDescription.Operations)
            {
                var operationInfo = _exampleProvider != null
                    ? new OperationInformationEx(operation, _exampleProvider)
                    : new OperationInformation(operation);

                operationInfo.LoadDocumentation(this, this);
                //merge type responses and method responses
                operationInfo.ErrorResponses = BuildOperationErrorResponses(ContractDocumentation.ErrorResponses,
                    operationInfo.ErrorResponses);
                Operations.Add(operationInfo);
            }
        }

        [DataMember]
        public TypeDocumentation ContractDocumentation { get; set; }

        [DataMember]
        public List<OperationInformation> Operations { get; private set; }

        [DataMember]
        public ApiInformation ApiInfo { get; private set; }

        public MethodDocumentation GetMethodDocumentation(OperationInformation operation)
        {
            var template = operation.UriTemplate != operation.Name
                ? new UriTemplate(operation.UriTemplate)
                : null;
            var method = operation.MethodInfo;
            var type = method.DeclaringType;
            var parameters = method.GetParameters();
            var methodParams = string.Join(",", parameters.Select(p => p.ParameterType.FullName));
            var methodKey = string.IsNullOrWhiteSpace(methodParams)
                ? string.Format("M:{0}.{1}", type.FullName, method.Name)
                : string.Format("M:{0}.{1}({2})", type.FullName, method.Name, methodParams);

            var methodComments = GetMemberDoc(methodKey);
            var documentation = new MethodDocumentation
            {
                Method = method,
                Summary = methodComments == null ? string.Empty : methodComments.Summary,
                Returns = methodComments == null ? string.Empty : methodComments.Returns
            };

            documentation.InputParameters = new List<MethodParamDocumentation>();

            // All custom attributes
            var methodAttributes = method.GetCustomAttributes(true);
            var typeAttribures = method.DeclaringType.GetCustomAttributes(true);

            // Input header parameters
            var headerAttributes = methodAttributes.OfType<RestHeaderAttribute>().ToArray();
            if (headerAttributes.Any())
            {
                documentation.InputParameters.AddRange(headerAttributes.Select(x => new MethodParamDocumentation
                {
                    IsRequired = x.Required,
                    ParamPlace = MethodParamPlace.Header,
                    ParameterName = x.Name,
                    Summary = x.Description,
                    ParameterType = typeof(string),
                    TypeDocumentation = null
                }));
            }

            // Authorization
            var authorizationAttributes = methodAttributes.OfType<RestAuthorizationAttribute>().ToArray();
            authorizationAttributes = authorizationAttributes.Any()
                ? authorizationAttributes
                : typeAttribures.OfType<RestAuthorizationAttribute>().ToArray();
            documentation.AuthorizationType = authorizationAttributes.Any() 
                ? authorizationAttributes.First().Type 
                : RestAuthorizationType.None;

            // Input parameters    
            documentation.InputParameters.AddRange(parameters
                .Select(p => GetMethodParamaterDocumentation(methodComments, p.Name, p.ParameterType, template, p))
                .ToList());

            // Output parameter
            var returnType = operation.MethodInfo.ReturnType;
            documentation.OutputParameter = GetMethodParamaterDocumentation(methodComments, string.Empty, returnType, null, null);
            if (documentation.OutputParameter != null)
            {
                documentation.OutputParameter.Summary = documentation.Returns;
            }

            // Error output parameter
            var faultType = GetFaultType(operation.MethodInfo);
            if (faultType != null)
            {
                documentation.ErrorOutputParameter = GetMethodParamaterDocumentation(methodComments, string.Empty, faultType, null, null);
            }

            return documentation;
        }

        public List<ResponseDocumentation> GetMethodErrorResponses(MethodInfo operationMethodInfo)
        {
            List<ResponseDocumentation> faultResponses = GetErrorResponses(InformationHelper.GetCustomAttributes<RestFaultContractAttribute>(operationMethodInfo));

            return faultResponses;
        }

        private TypeDocumentation GetDocumentation(Type type)
        {
            if (TypeCache.ContainsKey(type))
                return TypeCache[type];

            var typeDoc = CreateTypeDocumentation(type);

            TypeCache.TryAdd(type, typeDoc);

            if (typeDoc != null && typeDoc.Type != null && typeDoc.Properties == null)
            {
                typeDoc.Properties = !typeDoc.IsSystem
                    ? GetPropertyDocumentation(typeDoc.Type)
                    : new List<PropertyDocumentation>();
            }

            return typeDoc;
        }

        private TypeDocumentation CreateTypeDocumentation(Type type)
        {
            if (type == typeof(void))
                return null;

            var isDictionary = typeof(IDictionary).IsAssignableFrom(type);
            var isCollection = type.IsArray;
            var dataContractAttribute = Attribute.GetCustomAttribute(type, typeof(DataContractAttribute)) as DataContractAttribute;
            var isDataContract = dataContractAttribute != null;
            var isList = typeof(IList).IsAssignableFrom(type);
            var isNullable = IsNullable(type);

            var realType = type.IsGenericType && (isList || isNullable) ? GetGenericTypeArgument(type) : type;
            realType = isCollection ? type.GetElementType() : realType;

            var isSystemType = realType.Assembly.GetName().Name == "mscorlib";
            var isKeyValuePair = realType.Name == typeof(KeyValuePair<,>).Name;

            var typeKey = string.Format("T:{0}", realType.FullName);
            var typeInfo = GetMemberDoc(typeKey);
            var errorResponses = GetObjectFaultResponses(type);

            if (isDictionary || isKeyValuePair)
            {
                var dictType = isDictionary ? type : realType;
                var typeDictDocumentation = new TypeDictDocumentation();
                var types = dictType.GetGenericArguments();
                typeDictDocumentation.SetDictionary(GetDocumentation(types[0]), GetDocumentation(types[1]));
                return typeDictDocumentation;
            }

            return new TypeDocumentation
            {
                Type = realType,
                IsDataContract = isDataContract,
                DataContractName = dataContractAttribute != null && !string.IsNullOrWhiteSpace(dataContractAttribute.Name)
                    ? dataContractAttribute.Name
                    : null,
                Name = typeInfo == null ? realType.FullName : typeInfo.Name,
                Summary = typeInfo == null ? string.Empty : typeInfo.Summary,
                IsCollection = isCollection || isList,
                JsonTypeName = SystemTypeToJsonType(realType),
                IsNullable = isNullable,
                IsSystem = isSystemType,
                IsEnum = realType.IsEnum,
                ErrorResponses = errorResponses,
                IsRequired = typeInfo != null ? typeInfo.IsRequired : Constants.DefaultRequiredValue
            };
        }

        private Type GetGenericTypeArgument(Type type)
        {
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                return type.GetGenericArguments().FirstOrDefault();
            }
            return null;
        }

        private bool IsNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        private string SystemTypeToJsonType(Type type)
        {
            if (type == typeof(string))
            {
                return Resources.HelpPageString;
            }
            if (type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(DateTime?) || type == typeof(TimeSpan?))
            {
                return Resources.HelpPageDateTimeString;
            }
            if (type == typeof(bool))
            {
                return Resources.HelpPageBoolean;
            }
            if (type == typeof(int) || type == typeof(short))
            {
                return Resources.HelpPageNumeric;
            }
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            {
                return Resources.HelpPageFloatNumeric;
            }
            // Enum equal numeric
            if (type.IsEnum)
            {
                return Resources.HelpPageNumeric;
            }
            return Resources.HelpPageJsonObject;
        }

        private List<PropertyDocumentation> GetPropertyDocumentation(Type type)
        {
            var result = new List<PropertyDocumentation>();
            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                var realType = property.DeclaringType ?? type;
                var propKey = string.Format("P:{0}.{1}", realType.FullName, property.Name);
                var propInfo = GetMemberDoc(propKey);

                var dataMemeberAttribute = Attribute.GetCustomAttribute(property, typeof(DataMemberAttribute)) as DataMemberAttribute;
                var jsonPropertyAttribute = Attribute.GetCustomAttribute(property, typeof(JsonPropertyAttribute)) as JsonPropertyAttribute;
                var jsonIgnoreAttibute = Attribute.GetCustomAttribute(property, typeof(JsonIgnoreAttribute)) as JsonIgnoreAttribute;
                var restRequiredAttribute = Attribute.GetCustomAttribute(property, typeof(RestRequiredAttribute)) as RestRequiredAttribute;

                var isDataMember = (dataMemeberAttribute != null && jsonIgnoreAttibute == null) || jsonPropertyAttribute != null;
                var dataMemberName = string.Empty;
                if (dataMemeberAttribute != null)
                    dataMemberName = dataMemeberAttribute.Name;
                if (jsonPropertyAttribute != null)
                    dataMemberName = jsonPropertyAttribute.PropertyName;
                //default required is false
                var isRequired = Constants.DefaultRequiredValue;
                if (restRequiredAttribute != null)
                {
                    isRequired = restRequiredAttribute.Value;
                }
                else if (propInfo != null)
                {
                    isRequired = propInfo.IsRequired;
                }

                result.Add(new PropertyDocumentation
                {
                    Property = property,
                    Summary = propInfo == null ? string.Empty : propInfo.Summary,
                    IsDataMemeber = isDataMember,
                    DataMemberName = dataMemberName,
                    IsRequired = isRequired,
                    TypeDocumentation = GetDocumentation(property.PropertyType)
                });
            }

            return result;
        }

        private MethodParamDocumentation GetMethodParamaterDocumentation(MemberComment methodComments, string name, Type type, UriTemplate template, ParameterInfo parameterInfo)
        {
            var summaryInfo = methodComments != null
                ? methodComments.Parameters.FirstOrDefault(p => p.Name == name)
                : null;

            var place = MethodParamPlace.Body;
            if (template != null)
            {
                if (template.PathSegmentVariableNames.Contains(name.ToUpper()))
                {
                    place = MethodParamPlace.Uri;
                }
                if (template.QueryValueVariableNames.Contains(name.ToUpper()))
                {
                    place = MethodParamPlace.Query;
                }
            }

            var isRequired = summaryInfo != null ? summaryInfo.IsRequired : Constants.DefaultRequiredValue;

            if (parameterInfo != null)
            {
                var restAttribute = parameterInfo.GetCustomAttributes<RestRequiredAttribute>().FirstOrDefault();
                if (restAttribute != null)
                {
                    isRequired = restAttribute.Value;
                }
            }

            if (place == MethodParamPlace.Uri)
            {
                isRequired = true;
            }

            return new MethodParamDocumentation
            {
                ParameterName = name,
                ParameterType = type,
                TypeDocumentation = GetDocumentation(type),
                Summary = summaryInfo != null ? summaryInfo.Description : null,
                ParamPlace = place,
                IsRequired = isRequired
            };
        }

        private MemberComment GetMemberDoc(string key)
        {
            return _assemblies.Select(assembly => assembly.GetMemeberDoc(key)).FirstOrDefault(res => res != null);
        }

        private IEnumerable<Assembly> FindReferencedAssemblies(ContractDescription contractDescription)
        {
            foreach (var method in contractDescription.Operations.Select(o => o.SyncMethod))
            {
                var assemblies = method.GetParameters()
                    .Select(x => x.ParameterType.Assembly)
                    .Distinct(new AssemblyComparer());

                foreach (var assembly in assemblies)
                {
                    yield return assembly;
                }

                var faultType = GetFaultType(method);
                if (faultType != null)
                {
                    yield return faultType.Assembly;
                }
            }
            yield return contractDescription.ContractType.Assembly;
        }

        private Type GetFaultType(MethodInfo methodInfo)
        {
            var restAttibute = InformationHelper.GetCustomAttribute<RestFaultContractAttribute>(methodInfo);
            if (restAttibute != null)
            {
                return restAttibute.DetailType;
            }

            var attibute = InformationHelper.GetCustomAttribute<FaultContractAttribute>(methodInfo);
            if (attibute != null)
            {
                return attibute.DetailType;
            }
            return null;
        }

        private List<ResponseDocumentation> GetObjectFaultResponses(Type type)
        {
            List<ResponseDocumentation> faultResponses = GetErrorResponses(InformationHelper.GetCustomAttributes<RestFaultContractAttribute>(type));

            return faultResponses;
        }

        private List<ResponseDocumentation> GetErrorResponses(IEnumerable<RestFaultContractAttribute> restFaultContractAttributes)
        {
            List<ResponseDocumentation> faultResponses = new List<ResponseDocumentation>();
            if (restFaultContractAttributes != null)
            {
                foreach (var restFaultContractAttribute in restFaultContractAttributes)
                {
                    var responseDocumentation = new ResponseDocumentation()
                    {
                        Code = restFaultContractAttribute.Code,
                        Description = restFaultContractAttribute.Description,
                        Example = _exampleProvider.GetExample(restFaultContractAttribute.HelpExampleType, restFaultContractAttribute.DetailType),
                        Type = restFaultContractAttribute.DetailType,
                        HelpExampleType = restFaultContractAttribute.HelpExampleType,
                        Disabled = restFaultContractAttribute.Disabled,
                        ParamDocumentation = GetMethodParamaterDocumentation(null, null, restFaultContractAttribute.DetailType, null, null)
                    };

                    faultResponses.Add(responseDocumentation);
                }
            }

            return faultResponses;
        }

        private List<ResponseDocumentation> BuildOperationErrorResponses(List<ResponseDocumentation> contractDocumentationErrorResponses, List<ResponseDocumentation> methodErrorResponses)
        {
            if (contractDocumentationErrorResponses == null)
            {
                return methodErrorResponses;
            }

            if (methodErrorResponses == null)
            {
                return contractDocumentationErrorResponses;
            }

            List<ResponseDocumentation> result = new List<ResponseDocumentation>();

            foreach (var contractDocumentationErrorResponse in contractDocumentationErrorResponses)
            {
                var foundResponse = methodErrorResponses.FirstOrDefault(mer => mer.Code == contractDocumentationErrorResponse.Code);

                if (foundResponse != null)
                {
                    if (!foundResponse.Disabled)
                        result.Add(foundResponse);
                }
                else
                {
                    if (!contractDocumentationErrorResponse.Disabled)
                    {
                        result.Add(contractDocumentationErrorResponse);
                    }
                }
            }

            result.AddRange(methodErrorResponses.Where(mer => !mer.Disabled && !contractDocumentationErrorResponses.Any(cder => cder.Code == mer.Code)));

            return result.OrderBy(rd => rd.Code).ToList();
        }

        private ApiInformation GetApiInformation(Type type)
        {
            var apiInfo = type.GetCustomAttribute<RestApiAttribute>(true);
            return apiInfo == null ? null : new ApiInformation
            {
                ApiVersion = apiInfo.ApiVersion,
                LicenseTitle = apiInfo.LicenseTitle,
                LicenseUrl = apiInfo.LicenseUrl
            };
        }
    }

    internal interface ICommentProvider
    {
        MethodDocumentation GetMethodDocumentation(OperationInformation operation);
    }

    internal interface IResponseProvider
    {
        List<ResponseDocumentation> GetMethodErrorResponses(MethodInfo operationMethodInfo);
    }

    internal class AssemblyComparer : IEqualityComparer<Assembly>
    {
        #region Implementation of IEqualityComparer<in Assembly>

        /// <summary>Determines whether the specified objects are equal.</summary>
        /// <returns>true if the specified objects are equal; otherwise, false.</returns>
        /// <param name="x">The first object of type <paramref name="T" /> to compare.</param>
        /// <param name="y">The second object of type <paramref name="T" /> to compare.</param>
        public bool Equals(Assembly x, Assembly y)
        {
            if (ReferenceEquals(null, x) && ReferenceEquals(null, y))
                return true;
            if (ReferenceEquals(null, x) || ReferenceEquals(null, y))
                return false;
            if (ReferenceEquals(x, y))
                return true;
            return x.FullName == y.FullName;
        }

        /// <summary>Returns a hash code for the specified object.</summary>
        /// <returns>A hash code for the specified object.</returns>
        /// <param name="obj">The <see cref="T:System.Object" /> for which a hash code is to be returned.</param>
        /// <exception cref="T:System.ArgumentNullException">The type of <paramref name="obj" /> is a reference type and <paramref name="obj" /> is null.</exception>
        public int GetHashCode(Assembly obj)
        {
            return obj.FullName.GetHashCode();
        }

        #endregion
    }
}
