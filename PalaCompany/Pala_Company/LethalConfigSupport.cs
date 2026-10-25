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
            ConfigEntry<string> perEnemyLimits,
            ConfigEntry<int> defaultPerEnemyMax,
            ConfigEntry<bool> enableDebugKeys,
            ConfigEntry<bool> countOnlyModEnemies,
            ConfigEntry<bool> showSpawnTips,
            ConfigEntry<bool> kickEnabled,
            ConfigEntry<string> prefix,
            ConfigEntry<string> blocklist,
            ConfigEntry<string> kickChatroomId,
            ConfigEntry<string> kickAppKey,
            ConfigEntry<string> kickCluster,
            Action restartKick,
            ConfigEntry<bool> pollEnabled,
            ConfigEntry<float> pollCycleSeconds,
            ConfigEntry<float> pollVoteSeconds,
            ConfigEntry<bool> pollAllowVoteChange,
            ConfigEntry<NoVotesOption> pollNoVotesBehavior,
            ConfigEntry<string> pollBlocklist,
            ConfigEntry<bool> pollWritePollFile,
            ConfigEntry<LanguageOption> displayLanguage,
            ConfigEntry<int> pollMaxPollsPerRound,
            ConfigEntry<bool> pollShowTargetPlayer,
            ConfigEntry<bool> spawnCommandEnabled,
            ConfigEntry<bool> pollHudEnabled,
            ConfigEntry<HudAnchor> pollHudAnchor,
            ConfigEntry<float> pollHudOffsetX,
            ConfigEntry<float> pollHudOffsetY,
            ConfigEntry<float> pollHudFontSize,
            ConfigEntry<float> pollHudOpacity,
            ConfigEntry<bool> pollHudShowBorder,
            ConfigEntry<float> pollHudBorderThickness,
            ConfigEntry<string> pollHudBorderColor,
            ConfigEntry<float> pollHudBoxWidth,
            ConfigEntry<float> pollHudPadding,
            ConfigEntry<bool> pollHudShowLatestVote)
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
            LethalConfigManager.AddConfigItem(new IntSliderConfigItem(defaultPerEnemyMax, new IntSliderOptions
            {
                Min = 0,
                Max = 10,
                RequiresRestart = false
            }));
            LethalConfigManager.AddConfigItem(new IntSliderConfigItem(pollMaxPollsPerRound, new IntSliderOptions
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
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(pollCycleSeconds, new FloatSliderOptions
            {
                Min = 5f,
                Max = 600f,
                RequiresRestart = false
            }));
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(pollVoteSeconds, new FloatSliderOptions
            {
                Min = 5f,
                Max = 300f,
                RequiresRestart = false
            }));
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(pollHudOffsetX, new FloatSliderOptions
            {
                Min = -1000f,
                Max = 1000f,
                RequiresRestart = false
            }));
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(pollHudOffsetY, new FloatSliderOptions
            {
                Min = -1000f,
                Max = 1000f,
                RequiresRestart = false
            }));
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(pollHudFontSize, new FloatSliderOptions
            {
                Min = 8f,
                Max = 48f,
                RequiresRestart = false
            }));
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(pollHudOpacity, new FloatSliderOptions
            {
                Min = 0f,
                Max = 1f,
                RequiresRestart = false
            }));
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(pollHudBorderThickness, new FloatSliderOptions
            {
                Min = 0f,
                Max = 10f,
                RequiresRestart = false
            }));
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(pollHudBoxWidth, new FloatSliderOptions
            {
                Min = 120f,
                Max = 1000f,
                RequiresRestart = false
            }));
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(pollHudPadding, new FloatSliderOptions
            {
                Min = 0f,
                Max = 80f,
                RequiresRestart = false
            }));

            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(enableDebugKeys, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(countOnlyModEnemies, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(showSpawnTips, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(kickEnabled, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(pollEnabled, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(pollAllowVoteChange, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(pollWritePollFile, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(pollShowTargetPlayer, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(spawnCommandEnabled, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(pollHudEnabled, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(pollHudShowBorder, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(pollHudShowLatestVote, requiresRestart: false));

            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(prefix, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(blocklist, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(perEnemyLimits, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(kickChatroomId, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(kickAppKey, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(kickCluster, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<NoVotesOption>(pollNoVotesBehavior, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(pollBlocklist, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<LanguageOption>(displayLanguage, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<HudAnchor>(pollHudAnchor, requiresRestart: false));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(pollHudBorderColor, requiresRestart: false));

            LethalConfigManager.AddConfigItem(new GenericButtonConfigItem(
                "Kick",
                "Reconnect Kick",
                "Stop and restart the Kick chat connection using the current settings.",
                "Reconnect Kick",
                () => restartKick()));
        }
    }
}
