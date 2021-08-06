using System.Collections.Generic;

namespace ITA.Common.RestHelp.Interfaces
{
    public interface IHelpExtensions
    {
        List<HelpPageExtension> GetExtensions(HelpPageExtensionType type);
    }
}