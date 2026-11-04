Probably Stolen Portrait Framework v{version}
--------------------------------------------

Replaces NPC portraits, and the faces on the wanted poster, with pictures from portrait packs. Unofficial.
This framework contains no pictures itself: you also need a portrait pack (a separate mod that lists
dbfoxtw.portrait-framework as a prerequisite).
Supported game version: full release 1.0 (the main menu shows VERSION 100-REV6; the 100-REV9 beta branch
is also supported)

The code was written by AI (Claude); the feature design and in-game testing were done by the author.

Install
This mod uses the game's official mod format; MelonLoader is not needed, and it needs no capabilities.
1. Close the game and extract the dbfoxtw.portrait-framework folder from this archive into the Mods folder
   in the game folder (you should get Mods\dbfoxtw.portrait-framework\manifest.xml).
   Do the same with the portrait pack you want to use.
2. Start the game, activate "Portrait Framework" and the pack on the Mods screen of the main menu,
   and restart the game.
3. Load your save. Portraits the pack provides are replaced; everything else stays as it is.
When several packs are active, the pack higher in the Mods screen wins for each portrait.
Changing packs or their order needs a restart, like any mod.
Also on the Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=3830171392

Settings (gear button on the Mods screen)
- Pack author mode (default off): for pack makers; changed PNGs are reloaded within a few seconds,
  and every portrait key the game looks up is written to Player.log.

Making a portrait pack
No code needed: a pack is a manifest, a tiny DLL you copy as is, and PNG pictures named after the
game's portrait keys. Start from the Portrait Framework Example Pack; its README.txt is the full guide,
with the list of portrait keys.
- Example pack on the Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=3830171477
- The same guide in the Workshop discussions:
  https://steamcommunity.com/workshop/filedetails/discussion/3830171392/573805072991180986/
- Example pack download (zip):
  https://github.com/dbfoxtw/probably-stolen-mods/releases/tag/portrait-framework-v{version}

Other portrait mods
Portrait packs are mods that list Portrait Framework as a prerequisite; other mods that change
portraits may conflict.

Uninstall
Close the game and delete the Mods\dbfoxtw.portrait-framework folder (or deactivate it on the Mods screen).
Your save files are never read or written; uninstalling brings the original portraits back.

Source code and bug reports
https://github.com/dbfoxtw/probably-stolen-mods/tree/main/mods/portrait-framework
Report problems in the Workshop discussions ("Questions & bug reports") or GitHub Issues,
not to the game developers.

License
- This mod: MIT (LICENSE.txt). Picture content of portrait packs is the responsibility of each pack's author.
- Harmony (MIT) comes with the game and is not distributed with this mod.
- The game's text, names, and assets belong to Questing Goose Studio.
