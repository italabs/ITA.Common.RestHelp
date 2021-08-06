using System.Linq;
using System.ServiceModel.Description;
using System.Xml.Linq;
using ITA.Common.RestHelp.Data;
using ITA.Common.RestHelp.Interfaces;

namespace ITA.Common.RestHelp.Views
{
    internal abstract class HtmlBaseHelpView : BaseHelpView
    {
        public const string RESOURCE_FOLDER_NAME = "Resources";
        public const string STYLES_CSS_FILE_NAME = "styles.css";
        public const string SCRIPT_JS_FILE_NAME = "scripts.js";
        public const string JQUERY_TREEVIEW_CSS_FILE_NAME = "jquery_treeview_css.css";
        public const string JQUERY_TREEVIEW_JS_FILE_NAME = "jquery_treeview_js.js";
        public const string JQUERY_JS_FILE_NAME = "jquery_3_2_1_min.js";

        public const string IMAGE_FILE_NAME = "file.gif";
        public const string IMAGE_FOLDER_NAME = "folder.gif";
        public const string IMAGE_FOLDER_CLOSED_NAME = "folder_closed.gif";
        public const string IMAGE_TREEVIEW_DEFAULT_NAME = "treeview_default.gif";
        public const string IMAGE_TREEVIEW_DEFAULT_LINE_NAME = "treeview_default_line.gif";        

        public HtmlBaseHelpView(ContractInformation model, ServiceEndpoint endpoint, IUriHelper uriHelper)
            : base(model, endpoint, uriHelper)
        {
        }

        protected XDocument CreateBaseDocument(string title)
        {
            return new XDocument(
                new XDocumentType(HtmlHtmlKey, HtmlPublicId, HtmlSystemId, null),
                new XElement(HtmlHtmlElementName,
                    new XElement(HtmlHeadElementName,
                        new XElement(HtmlTitleElementName, title),
                        new XElement(HtmlMetaElementName, new XAttribute(HtmlCharsetAttributeName, HtmlCharset)),
                        new XElement(HtmlStyleLinkElementName,
                            new XAttribute(HtmlHrefAttributeName, UriHelper.GetRelativeUrl(GetFilePath(STYLES_CSS_FILE_NAME))),
                            new XAttribute(HtmlRelAttributeName, HtmlStylesheetClass), string.Empty),
                        new XElement(HtmlStyleLinkElementName,
                            new XAttribute(HtmlHrefAttributeName, UriHelper.GetRelativeUrl(GetFilePath(JQUERY_TREEVIEW_CSS_FILE_NAME))),
                            new XAttribute(HtmlRelAttributeName, HtmlStylesheetClass), string.Empty),
                        new XElement(HtmlScriptElementName,
                            new XAttribute(HtmlSrcClass, UriHelper.GetRelativeUrl(GetFilePath(JQUERY_JS_FILE_NAME))),
                            new XAttribute(HtmlTypeAttributeName, HtmlTextJavascript), string.Empty),
                        new XElement(HtmlScriptElementName,
                            new XAttribute(HtmlSrcClass, UriHelper.GetRelativeUrl(GetFilePath(JQUERY_TREEVIEW_JS_FILE_NAME))),
                            new XAttribute(HtmlTypeAttributeName, HtmlTextJavascript), string.Empty),
                        new XElement(HtmlScriptElementName,
                            new XAttribute(HtmlSrcClass, UriHelper.GetRelativeUrl(GetFilePath(SCRIPT_JS_FILE_NAME))),
                            new XAttribute(HtmlTypeAttributeName, HtmlTextJavascript), string.Empty)),
                    new XElement(HtmlBodyElementName)));
        }

        protected XDocument SetBody(XDocument document, XElement content)
        {
            document.Descendants(HtmlBodyElementName).First().Add(content);
            return document;
        }

        protected string GetString(string format, params object[] args)
        {
            return InformationHelper.GetString(format, args);
        }

        protected string GetFilePath(string fileName)
        {
            return string.Format("{0}/{1}", RESOURCE_FOLDER_NAME, fileName);
        }
    }

    public static class XElementExtension
    {
        public static XElement AddElement(this XElement element, XElement value)
        {
            if (value == null)
                return element;
            element.Add(value);
            return element;
        }

        public static XElement AddBlock(this XElement element, string key, string value)
        {
            element.Add(new XElement(BaseHelpView.HtmlPElementName, new XElement(BaseHelpView.HtmlBoldElementName, key),
                new XElement(BaseHelpView.HtmlSpanElementName, value)));
            return element;
        }

        public static XElement AddBlockIf(this XElement element, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return element;

            return element.AddBlock(key, value);
        }
    }
}