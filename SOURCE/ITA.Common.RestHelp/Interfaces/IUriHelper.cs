using System;

namespace ITA.Common.RestHelp.Interfaces
{
    internal interface IUriHelper
    {
        string GetHostAbsoluteUrl(string uri = null);

        string GetHelpexRelativeUrl(string path = null);

        string GetRelativeUrl(string path);

        UriTemplate GetRelativeTemplateUri(string uri);
    }
}