using System;
using System.Net;
using System.Runtime.Serialization;
using ITA.Common.RestHelp.Examples;

namespace ITA.Common.RestHelp.Data
{
    [DataContract]
    public class ResponseDocumentation
    {
        [DataMember]
        public HttpStatusCode Code { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Example { get; set; }

        public Type Type { get; set; }

        [DataMember]        
        public string TypeName
        {
            get { return Type != null ? Type.FullName : null; }
            set { }
        }

        [DataMember]
        public HelpExampleType HelpExampleType { get; set; }

        [DataMember]
        public bool Disabled { get; set; }

        [DataMember]
        public MethodParamDocumentation ParamDocumentation { get; set; }
    }
}
