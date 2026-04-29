using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldBoxMod
{
    public partial class WorldBoxMod : MonoBehaviour
    {
        public static bool SpawnAnyBuilding(WorldTile pTile, string buildingID)
        {
            if (pTile == null || string.IsNullOrEmpty(buildingID))
                return false;

            InitializeReflection();

            Building newBuilding = _addBuildingMethod?.Invoke(MapBox.instance.buildings,
                new object[] { buildingID, pTile, false, false, BuildPlacingType.New }) as Building;

            if (newBuilding == null)
            {
                EffectsLibrary.spawnAtTile("fx_bad_place", pTile, 0.25f);
                return false;
            }

            if (pTile.zone.city != null)
            {
                pTile.zone.city.listBuilding(newBuilding);
            }

            return true;
        }

        public static bool ForceSettle(WorldTile pTile)
        {
            if (pTile == null || pTile.zone == null)
            {
                Debug.Log("[WorldBoxMod] ForceSettle failed: tile or zone is null");
                return false;
            }
            try
            {
                Actor actor = null;
                TileZone zone = pTile.zone;

                pTile.doUnits(a =>
                {
                    if (actor == null && a.asset != null)
                        actor = a;
                    return true;
                });

                if (actor == null)
                {
                    Debug.LogWarning("[WorldBoxMod] No unit on tile or nearby to create city from!");
                    return false;
                }
                if (actor.kingdom == null)
                {
                    return false;
                }

                City newCity = World.world.cities.buildNewCity(actor, zone);
                actor.joinCity(newCity);
                if (newCity == null)
                {
                    Debug.LogError("[WorldBoxMod] Failed to build new city - buildNewCity returned null");
                    return false;
                }

                Debug.Log($"[WorldBoxMod] ✓ Created city: {newCity.name} at zone");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldBoxMod] ForceSettle error: {e.Message}");
                return false;
            }
        }

        public static bool ForceCapital(WorldTile pTile)
        {
            if (pTile == null || pTile.zone == null)
                return false;

            City city = pTile.zone.city;
            if (city == null)
            {
                Debug.Log("[WorldBoxMod] No city at this tile!");
                return false;
            }

            if (city.army != null && city.army.getCaptain() != null && city.army.getCaptain().kingdom != null)
            {
                Kingdom kingdom = city.army.getCaptain().kingdom;
                kingdom.setCapital(city);
                Debug.Log($"[WorldBoxMod] {city.name} is now the capital of {kingdom.name}");
                return true;
            }

            if (city.leader != null && city.leader.kingdom != null)
            {
                Kingdom kingdom = city.leader.kingdom;
                kingdom.setCapital(city);
                Debug.Log($"[WorldBoxMod] {city.name} is now the capital of {kingdom.name}");
                return true;
            }

            Debug.Log("[WorldBoxMod] Could not find kingdom for city!");
            return false;
        }

        public static void ForceCitizenship(WorldTile pTile)
        {
            if (pTile == null || pTile.zone == null)
                return;

            City newCity = pTile.zone.city;
            if (newCity == null)
            {
                Debug.Log("[WorldBoxMod] No city at this tile!");
                return;
            }

            List<Actor> actors = new List<Actor>();
            GetActorsAroundTile(pTile, 3, actors);

            foreach (Actor pActor in actors)
            {
                if (pActor.asset == null)
                    continue;

                pActor.joinCity(newCity);
                Debug.Log($"[WorldBoxMod] {pActor.getName()} has joined {newCity.name}");
            }
        }
    }
}
