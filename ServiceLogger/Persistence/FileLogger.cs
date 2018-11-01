using APILogger.Persistence.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace APILogger.Persistence
{
    public class FileLogger : LogPersistence, ILogger
    {
        #region Attributes

        private readonly string _logDefaultFileName = "ApiLogger";
        private readonly string _customFileNameIndicator = "LogFileName";
        private readonly string _customFilePathIndicator = "LogFilePath";
        private readonly string _customLogFilePathIndicatorFlag = "LogFilePathIndicator";
        private readonly string _errorFileName = "ApiLogger-error";
        private readonly string _logDefaultExtension = ".log";
        private readonly string _pipe = " | ";

        private const string _logLevelInfo = "info";
        private const string _logLevelFull = "full";
        private const string _logLevelDebug = "debug";

        private string _fileName;
        private string _filePath;

        #endregion Attributes

        #region Singleton

        /// <summary>
        /// Atributo utilizado para evitar problemas con multithreading en el singleton.
        /// </summary>
        private static readonly object syncRoot = new Object();

        private static volatile FileLogger instance;
        private readonly string _splitter = "################################################################################";
        private readonly string _softSplitter = "--------------------------------------------------------------------------------";

        public static FileLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new FileLogger();
                        }
                    }
                }
                return instance;
            }
        }

        #endregion Singleton

        private FileLogger()
        {
            string customLogFilePath = ConfigurationManager.AppSettings.Get(_customLogFilePathIndicatorFlag);
            _filePath = ConfigurationManager.AppSettings.Get(customLogFilePath);
        }

        #region Properties

        public string Filename
        {
            get
            {
                return _fileName ?? ( _fileName = GetLogFileName() );
            }
        }

        public string FilePath
        {
            get
            {
                return _filePath ?? ( _filePath = GetLogFilePath() );
            }
        }

        #endregion Properties

        private string GetLogFilePath()
        {
            string name = ConfigurationManager.AppSettings.Get(_customFilePathIndicator);
            if (string.IsNullOrEmpty(name))
            {
                return System.Reflection.Assembly.GetExecutingAssembly().Location;
            }
            return name;
        }

        private string GetLogFileName()
        {
            string name = ConfigurationManager.AppSettings.Get(_customFileNameIndicator);
            if (string.IsNullOrEmpty(name))
            {
                return _logDefaultFileName + _logDefaultExtension;
            }
            return name;
        }

        public void Log(
            Guid uuid,
           IDictionary<string, string> dGeneralInfo,
           IDictionary<string, string> dHeadersInfo,
           IDictionary<string, string> dActionArguments,
           IDictionary<string, string> dRequestInformation,
           string requestBody )
        {
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
            string logLine = DateTime.Now.ToString()
                + _pipe + uuid.ToString()
                + _pipe + requestScheme.ToUpper()
                + _pipe + serverProtocol.ToUpper()
                + _pipe + remoteAddr
                + _pipe + remoteHost
                + _pipe + localAddr
                + _pipe + requestMethod.ToUpper()
                + _pipe + requestURI
                + _pipe + controllerName
                + _pipe + controllerMethodName
                + _pipe + contentType
                + _pipe + requestTime;
            SaveLog(logLine);
        }

        #region Log File

        private void AppendToFile( string fileName, string payload )
        {
            StreamWriter writter = null;
            try
            {
                string path = Path.Combine(FilePath, fileName + _logDefaultExtension);
                FileStream file = File.Open(path, FileMode.Append, FileAccess.Write);
                writter = new StreamWriter(file);
                writter.WriteLine(payload);
                writter.Flush();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                string eventDatetime = DateTime.Now.ToString("yyyy-MM-ddTHHmmss.fff");
                string path = Path.Combine(FilePath, _errorFileName + eventDatetime + _logDefaultExtension);
                FileStream file = File.Open(path, FileMode.Append, FileAccess.Write);
                writter = new StreamWriter(file);
                writter.WriteLine(ex.Message);
                writter.WriteLine(ex.InnerException.ToString());
                writter.Flush();
            }
            finally
            {
                if (writter != null)
                {
                    writter.Close();
                    writter.Dispose();
                }
            }
        }

        #endregion Log File

        public override void SaveLog( string message, bool custom = false )
        {
            //Log the request on default file

            if (!custom)
            {
                AppendToFile(_logDefaultFileName, message);
            }
            else
            {
                AppendToFile(Filename, message);
            }
        }

        public void Log( Guid uuidRequest, string _logLevel,
            IDictionary<string, string> dGeneralInfo,
            IDictionary<string, string> dHeadersInfo,
            IDictionary<string, string> dActionArguments,
            IDictionary<string, string> dRequestInformation,
            string requestBody,
            bool isRequest
            )
        {
            string eventDateTime = DateTime.Now.ToString("yyyy-MM-dd T HH:mm:ss.fff");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(_splitter);
            sb.Append("# EventDateTime : ").AppendLine(eventDateTime);
            sb.Append("# IsRequest : ").AppendLine(isRequest.ToString());
            sb.Append("# Request URI : ").AppendLine(dGeneralInfo["Request URI"]);
            sb.Append("# Request Method : ").AppendLine(dRequestInformation["Request Method"].ToUpper());
            sb.Append("# Request Uuid : ").AppendLine(uuidRequest.ToString());
            sb.AppendLine(_splitter);
            sb.AppendLine(GetCustomLevelLogText(_logLevel, dGeneralInfo, dHeadersInfo, dActionArguments, dRequestInformation, requestBody));
            sb.AppendLine(_splitter);
            sb.Append("# End Request Uuid : ").AppendLine(uuidRequest.ToString());
            sb.Append("# EventDateTime : ").AppendLine(eventDateTime);
            sb.AppendLine(_splitter);

            SaveLog(sb.ToString(), true);
        }

        private string GetCustomLevelLogText(
            string logLevel, IDictionary<string, string> dGeneralInfo,
            IDictionary<string, string> dHeadersInfo,
            IDictionary<string, string> dActionArguments,
            IDictionary<string, string> dRequestInformation,
            string requestBody )
        {
            string st = null;
            switch (logLevel)
            {
                case _logLevelInfo:
                    st = getLogLevelInfo(dGeneralInfo, dHeadersInfo, dActionArguments, dRequestInformation, requestBody);
                    break;

                case _logLevelFull:
                    //st = getLogLevelFull(dGeneralInfo, dHeadersInfo, dActionArguments, dRequestInformation,requestBody);
                    break;

                case _logLevelDebug:
                    //st = getLogLevelDebug(dGeneralInfo, dHeadersInfo, dActionArguments, dRequestInformation,requestBody);
                    break;

                default:
                    st = getLogLevelInfo(dGeneralInfo, dHeadersInfo, dActionArguments, dRequestInformation, requestBody);
                    break;
            }
            return st;
        }

        private string getLogLevelInfo(
            IDictionary<string, string> dGeneralInfo,
            IDictionary<string, string> dHeadersInfo,
            IDictionary<string, string> dActionArguments,
            IDictionary<string, string> dRequestInformation,
            string requestBody )
        {
            StringBuilder sb = new StringBuilder();
            string requestMethod = dRequestInformation["Request Method"].ToUpper();
            sb.Append("Server Time : ").AppendLine(dGeneralInfo["Server Time"]);
            sb.Append("Request URI : ").AppendLine(dGeneralInfo["Request URI"]);
            sb.Append("Request Method : ").AppendLine(requestMethod);
            sb.Append("Controller Name : ").AppendLine(dGeneralInfo["ControllerName"]);
            sb.Append("Action Name (Method Name) : ").AppendLine(dActionArguments["ActionName"]);
            sb.Append("Request is File : ").AppendLine(dRequestInformation["Request is File"]);
            sb.Append("Local Address : ").AppendLine(dRequestInformation["LOCAL_ADDR"]);
            sb.Append("Remote Address : ").AppendLine(dRequestInformation["REMOTE_ADDR"]);
            sb.Append("Remote Host : ").AppendLine(dRequestInformation["REMOTE_HOST"]);
            sb.Append("Server Port : ").AppendLine(dRequestInformation["SERVER_PORT"]);
            sb.Append("Server Protocol : ").AppendLine(dRequestInformation["SERVER_PROTOCOL"]);
            sb.Append("Request Event (Local) Time : ").AppendLine(dRequestInformation["Request Local Time"]);
            sb.AppendLine(_softSplitter);
            sb.AppendLine("# HTTP RAW - Headers");
            sb.AppendLine(_softSplitter);
            sb.AppendLine(dRequestInformation["ALL_RAW"].TrimEnd());

            sb.AppendLine(_softSplitter);
            sb.AppendLine("# HTTP RAW - Parameters");
            sb.AppendLine(_softSplitter);

            List<KeyValuePair<string, string>> lParms = dActionArguments.ToList();
            foreach (KeyValuePair<string, string> argument in lParms)
            {
                sb.Append(argument.Key).Append(" : ").AppendLine(argument.Value);
            }

            if (!requestMethod.Equals("GET")
                || !requestMethod.Equals("COPY")
                || !requestMethod.Equals("HEAD")
                || !requestMethod.Equals("PURGE")
                || !requestMethod.Equals("UNLOCK"))
            {
                sb.AppendLine(_softSplitter);
                sb.AppendLine("# HTTP RAW - Body");
                sb.AppendLine(_softSplitter);
                sb.AppendLine(requestBody);
            }
            return sb.ToString();
        }
    }
}