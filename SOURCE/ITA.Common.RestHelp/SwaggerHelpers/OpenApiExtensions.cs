using System;
using ITA.Common.RestHelp.Data;
using ITA.Common.RestHelp.Interfaces;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

namespace ITA.Common.RestHelp.SwaggerHelpers
{
    internal static class OpenApiExtensions
    {
        public static OperationType MethodToOperationType(this string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentNullException("method");
            }
            switch (method.ToUpper())
            {
                case "GET": return OperationType.Get;
                case "POST": return OperationType.Post;
                case "PUT": return OperationType.Put;
                case "DELETE": return OperationType.Delete;
                case "OPTIONS": return OperationType.Options;
            }
            throw new Exception(string.Format("Not supported method '{0}'", method));
        }

        public static ParameterLocation ToParameterLocation(this MethodParamPlace methodParamPlace)
        {
            switch (methodParamPlace)
            {
                case MethodParamPlace.Query: return ParameterLocation.Query;
                case MethodParamPlace.Uri: return ParameterLocation.Path;
                case MethodParamPlace.Header: return ParameterLocation.Header;
            }

            throw new Exception(string.Format("Not supported method parameter place '{0}'", methodParamPlace));
        }

        public static OpenApiSpecVersion ToApiSpecVerison(this SwaggerVersion version)
        {
            switch (version)
            {
                case SwaggerVersion.V2_0:
                {
                    return OpenApiSpecVersion.OpenApi2_0;
                }
                case SwaggerVersion.V3_0:
                {
                    return OpenApiSpecVersion.OpenApi3_0;
                }
                default: return OpenApiSpecVersion.OpenApi3_0;
            }
        }
    }
}
