using APILogger.Persistence.Interfaces;
using DataMapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace APILogger.Persistence
{
    public class DatabaseLogger : ILogger
    {
        private readonly string _DatabaseLoggerAplicationNameIndicator = "ApplicationNameLog";
        private string _aplicationName = null;
        private readonly DataMapper<Object> mapper = DataMapper<object>.Instancia;

        #region Singleton

        /// <summary>
        /// Atributo utilizado para evitar problemas con multithreading en el singleton.
        /// </summary>
        private static readonly object syncRoot = new Object();

        private static volatile DatabaseLogger instance;

        public static DatabaseLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new DatabaseLogger();
                        }
                    }
                }
                return instance;
            }
        }

        #endregion Singleton

        private string GetAplicationName()
        {
            if (_aplicationName == null)
            {
                string tmp = ConfigurationManager.AppSettings.Get(_DatabaseLoggerAplicationNameIndicator);
                if (string.IsNullOrEmpty(tmp))
                {
                    throw new Exception("\"ApplicationNameLog\" key not found on web.config file.");
                }
                _aplicationName = tmp;
            }
            return _aplicationName;
        }

        public void Log( Guid uuidRequest, string _logLevel,
            IDictionary<string, string> dGeneralInfo,
            IDictionary<string, string> dHeadersInfo,
            IDictionary<string, string> dActionArguments,
            IDictionary<string, string> dRequestInformation,
            string requestBody,
            bool isRequest )
        {
            SqlParameterCollection parameters = new SqlCommand().Parameters;

            DateTime.TryParse(dGeneralInfo["Server Time"], out DateTime serverTime);
            DateTime.TryParse(dRequestInformation["Request Local Time"].Replace("[", "").Replace("]", ""), out DateTime requestEventTime);

            string requestMethod = dRequestInformation["Request Method"].ToUpper();
            parameters.AddWithValue("@ApplicationName", GetAplicationName());
            parameters.AddWithValue("@ServerEventTime", DateTime.Now);
            parameters.AddWithValue("@RequestURI", dGeneralInfo["Request URI"]);
            parameters.AddWithValue("@RequestMethod", requestMethod);
            parameters.AddWithValue("@RequestUuid", uuidRequest);
            parameters.AddWithValue("@RequestEventTime", requestEventTime);
            parameters.AddWithValue("@IsRequest", isRequest);
            parameters.AddWithValue("@ServerTime", serverTime);
            parameters.AddWithValue("@ServerProtocol", dRequestInformation["SERVER_PROTOCOL"]);
            parameters.AddWithValue("@IpLocalAddress", dRequestInformation["LOCAL_ADDR"]);
            parameters.AddWithValue("@IpRemoteAddress", dRequestInformation["REMOTE_ADDR"]);
            parameters.AddWithValue("@RemoteHost", dRequestInformation["REMOTE_HOST"]);
            parameters.AddWithValue("@ControllerName", dGeneralInfo["ControllerName"]);
            parameters.AddWithValue("@ActionName", dActionArguments["ActionName"]);
            parameters.AddWithValue("@ServerPort", Int32.Parse(dRequestInformation["SERVER_PORT"]));
            parameters.AddWithValue("@IsFile", bool.Parse(dRequestInformation["Request is File"]));

            //# HTTP RAW - Headers
            string raw = dRequestInformation["ALL_RAW"].TrimEnd();

            StringBuilder sb = new StringBuilder();
            //# HTTP RAW - Parameters

            dActionArguments.TryGetValue("ModuleVersionId", out string moduleVersionID);
            dActionArguments.TryGetValue("ModuleName", out string moduleName);

            parameters.AddWithValue("@ModuleVersionId", moduleVersionID);
            parameters.AddWithValue("@ModuleName", moduleName);

            dActionArguments.Remove("ActionName");
            dActionArguments.Remove("ModuleVersionId");
            dActionArguments.Remove("ModuleName");

            List<KeyValuePair<string, string>> lParms = dActionArguments.ToList();

            foreach (KeyValuePair<string, string> argument in lParms)
            {
                sb.Append("[ ").Append(argument.Key).Append(" : ").Append(argument.Value).AppendLine(" ]");
            }
            parameters.AddWithValue("@Parameters", sb.ToString());

            StringBuilder xmlHeaders = new StringBuilder();
            xmlHeaders.AppendLine("<headers>");
            //Serialize Object for store procedure
            List<KeyValuePair<string, string>> lHeaders = dHeadersInfo.ToList();
            foreach (KeyValuePair<string, string> argument in lHeaders)
            {
                xmlHeaders.Append("<header key =\"").Append(argument.Key).Append("\"");
                xmlHeaders.Append(" value =\"").Append(argument.Value).AppendLine("\"/>");
            }
            xmlHeaders.AppendLine("</headers>");

            parameters.AddWithValue("@HeadersXML", xmlHeaders.ToString());

            xmlHeaders.Clear();
            sb.Clear();

            if (requestMethod.Equals("GET")
                || requestMethod.Equals("COPY")
                || requestMethod.Equals("HEAD")
                || requestMethod.Equals("PURGE")
                || requestMethod.Equals("UNLOCK"))
            {
                // #HTTP RAW - Body
                parameters.AddWithValue("@BodyContent", requestBody);
            }
            else
            {
                parameters.AddWithValue("@BodyContent", null);
            }

            mapper.ExecuteGeneralSP("api.pa_AddApiLog", parameters);
        }

        public void Log( Guid uuidMessageIndentifier, IDictionary<string, string> dGeneralInfo, IDictionary<string, string> dHeadersInfo, IDictionary<string, string> dActionArguments, IDictionary<string, string> dRequestInformation, string requestBody )
        {
            string ApplicationName = GetAplicationName();
        }
    }
}