using System;

namespace ITA.Common.RestHelp.Interfaces
{
    [Flags]
    public enum RestAuthorizationType
    {
        None = 0,

        Basic = 1,

        Bearer = 2
    }
}
