using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel.Description;
using ITA.Common.RestHelp.Examples;
using ITA.Common.RestHelp.Interfaces;
using Newtonsoft.Json.Linq;

namespace ITA.Common.RestHelp.Data
{
    [DataContract]
    internal class OperationInformationEx : OperationInformation
    {
        private readonly IHelpExampleProvider _exampleProvider;

        public OperationInformationEx(OperationDescription operationDescription, IHelpExampleProvider exampleProvider)
            : base(operationDescription)
        {
            _exampleProvider = exampleProvider;
        }

        #region Overrides of OperationInformation

        protected override string GetJsonRequest()
        {
            var inputJsonParams = MethodDocumentation.InputParameters
                .Where(p => p.ParamPlace == MethodParamPlace.Body)
                .Select(x => GetExample(x, HelpExampleType.Input))
                .ToArray();

            return !inputJsonParams.Any() ? null : GetJsonString(inputJsonParams, IsRequestWrapped);
        }

        protected override string GetJsonResponse()
        {
            var outputJson = GetExample(MethodDocumentation.OutputParameter, HelpExampleType.Output) ?? MethodDocumentation.OutputParameter.ToJson();
            return outputJson == null ? null : GetJsonString(new[] { outputJson }, false);
        }

        protected override string GetJsonErrorResponse()
        {
            if (MethodDocumentation.ErrorOutputParameter == null)
                return null;

            var exampleType = HelpExampleType.Output;
            var attributes = InformationHelper.GetCustomAttribute<RestFaultContractAttribute>(MethodInfo);
            if (attributes != null)
            {
                exampleType = attributes.HelpExampleType;
            }
            var errorOutputJson = GetExample(MethodDocumentation.ErrorOutputParameter, exampleType) ?? MethodDocumentation.ErrorOutputParameter.ToJson();
            return errorOutputJson == null ? null : GetJsonString(new[] { errorOutputJson }, false);
        }

        #endregion

        private JToken GetExample(MethodParamDocumentation parameter, HelpExampleType currentType)
        {
            ParameterInfo methodParam = null;
            var exampleType = currentType;

            if (currentType == HelpExampleType.Input)
            {
                methodParam = MethodDocumentation.Method.GetParameters()
                    .FirstOrDefault(x => x.Name == parameter.ParameterName);
                if (methodParam != null)
                {
                    var exampleAttribute = methodParam.GetCustomAttributes(typeof(RestExampleAttribute), false)
                        .OfType<RestExampleAttribute>()
                        .ToList();
                    if (exampleAttribute.Any())
                    {
                        exampleType = exampleAttribute.First().ExampleType;
                    }
                }
            }
            else if (currentType == HelpExampleType.Output)
            {
                var exampleAttribute = MethodDocumentation.Method.ReturnTypeCustomAttributes.GetCustomAttributes(typeof(RestExampleAttribute), false)
                    .OfType<RestExampleAttribute>()
                    .ToList();
                if (exampleAttribute.Any())
                {
                    exampleType = exampleAttribute.First().ExampleType;
                }
            }

            var exampleStr = _exampleProvider.GetExample(exampleType, parameter.ParameterType);

            return !string.IsNullOrWhiteSpace(exampleStr) ? JToken.Parse(exampleStr) : parameter.ToJson();
        }
    }
}