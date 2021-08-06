using System;
using ITA.Common.RestHelp.Examples;

namespace ITA.Common.RestHelp.Interfaces
{
    public interface IHelpExampleProvider
    {
        string GetExample(HelpExampleType exampleType, Type entityType);
    }
}