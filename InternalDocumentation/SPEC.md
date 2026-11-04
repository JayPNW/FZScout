# Spec

A Windows system tray app that monitors EverQuest logs for zone bonuses and posts them to https://fangbreaker.zone and https://frostreaver.zone.

Most behavior is best learned from the code. This file records only intent that the code can't explain on its own.

- **API key:** never write it to a file or to the log.
- **Known limitations** inherited from [EverQuestLogWatcher](https://github.com/JayPNW/EverQuestLogWatcher), accepted for now:
  - There's no hint when EQ is running but no log is growing (`/log on` is off).
  - There's no signal when EQ exits, so a log keeps being tailed after the character logs out.

## Timing

Timers aren't driven by a clock. They are checked only when the character's next log message arrives. That way a character who stops playing never gets a false `none`.

## Reporting

Before reporting `none`, check what the site already shows. If it has a real bonus for the zone, assume the site is right and don't report.

Otherwise, when reporting a bonus different from what is shown on the site, log a warning.
