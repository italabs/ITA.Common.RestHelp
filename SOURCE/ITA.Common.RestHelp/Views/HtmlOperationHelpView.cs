using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Xml.Linq;
using ITA.Common.RestHelp.Data;
using ITA.Common.RestHelp.Interfaces;

namespace ITA.Common.RestHelp.Views
{
    internal class HtmlOperationHelpView : HtmlBaseHelpView
    {
        private static readonly ConcurrentDictionary<Uri, string> CachedContent = new ConcurrentDictionary<Uri, string>();
        private readonly List<HelpPageExtension> _extensionsHtmlBlock;
        private readonly List<HelpPageExtension> _extensionsExamples;

        public HtmlOperationHelpView(ContractInformation model, ServiceEndpoint endpoint, IUriHelper uriHelper, IHelpExtensions extensions)
            : base(model, endpoint, uriHelper)
        {
            _extensionsHtmlBlock = extensions != null ? extensions.GetExtensions(HelpPageExtensionType.OperationBlock) : null;
            _extensionsExamples = extensions != null ? extensions.GetExtensions(HelpPageExtensionType.ExampleBlock) : null;
        }

        public override Message Show(UriTemplate template, Uri uri)
        {
            if (WebOperationContext.Current == null)
            {
                return null;
            }
            var match = template.Match(Endpoint.Address.Uri, uri);
            if (match == null)
            {
                return null;
            }
            var operationName = match.BoundVariables[HelpPageBehavior.OPERATION_URI_PARAM];
            var content = CachedContent.GetOrAdd(uri, u => GenerateContent(operationName));
            return WebOperationContext.Current.CreateTextResponse(content, ContentType);
        }

        internal string GenerateContent(string operationName)
        {
            var document = CreateBaseDocument(Resources.HelpPageOperationTitle);
            var operation = Model.Operations.FirstOrDefault(o => o.Name == operationName);
            if (operation == null)
            {
                return string.Empty;
            }

            var operationUrl = UriHelper.GetHostAbsoluteUrl(operation.UriTemplate);

            var body = new XElement(HtmlDivElementName,
                new XAttribute(HtmlIdAttributeName, HtmlContentClass),
                new XElement(HtmlPElementName,
                    new XAttribute(HtmlClassAttributeName, HtmlHeading1Class),
                    GetString(Resources.HelpPageReferenceFor, operation.Name)),
                new XElement(HtmlAElementName,
                    new XAttribute(HtmlRelAttributeName, HtmlInputOperationClass),
                    new XAttribute(HtmlHrefAttributeName, UriHelper.GetHelpexRelativeUrl()), Resources.HelpPageOperationsAt));

            body.AddBlockIf(Resources.HelpPageOperation, operationName)
                .AddBlockIf(Resources.HelpPageUri, operationUrl)
                .AddBlockIf(Resources.HelpPageDescription, operation.Description)
                .AddBlockIf(Resources.HelpPageMethod, operation.Method)
                .AddElement(ToInputTree(operation.MethodDocumentation))
                .AddElement(ToOutputTree(operation.MethodDocumentation));

            if (_extensionsHtmlBlock != null)
            {
                foreach (var extension in _extensionsHtmlBlock.OrderBy(x => x.Order))
                {
                    body.AddBlock(extension.Title, string.Empty).AddElement(XElement.Parse(extension.HtmlContent));
                }
            }

            body.AddBlock(Resources.HelpPageResponseTitle, string.Empty).AddElement(GetErrorResponseContent(operation));

            body.AddBlock(Resources.HelpPageExamples, string.Empty);

            CreateOperationSamples(body, operation);

            if (_extensionsExamples != null)
            {
                foreach (var extension in _extensionsExamples.OrderBy(x => x.Order))
                {
                    body.Add(AddSample(extension.HtmlContent, extension.Title, HtmlResponseJson));
                }
            }

            document = SetBody(document, body);
            return document.ToString();
        }

        #region Operation tree view

        private XElement GetErrorResponseContent(OperationInformation operation)
        {
            var div = new XElement(HtmlUlElementName);
            
            var responses = new List<ResponseDocumentation>();
            if (operation.SuccessResponse != null)
            {
                responses.Add(operation.SuccessResponse);
            }

            if (operation.ErrorResponses != null)
            {
                responses.AddRange(operation.ErrorResponses);
            }

            foreach (var operationErrorResponse in responses)
            {
                div.Add(new XElement(HtmlLiElementName, string.Format("{0} - {1}", (int)operationErrorResponse.Code, operationErrorResponse.Description)));
            }

            return div;
        }

        private XElement ToInputTree(MethodDocumentation method)
        {
            var mainUl = new XElement(HtmlUlElementName,
                new XAttribute(HtmlIdAttributeName, HtmlInputOperationClass),
                new XAttribute(HtmlClassAttributeName, HtmlFileTreeClass),
                new XElement(HtmlSpanElementName,
                    new XElement(HtmlBoldElementName, Resources.HelpPageParameters)));

            ToTree(mainUl, Resources.HelpPageInputUrlParameters,
                method.InputParameters.Where(p => p.ParamPlace == MethodParamPlace.Uri).ToArray());

            ToTree(mainUl, Resources.HelpPageInputBodyParameters,
                method.InputParameters.Where(p => p.ParamPlace == MethodParamPlace.Body).ToArray());

            ToTree(mainUl, Resources.HelpPageInputQueryParameters,
                method.InputParameters.Where(p => p.ParamPlace == MethodParamPlace.Query).ToArray());

            return new XElement(HtmlDivElementName, new XElement(HtmlPElementName, string.Empty), mainUl);
        }

        private XElement ToOutputTree(MethodDocumentation method)
        {
            if (method == null || (method.OutputParameter == null && method.ErrorOutputParameter == null))
            {
                return new XElement(HtmlDivElementName, new XElement(HtmlPElementName, string.Empty));
            }

            var mainUl = new XElement(HtmlUlElementName,
                new XAttribute(HtmlIdAttributeName, HtmlOutputOperationClass),
                new XAttribute(HtmlClassAttributeName, HtmlFileTreeClass),
                new XElement(HtmlSpanElementName,
                    new XElement(HtmlBoldElementName, Resources.HelpPageReturnValue)));

            if (method.OutputParameter != null && method.OutputParameter.TypeDocumentation != null)
            {
                ToTree(mainUl, Resources.HelpPageOutputSuccess, new[] {method.OutputParameter});
            }

            if (method.ErrorOutputParameter != null && method.ErrorOutputParameter.TypeDocumentation != null)
            {
                ToTree(mainUl, Resources.HelpPageOutputFailure, new[] {method.ErrorOutputParameter});
            }

            return new XElement(HtmlDivElementName, new XElement(HtmlPElementName, string.Empty), mainUl);
        }

        private void ToTree(XElement container, string name, MethodParamDocumentation[] parameters)
        {
            if (!parameters.Any())
                return;

            var inputParams = new XElement(HtmlLiElementName,
                new XElement(HtmlSpanElementName, name,
                    new XAttribute(HtmlClassAttributeName, HtmlFolderClass)));
            var inputParamsContainer = new XElement(HtmlUlElementName, string.Empty);
            parameters.ToList().ForEach(p => inputParamsContainer.Add(ToTree(p)));
            inputParams.Add(inputParamsContainer);
            container.Add(inputParams);
        }

        private XElement ToTree(MethodParamDocumentation param)
        {
            var paramElement = new XElement(HtmlBoldElementName, string.IsNullOrWhiteSpace(param.ParameterName) ? string.Empty : string.Format("{0}: ", param.ParameterName));
            if (param.IsRequired)
            {
                paramElement.SetAttributeValue(HtmlClassAttributeName, HtmlRequiredClass);
            }

            XElement li = null;
            if (param.IsRequired)
            {
                var asteriks = new XElement(HtmlBoldElementName, string.IsNullOrWhiteSpace(param.ParameterName)
                    ? string.Empty
                    : string.Format("{0}", param.ParameterName));
                asteriks.SetAttributeValue(HtmlClassAttributeName, HtmlRequiredClass);

                li = new XElement(HtmlLiElementName,
                    new XElement(HtmlSpanElementName,
                        asteriks,
                        new XElement(HtmlBoldElementName, ": "),
                        param.TypeDocumentation.GetFullName(),
                        new XAttribute(HtmlClassAttributeName, HtmlFileClass)));
            }
            else
            {
                li = new XElement(HtmlLiElementName,
                    new XElement(HtmlSpanElementName,
                        new XElement(HtmlBoldElementName,
                            string.IsNullOrWhiteSpace(param.ParameterName)
                                ? string.Empty
                                : string.Format("{0}: ", param.ParameterName)),
                        param.TypeDocumentation.GetFullName(),
                        new XAttribute(HtmlClassAttributeName, HtmlFileClass)));
            }


            if (!string.IsNullOrWhiteSpace(param.Summary))
            {
                li.Add(new XElement(HtmlSpanElementName, param.Summary));
            }

            if (param.TypeDocumentation != null && !param.TypeDocumentation.IsSystem)
            {
                li.Add(ToTree(param.TypeDocumentation, new Stack<string>()));
            }
            return li;
        }

        private XElement ToTree(TypeDocumentation type, Stack<string> visitNodes)
        {
            if (visitNodes.Contains(type.Type.FullName)) return null;

            var ul = new XElement(HtmlUlElementName, new XElement(HtmlSpanElementName, string.Empty));
            visitNodes.Push(type.Type.FullName);
            foreach (var property in type.Properties.Where(p => p.IsDataMemeber))
            {
                ul.Add(ToTree(property, visitNodes));
            }
            visitNodes.Pop();
            return ul;
        }

        private XElement ToTree(Type enumType)
        {
            var title = enumType.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0
                ? Resources.HelpPageBimaskEnumeration
                : Resources.HelpPageEnumeration;

            var ul = new XElement(HtmlUlElementName, new XElement(HtmlSpanElementName, new XElement(HtmlBoldElementName, title)));
            foreach (var property in Enum.GetValues(enumType))
            {
                ul.Add(new XElement(HtmlLiElementName,
                    new XElement(HtmlSpanElementName,
                        string.Format("{0}={1}", property.ToString(), Convert.ToInt32(property)))));
            }
            return ul;
        }

        private XElement ToTree(PropertyDocumentation property, Stack<string> visitNodes)
        {
            var name = string.IsNullOrWhiteSpace(property.DataMemberName) ? property.Property.Name : property.DataMemberName;

            XElement li = null;
            if (property.IsRequired)
            {
                var asteriks = new XElement(HtmlBoldElementName, string.Format("{0}", name));
                asteriks.SetAttributeValue(HtmlClassAttributeName, HtmlRequiredClass);

                li = new XElement(HtmlLiElementName,
                    new XElement(HtmlSpanElementName,
                        asteriks,
                        new XElement(HtmlBoldElementName, ": "),
                        property.TypeDocumentation.GetFullName()));
            }
            else
            {
                li = new XElement(HtmlLiElementName,
                    new XElement(HtmlSpanElementName,
                        new XElement(HtmlBoldElementName, string.Format("{0}: ", name)),
                        property.TypeDocumentation.GetFullName()));
            }

            if (!string.IsNullOrWhiteSpace(property.Summary))
            {
                li.Add(new XElement(HtmlBrElementName, new XElement(HtmlSpanElementName, property.Summary)));
            }
            if (property.TypeDocumentation != null && property.TypeDocumentation.Type != null && !property.TypeDocumentation.IsSystem && !visitNodes.Contains(property.TypeDocumentation.Type.FullName))
            {
                li.Add(!property.TypeDocumentation.IsEnum
                    ? ToTree(property.TypeDocumentation, visitNodes)
                    : ToTree(property.TypeDocumentation.Type));
            }
            return li;
        }

        #endregion

        #region Samples

        private static void CreateOperationSamples(XElement element, OperationInformation operationInfo)
        {
            if (!string.IsNullOrWhiteSpace(operationInfo.JsonRequestSample))
            {
                element.Add(AddSample(operationInfo.JsonRequestSample, Resources.HelpPageJsonRequest, HtmlRequestJson));
            }
            if (!string.IsNullOrWhiteSpace(operationInfo.JsonResponseSample))
            {
                element.Add(AddSample(operationInfo.JsonResponseSample, Resources.HelpPageJsonResponse, HtmlResponseJson));
            }

            foreach (var operationInfoErrorResponse in operationInfo.ErrorResponses)
            {
                if (!string.IsNullOrEmpty(operationInfoErrorResponse.Example))
                {
                    element.Add(AddSample(operationInfoErrorResponse.Example,
                        string.Format(Resources.HelpPageErrorResponseExampleTitle, (int)operationInfoErrorResponse.Code,
                            operationInfoErrorResponse.Description), HtmlResponseJson));
                }
            }
        }

        private static XElement AddSample(string content, string title, string label)
        {
            if (string.IsNullOrEmpty(title))
            {
                return new XElement(HtmlPElementName, new XElement(HtmlPreElementName, new XAttribute(HtmlClassAttributeName, label), content));
            }
            return new XElement(HtmlPElementName,
                new XElement(HtmlAElementName,
                    new XAttribute(HtmlNameClass, label), title),
                    new XElement(HtmlPreElementName, new XAttribute(HtmlClassAttributeName, label), content));
        }

        #endregion
    }
}