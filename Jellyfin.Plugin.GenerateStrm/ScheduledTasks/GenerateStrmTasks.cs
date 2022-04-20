using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Serialization;
using System.Diagnostics;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.GenerateStrm.Manager;


namespace Jellyfin.Plugin.GenerateStrm.ScheduledTasks
{
    public class GenerateStrmTask : IScheduledTask
    {
        private readonly ILogger<GenerateStrmTask> _logger;
        private readonly ITaskManager _taskManager;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILocalizationManager _localization;
        public GenerateStrmTask(IJsonSerializer jsonSerializer, ITaskManager taskManager, ILogger<GenerateStrmTask> logger, ILocalizationManager localization)
        {
            _logger = logger;
            _taskManager = taskManager;
            _localization = localization;
            _jsonSerializer = jsonSerializer;
        }
        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (Plugin.Instance.Configuration.rclonePATH.Length > 0 && Plugin.Instance.Configuration.rcloneConfigPATH.Length > 0 && Plugin.Instance.Configuration.rcloneRemoteDrive.Length > 0 && Plugin.Instance.Configuration.rcloneMediaPATH.Length > 1)
            {
                Dictionary<string, string> filesSaved = new Dictionary<string, string>();
                foreach (string file in Plugin.Instance.Configuration.rcloneArquivosSalvos.Split(";", StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] temp = file.Split("=");
                    filesSaved.Add(temp[0], temp[1]);
                }
                updateFiles(filesSaved);
                Plugin.Instance.UpdateConfiguration(Plugin.Instance.Configuration);
                foreach (IScheduledTaskWorker item in _taskManager.ScheduledTasks)
                {
                    if (item.ScheduledTask.Key == "RefreshLibrary")
                    {
                        _taskManager.Execute(item, new TaskOptions());
                    }
                }
            }

            return Task.CompletedTask;
        }

        private void updateFiles(Dictionary<string, string> oldFiles)
        {
            try
            {
                Process rcloneProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Plugin.Instance.Configuration.rclonePATH,
                        Arguments = String.Format("lsf -R --config {0} --format {1} --hash {2} --separator \"{3}\" {4} \"{5}:{6}\"", new string[7] { Plugin.Instance.Configuration.rcloneConfigPATH, "ph", "MD5", ";", "--files-only", Plugin.Instance.Configuration.rcloneRemoteDrive, Plugin.Instance.Configuration.rcloneDrivePATH }),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = false
                    }
                };
                rcloneProcess.Start();
                Plugin.Instance.Configuration.rcloneArquivosSalvos = "";
                string standard_output;
                while ((standard_output = rcloneProcess.StandardOutput.ReadLine()) != null)
                {
                    Match result = Regex.Match(standard_output, @"((.+)\/((.+)\.(mkv|mp4|avi)));([\w\d]+)");
                    if (result.Success)
                    {
                        string pasta = Plugin.Instance.Configuration.rcloneMediaPATH;
                        foreach (var path in result.Groups[2].Value.Split("/", StringSplitOptions.RemoveEmptyEntries))
                        {
                            pasta = Path.Combine(pasta, path);
                        }
                        string arquivo = Path.Combine(pasta, $"{result.Groups[4].Value}.strm");
                        string md5 = result.Groups[6].Value;
                        if (md5 != oldFiles.GetValueOrDefault(arquivo, null))
                        {
                            Directory.CreateDirectory(pasta);
                            string url = $"{ServerManager.ipServer}:{ServerManager.portServer}/{HttpUtility.UrlPathEncode(result.Groups[1].Value)}";
                            using (StreamWriter sw = File.CreateText(arquivo))
                            {
                                sw.WriteLine(url);
                            }
                        }
                        oldFiles.Remove(arquivo);
                        Plugin.Instance.Configuration.rcloneArquivosSalvos += $"{arquivo}={md5};";
                    }
                }
                foreach (KeyValuePair<string, string> file in oldFiles)
                {
                    File.Delete(file.Key);
                }
            }
            catch (System.Exception e)
            {
                _logger.LogError("Deu Erro: " + e.Message);
            }
        }
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = TimeSpan.FromHours(24).Ticks
            };
        }
        public string Name => "Atualizar Arquivos Strm";
        public string Key => "UpdateStrmPath";
        public string Description => "Verifica se há atualizações no drive e atualiza/cria os arquivos strm";
        public string Category => _localization.GetLocalizedString("TasksLibraryCategory");
    }
}