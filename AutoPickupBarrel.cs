using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AutoPickupBarrel", "l3rady", "1.0.0")]
    [Description("Allows players to automatically pickup dropped loot from barrels on destroy. Aditional permissions for road sign auto pickup, one hit destroy and disabling gibs on destroy.")]

    public class AutoPickupBarrel : RustPlugin
    {
        private readonly string[] barrelContainerShortPrefabNames = { "loot_barrel_1", "loot_barrel_2", "loot-barrel-1", "loot-barrel-2", "oil_barrel" };
        private readonly string[] roadSignContainerShortPrefabNames = { "roadsign1", "roadsign2", "roadsign3", "roadsign4", "roadsign5", "roadsign6", "roadsign7", "roadsign8", "roadsign9" };

        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Auto pickup distance")]
            public float AutoPickupDistance = 3f;

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }

                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    PrintWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                PrintWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            PrintWarning($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        #endregion Configuration

        private void Init()
        {
            permission.RegisterPermission("AutoPickupBarrel.Barrel.On", this);
            permission.RegisterPermission("AutoPickupBarrel.Barrel.NoGibs", this);
            permission.RegisterPermission("AutoPickupBarrel.Barrel.InstaKill", this);
            permission.RegisterPermission("AutoPickupBarrel.RoadSign.On", this);
            permission.RegisterPermission("AutoPickupBarrel.RoadSign.NoGibs", this);
            permission.RegisterPermission("AutoPickupBarrel.RoadSign.InstaKill", this);
        }

        private object OnEntityTakeDamage(LootContainer lootContainer, HitInfo hitInfo)
        {
            // Check we have something to work with
            if (lootContainer == null
                || hitInfo == null
            ){
                return null;
            }

            // Check we are targetting barrels/roadsigns
            var lootContainerName = lootContainer.ShortPrefabName;
            if (lootContainerName == null){
                return null;
            }

            var autoPickupPrefab = "";
            if(isBarrel(lootContainerName)) {
                autoPickupPrefab = "Barrel";
            } else if(isRoadSign(lootContainerName)) {
                autoPickupPrefab = "RoadSign";
            } else {
                return null;
            }

            return autoPickup(lootContainer, hitInfo, autoPickupPrefab);
        }

        private bool isBarrel(string lootContainerName) {
            return barrelContainerShortPrefabNames.Contains(lootContainerName);
        }

        private bool isRoadSign(string lootContainerName) {
            return roadSignContainerShortPrefabNames.Contains(lootContainerName);
        }

        private object autoPickup(LootContainer lootContainer, HitInfo hitInfo, string autoPickupPrefab) {
            // Check player has permission
            var player = lootContainer.lastAttacker as BasePlayer ?? hitInfo.InitiatorPlayer;
            if (player == null
                || !permission.UserHasPermission(player.UserIDString, $"AutoPickupBarrel.{autoPickupPrefab}.On"))
            {
                return null;
            }

            // Check there is loot in the container
            var lootContainerInventory = lootContainer?.inventory;
            if (lootContainerInventory == null)
            {
                return null;
            }

            // Check barrel/roadsign is in range
            var lootContainerDistance = Vector2.Distance(player.transform.position, lootContainer.transform.position);
            if (lootContainerDistance > config.AutoPickupDistance)
            {
                return null;
            }


            // Check if InstaKill allowed or enough damage has been done to kill
            var lootContainerRemainingHealth = lootContainer.Health() - hitInfo.damageTypes.Total();
            if (!permission.UserHasPermission(player.UserIDString, $"AutoPickupBarrel.{autoPickupPrefab}.InstaKill")
                && lootContainerRemainingHealth > 0)
            {
                return null;
            }

            // Give player the loot from the barrel/roadsign
            for (int i = lootContainerInventory.itemList.Count - 1; i >= 0; i--)
            {
                player.GiveItem(lootContainerInventory.itemList[i], BaseEntity.GiveItemReason.PickedUp);
            }

            // Check the barrel/roadsign is empty
            if (lootContainerInventory.itemList == null || lootContainerInventory.itemList.Count <= 0)
            {
                NextTick(() =>
                {
                    // Kill the barrel/roadsign with or without gibs depending on permission
                    if (permission.UserHasPermission(player.UserIDString, $"AutoPickupBarrel.{autoPickupPrefab}.NoGibs"))
                    {
                        lootContainer?.Kill();

                    } else {
                        lootContainer?.Kill(BaseNetworkable.DestroyMode.Gib);

                    }
                });
            }

            return false;
        }
    }
}
