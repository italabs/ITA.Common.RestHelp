using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Description;
using System.Xml.Linq;
using ITA.Common.RestHelp.Data;
using ITA.Common.RestHelp.Interfaces;

namespace ITA.Common.RestHelp.Views
{
    internal class HtmlContractHelpView : HtmlBaseHelpView
    {
        private readonly List<HelpPageExtension> _pageExtensions;

        public HtmlContractHelpView(ContractInformation model, ServiceEndpoint endpoint, IUriHelper uriHelper, IHelpExtensions extensions)
            : base(model, endpoint, uriHelper)
        {
            _pageExtensions = extensions != null 
                ? extensions.GetExtensions(HelpPageExtensionType.Page)
                : null;
        }

        internal override string GenerateContent()
        {
            var document = CreateBaseDocument(Resources.HelpPageContractTitle);

            var table = new XElement(HtmlTableElementName, 
                new XElement(HtmlTrElementName,
                    new XElement(HtmlThElementName, Resources.HelpPageTableName),
                    new XElement(HtmlThElementName, Resources.HelpPageTableUri),
                    new XElement(HtmlThElementName, Resources.HelpPageTableMethod),
                    new XElement(HtmlThElementName, Resources.HelpPageTableDescription)));

            foreach (var info in Model.Operations.OrderBy(o => o.UriTemplate).GroupBy(o => o.UriTemplate))
            {
                var list = info.ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    var row = new XElement(HtmlTrElementName);

                    var operationPath = string.Format(HelpPageBehavior.OPERATION_URI_TEMPLATE.Replace("{" + HelpPageBehavior.OPERATION_URI_PARAM + "}", "{0}"), list[i].Name);

                    row.Add(new XElement(HtmlTdElementName,
                        new XAttribute(HtmlTitleAttributeName, list[i].Name),
                        new XElement(HtmlAElementName,
                            new XAttribute(HtmlRelAttributeName, HtmlInputOperationClass),
                            new XAttribute(HtmlHrefAttributeName, UriHelper.GetRelativeUrl(operationPath)),
                            list[i].Name)));
                        
                    if (i == 0)
                    {                        
                        row.Add(new XElement(HtmlTdElementName, info.Key, new XAttribute(HtmlRowspanAttributeName, list.Count)));
                    }

                    row.Add(new XElement(HtmlTdElementName, list[i].Method));
                    row.Add(new XElement(HtmlTdElementName, list[i].Description));

                    table.Add(row);                        
                }
            }
            
            var body = new XElement(HtmlDivElementName, 
                new XAttribute(HtmlIdAttributeName, HtmlContentClass), 
                new XElement(HtmlPElementName, 
                    new XAttribute(HtmlClassAttributeName, HtmlHeading1Class),
                    GetString(Resources.HelpPageHome, Model.ContractDocumentation.Type.Name)));
            
            var xmlMenuItem = new XElement(HtmlAElementName,
                    new XAttribute(HtmlRelAttributeName, HtmlInputOperationClass),
                    new XAttribute(HtmlTargerAttributeName, HtmlBlankClass),
                    new XAttribute(HtmlHrefAttributeName, UriHelper.GetRelativeUrl(HelpPageBehavior.XML_PAGE_URI)), Resources.HelpPageXmlRefValue);

            if (_pageExtensions != null)
            {
                body.Add(new XElement(HtmlPElementName, xmlMenuItem));
                foreach (var extension in _pageExtensions)
                {
                    body.Add(new XElement(HtmlPElementName, new XElement(HtmlAElementName,
                        new XAttribute(HtmlRelAttributeName, HtmlInputOperationClass),
                        new XAttribute(HtmlHrefAttributeName, UriHelper.GetRelativeUrl(extension.RelativeUrl)),
                        extension.MenuCaption ?? extension.Title)));
                }
            }
            else
            {
                body.Add(xmlMenuItem);
            }

            if (!string.IsNullOrWhiteSpace(Model.ContractDocumentation.Summary))
            {
                body.Add(new XElement(HtmlPElementName, new XElement(HtmlBoldElementName, Resources.HelpPageContractDescription)));
                body.Add(new XElement(HtmlPElementName, Model.ContractDocumentation.Summary));
            }

            body.Add(new XElement(HtmlPElementName, new XElement(HtmlBoldElementName, Resources.HelpPageBaseUri)));
            body.Add(new XElement(HtmlPElementName, UriHelper.GetHostAbsoluteUrl(string.Empty)));

            body.Add(new XElement(HtmlPElementName, new XElement(HtmlBoldElementName, Resources.HelpPageContractOperationList)));
            body.Add(table);

            return SetBody(document, body).ToString();
        }
    }
}