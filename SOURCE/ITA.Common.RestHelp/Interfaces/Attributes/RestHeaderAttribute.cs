using System;

namespace ITA.Common.RestHelp.Interfaces
{
    /// <summary>
    /// Attribute for set custom header to HTTP request.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class RestHeaderAttribute : Attribute
    {
        public RestHeaderAttribute(string name, bool required = false, string description = null)
        {
            Name = name;
            Required = required;
            Description = description;
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool Required { get; set; }
    }
}