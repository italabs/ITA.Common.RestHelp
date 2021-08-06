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
    internal class SwaggerHelpResolver : MessageFilter, IHelpResolver
    {
        private readonly Uri _endpointUri;
        private readonly IUriHelper _uriHelper;
        private readonly List<HelpViewResolverItem> _items;
        private readonly IHelpExtensions _extensions;

        public SwaggerHelpResolver(
            ServiceEndpoint endpoint,
            IUriHelper uriHelper,
            IHelpExampleProvider exampleProvider,
            IHelpExtensions extensions,
            SwaggerVersion version)
        {
            _uriHelper = uriHelper;
            _endpointUri = endpoint.Address.Uri;
            _extensions = extensions;

            var model = new ContractInformation(endpoint.Contract, exampleProvider);

            _items = new List<HelpViewResolverItem>
            {
                new HelpViewResolverItem
                {
                    Template = _uriHelper.GetRelativeTemplateUri(HelpPageBehavior.SWAGGER_URI),
                    View = new SwaggerDocumentView(model, endpoint, _uriHelper, version)
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

}
