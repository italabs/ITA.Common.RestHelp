using System.ServiceModel.Description;
using System.Xml.Linq;
using ITA.Common.RestHelp.Interfaces;

namespace ITA.Common.RestHelp.Views
{
    internal class HtmlExtensionHelpView : HtmlBaseHelpView
    {
        private readonly HelpPageExtension _page;

        public HtmlExtensionHelpView(ServiceEndpoint endpoint, IUriHelper uriHelper, HelpPageExtension page)
            : base(null, endpoint, uriHelper)
        {
            _page = page;
        }

        internal override string GenerateContent()
        {
            var document = CreateBaseDocument(Resources.HelpPageOperationTitle);

            var body = new XElement(HtmlDivElementName,
                new XAttribute(HtmlIdAttributeName, HtmlContentClass),
                new XElement(HtmlPElementName,
                    new XAttribute(HtmlClassAttributeName, HtmlHeading1Class),
                    _page.Title),
                new XElement(HtmlAElementName,
                    new XAttribute(HtmlRelAttributeName, HtmlInputOperationClass),
                    new XAttribute(HtmlHrefAttributeName, UriHelper.GetHelpexRelativeUrl()), "Back to main page"));

            body.Add(XElement.Parse(_page.HtmlContent));

            document = SetBody(document, body);

            return document.ToString();
        }
    }
}