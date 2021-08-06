using System;

namespace ITA.Common.RestHelp.Interfaces
{
    /// <summary>
    /// Attribute for set optional flags to properties for REST interface data models
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class RestRequiredAttribute : Attribute
    {
        public RestRequiredAttribute()
        {
            //set default required value
            Value = true;
        }

        public RestRequiredAttribute(bool value)
        {
            Value = value;
        }

        /// <summary>
        /// Property required flag
        /// </summary>
        public bool Value { get; set; }
    }
}
