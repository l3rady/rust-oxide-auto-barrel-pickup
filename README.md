A QOL plugin for players that auto picks up a barrel or road sign's loot on destroy instead of the player having to mash E to pick up. With permissions, you can also set barrels and road signs to destroy in one hit allowing for quicker looting along the roads.


## Features
* Allows players to auto pickup loot from a barrel when it is destroyed.
* Allows players to auto pickup loot from a road sign when it is destroyed.
* Permission to control who can auto pickup.
* Permission to allow players to 1 hit destroy barrels and road signs.
* Permission to disable gibs for barrels and road signs.
* Configurable distance from barrel or road sign to allow auto pickup.


## Changelog
### v1.2
* Implemented a scrap multiplier to barrel payout if the user is using a scrap tea. Because we empty the barrel before destroying, the games built in mechanic for awarding extra scrap doesn't work.


## Configuration
### Default Configuration
```json
{
  "Auto pickup distance": 3
}
```

* `"Auto pickup distance"` - Sets the max distance a player can be from a barrel to auto pickup its loot on destroy. Set to 0 to allow auto pickup from any distance.


## Permissions
* `"AutoPickupBarrel.Barrel.On"` - Allow player to auto pickup loot from a destroyed barrel.
* `"AutoPickupBarrel.RoadSign.On"` - Allow player to auto pickup loot from a destroyed road sign.
* `"AutoPickupBarrel.Barrel.InstaKill"` - Allow the player to one hit to destroy a barrel (like giving the barrel 1HP).
* `"AutoPickupBarrel.RoadSign.InstaKill"` - Allow the player to one hit to destroy a road sign (like giving the road sign 1HP).
* `"AutoPickupBarrel.Barrel.NoGibs"` - Remove gibs from being shown when a player destroys a barrel.
* `"AutoPickupBarrel.RoadSign.NoGibs"` - Remove gibs from being shown when a player destroys a roadsign.