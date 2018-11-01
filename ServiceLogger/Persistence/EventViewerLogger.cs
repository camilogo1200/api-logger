using APILogger.Persistence.Interfaces;
using DataMapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Text;

namespace APILogger.Persistence
{
    public class EventViewerLogger : ILogger
    {
        private readonly string _EventViewerLoggerAplicationNameIndicator = "ApplicationNameLog";
        private string _aplicationName = null;
        private readonly DataMapper<Object> mapper = DataMapper<object>.Instancia;

        private string GetAplicationName()
        {
            if (_aplicationName == null)
            {
                string tmp = ConfigurationManager.AppSettings.Get(_EventViewerLoggerAplicationNameIndicator);
                if (string.IsNullOrEmpty(tmp))
                {
                    throw new Exception("\"ApplicationNameLog\" key not found on web.config file.");
                }
                _aplicationName = tmp;
            }
            return _aplicationName;
        }

        #region Singleton

        /// <summary>
        /// Atributo utilizado para evitar problemas con multithreading en el singleton.
        /// </summary>
        private static readonly object syncRoot = new Object();

        private static volatile EventViewerLogger instance;

        public static EventViewerLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new EventViewerLogger();
                        }
                    }
                }
                return instance;
            }
        }

        #endregion Singleton

        public void Log( Guid uuidRequest, string _logLevel, IDictionary<string, string> dGeneralInfo, IDictionary<string, string> dHeadersInfo, IDictionary<string, string> dActionArguments, IDictionary<string, string> dRequestInformation, string requestBody, bool isRequest )
        {
            string ApplicationName = GetAplicationName();
            string nameLog = ( isRequest ) ? "Request" : "Response";
            nameLog += " Uuid : " + uuidRequest.ToString();

            dGeneralInfo.TryGetValue("Request URI", out string requestURI);
            dGeneralInfo.TryGetValue("ControllerName", out string controllerName);
            dActionArguments.TryGetValue("ActionName", out string controllerMethodName);
            dRequestInformation.TryGetValue("Request Method", out string requestMethod);
            dRequestInformation.TryGetValue("Content-Type", out string contentType);
            dRequestInformation.TryGetValue("Request Local Time", out string requestTime);
            dRequestInformation.TryGetValue("Request Scheme", out string requestScheme);
            dRequestInformation.TryGetValue("SERVER_PROTOCOL", out string serverProtocol);
            dRequestInformation.TryGetValue("REMOTE_ADDR", out string remoteAddr);
            dRequestInformation.TryGetValue("LOCAL_ADDR", out string localAddr);
            dRequestInformation.TryGetValue("REMOTE_HOST", out string remoteHost);
            StringBuilder logBuilder = new StringBuilder();

            logBuilder.Append("<").Append(ApplicationName).AppendLine(">");
            logBuilder.Append("<").Append(( isRequest ) ? "request" : "response").AppendLine(">");
            logBuilder.Append("<eventtime>").Append(DateTime.Now.ToString("yyyy-MM-dd T HH:mm:ss.fff")).AppendLine("</eventtime>");
            logBuilder.Append("<uuid>").Append(uuidRequest.ToString()).AppendLine("</uuid>");
            logBuilder.Append("<scheme>").Append(requestScheme.ToUpper()).AppendLine("</scheme>");
            logBuilder.Append("<servertime>").Append(dGeneralInfo["Server Time"]).AppendLine("</servertime>");
            logBuilder.Append("<serverProtocol>").Append(serverProtocol.ToUpper()).AppendLine("<serverProtocol>");
            logBuilder.Append("<remoteaddress>").Append(remoteAddr).AppendLine("</remoteaddress>");
            logBuilder.Append("<remotehost>").Append(remoteHost).AppendLine("</remotehost>");
            logBuilder.Append("<localaddress>").Append(localAddr).AppendLine("</localaddress>");
            logBuilder.Append("<httpmethod>").Append(requestMethod.ToUpper()).AppendLine("</httpmethod>");
            logBuilder.Append("<uri>").Append(requestURI).AppendLine("</uri>");
            logBuilder.Append("<controllername>").Append(controllerName).AppendLine("</controllername>");
            logBuilder.Append("<controllermethodname>").Append(controllerMethodName).AppendLine("</controllermethodname>");
            logBuilder.Append("<actionmethodname>").Append(dActionArguments["ActionName"]).AppendLine("</actionmethodname>");
            logBuilder.Append("<headers>").Append(dRequestInformation["ALL_RAW"].TrimEnd()).AppendLine("</headers>");
            logBuilder.Append("<contentType>").Append(contentType).AppendLine("</contentType>");
            logBuilder.Append("<requesttime>").Append(requestTime).AppendLine("</requesttime>");

            logBuilder.Append("</").Append(ApplicationName).AppendLine(">");
            logBuilder.Append("</").Append(( isRequest ) ? "request" : "response").AppendLine(">");

            if (!EventLog.SourceExists(ApplicationName))
            {
                EventLog.CreateEventSource(ApplicationName, ApplicationName);
            }

            using (EventLog eventLogApplication = new EventLog(ApplicationName))
            {
                eventLogApplication.Source = ApplicationName;
                eventLogApplication.WriteEntry(logBuilder.ToString(), EventLogEntryType.Information, 101, 1);
            }

            using (EventLog eventLogGeneral = new EventLog("Application"))
            {
                eventLogGeneral.Source = "Application";
                eventLogGeneral.WriteEntry(logBuilder.ToString(), EventLogEntryType.Information, 101, 1);
            }
            logBuilder.Clear();
        }

        public void Log( Guid uuidMessageIndentifier, IDictionary<string, string> dGeneralInfo, IDictionary<string, string> dHeadersInfo, IDictionary<string, string> dActionArguments, IDictionary<string, string> dRequestInformation, string requestBody )
        {
            throw new NotImplementedException();
        }
    }
}