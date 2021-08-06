using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using ITA.Common.RestHelp.Interfaces;

namespace ITA.Common.RestHelp
{
    public abstract class HelpPageBehavior : IEndpointBehavior, IHelpPageSettings, IUriHelper
    {
        public static string OPERATION_URI_PARAM = "operation";
        public static string OPERATIONS_URI_PARAM = "operations";
        public static string OPERATION_URI_TEMPLATE = "/" + OPERATIONS_URI_PARAM + "/{" + OPERATION_URI_PARAM + "}";
        public static string FILE_URI_PARAM = "file";
        public static string FILES_TEMPLATE = "/Resources/{" + FILE_URI_PARAM + "}";
        public static string IMAGES_TEMPLATE = "/{" + OPERATIONS_URI_PARAM + "}/Images/{" + FILE_URI_PARAM + "}";
        public static string XML_PAGE_URI = "/xml";
        public static string SWAGGER_URI = "/swagger.json";

        private Uri _endpointUri;

        public abstract string DefaultBaseUri { get; }

        protected HelpPageBehavior(string baseHelpUri)
        {
            BaseHelpUri = baseHelpUri;
        }

        protected HelpPageBehavior(IHelpPageSettings configuration)
        {
            Enabled = configuration.Enabled;
            BaseHelpUri = configuration.BaseHelpUri;
        }

        public bool Enabled { get; set; }

        public string BaseHelpUri { get; set; }

        public IHelpExampleProvider ExampleProvider { get; set; }

        public IHelpExtensions Extensions { get; set; }

        public string GetRelativeUrl(string path)
        {
            return ConcatUriSegments(false, _endpointUri.AbsolutePath, BaseHelpUri, path);
        }

        public string GetHelpexRelativeUrl(string path = null)
        {
            return ConcatUriSegments(false, _endpointUri.AbsolutePath, BaseHelpUri, path);
        }

        public string GetHostAbsoluteUrl(string path = null)
        {
            return ConcatUriSegments(true, _endpointUri.AbsoluteUri, path);
        }
        
        public UriTemplate GetRelativeTemplateUri(string uri)
        {
            return new UriTemplate(ConcatUriSegments(false, BaseHelpUri, uri));
        }

        private string ConcatUriSegments(bool isAbsolute, params string[] segments)
        {
            segments = segments.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim('/')).ToArray();
            var format = isAbsolute ? "{0}" : "/{0}";
            return string.Format(format, string.Join("/", segments));
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            if (!Enabled)
            {
                return;
            }
            var endpointAddress = endpoint.Address.Uri;
            if (endpointAddress.Scheme == Uri.UriSchemeHttp || endpointAddress.Scheme == Uri.UriSchemeHttps)
            {
                var address = endpointAddress.ToString();
                if (!address.EndsWith("/"))
                {
                    address = address + "/";
                }

                _endpointUri = new Uri(address);

                var helpPageUri = new Uri(address + (BaseHelpUri ?? DefaultBaseUri));
                var host = endpointDispatcher.ChannelDispatcher.Host;
                var helpDispatcher = this.CreateChannelDispatcher(host, endpoint, helpPageUri);
                host.ChannelDispatchers.Add(helpDispatcher);
            }
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            if (!Enabled)
            {
                return;
            }
            if (endpoint == null)
            {
                throw new RestHelpException(string.Format(ErrorMessages.E_REST_HELP_ENDPOINT_IS_NULL));
            }
            ValidateNoMessageHeadersPresent(endpoint);
            ValidateBinding(endpoint);
        }

        private void ValidateNoMessageHeadersPresent(ServiceEndpoint endpoint)
        {
            if (endpoint == null || endpoint.Address == null)
                return;

            var address = endpoint.Address;
            if (address.Headers.Count > 0)
            {
                throw new RestHelpException(string.Format(ErrorMessages.E_REST_HELP_CANNOT_HAVE_MESSAGE_HEADER, address));
            }
        }

        protected virtual void ValidateBinding(ServiceEndpoint endpoint)
        {
            ValidateIsWebHttpBinding(endpoint, this.GetType().ToString());
        }

        public abstract IHelpResolver CreateResolver(ServiceEndpoint endpoint);

        private ChannelDispatcher CreateChannelDispatcher(ServiceHostBase host, ServiceEndpoint endpoint, Uri helpPageUri)
        {
            var binding = new WebHttpBinding();
            if (helpPageUri.Scheme == Uri.UriSchemeHttps)
            {
                binding.Security.Mode = WebHttpSecurityMode.Transport;
            }
            var address = new EndpointAddress(helpPageUri);
            var bindingParameters = new BindingParameterCollection();            
            var channelListener = binding.BuildChannelListener<IReplyChannel>(helpPageUri, bindingParameters);
            var channelDispatcher = new ChannelDispatcher(channelListener, "HelpPageBinding", binding)
            {
                MessageVersion = MessageVersion.None
            };

            var viewResolver = CreateResolver(endpoint);

            var endpointDispatcher = new EndpointDispatcher(address, "HelpPageContract", "", true);
            var operationDispatcher = new DispatchOperation(endpointDispatcher.DispatchRuntime, "GetHelpPage", "*", "*")
            {                    
                Formatter = new PassthroughMessageFormatter(),
                Invoker = new HelpPageInvoker(viewResolver),
                SerializeReply = false,                    
                DeserializeRequest = false
            };

            endpointDispatcher.DispatchRuntime.InstanceProvider = new SingletonInstanceProvider(viewResolver);
            endpointDispatcher.DispatchRuntime.Operations.Add(operationDispatcher);
            endpointDispatcher.DispatchRuntime.InstanceContextProvider = new SimpleInstanceContextProvider();
            endpointDispatcher.DispatchRuntime.SingletonInstanceContext = new InstanceContext(host, viewResolver);
            endpointDispatcher.ContractFilter = viewResolver.GetMessageFilter();
            endpointDispatcher.AddressFilter = viewResolver.GetMessageFilter();
            endpointDispatcher.FilterPriority = 0;

            channelDispatcher.Endpoints.Add(endpointDispatcher);
            return channelDispatcher;
        }

        private static void ValidateIsWebHttpBinding(ServiceEndpoint serviceEndpoint, string behaviorName)
        {
            var binding = serviceEndpoint.Binding;
            if (binding.Scheme != "http" && binding.Scheme != "https")
            {
                throw new RestHelpException(string.Format(ErrorMessages.E_REST_HELP_INVALID_SCHEME, serviceEndpoint.Contract.Name, behaviorName));
            }
            if (binding.MessageVersion != MessageVersion.None)
            {
                throw new RestHelpException(string.Format(ErrorMessages.E_REST_HELP_INVALID_MESSAGE_VERSION, serviceEndpoint.Address.Uri.AbsoluteUri, behaviorName));
            }
            TransportBindingElement transportBindingElement = binding.CreateBindingElements().Find<TransportBindingElement>();
            if (transportBindingElement != null && !transportBindingElement.ManualAddressing)
            {
                throw new RestHelpException(string.Format(ErrorMessages.E_REST_HELP_NEED_MANUAL_ADDRESSING,
                    serviceEndpoint.Address.Uri.AbsoluteUri, behaviorName, transportBindingElement.GetType().Name));
            }
        }
    }
}
