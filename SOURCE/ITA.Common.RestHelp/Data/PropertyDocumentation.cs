using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace ITA.Common.RestHelp.Data
{
    [DataContract(IsReference = true)]
    public class PropertyDocumentation
    {
        public PropertyInfo Property { get; set; }

        [DataMember]
        public string Summary { get; set; }

        [DataMember]
        public string DataMemberName { get; set; }

        [DataMember]
        public bool IsDataMemeber { get; set; }

        [DataMember]
        public TypeDocumentation TypeDocumentation { get; set; }

        [DataMember]
        public int Level { get; set; }

        [DataMember]
        public bool IsRequired { get; set; }

        public JToken ToJson(Stack<string> visitNodes)
        {
            return new JProperty(DataMemberName ?? Property.Name, TypeDocumentation.ToJson(visitNodes));
        }
    }
}