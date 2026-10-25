using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KickChat;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using GameNetcodeStuff;

namespace PalaCompany
{
    public class ChatMessage
    {
        public string User;
        public string Text;
    }

    [BepInPlugin("miskin213.pala_company", "Pala Company", "1.0.1")]
    [BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private static readonly ConcurrentQueue<ChatMessage> Inbox = new ConcurrentQueue<ChatMessage>();

        // The ONE entry point for chat. Safe to call from any thread.
        public static void EnqueueChat(string user, string text)
        {
            Inbox.Enqueue(new ChatMessage { User = user, Text = text });
        }

        // friendly name -> internal enemy name (both normalized: lowercase letters/digits only)
        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
        {
            { "bracken", "flowerman" }, { "snareflea", "centipede" }, { "flea", "centipede" },
            { "thumper", "crawler" }, { "hygrodere", "blob" }, { "ghostgirl", "girl" },
            { "ghost", "girl" }, { "eyelessdog", "mouthdog" }, { "dog", "mouthdog" },
            { "coilhead", "spring" }, { "sporelizard", "puffer" }, { "lizard", "puffer" },
            { "lassoman", "lasso" }, { "spider", "bunkerspider" }, { "hoarder", "hoardingbug" },
            { "bug", "hoardingbug" }, { "giant", "forestgiant" }, { "leviathan", "earthleviathan" },
            { "baboon", "baboonhawk" }, { "hawk", "baboonhawk" }, { "bees", "redlocustbees" },
            { "killerbees", "redlocustbees" }
        };

        private static readonly string[] TestMessages =
        {
            "!spawn hygrodere", "!spawn baboon", "!spawn notarealenemy",
            "just chatting", "!spawn spore lizard"
        };

        private ConfigEntry<string> prefix, blocklist, kickChatroomId, kickAppKey, kickCluster, perEnemyLimits;
        private ConfigEntry<float> userCooldown, globalCooldown, spawnRadius;
        private ConfigEntry<int> maxLive, defaultPerEnemyMax;
        private ConfigEntry<bool> enableDebugKeys, countOnlyModEnemies, showSpawnTips, kickEnabled;
        private ConfigEntry<bool> logMenuHierarchy, customTitle;
        private ConfigEntry<bool> pollEnabled, pollAllowVoteChange, pollWritePollFile;
        private ConfigEntry<float> pollCycleSeconds, pollVoteSeconds;
        private ConfigEntry<string> pollNoVotesBehavior, pollBlocklist;
        private ConfigEntry<int> pollMaxPollsPerRound;
        private ConfigEntry<bool> pollShowTargetPlayer, spawnCommandEnabled;
        private ConfigEntry<string> displayLanguage;
        private KickChatClient kickChatClient;
        private Poll poll;
        private bool kickChatStopped;

        private readonly Dictionary<string, float> lastByUser = new Dictionary<string, float>();
        private readonly List<EnemyAI> modSpawnedEnemies = new List<EnemyAI>();
        private readonly HashSet<string> loggedInvalidPerEnemyLimitEntries =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private float lastGlobal = -999f;
        private float lastSpawnDisabledLog = float.NegativeInfinity;
        private Dictionary<string, EnemyType> enemyCache;
        private int testIndex, fakeViewer;

        private void Awake()
        {
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            prefix = Config.Bind("Commands", "Prefix", "!spawn", "Chat command that triggers a spawn.");
            spawnCommandEnabled = Config.Bind("Commands", "SpawnCommandEnabled", true, "Enable the chat spawn command.");
            userCooldown = Config.Bind("Limits", "UserCooldownSeconds", 30f, "Seconds between spawns per viewer.");
            globalCooldown = Config.Bind("Limits", "GlobalCooldownSeconds", 5f, "Seconds between any two spawns.");
            maxLive = Config.Bind("Limits", "MaxLiveEnemies", 8, "No spawns while this many enemies are alive.");
            spawnRadius = Config.Bind("Limits", "SpawnDistance", 9f, "How far from the target player enemies appear.");
            blocklist = Config.Bind("Limits", "Blocklist", "", "Comma-separated enemy names chat may NOT spawn.");
            perEnemyLimits = Config.Bind("Limits", "PerEnemyLimits", "", "Per-enemy max alive, e.g. \"Jester=1, Nutcracker=1, Bracken=2\". Names match chat commands incl. aliases. Values must be 1 or higher.");
            defaultPerEnemyMax = Config.Bind("Limits", "DefaultPerEnemyMax", 0, "Default per-enemy max alive; 0 disables the default limit.");
            enableDebugKeys = Config.Bind("Debug", "EnableDebugKeys", false, "Enable F9/F10/F11 debug keys.");
            countOnlyModEnemies = Config.Bind("Limits", "CountOnlyModEnemies", true, "Count only enemies spawned by this mod toward the live enemy limit.");
            showSpawnTips = Config.Bind("Display", "ShowSpawnTips", true, "Show a HUD tip after a successful spawn.");
            displayLanguage = Config.Bind("Display", "Language", "English", "Language used for player-facing text (English or Turkish).");
            UpdateLanguage();
            displayLanguage.SettingChanged += (_, __) => UpdateLanguage();
            logMenuHierarchy = Config.Bind("Menu", "LogHierarchy", false, "Log UI hierarchy details when the MainMenu scene loads.");
            customTitle = Config.Bind("Menu", "CustomTitle", true, "Replace the MainMenu title image with title.png.");
            kickEnabled = Config.Bind("Kick", "Enabled", false, "Connect to Kick chat.");
            kickChatroomId = Config.Bind("Kick", "ChatroomId", "0", "Kick chatroom ID.");
            kickAppKey = Config.Bind("Kick", "AppKey", "34bf7a0ff419a2a775b9", "Kick Pusher app key.");
            kickCluster = Config.Bind("Kick", "Cluster", "us2", "Kick Pusher cluster.");
            pollEnabled = Config.Bind("Poll", "Enabled", false, "Enable periodic chat polls.");
            pollCycleSeconds = Config.Bind("Poll", "CycleSeconds", 60f, "Seconds between polls.");
            pollVoteSeconds = Config.Bind("Poll", "VoteSeconds", 45f, "Seconds votes remain open.");
            pollMaxPollsPerRound = Config.Bind("Poll", "MaxPollsPerRound", 0, "Maximum polls per round; 0 is unlimited.");
            pollAllowVoteChange = Config.Bind("Poll", "AllowVoteChange", true, "Allow viewers to change their vote.");
            pollNoVotesBehavior = Config.Bind("Poll", "NoVotesBehavior", "Random", "Choose Random or Skip when a poll receives no votes.");
            pollBlocklist = Config.Bind("Poll", "PollBlocklist", "", "Comma-separated enemies that are never offered in polls.");
            pollWritePollFile = Config.Bind("Poll", "WritePollFile", true, "Write poll status to pala_poll.txt in the BepInEx config folder.");
            pollShowTargetPlayer = Config.Bind("Poll", "ShowTargetPlayer", true, "Include the targeted player in the poll result tip.");
            poll = new Poll(
                Logger.LogInfo,
                GetPollCandidates,
                SpawnPollWinner,
                ShowPollTip,
                Path.Combine(Paths.ConfigPath, "pala_poll.txt"));
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("ainavt.lc.lethalconfig"))
            {
                LethalConfigSupport.Register(
                    userCooldown, globalCooldown, maxLive, spawnRadius, perEnemyLimits, defaultPerEnemyMax,
                    enableDebugKeys, countOnlyModEnemies, showSpawnTips, kickEnabled,
                    prefix, blocklist, kickChatroomId, kickAppKey, kickCluster,
                    RestartKick,
                    pollEnabled, pollCycleSeconds, pollVoteSeconds, pollAllowVoteChange,
                    pollNoVotesBehavior, pollBlocklist, pollWritePollFile, displayLanguage,
                    pollMaxPollsPerRound, pollShowTargetPlayer, spawnCommandEnabled);
            }
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (kickEnabled.Value) StartKickClient();
            Logger.LogInfo("Pala Company Phase2 loaded!");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Logger.LogInfo("Scene loaded: " + scene.name);
            if (customTitle.Value && scene.name == "MainMenu")
                StartCoroutine(ApplyCustomTitle(scene));
            if (logMenuHierarchy.Value && scene.name == "MainMenu")
                StartCoroutine(LogMenuHierarchyAfterDelay(scene));
        }

        private IEnumerator ApplyCustomTitle(Scene scene)
        {
            yield return new WaitForSecondsRealtime(1f);

            Image image = null;
            Sprite customSprite = null;
            if (!TryApplyCustomTitle(out image, out customSprite))
                yield break;

            for (int elapsedMilliseconds = 0; elapsedMilliseconds < 10000; elapsedMilliseconds += 250)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                try
                {
                    if (image != null && image.sprite != customSprite)
                    {
                        string replacedName = image.sprite != null ? image.sprite.name : "<none>";
                        image.sprite = customSprite;
                        Logger.LogInfo("Restored custom menu title after replacing sprite: " + replacedName);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError("Failed to restore custom menu title: " + e);
                    break;
                }
            }
        }

        private bool TryApplyCustomTitle(out Image image, out Sprite customSprite)
        {
            image = null;
            customSprite = null;
            try
            {
                var titleObject = GameObject.Find("Canvas/MenuContainer/MainButtons/HeaderImage");
                if (titleObject == null)
                {
                    Logger.LogInfo("Custom menu title not applied: HeaderImage was not found.");
                    return false;
                }

                image = titleObject.GetComponent<Image>();
                if (image == null)
                {
                    Logger.LogInfo("Custom menu title not applied: HeaderImage has no Image component.");
                    return false;
                }

                byte[] imageData = null;
                string titlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "title.png");
                if (File.Exists(titlePath))
                {
                    imageData = File.ReadAllBytes(titlePath);
                }
                else
                {
                    var assembly = GetType().Assembly;
                    string resourceName = assembly.GetManifestResourceNames()
                        .FirstOrDefault(name => name.EndsWith("title.png", StringComparison.OrdinalIgnoreCase));
                    if (resourceName != null)
                    {
                        using (var stream = assembly.GetManifestResourceStream(resourceName))
                        using (var memory = new MemoryStream())
                        {
                            stream.CopyTo(memory);
                            imageData = memory.ToArray();
                        }
                    }
                }

                if (imageData == null)
                {
                    Logger.LogInfo("Custom menu title not applied: title.png was not found beside the plugin DLL or as an embedded resource.");
                    return false;
                }

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(texture, imageData, false))
                {
                    Destroy(texture);
                    Logger.LogError("Custom menu title not applied: title.png could not be loaded.");
                    return false;
                }

                customSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
                customSprite.name = "PalaCompanyTitle";

                var previousSprite = image.sprite;
                Logger.LogInfo(
                    "Replacing menu title sprite: name=" + (previousSprite != null ? previousSprite.name : "<none>")
                    + " rectSize=" + (previousSprite != null ? previousSprite.rect.size.ToString() : "<none>")
                    + " textureSize=" + (previousSprite != null
                        ? previousSprite.texture.width + "x" + previousSprite.texture.height
                        : "<none>"));
                image.sprite = customSprite;
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to apply custom menu title: " + e);
                return false;
            }
        }

        private IEnumerator LogMenuHierarchyAfterDelay(Scene scene)
        {
            yield return new WaitForSecondsRealtime(2f);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var image in root.GetComponentsInChildren<Image>(true))
                    {
                        var rectSize = image.rectTransform.rect.size;
                        Logger.LogInfo(
                            "[menu-ui] path=" + GetHierarchyPath(image.transform)
                            + " activeInHierarchy=" + image.gameObject.activeInHierarchy
                            + " component=" + image.GetType().Name
                            + " sprite=" + (image.sprite != null ? image.sprite.name : "<none>")
                            + " rectSize=" + rectSize);
                    }

                    foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                    {
                        var rectSize = text.rectTransform.rect.size;
                        Logger.LogInfo(
                            "[menu-ui] path=" + GetHierarchyPath(text.transform)
                            + " activeInHierarchy=" + text.gameObject.activeInHierarchy
                            + " component=" + text.GetType().Name
                            + " text=" + text.text
                            + " rectSize=" + rectSize);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to log MainMenu UI hierarchy: " + e);
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }

        public void RestartKick()
        {
            try
            {
                StopKickChatClient();
                kickChatClient = null;
                kickChatStopped = false;
                if (kickEnabled.Value) StartKickClient();
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to restart Kick chat: " + e);
            }
        }

        private void StartKickClient()
        {
            if (!long.TryParse(kickChatroomId.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var chatroomId)
                || chatroomId <= 0)
            {
                Logger.LogInfo("Kick chat is enabled, but Kick.ChatroomId is empty or invalid; the connection remains disabled.");
                return;
            }

            kickChatClient = new KickChatClient(
                chatroomId,
                kickAppKey.Value,
                kickCluster.Value,
                Logger.LogInfo);
            kickChatClient.OnMessage += HandleKickMessage;
            kickChatStopped = false;
            kickChatClient.Start();
        }

        private void HandleKickMessage(KickMessage msg)
        {
            if (msg != null && msg.MessageType == "message")
                EnqueueChat(msg.Username, msg.Content);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            StopKickChatClient();
        }

        private void OnApplicationQuit()
        {
            StopKickChatClient();
        }

        private void StopKickChatClient()
        {
            if (kickChatClient == null || kickChatStopped) return;
            kickChatStopped = true;
            kickChatClient.Stop();
        }

        private void Update()
        {
            try
            {
                var kb = Keyboard.current;
                if (enableDebugKeys.Value && kb != null)
                {
                    if (kb.f9Key.wasPressedThisFrame) ListEnemies();
                    if (kb.f10Key.wasPressedThisFrame)
                    {
                        string text = TestMessages[testIndex++ % TestMessages.Length];
                        string user = "viewer" + (++fakeViewer);
                        Logger.LogInfo("[fake] " + user + ": " + text);
                        EnqueueChat(user, text);
                    }
                    if (kb.f11Key.wasPressedThisFrame)
                    {
                        Logger.LogInfo("[fake] spammer: !spawn hoarding bug");
                        EnqueueChat("spammer", "!spawn hoarding bug");
                    }
                    if (kb.f5Key.wasPressedThisFrame)
                        poll.CastVote("pollviewer" + (++fakeViewer), 1, pollAllowVoteChange.Value);
                    if (kb.f6Key.wasPressedThisFrame)
                        poll.CastVote("pollviewer" + (++fakeViewer), 2, pollAllowVoteChange.Value);
                }

                bool forcePollToggle = enableDebugKeys.Value && kb != null && kb.f7Key.wasPressedThisFrame;
                var roundManager = RoundManager.Instance;
                var startOfRound = StartOfRound.Instance;
                bool hostInRound = roundManager != null && roundManager.IsServer
                    && startOfRound != null && startOfRound.shipHasLanded;
                poll.Update(
                    pollEnabled.Value,
                    hostInRound,
                    startOfRound != null && startOfRound.shipHasLanded,
                    pollCycleSeconds.Value,
                    pollVoteSeconds.Value,
                    pollMaxPollsPerRound.Value,
                    pollNoVotesBehavior.Value,
                    pollShowTargetPlayer.Value,
                    pollWritePollFile.Value,
                    forcePollToggle);

                int handled = 0;
                while (handled < 3 && Inbox.TryDequeue(out var msg))
                {
                    handled++;
                    Handle(msg);
                }
            }
            catch (Exception e) { Logger.LogError(e); }
        }

        private static string Norm(string s)
        {
            return new string((s ?? "").Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        }

        private bool TryResolveEnemyType(string name, out EnemyType type)
        {
            string key = Norm(name);
            if (Aliases.TryGetValue(key, out var target)) key = target;
            return GetEnemyTypes().TryGetValue(key, out type);
        }

        private Dictionary<EnemyType, int> ParsePerEnemyLimits()
        {
            var limits = new Dictionary<EnemyType, int>();
            foreach (var rawEntry in perEnemyLimits.Value.Split(','))
            {
                string entry = rawEntry.Trim();
                if (entry.Length == 0) continue;

                int separator = entry.IndexOf('=');
                if (separator <= 0 || separator != entry.LastIndexOf('='))
                {
                    LogInvalidPerEnemyLimit(entry);
                    continue;
                }

                string enemyName = entry.Substring(0, separator).Trim();
                string value = entry.Substring(separator + 1).Trim();
                if (enemyName.Length == 0
                    || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int limit)
                    || limit < 1
                    || !TryResolveEnemyType(enemyName, out var enemyType))
                {
                    LogInvalidPerEnemyLimit(entry);
                    continue;
                }

                limits[enemyType] = limit;
            }
            return limits;
        }

        private void LogInvalidPerEnemyLimit(string entry)
        {
            if (loggedInvalidPerEnemyLimitEntries.Add(entry))
                Logger.LogWarning("Ignoring invalid Limits.PerEnemyLimits entry: '" + entry + "'.");
        }

        private Dictionary<string, EnemyType> GetEnemyTypes()
        {
            if (enemyCache != null && enemyCache.Count > 0) return enemyCache;

            var result = new Dictionary<string, EnemyType>();
            var sor = StartOfRound.Instance;
            if (sor == null) return result;

            foreach (var level in sor.levels)
            {
                foreach (var list in new[] { level.Enemies, level.DaytimeEnemies, level.OutsideEnemies })
                {
                    foreach (var entry in list)
                    {
                        if (entry == null || entry.enemyType == null) continue;
                        result[Norm(entry.enemyType.enemyName)] = entry.enemyType;
                    }
                }
            }
            enemyCache = result;
            return result;
        }

        private void ListEnemies()
        {
            var types = GetEnemyTypes();
            Logger.LogInfo("Found " + types.Count + " enemy types:");
            foreach (var t in types.Values) Logger.LogInfo("Enemy: " + t.enemyName);
        }

        private void Reject(string user, string reason)
        {
            Logger.LogInfo("[rejected] " + user + ": " + reason);
        }

        private void Handle(ChatMessage msg)
        {
            string text = (msg.Text ?? "").Trim();
            if (Poll.TryParseVote(text, out int option))
            {
                if (poll.IsOpen)
                    poll.CastVote(msg.User, option, pollAllowVoteChange.Value);
                return;
            }

            string pre = prefix.Value.Trim();
            if (!text.StartsWith(pre + " ", StringComparison.OrdinalIgnoreCase)) return;

            if (!spawnCommandEnabled.Value)
            {
                float now = Time.time;
                if (now - lastSpawnDisabledLog >= 10f)
                {
                    Logger.LogInfo("[rejected] " + msg.User + ": spawn command disabled");
                    lastSpawnDisabledLog = now;
                }
                return;
            }

            string arg = text.Substring(pre.Length).Trim();
            if (arg.Length == 0) return;

            if (!TryResolveEnemyType(arg, out var type)) { Reject(msg.User, "unknown enemy '" + arg + "'"); return; }

            TrySpawn(msg.User, type, true, out _, out _);
        }

        private IList<EnemyType> GetPollCandidates()
        {
            var excludedNames = new HashSet<string>(StringComparer.Ordinal);
            var excludedTypes = new HashSet<EnemyType>();
            foreach (var name in (blocklist.Value + "," + pollBlocklist.Value).Split(','))
            {
                string normalized = Norm(name);
                if (normalized.Length == 0) continue;
                excludedNames.Add(normalized);
                if (TryResolveEnemyType(name, out var excludedType))
                    excludedTypes.Add(excludedType);
            }

            return GetEnemyTypes().Values
                .Where(type => type != null
                    && !excludedNames.Contains(Norm(type.enemyName))
                    && !excludedTypes.Contains(type)
                    && IsSpawnableNow(type))
                .Distinct()
                .ToList();
        }

        private PollSpawnResult SpawnPollWinner(EnemyType winner)
        {
            bool spawned = TrySpawn("poll", winner, false, out string reason, out var target);
            return new PollSpawnResult
            {
                Spawned = spawned,
                FailureReason = reason,
                TargetPlayerName = target != null ? target.playerUsername : null
            };
        }

        private void ShowPollTip(string header, string body)
        {
            if (HUDManager.Instance != null)
                HUDManager.Instance.DisplayTip(header, body);
        }

        private void UpdateLanguage()
        {
            Loc.Current = string.Equals(displayLanguage.Value, "Turkish", StringComparison.OrdinalIgnoreCase)
                ? Loc.Language.Turkish
                : Loc.Language.English;
        }

        public bool IsSpawnableNow(EnemyType type)
        {
            return EvaluateSpawn(type, "poll", false, out _, out _);
        }

        private bool EvaluateSpawn(
            EnemyType type,
            string requester,
            bool applyCooldowns,
            out string reason,
            out PlayerControllerB target)
        {
            reason = null;
            target = null;

            var rm = RoundManager.Instance;
            var sor = StartOfRound.Instance;
            if (rm == null || sor == null) { reason = "not in a game"; return false; }
            if (!rm.IsServer) { reason = "not the host"; return false; }
            if (!sor.shipHasLanded) { reason = "ship hasn't landed"; return false; }

            var blocked = new HashSet<string>(blocklist.Value.Split(',').Select(Norm));
            if (blocked.Contains(Norm(type.enemyName))) { reason = type.enemyName + " is blocked"; return false; }
            if (type.isOutsideEnemy && type.nestSpawnPrefab != null && type.requireNestObjectsToSpawn
                && !rm.enemyNestSpawnObjects.Any(n => n != null && n.enemyType == type))
            {
                reason = type.enemyName + " needs a nest that doesn't exist on this moon";
                return false;
            }

            if (applyCooldowns)
            {
                float now = Time.time;
                if (now - lastGlobal < globalCooldown.Value) { reason = "global cooldown"; return false; }
                if (lastByUser.TryGetValue(requester, out var last) && now - last < userCooldown.Value)
                {
                    reason = "your cooldown: " + Mathf.CeilToInt(userCooldown.Value - (now - last)) + "s left";
                    return false;
                }
            }

            int live;
            if (countOnlyModEnemies.Value)
            {
                modSpawnedEnemies.RemoveAll(e => e == null || e.isEnemyDead);
                live = modSpawnedEnemies.Count;
            }
            else
            {
                live = rm.SpawnedEnemies.Count(e => e != null && !e.isEnemyDead);
            }
            if (live >= maxLive.Value) { reason = "too many enemies alive (" + live + ")"; return false; }

            var enemyLimits = ParsePerEnemyLimits();
            int enemyLimit;
            if (!enemyLimits.TryGetValue(type, out enemyLimit))
                enemyLimit = defaultPerEnemyMax.Value;
            if (enemyLimit > 0)
            {
                if (countOnlyModEnemies.Value)
                    modSpawnedEnemies.RemoveAll(e => e == null || e.isEnemyDead);
                var enemiesToCount = countOnlyModEnemies.Value ? modSpawnedEnemies : rm.SpawnedEnemies;
                int enemyCount = enemiesToCount.Count(e => e != null && !e.isEnemyDead && e.enemyType == type);
                if (enemyCount >= enemyLimit)
                {
                    reason = type.enemyName + " limit reached (" + enemyCount + " alive)";
                    return false;
                }
            }

            bool wantInside = !type.isOutsideEnemy;
            var candidates = sor.allPlayerScripts
                .Where(p => p != null && p.isPlayerControlled && !p.isPlayerDead && !p.isInHangarShipRoom
                    && p.isInsideFactory == wantInside)
                .ToList();
            if (candidates.Count == 0)
            {
                reason = type.enemyName + " needs a player " + (wantInside ? "inside the facility" : "outside");
                return false;
            }

            target = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            return true;
        }

        private bool TrySpawn(
            string requester,
            EnemyType type,
            bool applyCooldowns,
            out string reason,
            out PlayerControllerB target)
        {
            target = null;
            if (!EvaluateSpawn(type, requester, applyCooldowns, out reason, out target))
            {
                Reject(requester, reason);
                return false;
            }

            var rm = RoundManager.Instance;
            Vector2 ring = UnityEngine.Random.insideUnitCircle.normalized * spawnRadius.Value;
            Vector3 wanted = target.transform.position + new Vector3(ring.x, 0f, ring.y);
            if (!NavMesh.SamplePosition(wanted, out var hit, 10f, NavMesh.AllAreas))
            {
                reason = "no walkable spot near target";
                Reject(requester, reason);
                return false;
            }

            var spawnedEnemyReference = rm.SpawnEnemyGameObject(hit.position, 0f, -1, type);
            if (applyCooldowns)
            {
                lastGlobal = Time.time;
                lastByUser[requester] = lastGlobal;
            }
            if (!spawnedEnemyReference.TryGet(out var networkObject))
            {
                Logger.LogError("Could not track spawned " + type.enemyName + " for the live enemy cap.");
                reason = string.Empty;
                return false;
            }
            var spawnedEnemy = networkObject.GetComponent<EnemyAI>();
            if (spawnedEnemy == null)
            {
                Logger.LogError("Spawned " + type.enemyName + " has no EnemyAI component; it cannot be tracked for the live enemy cap.");
                reason = string.Empty;
                return false;
            }
            modSpawnedEnemies.Add(spawnedEnemy);
            if (showSpawnTips.Value && requester != "poll" && HUDManager.Instance != null)
                HUDManager.Instance.DisplayTip(
                    Loc.T("tip.spawn.header", requester),
                    Loc.T("tip.spawn.body", type.enemyName));
            Logger.LogInfo("[spawned] " + type.enemyName + " for " + requester
                + " | dist=" + Vector3.Distance(hit.position, target.transform.position).ToString("F1")
                + " dy=" + (hit.position.y - target.transform.position.y).ToString("F1")
                + " playerInside=" + target.isInsideFactory);
            reason = string.Empty;
            return true;
        }
    }
}