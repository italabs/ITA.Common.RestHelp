using System;

namespace ITA.Common.RestHelp.Interfaces
{
    /// <summary>
    /// Authorization attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public sealed class RestAuthorizationAttribute : Attribute
    {
        public RestAuthorizationType Type { get; set; }
    }
}
