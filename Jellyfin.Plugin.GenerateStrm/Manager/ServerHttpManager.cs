using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.GenerateStrm.Manager
{
    public class ServerManager : IServerEntryPoint
    {
        private bool disposedValue = false;
        private Process rcloneHTTPProcess;

        public static string ipServer = "[::1]";
        public static string portServer = "5000";
        private ILogger<ServerManager> _logger;
        public ServerManager(ILogger<ServerManager> logger)
        {
            this.rcloneHTTPProcess = null;
            this._logger = logger;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (this.rcloneHTTPProcess != null)
                {
                    this.rcloneHTTPProcess.Kill(true);
                    this.rcloneHTTPProcess.WaitForExit();
                    this.rcloneHTTPProcess.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public Task RunAsync()
        {
            if (Plugin.Instance.Configuration.rclonePATH.Length > 0 && Plugin.Instance.Configuration.rcloneConfigPATH.Length > 0 && Plugin.Instance.Configuration.rcloneRemoteDrive.Length > 0)
            {
                try
                {
                    this.rcloneHTTPProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = Plugin.Instance.Configuration.rclonePATH,
                            Arguments = String.Format("serve http --config \"{0}\" --addr {1}:{2} --read-only \"{3}:{4}\"", new string[5]{Plugin.Instance.Configuration.rcloneConfigPATH, ipServer, portServer, Plugin.Instance.Configuration.rcloneRemoteDrive, Plugin.Instance.Configuration.rcloneDrivePATH}),
                            WindowStyle = ProcessWindowStyle.Hidden,
                            UseShellExecute = false
                        }
                    };
                    _logger.LogInformation(String.Format("Iniciando rclone server no endereço {0}:{1}", ipServer, portServer));
                    rcloneHTTPProcess.Start();
                }
                catch (System.Exception e)
                {
                    _logger.LogError("Rclone Server Error: "+e.Message);
                    this.rcloneHTTPProcess = null;
                }
            }
            else
            {
                _logger.LogError("Configure para poder iniciar, após configurar reinicie o servidor");
            }
            return Task.CompletedTask;
        }
    }
}