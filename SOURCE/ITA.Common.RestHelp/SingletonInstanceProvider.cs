using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace ITA.Common.RestHelp
{
    class SingletonInstanceProvider : IInstanceProvider
    {
        object instance;
        public SingletonInstanceProvider(object instance)
        {
            this.instance = instance;
        }
        public object GetInstance(InstanceContext instanceContext, Message message)
        {
            return instance;
        }

        public object GetInstance(InstanceContext instanceContext)
        {
            return instance;
        }

        public void ReleaseInstance(InstanceContext instanceContext, object instance)
        {
        }
    }
}
