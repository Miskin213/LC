# pala_company
BepInEx 5 plugin for Lethal Company (Unity 2022.3, Mono, netstandard2.1, HarmonyX).
Goal: Kick chat commands spawn enemies near players.

Rules:
- Never invent game class/method names. Verify them in /mod_reference (decompiled Assembly-CSharp). If unsure, say so.
- One small change at a time. Say which file it goes in.
- Unity calls on the main thread only. Background threads enqueue; Update() drains.
- Spawn only as host, only during a round.
- Wrap network code in try/catch, log with Logger.
- Settings via BepInEx ConfigEntry.
- No new NuGet packages without asking.
- Beginner: explain changes in 2-3 sentences.
- Enemies only target players in their own zone: EnemyAI.PlayerIsTargetable requires player.isInsideFactory != enemy.isOutside (isOutside = EnemyType.isOutsideEnemy). Never spawn an enemy near a player in the wrong zone.
- Do not "fix" zone problems with SetEnemyOutside; it only changes the host's copy.
- KickChatClient.cs must not reference UnityEngine or System.Text.Json (not available in netstandard2.1)