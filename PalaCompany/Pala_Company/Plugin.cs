using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace PalaCompany
{
    [BepInPlugin("yourname.pala_company", "Pala Company", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<string> testEnemyName;
        private bool loggedFirstUpdate;

        private void Awake()
        {
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            testEnemyName = Config.Bind("Test", "EnemyName", "",
                "Enemy to spawn with F8. Use an exact name from the F9 list.");
            Logger.LogInfo("Pala Company Phase1 loaded!");
        }

        private void Update()
        {
            if (!loggedFirstUpdate)
            {
                loggedFirstUpdate = true;
                Logger.LogInfo("Update is running. Keyboard is " + (Keyboard.current == null ? "NULL" : "ok"));
            }

            var kb = Keyboard.current;
            if (kb == null) return;
            try
            {
                if (kb.f9Key.wasPressedThisFrame) ListEnemies();
                if (kb.f8Key.wasPressedThisFrame) SpawnTestEnemy();
            }
            catch (System.Exception e) { Logger.LogError(e); }
        }

        private Dictionary<string, EnemyType> FindEnemyTypes()
        {
            var result = new Dictionary<string, EnemyType>(System.StringComparer.OrdinalIgnoreCase);
            var sor = StartOfRound.Instance;
            if (sor == null) return result;

            foreach (var level in sor.levels)
            {
                foreach (var list in new[] { level.Enemies, level.DaytimeEnemies, level.OutsideEnemies })
                {
                    foreach (var entry in list)
                    {
                        if (entry == null || entry.enemyType == null) continue;
                        result[entry.enemyType.enemyName] = entry.enemyType;
                    }
                }
            }
            return result;
        }

        private void ListEnemies()
        {
            var types = FindEnemyTypes();
            Logger.LogInfo("F9 pressed. Found " + types.Count + " enemy types.");
            foreach (var name in types.Keys)
                Logger.LogInfo("Enemy: " + name);
        }

        private void SpawnTestEnemy()
        {
            var rm = RoundManager.Instance;
            var sor = StartOfRound.Instance;
            if (rm == null || sor == null || !rm.IsServer)
            {
                Logger.LogInfo("Need to be the host, in a game.");
                return;
            }

            var player = sor.localPlayerController;
            if (player == null) return;

            if (!FindEnemyTypes().TryGetValue(testEnemyName.Value, out var type))
            {
                Logger.LogInfo("Unknown enemy name '" + testEnemyName.Value + "'. Press F9 for the list.");
                return;
            }

            Vector3 target = player.transform.position + player.transform.forward * 8f;
            if (!NavMesh.SamplePosition(target, out var hit, 10f, NavMesh.AllAreas))
            {
                Logger.LogInfo("No walkable spot nearby.");
                return;
            }

            rm.SpawnEnemyGameObject(hit.position, 0f, -1, type);
            Logger.LogInfo("Spawned " + type.enemyName);
        }
    }
}