using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace PalaCompany
{
    public class ChatMessage
    {
        public string User;
        public string Text;
    }

    [BepInPlugin("yourname.pala_company", "Pala Company", "1.0.0")]
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
            "!spawn hoarding bug", "!spawn hygrodere", "!spawn notarealenemy",
            "just chatting", "!spawn spore lizard"
        };

        private ConfigEntry<string> prefix, blocklist;
        private ConfigEntry<float> userCooldown, globalCooldown, spawnRadius;
        private ConfigEntry<int> maxLive;

        private readonly Dictionary<string, float> lastByUser = new Dictionary<string, float>();
        private float lastGlobal = -999f;
        private Dictionary<string, EnemyType> enemyCache;
        private int testIndex, fakeViewer;

        private void Awake()
        {
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            prefix = Config.Bind("Commands", "Prefix", "!spawn", "Chat command that triggers a spawn.");
            userCooldown = Config.Bind("Limits", "UserCooldownSeconds", 30f, "Seconds between spawns per viewer.");
            globalCooldown = Config.Bind("Limits", "GlobalCooldownSeconds", 5f, "Seconds between any two spawns.");
            maxLive = Config.Bind("Limits", "MaxLiveEnemies", 8, "No spawns while this many enemies are alive.");
            spawnRadius = Config.Bind("Limits", "SpawnDistance", 9f, "How far from the target player enemies appear.");
            blocklist = Config.Bind("Limits", "Blocklist", "", "Comma-separated enemy names chat may NOT spawn.");
            Logger.LogInfo("Pala Company Phase2 loaded!");
        }

        private void Update()
        {
            try
            {
                var kb = Keyboard.current;
                if (kb != null)
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
                }

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
            string pre = prefix.Value.Trim();
            if (!text.StartsWith(pre + " ", StringComparison.OrdinalIgnoreCase)) return;

            string arg = text.Substring(pre.Length).Trim();
            if (arg.Length == 0) return;

            var rm = RoundManager.Instance;
            var sor = StartOfRound.Instance;
            if (rm == null || sor == null) { Reject(msg.User, "not in a game"); return; }
            if (!rm.IsServer) { Reject(msg.User, "not the host"); return; }
            if (!sor.shipHasLanded) { Reject(msg.User, "ship hasn't landed"); return; }

            string key = Norm(arg);
            if (Aliases.TryGetValue(key, out var target)) key = target;
            if (!GetEnemyTypes().TryGetValue(key, out var type)) { Reject(msg.User, "unknown enemy '" + arg + "'"); return; }

            var blocked = new HashSet<string>(blocklist.Value.Split(',').Select(Norm));
            if (blocked.Contains(Norm(type.enemyName))) { Reject(msg.User, type.enemyName + " is blocked"); return; }

            float now = Time.time;
            if (now - lastGlobal < globalCooldown.Value) { Reject(msg.User, "global cooldown"); return; }
            if (lastByUser.TryGetValue(msg.User, out var last) && now - last < userCooldown.Value)
            {
                Reject(msg.User, "your cooldown: " + Mathf.CeilToInt(userCooldown.Value - (now - last)) + "s left");
                return;
            }

            int live = rm.SpawnedEnemies.Count(e => e != null && !e.isEnemyDead);
            if (live >= maxLive.Value) { Reject(msg.User, "too many enemies alive (" + live + ")"); return; }

            var candidates = sor.allPlayerScripts
                .Where(p => p != null && p.isPlayerControlled && !p.isPlayerDead && !p.isInHangarShipRoom)
                .ToList();
            if (candidates.Count == 0) { Reject(msg.User, "no valid player target"); return; }

            var player = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            Vector2 ring = UnityEngine.Random.insideUnitCircle.normalized * spawnRadius.Value;
            Vector3 wanted = player.transform.position + new Vector3(ring.x, 0f, ring.y);
            if (!NavMesh.SamplePosition(wanted, out var hit, 10f, NavMesh.AllAreas))
            {
                Reject(msg.User, "no walkable spot near target");
                return;
            }

            rm.SpawnEnemyGameObject(hit.position, 0f, -1, type);
            lastGlobal = now;
            lastByUser[msg.User] = now;
            Logger.LogInfo("[spawned] " + type.enemyName + " for " + msg.User
                + " | dist=" + Vector3.Distance(hit.position, player.transform.position).ToString("F1")
                + " dy=" + (hit.position.y - player.transform.position.y).ToString("F1")
                + " playerInside=" + player.isInsideFactory);
        }
    }
}