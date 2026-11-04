Portrait Framework Example Pack v1.0.0
------------------------------------------

An example portrait pack for Portrait Framework (dbfoxtw.portrait-framework): it replaces the Mentor's
portrait with a red placeholder marked "mentor" (the portrait key), so you can check that portrait packs
work. It is also the template for making your own pack: the guide below explains how.

To use it: you also need Portrait Framework. Activate both on the Mods screen of the main menu and
restart the game.

License: MIT for everything in this pack, the example picture included (see LICENSE.txt), so you can
copy it freely as the starting point of your own pack.


How to make a portrait pack
===========================
A portrait pack replaces the portraits of the customers and characters who come to your counter
(and the faces on the wanted poster) with your own pictures. Portrait Framework does the replacing;
your pack only contains pictures. You don't need to write any code.

Saves are never changed.


1. What a pack is
-----------------
A pack is a small mod folder:

    MyPortraitPack\
        manifest.xml               name, author, version of your pack
        PortraitFrameworkPack.dll  copy it as is (see below)
        preview.png                optional: the banner shown on the Mods screen
        Portraits\
            mentor.png
            maleScav3.png
            ...

PortraitFrameworkPack.dll is there so that the game loads your folder as a mod (the game only loads
mods that contain a DLL); it does nothing else. Every pack uses the same file: copy it as is,
don't rename it, don't replace it.


2. Start from this example pack
-------------------------------
1. Copy this whole folder. If you got it from the Steam Workshop, it is in
   <Steam folder>\steamapps\workshop\content\4348910\3830171477\
2. Paste the copy into the game's Mods folder (in Steam, right-click Probably Stolen > Manage >
   Browse local files, then open Mods). From here on, change only this copy.
3. Replace the example's mentor.png with your own pictures. Delete README.txt and LICENSE.txt
   if you don't need them.


3. Edit manifest.xml
--------------------
Open manifest.xml in a text editor (Notepad is fine) and change:

- <ID>: a unique ID for your pack, for example yourname.portraits.pirates
  (letters, digits, '.', '_' and '-', at most 64 characters). Don't keep the example's ID:
  two mods with the same ID clash, and only one of them loads.
- <Name>, <Author>, <ModVersion>, <Description>: shown on the Mods screen.

Keep these lines exactly as they are:

    <Prerequisites>
      <Mod>dbfoxtw.portrait-framework</Mod>
    </Prerequisites>
    <Assemblies>
      <Assembly>PortraitFrameworkPack.dll</Assembly>
    </Assemblies>
    <Capabilities />

The Prerequisites part is how Portrait Framework recognizes your folder as a portrait pack.


4. Pictures
-----------
- One PNG per portrait, in the Portraits folder, named after the portrait key: Portraits\mentor.png
  replaces the Mentor. The keys are listed in section 8. Portraits you don't include stay original.
- Size: 288 x 211 pixels, transparent background. The game draws pixel art at 1:1 (it renders the
  scene at 960 x 540 and scales the whole screen up), so a bigger picture doesn't add detail.
  Other sizes still work: they are scaled to the original height (the log warns when the shape
  differs from 288 x 211).
- Composition: a bust, cut at the chest by the bottom edge; the counter hides the lowest part.
  Look at the original in the game's Sprite Viewer (section 5) and keep the head and shoulders
  about where the original has them.
- Wanted poster: the nine faces on the poster are made automatically from wanted1 ... wanted9.
  To draw the whole poster yourself, add Portraits\wanted.png (666 x 930).
- Don't put the game's original pictures in your pack: the official mod rules forbid redistributing
  game assets.


5. Testing
----------
1. On the Mods screen of the main menu, activate Portrait Framework and your pack, then restart.
2. Open the settings of Portrait Framework (gear button on the Mods screen) and turn on
   "Pack author mode". While the game runs, pictures you save are reloaded within a few seconds,
   and the log lists the portraits your pack provides and each portrait key the game asks for.
3. The log is Player.log in
   %USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen\
   Look for lines starting with [Portrait Framework].

To see every portrait key with its current picture (yours or the original), use the game's
developer tools:
1. Turn them on: see "Turning the tools on with dev.txt" in the official modding guide
   https://github.com/dragonpan2/probably-stolen-modding/blob/main/ModdingGuide.md
2. During a day in your store, press F9 > Special > Open Sprite Viewer.
3. When you're done, delete dev.txt. Never ship it with your pack.


6. Several packs
----------------
Every active pack is used. When two packs replace the same portrait, the pack higher on the Mods
screen's active list wins. Drag packs to change the order, then restart the game.
Only mods that list Portrait Framework as a prerequisite are packs and follow this order; other mods
that change portraits may conflict.


7. Publishing on the Steam Workshop
-----------------------------------
The game has no upload button. Upload with steamcmd, following "Publishing with steamcmd" in the
official modding guide (link above):
1. Write a .vdf file (example below). "visibility" "2" keeps the item hidden.
2. Run steamcmd to upload it.
3. Open your item's Workshop page and use "Add/Remove Required Items" to add Portrait Framework.
   Steam then offers to subscribe to Portrait Framework together with your pack. Also mention in
   your description that Portrait Framework is required.
4. Test your item, then make it public on its Workshop page.

Example .vdf:

    "workshopitem"
    {
        "appid"            "4348910"
        "publishedfileid"  "0"
        "contentfolder"    "C:\\...\\Probably Stolen\\Mods\\MyPortraitPack"
        "previewfile"      "C:\\...\\Probably Stolen\\Mods\\MyPortraitPack\\preview.png"
        "visibility"       "2"
        "title"            "My Portrait Pack"
        "description"      "Portraits for Portrait Framework."
        "changenote"       "First release"
    }


8. Portrait keys
----------------
Write the file names exactly as listed: upper and lower case matter on some systems (Linux, Steam Deck).

Story characters
  mentor              The Mentor
  joe                 Joe Wild
  sawyer              Sawyer
  officer_lun         Officer Lun
  landlord            The Landlord
  courrier            couriers (GP Mining Courier, Red Imp Courier)
  george              Georgio
  inventor            Dr. Jackson
  eleanor1            Eleanor
  eleanor2            Eleanor, without her jewelry
  eleanor3            Eleanor, sad
  eleanor_rev         Eleanor, another outfit
  eleanor_scav        Eleanor, another outfit
  serpico             Cadet Serpico
  serpico_rev         Cadet Serpico, another outfit
  serpico_scav        Cadet Serpico, another outfit
  lou_sec             Lou, another outfit
  nellis              Chief Engineer Nellis
  grandpa_cheng       Grandpa Cheng
  waffle_enjoyer      Foodie Tourist
  case_enjoyer        Case Enjoyer
  scavElder           Lower Level Elder (*)
  trailer_rev1, trailer_scav1, trailer_scav4 (*), trailer_scav5 (*)   one-off customers
  default             used when a customer has no portrait (*)

  (*) The game has no picture for these keys: without a pack, these customers show a random
      scavenger each time. A pack can give them a fixed face.

Other named customers
  merchant            Merchant, Supplier, Water Trader and other traders
  shadyMerchant       Shady Merchant, Loan Shark, Scalper
  admin               Trade Board Official, Appraiser Board Member
  retired_farmer      Retired Farmer
  retired_chemist     Retired Chemist
  retired_aquafarmer  Retired Aquafarmer
  cartel_handler      Cartel Handler
  blackmailer         Blackmailer

Wanted poster
  wanted1 ... wanted9   the nine faces on the wanted poster

Random customers (each face is shared by many kinds of customers)
  maleScav1-13      femaleScav1-14     Scavengers
  maleSec1-6        femaleSec1-5       Security
  maleSecOff1-3     femaleSecOff1-2    Security, off duty
  maleMed1-2        femaleMed1-2       medics
  maleEng1-6        femaleEng1-2       engineers
  maleSci1-2        femaleSci1-2       scientists
  maleLower1-11     femaleLower1-14    Lower Level residents
  maleUpper1-9      femaleUpper1-13    Upper Level residents
  maleRev1-4        femaleRev1-7       Revolution
  maleBM1-6         femaleBM1-5        Black Market
  maleChef1-3       femaleChef1-2      chefs
  maleFarmer1-4     femaleFarmer1-2    farmers
  maleMerc1-4       femaleMerc1-4      mercenaries
  augLLMale1-3      augLLFemale1-3     augmented, Lower Level
  augBMMale1-3      augBMFemale1-3     augmented, Black Market
  augULMale1-3      augULFemale1-3     augmented, Upper Level

"maleScav1-13" means maleScav1.png, maleScav2.png ... maleScav13.png.
New numbers (for example maleUpper10.png) are not supported: the game picks random customers
only from the faces it has.
