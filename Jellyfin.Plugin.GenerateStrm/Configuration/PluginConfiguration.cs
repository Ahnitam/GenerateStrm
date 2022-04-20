using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.GenerateStrm.Configuration{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            rclonePATH = "";
            rcloneConfigPATH = "";
            rcloneRemoteDrive = "";
            rcloneDrivePATH = "/";
            rcloneMediaPATH = "";
            rcloneArquivosSalvos = "";
        }
        public string rclonePATH { get; set; }
        public string rcloneMediaPATH { get; set; }
        public string rcloneDrivePATH { get; set; }
        public string rcloneConfigPATH { get; set; }
        public string rcloneRemoteDrive { get; set; }
        public string rcloneArquivosSalvos { get; set; }
    }
}