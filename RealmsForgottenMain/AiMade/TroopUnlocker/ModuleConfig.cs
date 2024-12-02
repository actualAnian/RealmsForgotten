using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace T7TroopUnlocker
{
  public class ModuleConfig
  {
    private readonly System.Configuration.Configuration config;

    public ModuleConfig(string filename) => this.config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
    {
      ExeConfigFilename = filename
    }, ConfigurationUserLevel.None);

    public string GetSpecificConfig(string ConfigName) => ((IEnumerable<string>) this.config.AppSettings.Settings.AllKeys).Contains<string>(ConfigName) ? this.config.AppSettings.Settings[ConfigName].Value : (string) null;
  }
}
