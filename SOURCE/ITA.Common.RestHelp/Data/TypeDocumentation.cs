using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace ITA.Common.RestHelp.Data
{
    [KnownType(typeof(TypeDictDocumentation))]
    [DataContract(IsReference = true)]
    public class TypeDocumentation
    {
        public Type Type { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string JsonTypeName { get; set; }

        [DataMember]
        public string Summary { get; set; }

        [DataMember]
        public bool IsDataContract { get; set; }

        [DataMember]
        public string DataContractName { get; set; }

        [DataMember]
        public bool IsCollection { get; set; }

        [DataMember]
        public bool IsNullable { get; set; }

        [DataMember]
        public bool IsSystem { get; set; }

        [DataMember]
        public bool IsEnum { get; set; }

        [DataMember]
        public bool IsRequired { get; set; }

        [DataMember]
        public List<PropertyDocumentation> Properties { get; set; }

        [DataMember]
        public List<ResponseDocumentation> ErrorResponses { get; set; }

        public virtual string GetFullName()
        {
            return string.Format("{3}{0} ({1}{2})", JsonTypeName, DataContractName ?? Type.FullName,
                IsCollection ? "[]" : "", IsCollection ? "Array of " : string.Empty);
        }

        public virtual JToken ToJson(Stack<string> visitNodes)
        {
            if (Type == null)
                return null;

            var item = ToJsonObject(visitNodes);
            if (IsCollection)
                return new JArray(item);

            return item;
        }

        protected virtual JToken ToJsonObject(Stack<string> visitNodes)
        {            
            if (Properties != null && Properties.Any() && Type != null)
            {
                if (visitNodes.Contains(Type.FullName))
                    return new JValue(InformationHelper.GetConstantValue(Type));

                visitNodes.Push(Type.FullName);
                var result = new JObject(Properties.Where(p => p.IsDataMemeber).Select(p => p.ToJson(visitNodes)).Where(x => x != null));
                visitNodes.Pop();
                return result;
            }

            return new JValue(InformationHelper.GetConstantValue(Type));
        }
    }

    [DataContract(IsReference = true)]
    public class TypeDictDocumentation : TypeDocumentation
    {
        public const string KeyName = "key";
        public const string ValueName = "value";

        public void SetDictionary(TypeDocumentation keyType, TypeDocumentation valueType)
        {
            IsCollection = true;
            Properties = new List<PropertyDocumentation>();
            Properties.Add(new PropertyDocumentation
            {
                DataMemberName = KeyName,
                IsDataMemeber = true,
                Property = null,
                Summary = keyType.Summary,
                TypeDocumentation = keyType,
                IsRequired = true
            });
            Properties.Add(new PropertyDocumentation
            {
                DataMemberName = ValueName,
                IsDataMemeber = true,
                Property = null,
                Summary = valueType.Summary,
                TypeDocumentation = valueType,
                IsRequired = true
            });
        }

        public override string GetFullName()
        {
            return Resources.HelpPageDictionary;
        }
    }
}