using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;

namespace ITA.Common.RestHelp.Data
{
    internal class InformationHelper
    {
        public static WebMessageFormat DefaultOutgoingRequestFormat = WebMessageFormat.Xml;
        public static WebMessageFormat DefaultOutgoingReplyFormat = WebMessageFormat.Xml;
        public static WebMessageBodyStyle DefaultBodyStyle = WebMessageBodyStyle.Bare;
        public const string POST_METHOD = "POST";
        public const string GET_METHOD = "GET";

        public static string GetOperationMethod(OperationDescription description)
        {
            var invokeAttr = description.Behaviors.OfType<WebInvokeAttribute>().FirstOrDefault();
            var getAttr = description.Behaviors.OfType<WebGetAttribute>().FirstOrDefault();

            return invokeAttr != null
                ? (string.IsNullOrWhiteSpace(invokeAttr.Method) ? POST_METHOD : invokeAttr.Method)
                : (getAttr != null ? GET_METHOD : POST_METHOD);
        }

        public static string GetOperationDescription(OperationDescription description)
        {
            var descriptionAttr = Attribute.GetCustomAttribute(description.SyncMethod, typeof(DescriptionAttribute)) as DescriptionAttribute;

            return descriptionAttr != null
                ? descriptionAttr.Description
                : null;
        }

        public static string GetUriTemplate(OperationDescription description)
        {
            var invokeAttr = description.Behaviors.OfType<WebInvokeAttribute>().FirstOrDefault();
            var getAttr = description.Behaviors.OfType<WebGetAttribute>().FirstOrDefault();

            var uriTemplate = invokeAttr != null
                ? (string.IsNullOrWhiteSpace(invokeAttr.UriTemplate) ? null : invokeAttr.UriTemplate)
                : (getAttr != null ? getAttr.UriTemplate : null);

            return string.IsNullOrWhiteSpace(uriTemplate)
                ? description.Name
                : uriTemplate;
        }

        public static WebMessageBodyStyle GetBodyStyle(OperationDescription od)
        {
            var wga = od.Behaviors.Find<WebGetAttribute>();
            var wia = od.Behaviors.Find<WebInvokeAttribute>();
            if (wga != null)
                return wga.BodyStyle;
            if (wia != null)
                return wia.BodyStyle;
            return DefaultBodyStyle;
        }

        public static bool SupportsJsonFormat(OperationDescription od)
        {
            return od.Behaviors.Find<DataContractSerializerOperationBehavior>() != null;
        }

        public static string GetString(string res, params object[] args)
        {
            return string.Format(res, args);
        }

        internal static bool IsUntypedMessage(MessageDescription message)
        {
            if (message == null)
                return false;
            if (message.Body.ReturnValue != null && message.Body.Parts.Count == 0 && message.Body.ReturnValue.Type == typeof(Message))
                return true;
            if (message.Body.ReturnValue == null && message.Body.Parts.Count == 1)
                return message.Body.Parts[0].Type == typeof(Message);
            return false;
        }

        internal static bool IsTypedMessage(MessageDescription message)
        {
            if (message != null)
                return message.MessageType != null;
            return false;
        }

        public static Type[] GetRequestBodyTypes(OperationDescription od, string uriTemplate)
        {
            if (od.Behaviors.Contains(typeof(WebGetAttribute)))
                return new []{typeof(void)};

            if (IsUntypedMessage(od.Messages[0]))
                return new []{typeof(Message)};

            if (IsTypedMessage(od.Messages[0]))
                return new [] {od.Messages[0].MessageType};

            var template = new UriTemplate(uriTemplate);
            var source = od.Messages[0].Body.Parts.Where(
                part => !template.PathSegmentVariableNames.Contains(part.Name.ToUpperInvariant()) &&
                        !template.QueryValueVariableNames.Contains(part.Name.ToUpperInvariant())).ToList();

            if (!source.Any())
                return new [] {typeof(void)};

            return source.Select(s => s.Type).ToArray();
        }

        public static MessagePartDescription[] GetRequestBodyParts(OperationDescription od, string uriTemplate)
        {
            if (od.Behaviors.Contains(typeof(WebGetAttribute)))
                return new MessagePartDescription[0];

            if (IsUntypedMessage(od.Messages[0]))
                return new MessagePartDescription[0];

            var template = new UriTemplate(uriTemplate);
            return od.Messages[0].Body.Parts.Where(
                part => !template.PathSegmentVariableNames.Contains(part.Name.ToUpperInvariant()) &&
                        !template.QueryValueVariableNames.Contains(part.Name.ToUpperInvariant())).ToArray();
        }

        public static Type GetResponseBodyType(OperationDescription od)
        {
            if (IsUntypedMessage(od.Messages[1]))
                return typeof(Message);
            if (IsTypedMessage(od.Messages[1]))
                return od.Messages[1].MessageType;
            if (od.Messages[1].Body.Parts.Count > 0)
                return null;           
            return od.Messages[1].Body.ReturnValue.Type;
        }

        internal static string GetUriTemplateOrDefault(OperationDescription operationDescription)
        {
            var str = GetUriTemplate(operationDescription);
            if (str == null && GetOperationMethod(operationDescription) == "GET")
                str = MakeDefaultGetUTString(operationDescription);
            if (str == null)
                str = operationDescription.Name;
            return str;
        }

        private static string MakeDefaultGetUTString(OperationDescription od)
        {
            var stringBuilder = new StringBuilder(od.Name);
            if (!IsUntypedMessage(od.Messages[0]))
            {
                stringBuilder.Append("?");
                foreach (var part in od.Messages[0].Body.Parts)
                {
                    string decodedName = part.Name;
                    stringBuilder.Append(decodedName);
                    stringBuilder.Append("={");
                    stringBuilder.Append(decodedName);
                    stringBuilder.Append("}&");
                }
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }
            return stringBuilder.ToString();
        }

        public static object GetConstantValue(Type type)
        {
            var typeName = type.Name;
            if (typeName == "String")
            {
                return GetString(Resources.HelpExampleGeneratorStringContent);
            }
            if (typeName == "Int32")
                return (int)123;
            if (typeName == "UInt32")
                return (uint)123456;
            if (typeName == "Int64")
                return (long)123456;
            if (typeName == "UInt64")
                return (ulong)123456;
            if (typeName == "Int16")
                return (short)123;
            if (typeName == "UInt16")
                return (ushort)123;
            if (typeName == "Byte")
                return (byte)123;
            if (typeName == "Decimal")
                return (decimal)123.4567;
            if (typeName == "Float")
                return (float)123.4567;
            if (typeName == "Double")
                return (double)123.4567;
            if (typeName == "Boolean")
                return true;
            if (typeName == "DateTime" || typeName == "TimeSpan")
                return DateTime.Now;
            if (typeName == "Guid")
                return Guid.NewGuid().ToString();
            return (string)null;
        }

        public static T GetCustomAttribute<T>(MethodInfo methodInfo) where T : Attribute
        {
            if (methodInfo == null)
                return null;

            var attributes = methodInfo.GetCustomAttributes(typeof(T), false);
            return attributes.OfType<T>().FirstOrDefault();
        }

        public static IEnumerable<T> GetCustomAttributes<T>(MethodInfo methodInfo) where T : Attribute
        {
            if (methodInfo == null)
                return null;

            var attributes = methodInfo.GetCustomAttributes(typeof(T), false);
            return attributes.OfType<T>();
        }

        public static IEnumerable<T> GetCustomAttributes<T>(Type type) where T : Attribute
        {
            if (type == null)
                return null;

            var attributes = type.GetCustomAttributes(typeof(T), false);
            return attributes.OfType<T>();
        }
    }
}