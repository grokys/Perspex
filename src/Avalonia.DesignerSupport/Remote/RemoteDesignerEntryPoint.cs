using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using Avalonia.Controls;
using Avalonia.DesignerSupport.Remote.HtmlTransport;
using Avalonia.Remote.Protocol;
using Avalonia.Remote.Protocol.Designer;
using Avalonia.Remote.Protocol.Viewport;
using Avalonia.Threading;

namespace Avalonia.DesignerSupport.Remote
{
    public class RemoteDesignerEntryPoint
    {
        private static ClientSupportedPixelFormatsMessage s_supportedPixelFormats;
        private static ClientViewportAllocatedMessage s_viewportAllocatedMessage;
        private static ClientRenderInfoMessage s_renderInfoMessage;
        private static double s_lastRenderScaling = 1.0;

        private static IAvaloniaRemoteTransportConnection s_transport;
        class CommandLineArgs
        {
            public string AppPath { get; set; }
            public Uri Transport { get; set; }
            public Uri HtmlMethodListenUri { get; set; }
            public string Method { get; set; } = Methods.AvaloniaRemote;
            public string SessionId { get; set; } = Guid.NewGuid().ToString();
            public bool Debug { get; set; }
        }

        internal static class Methods
        {
            public const string AvaloniaRemote = "avalonia-remote";
            public const string Win32 = "win32";
            public const string Html = "html";
        }

        static Exception Die(string error)
        {
            if (error != null)
            {
                Console.Error.WriteLine(error);
                Console.Error.Flush();
            }
            Environment.Exit(1);
            return new Exception("APPEXIT");
        }

        static void Log(string message) => Console.WriteLine(message);

        static Exception PrintUsage()
        {
            Console.Error.WriteLine("Usage: --transport transport_spec --session-id sid --method method app");
            Console.Error.WriteLine();
            Console.Error.WriteLine("--transport: transport used for communication with the IDE");
            Console.Error.WriteLine("    'tcp-bson' (e. g. 'tcp-bson://127.0.0.1:30243/') - TCP-based transport with BSON serialization of messages defined in Avalonia.Remote.Protocol");
            Console.Error.WriteLine("    'file' (e. g. 'file://C://my/file.xaml' - pseudo-transport that triggers XAML updates on file changes, useful as a standalone previewer tool, always uses http preview method");
            Console.Error.WriteLine();
            Console.Error.WriteLine("--session-id: session id to be sent to IDE process");
            Console.Error.WriteLine();
            Console.Error.WriteLine("--method: the way the XAML is displayed");
            Console.Error.WriteLine("    'avalonia-remote' - binary image is sent via transport connection in FrameMessage");
            Console.Error.WriteLine("    'win32' - XAML is displayed in win32 window (handle could be obtained from UpdateXamlResultMessage), IDE is responsible to use user32!SetParent");
            Console.Error.WriteLine("    'html' - Previewer starts an HTML server and displays XAML previewer as a web page");
            Console.Error.WriteLine();
            Console.Error.WriteLine("--html-url - endpoint for HTML method to listen on, e. g. http://127.0.0.1:8081");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Example: --transport tcp-bson://127.0.0.1:30243/ --session-id 123 --method avalonia-remote MyApp.exe");
            Console.Error.Flush();
            return Die(null);
        }
        
        static CommandLineArgs ParseCommandLineArgs(string[] args)
        {
            var rv = new CommandLineArgs();
            Action<string> next = null;
            try
            {
                foreach (var arg in args)
                {
                    if (next != null)
                    {
                        next(arg);
                        next = null;
                    }
                    else if (arg == "--transport")
                        next = a => rv.Transport = new Uri(a, UriKind.Absolute);
                    else if (arg == "--method")
                        next = a => rv.Method = a;
                    else if (arg == "--html-url")
                        next = a => rv.HtmlMethodListenUri = new Uri(a, UriKind.Absolute);
                    else if (arg == "--session-id")
                        next = a => rv.SessionId = a;
                    else if (arg == "--debug")
                    {
                        rv.Debug = true;
                        next = null;
                    }
                    else if (rv.AppPath == null)
                        rv.AppPath = arg;
                    else
                        PrintUsage();

                }
                if (rv.AppPath == null || rv.Transport == null)
                    PrintUsage();
            }
            catch
            {
                PrintUsage();
            }

            if (next != null)
                PrintUsage();
            return rv;
        }

        static IAvaloniaRemoteTransportConnection CreateTransport(CommandLineArgs args)
        {
            var transport = args.Transport;
            if (transport.Scheme == "tcp-bson")
            {
                return new BsonTcpTransport().Connect(IPAddress.Parse(transport.Host), transport.Port).Result;
            }

            if (transport.Scheme == "file")
            {
                return new FileWatcherTransport(transport, args.AppPath);
            }
            PrintUsage();
            return null;
        }
        
        interface IAppInitializer
        {
           IAvaloniaRemoteTransportConnection ConfigureApp(IAvaloniaRemoteTransportConnection transport, CommandLineArgs args, object obj);
        }

        class AppInitializer : IAppInitializer
        {
            public IAvaloniaRemoteTransportConnection ConfigureApp(IAvaloniaRemoteTransportConnection transport,
                CommandLineArgs args, object obj)
            {
                var builder = (AppBuilder)obj;
                builder = builder.UseStandardRuntimePlatformSubsystem();
                if (args.Method == Methods.AvaloniaRemote)
                    builder.UseWindowingSubsystem(() => PreviewerWindowingPlatform.Initialize(transport));
                if (args.Method == Methods.Html)
                {
                    transport = new HtmlWebSocketTransport(transport,
                        args.HtmlMethodListenUri ?? new Uri("http://localhost:5000"));
                    builder.UseWindowingSubsystem(() =>
                        PreviewerWindowingPlatform.Initialize(transport));
                }

                if (args.Method == Methods.Win32)
                    builder.UseWindowingSubsystem(GetInitializer("Avalonia.Win32"), "Win32");
                builder.SetupWithoutStarting();
                return transport;
            }

            private static Action GetInitializer(string assemblyName) => () =>
            {
                var assembly = Assembly.Load(new AssemblyName(assemblyName));
                var platformClassName = assemblyName.Replace("Avalonia.", string.Empty) + "Platform";
                var platformClassFullName = assemblyName + "." + platformClassName;
                var platformClass = assembly.GetType(platformClassFullName);
                var init = platformClass!.GetRuntimeMethod("Initialize", Type.EmptyTypes);
                init!.Invoke(null, null);
            };
        }

        public static void Main(string[] cmdline)
        {
            var args = ParseCommandLineArgs(cmdline);
            if (args.Debug)
            {
                Avalonia.Utilities.DebuggerHelper.Launch(Log);
            }
            var transport = CreateTransport(args);
            if (transport is ITransportWithEnforcedMethod enforcedMethod)
                args.Method = enforcedMethod.PreviewerMethod;
            var asm = Assembly.LoadFrom(System.IO.Path.GetFullPath(args.AppPath));
            var entryPoint = asm.EntryPoint ?? throw Die($"Assembly {args.AppPath} doesn't have an entry point");
            Log($"Initializing application in design mode");
            Design.IsDesignMode = true;
            Log($"Obtaining AppBuilder instance from {entryPoint.DeclaringType!.FullName}");
            var appBuilder = AppBuilder.Configure(entryPoint.DeclaringType);
            var initializer = (IAppInitializer)Activator.CreateInstance(typeof(AppInitializer));
            transport = initializer.ConfigureApp(transport, args, appBuilder);
            s_transport = transport;
            transport.OnMessage += OnTransportMessage;
            transport.OnException += (t, e) => Die(e.ToString());
            transport.Start();
            Log("Sending StartDesignerSessionMessage");
            transport.Send(new StartDesignerSessionMessage { SessionId = args.SessionId });

            Dispatcher.UIThread.MainLoop(CancellationToken.None);
        }


        private static void RebuildPreFlight()
        {
            PreviewerWindowingPlatform.PreFlightMessages = new List<object>
            {
                s_supportedPixelFormats,
                s_viewportAllocatedMessage,
                s_renderInfoMessage
            };
        }

        private static Window s_currentWindow;
        private static void OnTransportMessage(IAvaloniaRemoteTransportConnection transport, object obj) => Dispatcher.UIThread.Post(static arg =>
        {
            if (arg is ClientSupportedPixelFormatsMessage formats)
            {
                s_supportedPixelFormats = formats;
                RebuildPreFlight();
            }
            if (arg is ClientRenderInfoMessage renderInfo)
            {
                s_renderInfoMessage = renderInfo;
                RebuildPreFlight();
            }
            if (arg is ClientViewportAllocatedMessage viewport)
            {
                s_viewportAllocatedMessage = viewport;
                RebuildPreFlight();
            }
            if (arg is UpdateXamlMessage xaml)
            {
                if (s_currentWindow is not null)
                    s_lastRenderScaling = s_currentWindow.RenderScaling;

                try
                {
                    s_currentWindow?.Close();
                }
                catch
                {
                    //Ignore
                }
                s_currentWindow = null;
                try
                {
                    s_currentWindow = DesignWindowLoader.LoadDesignerWindow(xaml.Xaml, xaml.AssemblyPath, xaml.XamlFileProjectPath, s_lastRenderScaling);
                    s_transport.Send(new UpdateXamlResultMessage() { Handle = s_currentWindow.PlatformImpl?.Handle?.Handle.ToString() });
                }
                catch (Exception e)
                {
                    s_transport.Send(new UpdateXamlResultMessage
                    {
                        Error = e.ToString(),
                        Exception = new ExceptionDetails(e),
                    });
                }
            }
        }, obj);
    }
}
