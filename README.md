# Resource Hunter Bonus Scout

A console app that watches your EverQuest logs for Resource Hunter zone bonuses and reports them to [fangbreaker.zone](https://fangbreaker.zone) and [frostreaver.zone](https://frostreaver.zone) as you travel. Every player who runs it helps fill in the day's bonus map for everyone.

# Getting Started

1. In EverQuest enable logging by typing `/log on`.
1. [Download](https://github.com/JayPNW/FZScout/releases/latest) and run Resource Hunter Bonus Scout.
    - If Windows says it protected your PC, click **More info**, then **Run anyway**. This only happens on the first install.
    - If this doesn't work for you, please report a bug to this project.
1. [Create an API key](docs/create-api-key.md). One key works on both sites.
1. Give the key to the app. It is stored in Windows Credential Manager.

When it starts, the app prints its version, whether your API key is working, rejected, or missing, and which EverQuest log files it is watching. Only the Fangbreaker and Frostreaver servers are reported. Leave it running while you play.

# Thanks

Extra super thanks to Winter.xo for creating the site and the API that this modest client posts to.
