using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using ITA.Common.RestHelp.Data;
using ITA.Common.RestHelp.Interfaces;
using ITA.Common.RestHelp.SwaggerHelpers;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace ITA.Common.RestHelp.Views
{
    /// <summary>
    /// Swagger help view implementation
    /// </summary>
    internal class SwaggerDocumentView : IHelpView
    {
        private const string BASIC_HTTP_AUTH = "http_basic";
        private const string BEARER_HTTP_AUTH = "Bearer";
        private readonly ContractInformation _model;
        private readonly ServiceEndpoint _endpoint;
        private readonly IUriHelper _uriHelper;
        private readonly SchemaReferenceRegistry _registry;
        private readonly SwaggerVersion _version;

        public SwaggerDocumentView(
            ContractInformation model,
            ServiceEndpoint endpoint,
            IUriHelper uriHelper,
            SwaggerVersion version)
        {
            _endpoint = endpoint;
            _uriHelper = uriHelper;
            _model = model;
            _registry = new SchemaReferenceRegistry();
            _version = version;
        }

        /// <summary>
        /// Response content type
        /// </summary>
        public string ContentType
        {
            get { return "application/json"; }
        }

        /// <summary>
        /// Generate swagger output json
        /// </summary>
        /// <param name="template"></param>
        /// <param name="uri"></param>
        /// <returns></returns>
        public Message Show(UriTemplate template, Uri uri)
        {
            try
            {
                var contentType = ContentType;
                var swaggerJson = CreateSwaggerDocument();

                var response = WebOperationContext.Current.OutgoingResponse;

                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, X-Requested-With");

                var memoryStream = new MemoryStream();

                swaggerJson.SerializeAsJson(memoryStream, _version.ToApiSpecVerison());
                memoryStream.Seek(0, SeekOrigin.Begin);

                return WebOperationContext.Current.CreateStreamResponse(memoryStream, contentType);
            }
            catch (Exception exc)
            {
                throw new RestHelpException(ErrorMessages.E_SWAGGER_HELP_GENERATING_ERROR, exc);
            }
        }

        public string GenerateContent()
        {
            var swaggerJson = CreateSwaggerDocument();
            return swaggerJson.Serialize(_version.ToApiSpecVerison(), OpenApiFormat.Json);
        }

        /// <summary>
        /// Generate Open API document
        /// </summary>
        /// <returns></returns>
        private OpenApiDocument CreateSwaggerDocument()
        {
            var document = new OpenApiDocument();
            document.Components = new OpenApiComponents();

            AddApiInfo(document);
            AddServers(document);
            AddAuthenticationSchemes(document);
            AddOperations(document);
            AddComponents(document);
            AddOperationsSecurity(document);

            return document;
        }

        /// <summary>
        /// Add security options for operation
        /// </summary>
        /// <param name="document"></param>
        private void AddOperationsSecurity(OpenApiDocument document)
        {
            foreach (var documentPath in document.Paths)
            {
                foreach (var operation in documentPath.Value.Operations)
                {
                    var modelOperation = _model.Operations.FirstOrDefault(x => x.Name == operation.Value.OperationId);
                    if (modelOperation == null ||
                        modelOperation.MethodDocumentation == null ||
                        modelOperation.MethodDocumentation.AuthorizationType == RestAuthorizationType.None)
                    {
                        continue;
                    }
                    operation.Value.Security = CreateSecuritySchemaReference(modelOperation.MethodDocumentation.AuthorizationType);
                }
            }
        }

        private List<OpenApiSecurityRequirement> CreateSecuritySchemaReference(RestAuthorizationType type)
        {
            var result = new List<OpenApiSecurityRequirement>();
            if (type == RestAuthorizationType.None)
            {
                return result;
            }
            var authSchemaNames = new List<string>();
            if (type.HasFlag(RestAuthorizationType.Basic))
            {
                authSchemaNames.Add(BASIC_HTTP_AUTH);
            }
            if (type.HasFlag(RestAuthorizationType.Bearer))
            {
                authSchemaNames.Add(BEARER_HTTP_AUTH);
            }
            foreach (var schema in authSchemaNames)
            {
                var securityRequirement = new OpenApiSecurityRequirement();
                securityRequirement.Add(new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference()
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = schema
                    }
                }, new List<string>());
                result.Add(securityRequirement);
            }

            return result;
        }

        /// <summary>
        /// Add authentication schemes
        /// </summary>
        /// <param name="document"></param>
        private void AddAuthenticationSchemes(OpenApiDocument document)
        {
            if (_model.Operations.Any(x => x.MethodDocumentation.AuthorizationType.HasFlag(RestAuthorizationType.Basic)))
            {
                document.Components.SecuritySchemes.Add(BASIC_HTTP_AUTH, new OpenApiSecurityScheme()
                {
                    Scheme = "basic",
                    Type = SecuritySchemeType.Http
                });
            }
            if (_model.Operations.Any(x => x.MethodDocumentation.AuthorizationType.HasFlag(RestAuthorizationType.Bearer)))
            {
                document.Components.SecuritySchemes.Add(BEARER_HTTP_AUTH, new OpenApiSecurityScheme()
                {
                    Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter your JWT token in the text input below.",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer"
                });
            }
        }

        /// <summary>
        /// Add components to Open API document
        /// </summary>
        /// <param name="document"></param>
        private void AddComponents(OpenApiDocument document)
        {
            foreach (var registryReference in _registry.References)
            {
                document.Components.Schemas.Add(registryReference.Key, registryReference.Value);
            }
        }

        /// <summary>
        /// Add operations to Open API document
        /// </summary>
        /// <param name="document"></param>
        private void AddOperations(OpenApiDocument document)
        {
            var paths = new OpenApiPaths();

            var actualOperations = FilterOperations(_model.Operations);

            foreach (var operation in actualOperations.OrderBy(op => op.UriTemplate))
            {
                var path = operation.UriTemplate.Contains("?") ? operation.UriTemplate.Substring(0, operation.UriTemplate.IndexOf("?")) : operation.UriTemplate;

                var openApiPathItem = paths.ContainsKey(path) ? paths[path] : new OpenApiPathItem();

                var openApiOperation = CreateOperation(operation);

                openApiOperation.Tags = new List<OpenApiTag>(new OpenApiTag[]
                {
                    new OpenApiTag()
                    {
                        Name = GetUriFirstSegment(path)
                    }
                });

                openApiPathItem.Operations.Add(operation.Method.MethodToOperationType(), openApiOperation);

                if (!paths.ContainsKey(path))
                {
                    paths.Add(path, openApiPathItem);
                }
            }

            document.Paths = paths;
        }

        /// <summary>
        /// Filters the irrelevant operations.
        /// </summary>
        /// <param name="operations">The operations.</param>
        /// <returns>The actual operations.</returns>
        private IEnumerable<OperationInformation> FilterOperations(IEnumerable<OperationInformation> operations)
        {
            foreach (var operation in operations)
            {
                if (operation.Method.MethodToOperationType() == OperationType.Options)
                {
                    continue;
                }

                if (operation.UriTemplate.Contains("*"))
                {
                    continue;
                }

                yield return operation;
            }
        }

        /// <summary>
        /// Get parameter Open API scheme
        /// </summary>
        /// <param name="paramDoc"></param>
        /// <returns></returns>
        private OpenApiSchema GetParameterScheme(MethodParamDocumentation paramDoc)
        {
            return _registry.FindOrAddReference(paramDoc.ParameterType, paramDoc.TypeDocumentation);
        }

        /// <summary>
        /// Get first segment from uri for operation grouping tag
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetUriFirstSegment(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            return path.Split('/').Select(segment =>
                {
                    if (segment == null)
                    {
                        return segment;
                    }

                    segment = segment.Trim().Trim('/');
                    return segment;
                })
                .FirstOrDefault(segment => !string.IsNullOrEmpty(segment));
        }

        /// <summary>
        /// Create Open API operation
        /// </summary>
        /// <param name="operation"></param>
        /// <returns></returns>
        private OpenApiOperation CreateOperation(OperationInformation operation)
        {
            var methodDoc = operation.MethodDocumentation;
            var result = new OpenApiOperation
            {
                OperationId = operation.Name,
                Description = operation.Description,
                Summary = methodDoc.Summary,
                Parameters = new List<OpenApiParameter>()
            };
            foreach (var methodParamDocumentation in methodDoc.InputParameters)
            {
                if (methodParamDocumentation.ParamPlace == MethodParamPlace.Body)
                {
                    result.RequestBody = new OpenApiRequestBody()
                    {
                        Description = methodParamDocumentation.Summary,
                        Required = methodParamDocumentation.IsRequired,
                        Content = new Dictionary<string, OpenApiMediaType>()
                        {
                            {"application/json", new OpenApiMediaType()
                            {
                                Schema = GetParameterScheme(methodParamDocumentation),
                                Example = string.IsNullOrEmpty(operation.JsonRequestSample) ? null
                                    : new OpenApiString(operation.JsonRequestSample)
                            }}
                        }
                    };
                }
                else
                {
                    var parameter = new OpenApiParameter
                    {
                        In = methodParamDocumentation.ParamPlace.ToParameterLocation(),
                        Name = methodParamDocumentation.ParameterName,
                        Description = methodParamDocumentation.Summary,
                        Required = methodParamDocumentation.ParamPlace.ToParameterLocation() == ParameterLocation.Path || methodParamDocumentation.IsRequired,
                        Schema = GetParameterScheme(methodParamDocumentation),
                    };
                    result.Parameters.Add(parameter);
                }
            }

            result.Responses.Add(((int)OperationInformation.SuccessCode).ToString(), CreateSuccessfullResponse(operation));

            var failureResponses = CreateFailureResponses(operation);
            foreach (var failureResponse in failureResponses)
            {
                result.Responses.Add(failureResponse.Key, failureResponse.Value);
            }

            return result;
        }

        /// <summary>
        /// Creates successfull response for Open API document
        /// </summary>
        /// <param name="operation"></param>
        /// <returns></returns>
        private OpenApiResponse CreateSuccessfullResponse(OperationInformation operation)
        {
            if (operation.SuccessResponse != null)
            {
                var response = new OpenApiResponse()
                {
                    Description = operation.SuccessResponse.Description
                };

                if (operation.SuccessResponse.Type != null &&
                    !operation.SuccessResponse.Type.FullName.Equals("System.Void",
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    response.Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            "application/json", new OpenApiMediaType()
                            {
                                Schema = GetParameterScheme(operation.SuccessResponse.ParamDocumentation),
                                Example = string.IsNullOrEmpty(operation.SuccessResponse.Example)
                                    ? null
                                    : new OpenApiString(operation.SuccessResponse.Example)
                            }
                        }
                    };
                }

                return response;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Creates failure responses for Open API document
        /// </summary>
        /// <param name="operation"></param>
        /// <returns></returns>
        private Dictionary<string, OpenApiResponse> CreateFailureResponses(OperationInformation operation)
        {
            Dictionary<string, OpenApiResponse> failureResponses = new Dictionary<string, OpenApiResponse>();

            foreach (var responseDocumentation in operation.ErrorResponses)
            {
                failureResponses.Add(((int)responseDocumentation.Code).ToString(), new OpenApiResponse()
                {
                    Description = responseDocumentation.Description,
                    Content = !string.IsNullOrEmpty(responseDocumentation.Example)
                        ? new Dictionary<string, OpenApiMediaType>
                        {
                            {
                                "application/json", new OpenApiMediaType()
                                {
                                    Schema = responseDocumentation.Type != null ? GetParameterScheme(responseDocumentation.ParamDocumentation) : null,
                                    Example = new OpenApiString(responseDocumentation.Example)
                                }
                            }
                        }
                        : null
                });
            }

            return failureResponses;
        }

        /// <summary>
        /// Add servers description to Open API document
        /// </summary>
        /// <param name="document"></param>
        private void AddServers(OpenApiDocument document)
        {
            document.Servers = new List<OpenApiServer>
            {
                new OpenApiServer {Url = _endpoint.Address.Uri.ToString()}
            };
        }

        /// <summary>
        /// Add API information to Open API document
        /// </summary>
        /// <param name="document"></param>
        private void AddApiInfo(OpenApiDocument document)
        {
            document.Info = new OpenApiInfo
            {
                Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                Title = _model.ContractDocumentation.Summary
            };

            if (_model.ApiInfo != null)
            {
                if (!string.IsNullOrWhiteSpace(_model.ApiInfo.ApiVersion))
                {
                    document.Info.Version = _model.ApiInfo.ApiVersion;
                }

                if (!string.IsNullOrWhiteSpace(_model.ApiInfo.LicenseTitle) &&
                    !string.IsNullOrWhiteSpace(_model.ApiInfo.LicenseUrl))
                {
                    Uri resultUri = null;

                    Uri.TryCreate(_model.ApiInfo.LicenseUrl, UriKind.RelativeOrAbsolute, out resultUri);

                    document.Info.License = new OpenApiLicense
                    {
                        Name = _model.ApiInfo.LicenseTitle,
                        Url = resultUri
                    };
                }
            }
        }
    }
}
