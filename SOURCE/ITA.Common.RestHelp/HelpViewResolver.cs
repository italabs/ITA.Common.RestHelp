using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using ITA.Common.RestHelp.Data;
using ITA.Common.RestHelp.Interfaces;
using ITA.Common.RestHelp.Views;

namespace ITA.Common.RestHelp
{
    internal class HelpViewResolver : MessageFilter, IHelpResolver
    {
        private readonly Uri _endpointUri;
        private readonly IUriHelper _uriHelper;
        private readonly List<HelpViewResolverItem> _items;
        private readonly IHelpExtensions _extensions;

        public HelpViewResolver(ServiceEndpoint endpoint, IUriHelper uriHelper, IHelpExampleProvider exampleProvider, IHelpExtensions extensions)
        {
            _uriHelper = uriHelper;
            _endpointUri = endpoint.Address.Uri;
            _extensions = extensions;

            var model = new ContractInformation(endpoint.Contract, exampleProvider);

            _items = new List<HelpViewResolverItem>
            {
                new HelpViewResolverItem
                {
                    Template = _uriHelper.GetRelativeTemplateUri(HelpPageBehavior.XML_PAGE_URI),
                    View = new XmlDataHelpView(model, endpoint, _uriHelper)
                },
                new HelpViewResolverItem
                {
                    Template = _uriHelper.GetRelativeTemplateUri(string.Empty), 
                    View = new HtmlContractHelpView(model, endpoint, _uriHelper, _extensions)
                },
                new HelpViewResolverItem
                {
                    Template = _uriHelper.GetRelativeTemplateUri(HelpPageBehavior.OPERATION_URI_TEMPLATE), 
                    View = new HtmlOperationHelpView(model, endpoint, _uriHelper, extensions)
                },
                new HelpViewResolverItem
                {
                    Template = _uriHelper.GetRelativeTemplateUri(HelpPageBehavior.FILES_TEMPLATE),
                    View = new FileHelpView(endpoint, _uriHelper)
                },
                new HelpViewResolverItem
                {
                    Template = _uriHelper.GetRelativeTemplateUri(HelpPageBehavior.IMAGES_TEMPLATE),
                    View = new FileHelpView(endpoint, _uriHelper)
                }                       
            };

            if (_extensions != null)
            {
                foreach (var extension in _extensions.GetExtensions(HelpPageExtensionType.Page))
                {
                    _items.Add(new HelpViewResolverItem
                    {
                        Template = _uriHelper.GetRelativeTemplateUri(extension.RelativeUrl),
                        View = new HtmlExtensionHelpView(endpoint, _uriHelper, extension)
                    });
                }
            }
        }
       
        public Message Resolve(Uri uri)
        {
            var item = GetTemplate(uri);
            if (item == null)
            {
                return null;
            }

            return item.View.Show(item.Template, uri);
        }

        public MessageFilter GetMessageFilter()
        {
            return this;
        }

        public override bool Match(MessageBuffer buffer)
        {
            return true;
        }

        public override bool Match(Message message)
        {
            return GetTemplate(message.Headers.To) != null;
        }

        private HelpViewResolverItem GetTemplate(Uri uri)
        {
            var hostUri = new Uri(_endpointUri.GetLeftPart(UriPartial.Authority));

            var candidate = new Uri(hostUri, uri.AbsolutePath);

            foreach (var item in _items)
            {
                var matchResult = item.Template.Match(_endpointUri, candidate);
                if (matchResult != null)
                {
                    return item;
                }
            }
            return null;
        }        
    }

    internal class HelpViewResolverItem
    {        
        public UriTemplate Template { get; set; }

        public IHelpView View { get; set; }
    }
}