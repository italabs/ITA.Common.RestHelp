using System;
using System.Net;
using ITA.Common.RestHelp.Examples;

namespace ITA.Common.RestHelp.Interfaces
{
    /// <summary>
    /// Attribute for fault responses via REST interface
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = true)]
    public class RestFaultContractAttribute : Attribute
    {
        public RestFaultContractAttribute(HttpStatusCode code, string description, Type detailType)
            : this(code,description, detailType, HelpExampleType.Output)
        {
        }

        public RestFaultContractAttribute(HttpStatusCode code, string description, Type detailType, HelpExampleType exampleType, bool disabled = false)
        {
            Code = code;
            Description = description;
            DetailType = detailType;
            HelpExampleType = exampleType;
            Disabled = disabled;
        }

        /// <summary>
        /// Return type
        /// </summary>
        public Type DetailType { get; private set; }        

        /// <summary>
        /// Help example type
        /// </summary>
        public HelpExampleType HelpExampleType { get; private set; }

        /// <summary>
        /// Error return HTTP status code
        /// </summary>
        public HttpStatusCode Code { get; private set; }

        /// <summary>
        /// Error description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Error disabled flag
        /// </summary>
        public bool Disabled { get; set; }
    }
}