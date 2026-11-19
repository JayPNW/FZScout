# FZScout

Set-and-forget app that watches for Everquest Resource Hunter zone bonuses and automatically reports them to [fangbreaker.zone](https://fangbreaker.zone) and [frostreaver.zone](https://frostreaver.zone). Every player who runs it helps fill in the day's bonus list for everyone.

# Getting Started

1. In EverQuest enable logging by typing `/log on`.
1. [Download](https://github.com/JayPNW/FZScout/releases/latest/download/FZScout-win-Setup.exe) and run `FZScout-win-Setup.exe`. (You may need to bypass a Windows SmartScreen warning.)
1. [Create an API key](create-api-key.md) for Fangbreaker.zone/Frostreaver.zone.
1. Paste the API key into FZScout Settings.

The FZScout icon should appear in your system tray notification area. 
Leave it running while you play and it will automatically report zone bonuses as you play.

# Tray Icons

| Icon                                            | Meaning                                                             |
|-------------------------------------------------|---------------------------------------------------------------------|
| ![Emerald gem](assets/FZScout.png)              | FZScout is watching your log and ready to report bonuses.           |
| ![Emerald hourglass](assets/FZScoutWaiting.png) | Waiting for timeout to report "none".                               |
| ![Grey gem](assets/FZScoutNotScouting.png)      | Waiting for EQ to start.                                            |
