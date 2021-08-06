using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace ITA.Common.RestHelp
{
    class PassthroughMessageFormatter : IDispatchMessageFormatter
    {
        public void DeserializeRequest(Message message, object[] parameters)
        {
            parameters[0] = message;
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            return (Message)result;
        }
    }
}
