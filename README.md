![Notice Board](https://raw.githubusercontent.com/KosmosisDire/TeammateRevive/main/readme/Notice.png?)
## Description:

Survivors can revive their fallen colleagues, but it comes at the price of their own health. A skull totem marks where the player died. Stand within the circle to begin, but stay at your own risk ;) <br> <br> All players need this mod

<br>

### Integration:
* Use [InLobbyConfig](https://thunderstore.io/package/KingEnderBrine/InLobbyConfig/) to configure this plugin in-game. 
* [ItemStatsMod](https://thunderstore.io/package/ontrigger/ItemStatsMod/)
* [BetterUI](https://thunderstore.io/package/XoXFaby/BetterUI/)

<br>

### Feedback:

Feedback would be greatly appreciated. If you encounter **any** bugs or want to discuss new or existing features, **please** submit an issue on Github, or contact **KosmosisDire#4195** or **amadare#8308** on Discord!

<br> <br>

## Default Game Mode:

The default and most simple game mode. When a teammate dies you can revive them by staying inside of the red bubble around where they died. Reviving drains your health over time to balance things out. This keeps you from reviving during battle, but once battle is over it isn't too dangerous to revive.

<br> <br>

## Death Curse Game Mode:

This is a new optional game mode which fleshes out the experience with more difficulty and revival mechanics. There are a number of under the hood things to make the whole system balanced and adjust to player skill.

### **Reviving**

Reviving is the same, but after you revive someone you will be linked to that player for a time. This just means that you helped revive the player, and this will effect things under the hood.

### **Death Curse**

![Death Curse Icon](https://raw.githubusercontent.com/KosmosisDire/TeammateRevive/main/readme/curse.png)

When a player is revived, they will receive the **Death Curse**, along with one random other linked player. **Death Curse** is a debuff that reduces **Max HP/ Shield** simmilar to Shaped Glass. These debuffs will stack!

### **Charon's Obol**
![Charon's Obol](https://raw.githubusercontent.com/KosmosisDire/TeammateRevive/main/readme/obol.png)

The **Charon's Obol** item makes revival faster and easier. It can also be consumed to make revival instant without getting cursed.

#### **Death Curse** and **Obol**

Every time you enter a new stage, **1 Curse** will be removed. The number removed goes up by **1** for every **Obol** you have.

### **Dead Man's Hand**  -  Lunar Item
![Dead Man's Hand](https://raw.githubusercontent.com/KosmosisDire/TeammateRevive/main/readme/lunar_hand.png)

* **Revive** dead teammates **everywhere** on the map.
* First item **increases** revival time by x2
* Every subsequent item **decreases** revival time when you are reviving


<br> <br>

## Change Notes:

### 4.1.1
* Updated for the **Survivors of the Void** update!
* Fixed some other unrelated bugs
* Clean up project structure
* Thanks to nathanpovo and amadare for their work on this patch

<br>

### 4.0.1
* Checkout **4.0.0** for the most important recent changes!
* Fixed bug where a buff was not hidden
* updated readme typos :|

<br>

### 4.0.0 - **big thanks to amadare**
* Huge **refactoring** of whole codebase
* A lot of **bug fixes** and general **improvements**
* The **revival time** now makes actual sense.
* Added optional **Death Curse** mechanic and **Charon's Obol** that adds more depth to revive mechanic
* Added **ItemStats** & **BetterUI** mods integration
* Added revival **progress bar**
* Changed and tweaked revival formula
* Network optimization
* Every revival aspect is highly **configurable**
* Revival range is now increased depending on players inside
* Added some debugging tools
* Trancendence used to break it, but now it's a feature ;)

<br>

### 3.3.8
* Fixed bug that caused players to instantly respawn after their first revival.

<br>

### 3.3.7
* Fixed stupid bug that made the whole mod do nothing and break.

<br>

### 3.3.6
* Fixed possible incompatibility with drop in multiplayer
* Made more reliable

<br>

### 3.3.5
* Fixed a bug that caused skulls to not spawn - not tested but pretty sure this will fix it
* Please submit an issue on Github if there is still a problem!

<br>

### 3.3.4
* Fixed a bug that made the mod think the players hadn't died

<br>

### 3.3.3
* Hopefully fixed a bug that caused a NRE on player death.

<br>

### 3.3.2
* Now when a player dies in midair the totem / skull will spawn at the closest ground location  (rather than midair).
* Damage numbers now show on both client and server.
* Lighting should now also sync to the clients as well.
* Fixed a bug where the mod didn't recognize if the player was revived by something other than this mod.

<br>

### 3.1.0
* Works now lol
* I actually tested it this time :P

<br>

### 3.0.0
* Actually fixed a bug that kept it from working after the first stage
* Fixed a TON of other networking bugs.
* Everything should actually work now.

<br>

### 2.3.2
* Hopefully fixed bug where it wouldn't do anything after the first stage
* Fixed a bug where clients would run server-side code
* Hopefully everything works decently now? sorry for so many updates all at once

<br>

### 2.2.1
* Fixed some more networking bugs hopefully

<br>

### 2.2.0
* Hopefully fixed networking for the most part! It was broken this whole time and I didn't realize

<br>

### 2.1.0
* Visual Changes
    * Made range circle more visible
    * Made damage effect less visible (only shows damage numbers)
    * Shows healing numbers on the player skull
    * light now goes back to red if you leave the revival radius
* Revival should now be quicker
* Config items DO NOT WORK, but I can't figure out how to get rid of them without the user deleting the file
* Players can now use up all their health down to 1HP while reviving someone.

<br>

### 2.0.0
* Major visual update
    * A red-lit skull marks the place of death
    * An 'X' marker visible through any terrain now shows above the skull.
    * Dynamic lighting queues how close the player is to revival.
    * A circle on the ground shows the range you must stay in to continue reviving your teammate.
* Fixed Bugs:
    * Fixed a bug that caused hundreds of Null Reference Exceptions
    * Made it so the player list is only populated once, unless new players join the game.
    * Mod must now be downloaded by each player.
* Removed many unnecissary logs.
* Miscelaneous changes to the system.
   
<br>
 
### 1.0.6
* Made it so it actually works this time (WOW!)
* Added a skull that hovers over a player's location on death (this is used as the revival point)
* Made health drain from the reviving player and add onto the dead player (speed depends on player level)
* Still lots of bugs and polish and visual changes that need to be added!

<br>

### 1.0.5
* Hopefully fixed players being able to revive themselves
* (I didn't fix it it broke the entire thing... yay)

<br>

### 1.0.4
* Got mod actually working
* Bugs:
    * Players can revive themselves after they die :|

<br>

### 1.0.3
* fixed versioning

<br>

### 1.0.2
* Fixed a few problems (still not functional yet)

<br>

### 1.0.1:
* Fixed typos
* updated artwork

<br>

### 1.0.0:
* Initial "release" (not really a release but, eh)
* was put on here for testing multiplayer, but accidentally started at 1.0.0 unstead of 0.0.1 :|


<br>

<br>

<br>

Keywords: respawn - heal - bleed - steal - give - multiplayer
