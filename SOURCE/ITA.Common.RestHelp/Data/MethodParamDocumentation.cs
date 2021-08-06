using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace ITA.Common.RestHelp.Data
{
    [DataContract]
    public class MethodParamDocumentation
    {        
        [DataMember]
        public TypeDocumentation TypeDocumentation { get; set; }

        [DataMember]
        public string Summary { get; set; }

        [DataMember]
        public MethodParamPlace ParamPlace { get; set; }

        public Type ParameterType { get; set; }

        [DataMember]
        public string ParameterName { get; set; }

        [DataMember]
        public bool IsRequired { get; set; }

        public JToken ToJson()
        {
            if (TypeDocumentation == null)
                return null;

            var stack = new Stack<string>();

            return string.IsNullOrWhiteSpace(ParameterName)
                ? TypeDocumentation.ToJson(stack)
                : new JProperty(ParameterName, TypeDocumentation.ToJson(stack));
        }
    }
}
