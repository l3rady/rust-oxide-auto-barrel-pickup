A QOL plugin for players that auto picks up a barrels or road signs loot on destroy instead of the player having to mash E to pickup. With permissions you can also set barrels and road signs to destory in one hit allowing for quicker looting along the roads.

## Features
* Allows players to auto pickup loot from a barrel when it is destroyed.
* Allows players to auto pickup loot from a road sign when it is destroyed.
* Permissions to control who can auto pickup.
* Permissions to allow players to 1 hit destroy barrels and road signs.
* Permissions to disable gibs for barrels and road signs.
* Configurable distance from barrel or road sign to allow auto pickup.


## Configuration
### Default Configuration
```json
{
  "Auto pickup distance": 3
}
```

* `"Auto pickup distance"` - Sets the max distance a player can be from a barrel to auto pickup its loot on destroy. Set to 0 to allow auto pickup from any distance.


## Permissions
 * `"AutoPickupBarrel.Barrel.On"` - Allow player to auto pickup loot from a destoryed barrel.
 * `"AutoPickupBarrel.RoadSign.On"` - Allow player to auto pickup loot from a destoryed road sign.
 * `"AutoPickupBarrel.Barrel.InstaKill"` - Allow player to one hit destroy a barrel (like giving the barrel 1HP).
 * `"AutoPickupBarrel.RoadSign.InstaKill"` - Allow player to one hit destroy a road sign (like giving the road sign 1HP).
 * `"AutoPickupBarrel.Barrel.NoGibs"` - Remove gibs from being shown when a player destroys a barrel.
 * `"AutoPickupBarrel.RoadSign.NoGibs"` - Remove gibs from being shown when a player destroys a roadsign.