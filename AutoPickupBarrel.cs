using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Auto Pickup Barrel", "l3rady", "1.2")]
    [Description("Allows players to pick up dropped loot from barrels and road signs on destroy automatically.")]

    public class AutoPickupBarrel : RustPlugin
    {
        private readonly string[] BarrelContainerShortPrefabNames = { "loot_barrel_1", "loot_barrel_2", "loot-barrel-1", "loot-barrel-2", "oil_barrel" };
        private readonly string[] RoadSignContainerShortPrefabNames = { "roadsign1", "roadsign2", "roadsign3", "roadsign4", "roadsign5", "roadsign6", "roadsign7", "roadsign8", "roadsign9" };

        #region Configuration

        private Configuration Settings;

        public class Configuration
        {
            [JsonProperty("Auto pickup distance")]
            public float AutoPickupDistance = 3f;

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadDefaultConfig() => Settings = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                Settings = Config.ReadObject<Configuration>();
                if (Settings == null)
                {
                    throw new JsonException();
                }

                if (!Settings.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
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
            Config.WriteObject(Settings, true);
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

        private object OnEntityTakeDamage(LootContainer LootEntContainer, HitInfo HitEntInfo)
        {
            // Check we have something to work with
            if (LootEntContainer == null
                || HitEntInfo == null
            ){
                return null;
            }

            // Check we are targetting barrels/roadsigns
            var LootEntContainerName = LootEntContainer.ShortPrefabName;
            if (LootEntContainerName == null){
                return null;
            }

            var AutoPickupPrefab = "";
            if(IsBarrel(LootEntContainerName)) {
                AutoPickupPrefab = "Barrel";
            } else if(IsRoadSign(LootEntContainerName)) {
                AutoPickupPrefab = "RoadSign";
            } else {
                return null;
            }

            return AutoPickup(LootEntContainer, HitEntInfo, AutoPickupPrefab);
        }

        private bool IsBarrel(string LootEntContainerName) {
            return BarrelContainerShortPrefabNames.Contains(LootEntContainerName);
        }

        private bool IsRoadSign(string LootEntContainerName) {
            return RoadSignContainerShortPrefabNames.Contains(LootEntContainerName);
        }

        private object AutoPickup(LootContainer lootContainer, HitInfo hitInfo, string autoPickupPrefab)
        {
            var player = lootContainer.lastAttacker as BasePlayer ?? hitInfo.InitiatorPlayer;
            if (player == null || !HasAutoPickupPermission(player, autoPickupPrefab))
            {
                return null;
            }

            var lootContainerInventory = lootContainer?.inventory;
            if (lootContainerInventory == null)
            {
                return null;
            }

            if (!IsWithinAutoPickupRange(player, lootContainer))
            {
                return null;
            }

            if (!CanAutoPickup(lootContainer, player, autoPickupPrefab))
            {
                return null;
            }

            ApplyScrapMultiplier(lootContainerInventory, GetPlayerScrapMultiplier(player));
            GiveItemsToPlayer(player, lootContainerInventory);

            if (IsLootContainerEmpty(lootContainerInventory))
            {
                HandleEmptyLootContainer(lootContainer, player, autoPickupPrefab);
            }

            return false;
        }

        private float? GetPlayerScrapMultiplier(BasePlayer player)
        {
            var playerModifiers = player.modifiers.All;

            if (playerModifiers != null)
            {
                var scrapModifier = playerModifiers
                    .OfType<Modifier>()
                    .FirstOrDefault(modifier => (int)modifier.GetType().GetProperty("Type").GetValue(modifier) == 5);

                if (scrapModifier != null)
                {
                    return (float)scrapModifier.Value;
                }
            }

            return null;
        }

        private bool HasAutoPickupPermission(BasePlayer player, string autoPickupPrefab)
        {
            return permission.UserHasPermission(player.UserIDString, $"AutoPickupBarrel.{autoPickupPrefab}.On");
        }

        private bool IsWithinAutoPickupRange(BasePlayer player, LootContainer lootContainer)
        {
            if (Settings.AutoPickupDistance <= 0)
            {
                return true;
            }

            var distance = Vector2.Distance(player.transform.position, lootContainer.transform.position);
            return distance <= Settings.AutoPickupDistance;
        }

        private bool CanAutoPickup(LootContainer lootContainer, BasePlayer player, string autoPickupPrefab)
        {
            var remainingHealth = lootContainer.Health() - lootContainer.lastHit.damageTypes.Total();
            return permission.UserHasPermission(player.UserIDString, $"AutoPickupBarrel.{autoPickupPrefab}.InstaKill") || remainingHealth <= 0;
        }

        private void ApplyScrapMultiplier(ItemContainer container, float? scrapMultiplier)
        {
            if (scrapMultiplier != null)
            {
                foreach (var item in container.itemList)
                {
                    if (item.info.shortname == "scrap")
                    {
                        item.amount += (int)Math.Round(scrapMultiplier.Value * item.amount);
                    }
                }
            }
        }

        private void GiveItemsToPlayer(BasePlayer player, ItemContainer container)
        {
            foreach (var item in container.itemList)
            {
                player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
            }
        }

        private bool IsLootContainerEmpty(ItemContainer container)
        {
            return container.itemList == null || container.itemList.Count <= 0;
        }

        private void HandleEmptyLootContainer(LootContainer lootContainer, BasePlayer player, string autoPickupPrefab)
        {
            NextTick(() =>
            {
                if (HasNoGibsPermission(player, autoPickupPrefab))
                {
                    lootContainer?.Kill();
                }
                else
                {
                    lootContainer?.Kill(BaseNetworkable.DestroyMode.Gib);
                }
            });
        }

        private bool HasNoGibsPermission(BasePlayer player, string autoPickupPrefab)
        {
            return permission.UserHasPermission(player.UserIDString, $"AutoPickupBarrel.{autoPickupPrefab}.NoGibs");
        }
    }
}
