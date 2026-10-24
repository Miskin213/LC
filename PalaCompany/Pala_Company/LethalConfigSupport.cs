using System;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;

namespace PalaCompany
{
    internal static class LethalConfigSupport
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Register(
            ConfigEntry<float> userCooldown,
            ConfigEntry<float> globalCooldown,
            ConfigEntry<int> maxLive,
            ConfigEntry<float> spawnRadius,
            ConfigEntry<bool> enableDebugKeys,
            ConfigEntry<bool> countOnlyModEnemies,
            ConfigEntry<bool> showSpawnTips,
            ConfigEntry<bool> kickEnabled,
            ConfigEntry<string> prefix,
            ConfigEntry<string> blocklist,
            ConfigEntry<string> kickChatroomId,
            ConfigEntry<string> kickAppKey,
            ConfigEntry<string> kickCluster,
            Action restartKick)
        {
            LethalConfigManager.SkipAutoGen();

            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(userCooldown, new FloatSliderOptions
            {
                Min = 0f,
                Max = 300f,
                RequiresRestart = false
            }));
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(globalCooldown, new FloatSliderOptions
            {
                Min = 0f,
                Max = 120f,
                RequiresRestart = false
            }));
            LethalConfigManager.AddConfigItem(new IntSliderConfigItem(maxLive, new IntSliderOptions
            {
                Min = 0,
                Max = 50,
                RequiresRestart = false
            }));
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(spawnRadius, new FloatSliderOptions
            {
                Min = 1f,
                Max = 50f,
                RequiresRestart = false
            }));

            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(enableDebugKeys, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(countOnlyModEnemies, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(showSpawnTips, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(kickEnabled, requiresRestart: false));

            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(prefix, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(blocklist, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(kickChatroomId, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(kickAppKey, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(kickCluster, requiresRestart: false));

            LethalConfigManager.AddConfigItem(new GenericButtonConfigItem(
                "Kick",
                "Reconnect Kick",
                "Stop and restart the Kick chat connection using the current settings.",
                "Reconnect Kick",
                () => restartKick()));
        }
    }
}
