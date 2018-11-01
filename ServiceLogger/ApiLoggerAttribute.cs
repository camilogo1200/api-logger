using APILogger.Persistence;
using APILogger.Persistence.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace ServiceLogger
{
    [AttributeUsage(AttributeTargets.All)]
    public class ApiLoggerAttribute : ActionFilterAttribute
    {
        #region Attributes

        private Guid _uuidMessageIndentifier;
        private readonly string _logLevelIndicatorFlag = "LogLevelIndicatorFlag";
        private string _logLevel = "info";

        #endregion Attributes

        public bool Database { get; set; }
        public bool TextFile { get; set; }
        public bool EventViewer { get; set; }

        private readonly ILogger fileLogger = FileLogger.Instance;
        private readonly ILogger databaseLogger = DatabaseLogger.Instance;
        private readonly ILogger eventViewer = EventViewerLogger.Instance;

        public ApiLoggerAttribute()
        {
        }

        public override Task OnActionExecutingAsync( HttpActionContext actionContext, CancellationToken cancellationToken )
        {
            string logLevelIndicator = ConfigurationManager.AppSettings.Get(_logLevelIndicatorFlag);
            _logLevel = ConfigurationManager.AppSettings.Get(logLevelIndicator);

            IDictionary<string, string> dgeneralInfo = GetContextInformation(actionContext);
            IDictionary<string, string> dHeadersInfo = GetActionHeadersInformation(actionContext);
            IDictionary<string, string> dActionArguments = GetActionArgumentsInformation(actionContext);
            IDictionary<string, string> dRequestInformation = GetRequestInformation(actionContext);

            string requestBody = RequestBody();

            _uuidMessageIndentifier = Guid.NewGuid();
            fileLogger.Log(_uuidMessageIndentifier, dgeneralInfo, dHeadersInfo, dActionArguments, dRequestInformation, requestBody);

            if (TextFile)
                fileLogger.Log(_uuidMessageIndentifier, _logLevel, dgeneralInfo, dHeadersInfo, dActionArguments, dRequestInformation, requestBody, true);
            if (Database)
            {
                databaseLogger.Log(_uuidMessageIndentifier, _logLevel, dgeneralInfo, dHeadersInfo, dActionArguments, dRequestInformation, requestBody, true);
            }
            if (EventViewer)
            {
                eventViewer.Log(_uuidMessageIndentifier, _logLevel, dgeneralInfo, dHeadersInfo, dActionArguments, dRequestInformation, requestBody, true);//LofInfoToEventViewer();
            }

            return base.OnActionExecutingAsync(actionContext, cancellationToken);
        }

        public string RequestBody()
        {
            var bodyStream = new StreamReader(HttpContext.Current.Request.InputStream);
            bodyStream.BaseStream.Seek(0, SeekOrigin.Begin);
            var bodyText = bodyStream.ReadToEnd();
            return bodyText;
        }

        #region Request Information

        private IDictionary<string, string> GetRequestInformation( HttpActionContext actionContext )
        {
            IDictionary<string, string> dRequestInfo = new Dictionary<string, string>();
            HttpRequestMessage requestMessage = actionContext.Request;
            HttpRequestContext requestContext = actionContext.RequestContext;

            Uri uri = requestMessage.RequestUri;

            string requestMethod = requestMessage.Method.Method;
            string requestAbsoluteURI = uri.AbsoluteUri;
            string absolutePath = uri.AbsolutePath;
            string localPath = uri.LocalPath;
            string authority = uri.Authority;
            string host = uri.Host;
            bool isFile = uri.IsFile;
            int requestPort = uri.Port;
            string scheme = uri.Scheme;

            dRequestInfo.Add("Request Method", requestMethod);
            dRequestInfo.Add("Request Absolute URI", requestAbsoluteURI);
            dRequestInfo.Add("Request Absolute Path", absolutePath);
            dRequestInfo.Add("Request Local Path", localPath);
            dRequestInfo.Add("Request Authority", authority);
            dRequestInfo.Add("Request Host", host);
            dRequestInfo.Add("Request is File", isFile.ToString());
            dRequestInfo.Add("Request Request Port", requestPort.ToString());
            dRequestInfo.Add("Request Scheme", scheme);

            HttpContentHeaders contentHeaders = requestMessage.Content.Headers;

            foreach (KeyValuePair<string, IEnumerable<string>> iHeaders in contentHeaders)
            {
                string key = iHeaders.Key;
                IEnumerable<string> lValues = iHeaders.Value;
                string values = string.Join(",", lValues.ToArray());
                dRequestInfo.Add(key, values);
            }

            //DateTime requestTimestamp =
            HttpRequestBase rMessage = ( ( HttpContextWrapper )actionContext.Request.Properties["MS_HttpContext"] ).Request;
            //rMessage.ServerVariables.

            List<string> lServerVars = rMessage.ServerVariables.AllKeys.ToList();
            string val = null;
            foreach (string key in lServerVars)
            {
                val = rMessage.ServerVariables[key];
                if (!string.IsNullOrEmpty(val))
                {
                    dRequestInfo.Add(key, val);
                }
            }

            string LogonUser = rMessage.LogonUserIdentity.Name;
            dRequestInfo.Add("Request Logon User", LogonUser);
            if (rMessage.LogonUserIdentity.UserClaims.Any())
            {
                Claim c = rMessage.LogonUserIdentity.UserClaims.ToList()[0];
                string issuer = c.Issuer;
                string valueUse = c.Value;
                string originalIssuer = c.OriginalIssuer;

                dRequestInfo.Add("LogonUserIdentity  - issuer", issuer);
                dRequestInfo.Add("LogonUserIdentity  - originalIssuer", originalIssuer);
                dRequestInfo.Add("LogonUserIdentity  - value", valueUse);
            }

            DateTime requestTime = rMessage.RequestContext.HttpContext.Timestamp;

            dRequestInfo.Add("Request Timestamp ( Long Date )", requestTime.ToLongDateString());
            dRequestInfo.Add("Request Local Time", "[" + requestTime.ToLocalTime().ToString() + "]");
            return dRequestInfo;
        }

        #endregion Request Information

        #region Action Arguments Information

        private IDictionary<string, string> GetActionArgumentsInformation( HttpActionContext actionContext )
        {
            HttpActionDescriptor actionDescriptor = actionContext.ActionDescriptor;
            IDictionary<string, string> dActionArguments = new Dictionary<string, string>
            {
                { "ActionName", actionDescriptor.ActionName }
            };

            IList<KeyValuePair<string, object>> lArguments = actionContext.ActionArguments.ToList();
            foreach (KeyValuePair<string, object> argument in lArguments)
            {
                string key = argument.Key;
                string jsonObject = JsonConvert.SerializeObject(argument.Value);
                dActionArguments.Add(key, jsonObject);
            }
            Type type = ( ( System.Web.Http.Controllers.ReflectedHttpActionDescriptor )actionDescriptor ).MethodInfo.DeclaringType;
            string moduleVersionId = type.Module.ModuleVersionId.ToString();
            string ModuleName = type.Module.Name;

            dActionArguments.Add("ModuleVersionId", moduleVersionId);
            dActionArguments.Add("ModuleName", ModuleName);

            return dActionArguments;
        }

        #endregion Action Arguments Information

        #region Headers Information

        private IDictionary<string, string> GetActionHeadersInformation( HttpActionContext actionContext )
        {
            IEnumerator<KeyValuePair<string, IEnumerable<string>>> eHeaders = actionContext.Request.Headers.GetEnumerator();
            IDictionary<string, string> dHeaders = new Dictionary<string, string>();
            while (eHeaders.MoveNext())
            {
                KeyValuePair<string, IEnumerable<string>> kHeaders = eHeaders.Current;
                string key = kHeaders.Key;
                IEnumerable<string> iHeaderV = kHeaders.Value;
                string values = string.Join(",", iHeaderV);
                dHeaders.Add(key, values);
            }
            return dHeaders;
        }

        #endregion Headers Information

        #region Controller Context

        private IDictionary<string, string> GetContextInformation( HttpActionContext actionContext )
        {
            HttpControllerContext controllerContext = actionContext.ControllerContext;

            IDictionary<string, string> dGralInfo = new Dictionary<string, string>();
            string contextCtrlName = controllerContext.ControllerDescriptor.ControllerType.Name;
            string contextCtrlFullname = controllerContext.ControllerDescriptor.ControllerType.FullName;
            string contextCtrlNamespace = controllerContext.ControllerDescriptor.ControllerType.Namespace;
            string contextCtrlAssemblyFullname = controllerContext.ControllerDescriptor.ControllerType.Assembly.FullName;
            string contextCtrlAssemblyName = controllerContext.ControllerDescriptor.ControllerType.Assembly.GetName().ToString();
            string contextCtrlBaseType = controllerContext.ControllerDescriptor.ControllerType.BaseType.ToString();
            string requestURI = controllerContext.Request.RequestUri.ToString();

            //TODO getLogFileName ip addres

            dGralInfo.Add("Server Time", DateTime.Now.ToString());
            dGralInfo.Add("Request URI", requestURI);
            dGralInfo.Add("ControllerName", contextCtrlName);
            dGralInfo.Add("ControllerFullName", contextCtrlFullname);
            dGralInfo.Add("ControllerNamespace", contextCtrlNamespace);
            dGralInfo.Add("ControllerAssemblyFullName", contextCtrlAssemblyFullname);
            dGralInfo.Add("ControllerAssemblyName", contextCtrlAssemblyName);
            dGralInfo.Add("ControllerBaseType", contextCtrlBaseType);
            return dGralInfo;
        }

        #endregion Controller Context

        public override Task OnActionExecutedAsync( HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken )
        {
            return base.OnActionExecutedAsync(actionExecutedContext, cancellationToken);
        }
    }
}