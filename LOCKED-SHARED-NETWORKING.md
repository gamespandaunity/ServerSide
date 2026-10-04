# 🔒 LOCKED: Shared Networking Layer

**Do NOT edit these files to fix a single game. They are shared by EVERY networked game.**

| Locked file | Why |
|---|---|
| `Assets/_Network/NetworkGameManager.cs` | Owns reconnect, background/resume, win-lose-on-disconnect for ALL games |
| `Assets/ThirdParty/Mirror 1/MirrorNetwork.cs` | Owns the Mirror transport, auto-reconnect + disconnect handling for ALL games |

## Why this rule exists

On build **1.8.2**, changes made to these shared files (the cricket reconnect work + the
all-games "ghost game" win/lose rework) **broke every other networked game at once** —
tester bug reports: Snooker "game got stuck", 8-Ball "waiting panel", Lobby/Highway
"wrong win/lose / win shows draw", Ludo "voice chat". The individual games were **locked and
working** months earlier; only the shared layer under them changed. The git history even shows
one such shared change was reverted once already.

A change here has a blast radius across **8-Ball, Snooker, Ludo, Cricket, Poker, Carrom,
Highway, and the Lobby**. One game's fix becomes six games' regression.

## What to do instead — fix in the GAME's own scripts

Every networked game has its own network script where its fixes belong:

| Game | Put fixes here |
|---|---|
| Snooker | `_Games/Snokker/Scripts/SnokerNetwork.cs`, `StickManager.cs`, `SnokerUIManager.cs` |
| 8-Ball Pool | `_Games/8Ball pool/MyEightBallNetwork.cs` (+ `BallPool/ShotController.cs`) |
| Cricket | `_Games/CRICKET/Scripts/Multiplayer/CricketNetworkManager.cs` (+ `Gameplay/Core/GroundController.*`) |
| Ludo / Poker / Carrom / … | that game's own `*Network` / manager script |

- Subscribe to the shared manager's existing events/hooks from the game's own script; don't add
  game-specific branches inside the shared manager.
- Make each game's reconnect **resilient** (null-guard / defer its state-restore RPCs) so a future
  shared-timing change can't crash it.

## Enforcement

A `pre-commit` hook (`.githooks/pre-commit`) **blocks** commits that touch the locked files.
Activate it once per clone:

```sh
git config core.hooksPath .githooks
```

(Windows Git Bash runs it automatically after that.) Ask every teammate — including whoever
works on the `Mohsin-Casino-*` / `Wasi` branches — to run that one command.

## The rare, deliberate exception

If a shared change is genuinely required **and you have tested EVERY networked game's reconnect +
win/lose**, override the hook for that one commit:

```sh
ALLOW_SHARED_NET=1 git commit ...
```

If you do, mirror the identical change into **both** `RituGames` and `RituGamesServer` (byte-identical
Cmd/Rpc/SyncVar), and smoke-test all games — not just the one you changed.
