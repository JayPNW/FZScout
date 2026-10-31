# Spec

A console app that monitors Everquest logs for zone bonuses and posts them to the https://fangbreaker.zone and https://frostreaver.zone.

## Tech stack

- C#

## App behavior

- **Single instance:** a named mutex. A second copy prints an error waits for the user to press a key.
- **API key:** one key works on both sites. Store it in Windows Credential Manager. Never write it to a file or to the log.
- When the app launches it prints out:
  - The app version.
  - The status of the API key (working, rejected, or missing). To check it, POST `none` for `The Nexus` on Fangbreaker.
- When the key is missing or rejected, ask for one at the console. Keep watching logs while waiting for the answer.
- Errors are red. Successful uploads are yellow.
  - List of EQ Log files it is watching.


## Logging hint

If an EQ process has run for 5 minutes and no `eqlog_*.txt` in its `Logs` folder has grown:
    "No EverQuest log is growing. Type `/log on` in EverQuest." Show it once per EQ process. This includes logs for other servers, because any growing log means logging is on.

## Finding logs

- Every 30 seconds, look for `eqgame.exe` processes.
  - Get each exe path with `QueryFullProcessImageName`, which works even when EQ runs elevated.
- In the `Logs` folder next to the exe, a file `eqlog_{CharacterName}_{server}.txt` is active when it grows while EQ is running from that folder.
- Tail each active log once, even if several EQ processes share the folder. Start at the end of the file. Stop when no EQ process from that folder is running.

## Servers

The `{server}` in the log file name picks the site, ignoring case. Other servers are ignored.

| Log file      | Site                     | `serverName`  |
|---------------|--------------------------|---------------|
| `fangbreaker` | https://fangbreaker.zone | `Fangbreaker` |
| `frostreaver` | https://frostreaver.zone | `Frostreaver` |


## EQ Log parsing

See @EverquestLogs.md for details.

Strip the timestamp (`[Mon Sep 14 16:10:29 2026] `) from each line.

### Zone

- `You have entered {zone}.` changes the current zone, except lines that start with `You have entered an area` (like `You have entered an area where levitation effects do not function.`).
- Some zone names differ between site and log. See @Zones.md for the full mapping.
- If it's still not on the list, the current zone is unknown. Log it as an error and invite the user to report it.
- Otherwise log `{character} entered {zone}.` (the zone as written in the EQ log).


### Bonus message

A bonus message applies to the current zone. Ignore it if the current zone is unknown, or if its key isn't in the server's `types` list.

### Reset message

Once a day all zone bonuses reset. Watch for new bonus messages indicating the updated bonuses.

### Implementation notes

Each type of log message should become a LogEvent, e.g. `KillLogEvent` or `BonusReportLogEvent` following the strategy pattern.

## Timing

- When a zone is entered, start the 5-minute idle timer for reporting `none` if no bonus message appears.
- When a kill occurs, start a 5-second timer to watch for a bonus message. If no bonus message appears within 5 seconds, report `none`.
- When a daily reset message appears, restart the 5-minute idle timer for the current zone.
- When the camp message appears, disable the idle timer. Resume it when the player abandons their camp preparations.

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
| 401 bad key         | Log "API key rejected" and ask for a new key. Stop until the key changes.     |
| 403 banned          | Log it. Stop reporting to that site until restart.                            |
| 404 zone not found  | Reload the zones list. Don't retry.                                           |
| 500, other, network | Retry once after 1 minute, then drop.                                         |

A waiting retry is replaced by a newer report for the same character and zone. Waiting retries are lost on exit.

If loading `types` or `zones` fails, retry once after 1 minute. If that fails too, try again the next time the list is needed.
