# Spec

A console app that monitors Everquest logs for zone bonuses and posts them to the https://fangbreaker.zone and https://frostreaver.zone.

## Tech stack

- C#

## App behavior

- **Single instance:** a named mutex. A second copy prints an error waits for the user to press a key.
- **API key:** one key works on both sites. Store it in Windows Credential Manager. Never write it to a file or to the log.
- When the app launches it prints out:
  - The app version.
  - The status of the API key (working, rejected, or couldn't be checked). To check it, POST `none` for `The Nexus` on Fangbreaker.
- When the key is missing or rejected, ask for one at the console. Don't watch logs until a key is accepted. Entering nothing exits.
- Errors are red. Successful uploads are yellow.

## Finding logs

Finding `eqgame.exe` processes, finding each one's `Logs\eqlog_{CharacterName}_{server}.txt`, and tailing it, is delegated to the [EverQuestLogWatcher](https://github.com/JayPNW/EverQuestLogWatcher) NuGet package (`EverQuestLogWatcher().WatchAsync(...)`).

Known limitations inherited from the library, accepted for now:
- There's no hint when EQ is running but no log is growing (`/log on` is off).
- There's no signal when EQ exits, so a log keeps being tailed even after the character logs out. See Timing for how FZScout avoids a false `none` report because of this.

## Servers

The `{server}` in the log file name picks the site, ignoring case. Other servers are ignored.

| Log file      | Site                     | `serverName`  |
|---------------|--------------------------|---------------|
| `fangbreaker` | https://fangbreaker.zone | `Fangbreaker` |
| `frostreaver` | https://frostreaver.zone | `Frostreaver` |


## EQ Log parsing

See @EverquestLogs.md for details.

Recognizing a message (stripping the timestamp, matching it against known patterns, and excluding lines like `You have entered an area where levitation effects do not function.` from being a real zone change) is delegated to the EverQuestLogWatcher library's `EverQuestLogLineParser`. `CharacterScout` dispatches directly on the library's event types; the only FZScout-specific translation left is turning a bonus announcement's free text into a `BonusKey` (`BonusKey.FromAnnouncementText`).

### Zone

- `You have entered {zone}.` changes the current zone.
- Some zone names differ between site and log. See @Zones.md for the full mapping.
- If it's still not on the list, the current zone is unknown. Many of these never get a bonus, like The Plane of Knowledge, so that alone isn't an error.
- Log `{character} entered {zone}.` (the zone as written in the EQ log).


### Bonus message

A bonus message applies to the current zone. If the current zone is not recognized, log it as an error and invite the user to report it. If the bonus key isn't in the server's `types` list, log it as an error, invite the user to report it, and don't report anything for this zone. If no bonus message appears, `none` is reported instead; see Timing.

### Reset message

Once a day all zone bonuses reset. Watch for new bonus messages indicating the updated bonuses.

## Timing

- When a zone is entered, start the 5-minute idle timer for reporting `none` if no bonus message appears.
- When a kill occurs, start a 5-second timer to watch for a bonus message. If no bonus message appears within 5 seconds, report `none`.
- When a daily reset message appears, restart the 5-minute idle timer for the current zone.
- When the camp message appears, stop the idle timer. Abandoning the camp doesn't restart it.
- A timer firing isn't driven by a clock: it's checked only when the character's next log message arrives, right before handling that message. A character who stops playing before another message arrives never gets a false `none`.

## Reporting

- Report when a bonus message appears.
- Report `none` when the appropriate timer expires with no bonus message.
- Before reporting `none`, GET `/api/servers/{server}/bonuses/today`. If the site already shows a bonus for the zone (anything but `none` or `unconfirmed`), don't report: assume the site is right. If that check fails, log it and skip the report.

### Responses

Every failure is logged as an error.

| Response            | Result                                                                        |
|---------------------|-------------------------------------------------------------------------------|
| 200                 | Success.                                                                      |
| 400 bad payload     | Don't retry.                                                                  |
| 401 bad key         | Log "API key rejected". At startup, ask for a new key. Later, exit.           |
| 403 banned          | Don't retry.                                                                  |
| 404 zone not found  | Don't retry.                                                                  |
| 500, other, network | Don't retry.                                                                  |

Each server's `types` and `zones` lists are loaded the first time they're needed. If loading fails, try again the next time they're needed.
