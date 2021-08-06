using System;
using System.ServiceModel.Channels;

namespace ITA.Common.RestHelp.Interfaces
{
    internal interface IHelpView
    {
        string ContentType { get; }

        Message Show(UriTemplate template, Uri uri);
    }
}