using System;
using System.Runtime.Serialization;

namespace ITA.Common.RestHelp
{
    /// <summary>
    /// RestHelp exception
    /// Serializable
    /// </summary>
    [Serializable]
    public class RestHelpException : Exception
    {
        public RestHelpException(string message)
            : base(message)
        {
        }

        public RestHelpException(string message, Exception exception)
            : base(message, exception)
        {
        }

        public RestHelpException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}
