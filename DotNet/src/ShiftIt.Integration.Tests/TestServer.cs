using System;
using System.Net;
using System.Text;
using System.Threading;

namespace ShiftIt.Integration.Tests
{
    /// <summary>
    /// A simple web listener class
    /// </summary>
    public class TestServer:IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Func<HttpListenerRequest, HttpListenerResponse, string> _responderMethod;
        public readonly int Port;

        /// <summary>
        /// Start a web listener, given a responder function.
        /// This will always listen on a localhost port.
        /// </summary>
        public TestServer(Func<HttpListenerRequest, HttpListenerResponse, string> method)
        {
            if (!HttpListener.IsSupported) throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");
            if (method == null) throw new ArgumentException("A responder method must be provided", "method");

            if ( ! TryBindListenerOnFreePort(out _listener, out Port, out var lastError)) {
                throw new Exception("Failed to find an available ephemeral IP port. Possible a permissions issue. " + lastError);
            }

            _responderMethod = method;
            ListenerLoop();
        }

        /// <summary>
        /// Get the port in use
        /// </summary>
        public int GetPort() {
            return Port;
        }

        private static bool TryBindListenerOnFreePort(out HttpListener httpListener, out int port, out Exception lastError)
        {
            // IANA suggested range for dynamic or private ports
            const int MinPort = 49215;
            const int MaxPort = 65535;
            lastError = new Exception("No error recorded");

            for (port = MinPort; port < MaxPort; port++)
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add($"http://+:{port}/");
                httpListener.Prefixes.Add($"http://*:{port}/");
                try
                {
                    httpListener.Start();
                    return true;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    // nothing to do here -- the listener disposes itself when Start fails
                }
            }

            port = 0;
            httpListener = null;
            return false;
        }

        private void ListenerLoop()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    while (_listener.IsListening)
                        ThreadPool.QueueUserWorkItem(c =>
                        {
                            var ctx = c as HttpListenerContext;
                            if (ctx == null) return;
                            Run(ctx);
                        }, _listener.GetContext());
                }
                catch (HttpListenerException)
                {
                    Ignore();
                }
            });

        }

        private void Run(HttpListenerContext ctx)
        {
            try
            {
                ctx.Response.SendChunked = true; // Cheat: we don't have to set a content-length.
                var rstr = _responderMethod(ctx.Request, ctx.Response);
                if (rstr != null)
                {
                    var buf = Encoding.UTF8.GetBytes(rstr);
                    ctx.Response.ContentLength64 = buf.Length;
                    ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                }
            }
            catch (Exception ex_inner)
            {
                Console.WriteLine("Error: " + ex_inner);
            }
            finally
            {
                ctx.Response.OutputStream.Flush();
                ctx.Response.OutputStream.Dispose();
            }
        }

        private void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Stop();
        }

        private static void Ignore() { }
    }
}