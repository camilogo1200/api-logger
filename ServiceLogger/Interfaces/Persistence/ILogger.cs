using System;
using System.Collections.Generic;

namespace APILogger.Persistence.Interfaces
{
    public interface ILogger
    {
        void Log(
            Guid uuidRequest,
            string _logLevel,
            IDictionary<string, string> dGeneralInfo,
            IDictionary<string, string> dHeadersInfo,
            IDictionary<string, string> dActionArguments,
            IDictionary<string, string> dRequestInformation,
            string requestBody,
            bool v );

        void Log(
            Guid uuidMessageIndentifier,
            IDictionary<string, string> dGeneralInfo,
            IDictionary<string, string> dHeadersInfo,
            IDictionary<string, string> dActionArguments,
            IDictionary<string, string> dRequestInformation,
            string requestBody );
    }
}