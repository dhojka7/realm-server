using RotMG.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Xml.Linq;

namespace RotMG.Networking
{
    public static partial class AppServer
    {
        private static bool _terminating;
        private static HttpListener _listener;
        private static ManualResetEvent _listenEvent;

        public static void Init()
        {
            _listenEvent = new ManualResetEvent(true);
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{Settings.Address}:{Settings.Ports[0]}/");
        }

        public static void Stop()
        {
            _terminating = true;
            Thread.Sleep(200);
        }

        public static void Start()
        {
            _listener.Start();
            Program.Print(PrintType.Info, $"Started AppServer listening at <{_listener.Prefixes.First()}>");

            while (!_terminating)
            {
                try
                {
                    //Wait for new request.
                    HttpListenerContext context = _listener.GetContext();
                    //Process request and push work to main thread.
                    string request = context.Request.Url.LocalPath;
#if DEBUG
                            Program.Print(PrintType.Debug, $"Received <{request}> request from <{context.Request.RemoteEndPoint}>");
#endif

                    NameValueCollection query;
                    using (StreamReader r = new StreamReader(context.Request.InputStream))
                        query = HttpUtility.ParseQueryString(r.ReadToEnd());

                    byte[] buffer = null;
                    switch (request)
                    {
                        case "/char/list":
                            buffer = CharList(context, query);
                            break;
                        case "/account/verify":
                            buffer = Verify(context, query);
                            break;
                        case "/account/register":
                            buffer = Register(context, query);
                            break;
                        case "/fame/list":
                            buffer = FameList(context, query);
                            break;
                        case "/char/fame":
                            buffer = CharFame(context, query);
                            break;
                        case "/char/delete":
                            buffer = CharDelete(context, query);
                            break;
                        case "/account/purchaseCharSlot":
                            buffer = AccountPurchaseCharSlot(context, query);
                            break;
                        case "/account/purchaseSkin":
                            buffer = AccountPurchaseSkin(context, query);
                            break;
                        case "/account/changePassword":
                            buffer = AccountChangePassword(context, query);
                            break;
                        default:
                            Resources.WebFiles.TryGetValue(request, out buffer);
                            break;
                    }

#if DEBUG
                            foreach (string k in query.AllKeys)
                                Program.Print(PrintType.Debug, $"<{k}> <{query[k]}>");
#endif

                    if (buffer == null)
                    {
#if DEBUG
                                Program.Print(PrintType.Warn, $"No request handler for <{request}> request from <{context.Request.RemoteEndPoint}>");
#endif
                        buffer = WriteError("Internal server error");
                    }

                    //Send data and move onto next request in the next iteration of the loop
                    context.Response.ContentType = "text/*";
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    context.Response.Close();

                    Thread.Sleep(10);
                }
#if DEBUG
                catch (Exception ex)
                {
                    Program.Print(PrintType.Error, ex.ToString());
                }
#endif
#if RELEASE
                catch
                {

                }
#endif
            }
        }

        private static string GetIPFromContext(HttpListenerContext context)
        {
#if DEBUG
            if (context == null)
                throw new Exception("Undefined HttpListener context.");
#endif
            return context.Request.RemoteEndPoint.Address.ToString().Split(':')[0];
        }

        private static byte[] Write(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        private static byte[] WriteSuccess(string value = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Encoding.UTF8.GetBytes("<Success/>");
            return Encoding.UTF8.GetBytes($"<Success>{value}</Success>");
        }

        private static byte[] WriteError(string value = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Encoding.UTF8.GetBytes("<Error/>");
            return Encoding.UTF8.GetBytes($"<Error>{value}</Error>");
        }
    }
}
