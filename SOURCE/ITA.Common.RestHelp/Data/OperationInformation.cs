using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using ITA.Common.RestHelp.Examples;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ITA.Common.RestHelp.Data
{
    [DataContract]
    [KnownType(typeof(OperationInformationEx))]
    internal class OperationInformation
    {
        public const HttpStatusCode SuccessCode = HttpStatusCode.OK;

        public OperationInformation(OperationDescription operationDescription)
        {
            Name = operationDescription.Name;
            MethodInfo = operationDescription.SyncMethod;
            Method = InformationHelper.GetOperationMethod(operationDescription);
            UriTemplate = InformationHelper.GetUriTemplateOrDefault(operationDescription);
            BodyStyle = InformationHelper.GetBodyStyle(operationDescription);
            IsRequestWrapped = BodyStyle == WebMessageBodyStyle.WrappedRequest ||
                               BodyStyle == WebMessageBodyStyle.Wrapped;
            IsResponseWrapped = BodyStyle == WebMessageBodyStyle.WrappedResponse ||
                                BodyStyle == WebMessageBodyStyle.Wrapped;
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Method { get; set; }

        [DataMember]
        public string UriTemplate { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public MethodDocumentation MethodDocumentation { get; set; }

        [DataMember]
        public WebMessageBodyStyle BodyStyle { get; private set; }

        [DataMember]
        public string JsonRequestSample { get; private set; }

        [DataMember]
        public string JsonResponseSample { get; private set; }

        [DataMember]
        public string JsonErrorResponseSample { get; private set; }

        public bool IsRequestWrapped { get; set; }

        public bool IsResponseWrapped { get; set; }

        public MethodInfo MethodInfo { get; set; }

        public void LoadDocumentation(ICommentProvider commentProvider, IResponseProvider responseProvider)
        {
            MethodDocumentation = commentProvider.GetMethodDocumentation(this);

            Description = MethodDocumentation.Summary;

            JsonRequestSample = GetJsonRequest();

            JsonResponseSample = GetJsonResponse();

            JsonErrorResponseSample = GetJsonErrorResponse();

            CreateResponses(responseProvider);
        }

        private void CreateResponses(IResponseProvider responseProvider)
        {
            ErrorResponses = responseProvider.GetMethodErrorResponses(MethodInfo);

            SuccessResponse = new ResponseDocumentation()
            {
                Code = SuccessCode,
                Description = MethodDocumentation.OutputParameter == null || string.IsNullOrEmpty(MethodDocumentation.OutputParameter.Summary) 
                    ? Resources.HelpPageSuccessOperationDescription 
                    : MethodDocumentation.OutputParameter.Summary,
                ParamDocumentation = MethodDocumentation.OutputParameter,
                Type = MethodDocumentation.OutputParameter != null ? MethodDocumentation.OutputParameter.ParameterType : null,
                Example = JsonResponseSample,
                HelpExampleType = HelpExampleType.Output
            };
        }

        protected virtual string GetJsonRequest()
        {
            var inputJsonParams = MethodDocumentation.InputParameters.Where(p => p.ParamPlace == MethodParamPlace.Body)
                .Select(p => p.ToJson()).ToArray();

            if (!inputJsonParams.Any())
            {
                return null;
            }

            return GetJsonString(inputJsonParams, IsRequestWrapped);
        }

        protected virtual string GetJsonResponse()
        {
            var outputJson = MethodDocumentation.OutputParameter.ToJson();
            if (outputJson == null)
            {
                return null;
            }
            return GetJsonString(new[] { outputJson }, false);
        }

        protected virtual string GetJsonErrorResponse()
        {
            if (MethodDocumentation.ErrorOutputParameter == null)
                return null;

            var errorOutputJson = MethodDocumentation.ErrorOutputParameter.ToJson();
            if (errorOutputJson == null)
            {
                return null;
            }
            return GetJsonString(new[] { errorOutputJson }, false);
        }

        protected string GetJsonString(JToken[] tokens, bool isWrapped)
        {
            if (tokens.Length == 1 && !isWrapped)
            {
                var token = tokens[0];
                var jproperty = token as JProperty;
                if (jproperty != null)
                {
                    return jproperty.Value.ToString(Formatting.Indented);
                }
                return token.ToString(Formatting.Indented);
            }

            var jobject = new JObject(tokens);
            return jobject.ToString(Formatting.Indented);
        }

        [DataMember]
        public List<ResponseDocumentation> ErrorResponses { get; set; }

        [DataMember]
        public ResponseDocumentation SuccessResponse { get; set; }
    }
}