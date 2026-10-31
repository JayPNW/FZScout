# Everquest Logs

## Overview

EQ logs live in the eq directory, e.g. `...\EverQuest\Logs\eqlog_{CHARACTERNAME}_fangbreaker.txt` and contents look like:

```text
[Mon Sep 14 16:10:29 2026] Welcome to EverQuest!
```

Timestamps are in local time. `Welcome to EverQuest!` appears at login and is followed by a zone entry. Logging out writes nothing.

## Zone Entry

When you enter a zone the message will look like:

```text
You have entered The Halls of Honor.
```

The same wording is used for areas inside a zone, which don't change the zone:

```text
You have entered an area where levitation effects do not function.
You have entered an area where Bind Affinity is allowed.
```

## Bonus message

A little while after entering a zone one of the following messages may appear in the log indicating a bonus:

```text
This land is saturated with unstable magic, enhancing the benefits of offerings for all who venture here.
This land is saturated with unstable magic, enhancing the potency of any offerings made here.
This land is saturated with unstable magic, improving the experience tied to abilities for all who venture here.
This land is saturated with unstable magic, improving the experience tied to items for all who venture here.
This land is saturated with unstable magic, improving the experiences of all who venture here.
This land is saturated with unstable magic, improving the rate at which proficiency is gained while adventuring here.
This land is saturated with unstable magic, magnifying the recognition one may receive from their adventures here.
This land is saturated with unstable magic, multiplying the coin that may be found here.
This land is saturated with unstable magic, multiplying the loot that may be found here.
This land is saturated with unstable magic, which has drawn many creatures of exceptional rarity here.
This land is saturated with unstable magic, which has drawn numerous mundane creatures here.
```

The message will appear after your first kill in the zone, or after 3 to 3.5 minutes of being in the zone without a kill.
If you get to that point without a kill and there's still no bonus message, it means the zone has no bonus today.

## Kill messages

Kill messages look like this:

```text
You have slain a Stillmoon leopard!
A stormrider was slain by Bowslinger!
A wolf was slain by Bob`s pet!
A gnoll pup was slain by Alice`s warder!
Kekab was slain by A jord militis!
Kixu has been slain by an elite kyv hunter!
```

Only "You have slain..." and "... was slain by {PlayerName}`s pet!" or "...{PlayerName}`s warder!" indicate a kill made by the player, and only these messages are relevant for identifying "none" zone bonus. Named pets (e.g. from mages and necromancers) aren't automatically recognized as belonging to the player and so can't be counted on as proof of no bonus.

## Real Log Analysis
In a real log from Sep 14 to Oct 27, 2026:

- Without a kill, the message came 178–209 seconds after entry.
- When a kill triggered it, the message came in the same second as the kill.
- Many kills didn't trigger the message: kills by NPCs, by other players, and occasionally your own. Of the kills that came first in a visit, 21 of 80 were followed by the message minutes later instead.
- The message appeared once per zone visit, except at the daily reset.

## Daily reset

When the server resets the day's bonuses, this appears. 

```text
Fonts of unstable magic are erupting and receding in response to some nameless power. The magic is cascading throughout Norrath and into the Planes beyond. Hunt for the lands that have been most recently saturated with this magic to reap their many and varied resources.
```

It comes at about midnight Pacific time, but watch for the message, not the clock.

After the reset the new day's bonus messages will show up on a similar timeline: triggered either by your first kill in a zone or by spending a few minutes there without a kill.

## Camping

```text
It will take you about 30 seconds to prepare your camp.
It will take about 25 more seconds to prepare your camp.
You abandon your preparations to camp.
```

The countdown repeats every 5 seconds.
