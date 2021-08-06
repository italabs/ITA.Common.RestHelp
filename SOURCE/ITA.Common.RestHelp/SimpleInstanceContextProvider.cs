using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace ITA.Common.RestHelp
{
    class SimpleInstanceContextProvider : IInstanceContextProvider
    {
        public InstanceContext GetExistingInstanceContext(Message message, IContextChannel channel)
        {
            return null;
        }

        public void InitializeInstanceContext(InstanceContext instanceContext, Message message, IContextChannel channel)
        {
        }

        public bool IsIdle(InstanceContext instanceContext)
        {
            return false;
        }

        public void NotifyIdle(InstanceContextIdleCallback callback, InstanceContext instanceContext)
        {
        }
    }
}
