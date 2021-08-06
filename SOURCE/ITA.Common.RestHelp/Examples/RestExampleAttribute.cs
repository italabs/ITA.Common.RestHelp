using System;

namespace ITA.Common.RestHelp.Examples
{
    public class RestExampleAttribute : Attribute
    {
        public RestExampleAttribute(HelpExampleType exampleType)
        {
            ExampleType = exampleType;
        }

        public HelpExampleType ExampleType { get; set; }
    }
}