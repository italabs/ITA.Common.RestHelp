using System;

namespace ITA.Common.RestHelp.Interfaces
{
    /// <summary>
    /// Attribute for set API information.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class RestApiAttribute : Attribute
    {
        public string LicenseTitle { get; set; }

        public string LicenseUrl { get; set; }

        public string ApiVersion { get; set; }
    }
}