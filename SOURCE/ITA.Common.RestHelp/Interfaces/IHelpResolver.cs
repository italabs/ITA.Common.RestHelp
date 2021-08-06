using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace ITA.Common.RestHelp.Interfaces
{
    public interface IHelpResolver
    {
        Message Resolve(Uri uri);
        MessageFilter GetMessageFilter();
    }
}
