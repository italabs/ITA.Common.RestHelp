using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using ITA.Common.RestHelp.Data;
using ITA.Common.RestHelp.Interfaces;

namespace ITA.Common.RestHelp.Views
{
    internal abstract class BaseHelpView : IHelpView
    {
        internal const string HtmlHtmlKey = "html";
        internal const string HtmlPublicId = "-//W3C//DTD XHTML 1.0 Transitional//EN";
        internal const string HtmlSystemId = "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd";
        internal const string HtmlHtmlElementName = "{http://www.w3.org/1999/xhtml}html";
        internal const string HtmlHeadElementName = "{http://www.w3.org/1999/xhtml}head";
        internal const string HtmlTitleElementName = "{http://www.w3.org/1999/xhtml}title";
        internal const string HtmlMetaElementName = "{http://www.w3.org/1999/xhtml}meta";
        internal const string HtmlStyleLinkElementName = "{http://www.w3.org/1999/xhtml}link";
        internal const string HtmlScriptElementName = "{http://www.w3.org/1999/xhtml}script";
        internal const string HtmlBodyElementName = "{http://www.w3.org/1999/xhtml}body";
        internal const string HtmlPElementName = "{http://www.w3.org/1999/xhtml}p";
        internal const string HtmlTableElementName = "{http://www.w3.org/1999/xhtml}table";
        internal const string HtmlTrElementName = "{http://www.w3.org/1999/xhtml}tr";
        internal const string HtmlThElementName = "{http://www.w3.org/1999/xhtml}th";
        internal const string HtmlTdElementName = "{http://www.w3.org/1999/xhtml}td";
        internal const string HtmlDivElementName = "{http://www.w3.org/1999/xhtml}div";
        internal const string HtmlAElementName = "{http://www.w3.org/1999/xhtml}a";
        internal const string HtmlUlElementName = "{http://www.w3.org/1999/xhtml}ul";
        internal const string HtmlLiElementName = "{http://www.w3.org/1999/xhtml}li";
        internal const string HtmlSpanElementName = "{http://www.w3.org/1999/xhtml}span";
        internal const string HtmlBoldElementName = "{http://www.w3.org/1999/xhtml}b";
        internal const string HtmlBrElementName = "{http://www.w3.org/1999/xhtml}br";
        internal const string HtmlPreElementName = "{http://www.w3.org/1999/xhtml}pre";
        internal const string HtmlClassAttributeName = "class";
        internal const string HtmlTypeAttributeName = "type";
        internal const string HtmlTitleAttributeName = "title";
        internal const string HtmlHrefAttributeName = "href";
        internal const string HtmlCharsetAttributeName = "charset";
        internal const string HtmlRelAttributeName = "rel";
        internal const string HtmlTargerAttributeName = "target";
        internal const string HtmlIdAttributeName = "id";
        internal const string HtmlRowspanAttributeName = "rowspan";
        internal const string HtmlHeading1Class = "heading1";
        internal const string HtmlContentClass = "content";
        internal const string HtmlInputOperationClass = "inputOperation";
        internal const string HtmlOutputOperationClass = "outputOperation";
        internal const string HtmlOutputErrorOperationClass = "outputErrorOperation";
        internal const string HtmlTextJavascript = "text/javascript";
        internal const string HtmlFileTreeClass = "filetree";
        internal const string HtmlFolderClass = "folder";
        internal const string HtmlFileClass = "file";
        internal const string HtmlNameClass = "name";
        internal const string HtmlBlankClass = "_blank";
        internal const string HtmlSrcClass = "src";
        internal const string HtmlStylesheetClass = "stylesheet";        
        internal const string HtmlRequestJson = "request-json";
        internal const string HtmlResponseJson = "response-json";
        internal const string HtmlCharset = "utf-8";
        internal const string HtmlRequiredClass = "required";

        protected readonly ContractInformation Model;
        protected readonly ServiceEndpoint Endpoint;
        protected readonly IUriHelper UriHelper;
        private string _content;

        public BaseHelpView(ContractInformation model, ServiceEndpoint endpoint, IUriHelper uriHelper)
        {
            Endpoint = endpoint;
            UriHelper = uriHelper;
            Model = model;
        }

        public virtual string ContentType
        {
            get { return Resources.HelpPageTextHtml; }
        }

        public virtual Message Show(UriTemplate template, Uri uri)
        {
            if (string.IsNullOrWhiteSpace(_content))
            {
                _content = GenerateContent();
            }
            return WebOperationContext.Current.CreateTextResponse(_content, ContentType);
        }

        internal virtual string GenerateContent()
        {
            return string.Empty;
        }
    }
}