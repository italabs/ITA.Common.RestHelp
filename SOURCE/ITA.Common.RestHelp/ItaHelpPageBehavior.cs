using System.ServiceModel.Description;
using ITA.Common.RestHelp.Interfaces;

namespace ITA.Common.RestHelp
{
    public class ItaHelpPageBehavior : HelpPageBehavior
    {
        public static string DEFAULT_BASE_URI = "helpex";

        public ItaHelpPageBehavior() : base(DEFAULT_BASE_URI)
        {
        }

        public ItaHelpPageBehavior(IHelpPageSettings configuration) : base(configuration)
        {
        }

        public override string DefaultBaseUri
        {
            get { return DEFAULT_BASE_URI; }
        }

        public override IHelpResolver CreateResolver(ServiceEndpoint endpoint)
        {
            return new HelpViewResolver(endpoint, this, ExampleProvider, Extensions);
        }
    }
}
