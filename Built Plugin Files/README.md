## Description:
Survivors can revive their fallen colleagues, but it comes at the price of their own health. A skull marks where the player died. Stand within the circle to begin, but stay at your own risk :)

## To Do:
* Allow an interaction to start reviving a player (rather than automatically starting when inside the circle.
* Add customizability / config
* Test it on the real network rather than only on splitscreen
* Add better indicator for how close the player is to revival

## Change Notes:
* 2.3.2
    * Hopefully fixed bug where it wouldn't do anything after the first stage
    * Fixed a bug where clients would run server-side code
    * Hopefully everything works decently now? sorry for so many updates all at once

* 2.2.1
    * Fixed some more networking bugs hopefully

* 2.2.0
    * Hopefully fixed networking for the most part! It was broken this whole time and I didn't realize

* 2.1.0
    * Visual Changes
        * Made range circle more visible
        * Made damage effect less visible (only shows damage numbers)
        * Shows healing numbers on the player skull
        * light now goes back to red if you leave the revival radius
    * Revival should now be quicker
    * Config items DO NOT WORK, but I can't figure out how to get rid of them without the user deleting the file
    * Players can now use up all their health down to 1HP while reviving someone.

* 2.0.0
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
        

* 1.0.6
    * Made it so it actually works this time (WOW!)
    * Added a skull that hovers over a player's location on death (this is used as the revival point)
    * Made health drain from the reviving player and add onto the dead player (speed depends on player level)
    * Still lots of bugs and polish and visual changes that need to be added!

* 1.0.5
    * Hopefully fixed players being able to revive themselves
    * (I didn't fix it it broke the entire thing... yay)

* 1.0.4
    * Got mod actually working
    * Bugs:
        * Players can revive themselves after they die :|

* 1.0.3
    * fixed versioning

* 1.0.2
    * Fixed a few problems (still not functional yet)

* 1.0.1:
    * Fixed typos
    * updated artwork

* 1.0.0:
    * Initial "release" (not really a release but, eh)
    * was put on here for testing multiplayer, but accidentally started at 1.0.0 unstead of 0.0.1 :|


(Keywords: respawn - heal - bleed - steal - give - multiplayer)
