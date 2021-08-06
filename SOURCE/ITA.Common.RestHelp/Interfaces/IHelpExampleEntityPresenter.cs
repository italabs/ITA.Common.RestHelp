using System;
using System.Collections.Generic;
using ITA.Common.RestHelp.Examples;

namespace ITA.Common.RestHelp.Interfaces
{
    public interface IHelpExampleEntityPresenter
    {
        Type TargetType { get; }

        Dictionary<HelpExampleType, string> GetExamples();
    }
}