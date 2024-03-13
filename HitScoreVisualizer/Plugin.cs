using HitScoreVisualizer.Installers;
using HitScoreVisualizer.Settings;
using Hive.Versioning;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Loader;
using IPA.Logging;
using SiraUtil.Zenject;

namespace HitScoreVisualizer
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static Version Version { get; private set; } = null!;

        [Init]
        public void Init(Logger logger, Config config, PluginMetadata pluginMetadata, Zenjector zenject)
        {
            Version = pluginMetadata.Version;

            zenject.UseLogger(logger);
            zenject.UsePluginId("com.hitScoreVisualizer.plugin");

            zenject.OnApp<HsvAppInstaller>().WithConfig<HsvAppInstaller, HSVConfig>(config);
            zenject.OnMenu<HsvMenuInstaller>().OnlyForMultiplayer();
            zenject.OnGame<HsvGameInstaller>().OnlyForStandard();

            // If you want to install for specific locations like Tutorial and Player, you might need to handle it differently.
            // It's suggested to use the provided hooks like OnApp, OnMenu, OnGame for better compatibility across different Beat Saber versions.
        }
    }
}
