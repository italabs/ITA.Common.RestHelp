using System.ServiceModel.Description;
using ITA.Common.RestHelp.Interfaces;

namespace ITA.Common.RestHelp
{
    public class SwaggerHelpPageBehavior : HelpPageBehavior, ISwaggerHelpPageSettings
    {
        public static string DEFAULT_BASE_URI = "swagger";

        public override string DefaultBaseUri
        {
            get { return DEFAULT_BASE_URI; }
        }

        public override IHelpResolver CreateResolver(ServiceEndpoint endpoint)
        {
            return new SwaggerHelpResolver(endpoint, this, ExampleProvider, Extensions, SwaggerHelpVersion);
        }

        public SwaggerHelpPageBehavior() : base(DEFAULT_BASE_URI)
        {
        }

        public SwaggerHelpPageBehavior(ISwaggerHelpPageSettings configuration) : base(configuration)
        {
            SwaggerHelpVersion = configuration.SwaggerHelpVersion;
        }

        public SwaggerVersion SwaggerHelpVersion { get; set; }
    }
}
