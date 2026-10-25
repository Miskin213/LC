# Pala Company

A Lethal Company mod that lets **Kick chat spawn enemies** near the players.

A viewer types `!spawn bracken` and a Bracken shows up. The streamer stays in control with cooldowns, caps, per-enemy limits and a blocklist, all adjustable in game.

> Unofficial fan mod. Not affiliated with Lethal Company, its developer, or Kick.

## Features

- `!spawn <enemy>` from Kick chat, with forgiving names and aliases (`bracken`, `thumper`, `hoarding bug`...)
- Zone-aware spawning: indoor enemies appear near players inside the facility, outdoor enemies near players outside, so they actually chase their target
- Per-viewer cooldown, global cooldown, a cap on live enemies, and per-enemy limits (for example only one Jester at a time)
- Blocklist for enemies chat may not spawn
- On-screen tip showing who spawned what
- In-game settings menu through [LethalConfig](https://thunderstore.io/c/lethal-company/p/AinaVT/LethalConfig/) (optional)
- Custom main menu title (can be switched off)
- No account, login or token needed: the mod reads public chat

## Requirements

- Lethal Company with BepInEx 5
- **Only the host needs the mod.** It spawns the game's own enemies, so players without the mod can still join. Multiplayer testing is ongoing, so please report problems.
- LethalConfig is optional. Without it, edit the config file instead.

## Install

- **Mod manager:** search for *Pala Company* in r2modman or the Thunderstore app and install it.
- **Manual:** put `pala_company.dll` in `BepInEx/plugins`.

## Set up Kick chat

1. Launch the game once so the config file is created: `BepInEx/config/miskin213.pala_company.cfg`.
2. Find your channel's chatroom ID. In a normal browser, open `https://kick.com/api/v2/channels/<your channel name>` and look for `"chatroom":{"id":` followed by a number.
3. In the config (or in the LethalConfig menu), set under `[Kick]`:
   - `Enabled = true`
   - `ChatroomId = <that number>`
4. Host a lobby and land on a moon. Type `!spawn baboon` in chat to test.

After changing Kick settings in the LethalConfig menu, press **Reconnect Kick** to apply them without restarting the game.

## Commands

| Command | What it does |
|---|---|
| `!spawn <name>` | Spawns that enemy near a random living player who is out of the ship |

Names ignore capitals, spaces and punctuation, so `Hoarding Bug`, `hoardingbug` and `hoarding bug` are the same. The prefix can be changed in the config.

| Type this | Enemy |
|---|---|
| `bracken`, `flowerman` | Bracken |
| `flea`, `snareflea`, `centipede` | Snare flea |
| `thumper`, `crawler` | Thumper |
| `hygrodere`, `blob` | Hygrodere |
| `ghost`, `ghostgirl`, `girl` | Ghost girl |
| `eyelessdog`, `dog`, `mouthdog` | Eyeless dog |
| `coilhead`, `spring` | Coil-head |
| `sporelizard`, `lizard`, `puffer` | Spore lizard |
| `lasso`, `lassoman` | Lasso man |
| `spider`, `bunkerspider` | Bunker spider |
| `hoarder`, `bug`, `hoardingbug` | Hoarding bug |
| `giant`, `forestgiant` | Forest giant |
| `leviathan`, `earthleviathan` | Earth leviathan |
| `baboon`, `hawk`, `baboonhawk` | Baboon hawk |
| `bees`, `killerbees`, `redlocustbees` | Red locust bees |

Other enemies work with the game's own name: `nutcracker`, `jester`, `masked`, `butler`, `maneater`, `tulipsnake`, `claysurgeon`, `radmech`, `giantkiwi`, `stingray`, `manticoil`, `docilelocustbees`, `feiopar`, `cadavergrowths`.

### Why a spawn might be rejected

The host's console log prints a `[rejected]` line with the reason.

- **Wrong zone.** Indoor enemies need a player inside the facility, outdoor enemies need a player outside. Players in the ship don't count.
- **Cooldown.** Per viewer and global.
- **Too many alive.** The global cap or that enemy's own limit was reached.
- **Not in a round.** The ship must be landed.
- **Unknown or blocked name.**
- **Missing nest.** A few outdoor enemies need a nest that some moons don't have.

## Configuration

Every setting is in the config file and in the LethalConfig menu. All except the Kick settings apply instantly.

| Section | Setting | Default | What it does |
|---|---|---|---|
| Commands | `Prefix` | `!spawn` | The chat command |
| Limits | `UserCooldownSeconds` | `30` | Wait between spawns per viewer |
| Limits | `GlobalCooldownSeconds` | `5` | Wait between any two spawns |
| Limits | `MaxLiveEnemies` | `8` | No spawns while this many are alive |
| Limits | `CountOnlyModEnemies` | `true` | Count only enemies spawned by this mod toward the caps |
| Limits | `SpawnDistance` | `9` | How far from the target player enemies appear |
| Limits | `Blocklist` | empty | Comma-separated enemies chat may not spawn |
| Limits | `PerEnemyLimits` | empty | For example `Jester=1, Nutcracker=1, Bracken=2` |
| Limits | `DefaultPerEnemyMax` | `0` | Max alive for enemies not listed above (0 = no limit) |
| Display | `ShowSpawnTips` | `true` | Show an on-screen tip for each spawn |
| Menu | `CustomTitle` | `true` | Use the custom main menu title |
| Kick | `Enabled` | `false` | Connect to Kick chat |
| Kick | `ChatroomId` | | Your channel's chatroom ID |
| Kick | `AppKey`, `Cluster` | | Kick's chat connection details (see Troubleshooting) |
| Debug | `EnableDebugKeys` | `false` | F9 lists enemies, F10 and F11 send fake chat messages |

## Troubleshooting

- **Nothing happens when chat types the command.** Check that `Kick.Enabled` is on, the chatroom ID is right, you are the host, and the ship has landed. Then read the `[rejected]` lines in the console.
- **"Kick rejected the app key".** Kick has rotated its chat connection key before and can again. Update `Kick.AppKey` and `Kick.Cluster` and press **Reconnect Kick**, or check this repo's issues for the current values.
- **Can't find the chatroom ID.** Kick's site sometimes blocks the lookup page. Try another browser, or find the number in the browser's developer tools on your channel's chat page.

## Known limitations

- Kick chat is read through an undocumented connection, so Kick can break it at any time. Check Kick's terms yourself before relying on it for a stream.
- Some enemies may behave oddly when spawned this way. Use the blocklist for any that cause problems.
- Tested mostly in solo and small lobbies.

## Building from source

You need the .NET SDK, Lethal Company, and a BepInEx 5 profile.

1. Copy `LocalPaths.props.example` to `LocalPaths.props` and set:
   - `GameManaged`: your `Lethal Company_Data\Managed` folder
   - `BepCore`: the `BepInEx\core` folder of a profile with BepInEx installed
2. Download LethalConfig from Thunderstore and put `LethalConfig.dll` in a `libs` folder next to the project file.
3. Run `dotnet build`. The output is `bin/Debug/netstandard2.1/pala_company.dll`.

`LocalPaths.props` and `libs/` are git-ignored on purpose, and the game's own code is never included in this repo.

## Project layout

- `Plugin.cs`: config, the chat queue, command handling, limits and spawning
- `KickChatClient.cs`: self-contained Kick chat reader, with no Unity references
- `LethalConfigSupport.cs`: optional LethalConfig menu, loaded only when LethalConfig is installed

## Credits

Built with BepInEx. Settings menu support by [LethalConfig](https://thunderstore.io/c/lethal-company/p/AinaVT/LethalConfig/) by AinaVT.

Made for our Hoca, Palababayigit

## License

[MIT](LICENSE)
