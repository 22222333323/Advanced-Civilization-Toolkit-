using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace WorldBoxMod
{
    public partial class WorldBoxMod : MonoBehaviour
    {
        private static void GetActorsAroundTile(WorldTile centerTile, int radius, List<Actor> resultList)
        {
            if (centerTile == null || resultList == null)
                return;

            resultList.Clear();

            List<WorldTile> tilesToCheck = new List<WorldTile>();
            tilesToCheck.Add(centerTile);

            for (int r = 0; r < radius; r++)
            {
                List<WorldTile> nextTiles = new List<WorldTile>();
                foreach (WorldTile tile in tilesToCheck)
                {
                    if (tile.neighboursAll != null)
                    {
                        foreach (WorldTile neighbor in tile.neighboursAll)
                        {
                            if (neighbor != null && !tilesToCheck.Contains(neighbor) && !nextTiles.Contains(neighbor))
                            {
                                nextTiles.Add(neighbor);
                            }
                        }
                    }
                }
                tilesToCheck.AddRange(nextTiles);
            }

            foreach (WorldTile tile in tilesToCheck)
            {
                tile.doUnits(actor => resultList.Add(actor));
            }
        }

        public static void ChangeUnitToWarrior(WorldTile pTile)
        {
            if (pTile == null)
                return;

            InitializeReflection();

            Actor target = null;
            pTile.doUnits(a =>
            {
                if (target == null && a.asset != null)
                    target = a;
                return true;
            });

            if (target == null || target.city == null)
                return;

            if (target.isBaby())
            {
                target.removeTrait("peaceful");
            }

            if (target.is_profession_warrior)
                return;

            _setProfessionMethod?.Invoke(target, new object[] { UnitProfession.Warrior, true });

            if (target.equipment != null && target.equipment.weapon.isEmpty())
            {
                City.giveItem(target, target.city.getEquipmentList(EquipmentType.Weapon), target.city);
            }

            if (target.city.getArmy() == null && target.city.army == null)
            {
                Army army = MapBox.instance.armies.newArmy(target, target.city);
                target.city.army = army;
            }

            target.city.status.warriors_current++;
            target.setStatsDirty();
            target.startShake(0.3f, 0.1f, true, true);
            target.startColorEffect(ActorColorEffect.White);
        }

        public static void ChangeUnitToCivilian(WorldTile pTile)
        {
            if (pTile == null)
                return;

            InitializeReflection();

            Actor target = null;
            pTile.doUnits(a =>
            {
                if (target == null && a.asset != null)
                    target = a;
                return true;
            });

            if (target == null || target.city == null || target.isBaby())
                return;

            if (target.is_profession_warrior && target.city != null)
            {
                if (target.army != null)
                {
                    FieldInfo unitsField = typeof(Army).BaseType.GetField("units", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (unitsField != null)
                    {
                        List<Actor> units = unitsField.GetValue(target.army) as List<Actor>;
                        units?.Remove(target);
                    }
                    target.army = null;
                }
                target.city.status.warriors_current--;
            }

            if (target.is_profession_leader && target.city != null)
            {
                target.city.removeLeader();
            }

            _setProfessionMethod?.Invoke(target, new object[] { UnitProfession.Unit, true });
            target.setStatsDirty();
            target.startShake(0.3f, 0.1f, true, true);
            target.startColorEffect(ActorColorEffect.White);
        }

        public static void ChangeUnitToKing(WorldTile pTile)
        {
            if (pTile == null)
                return;

            InitializeReflection();

            Actor target = null;
            pTile.doUnits(a =>
            {
                if (target == null && a.asset != null)
                    target = a;
                return true;
            });

            if (target == null || target.kingdom == null || target.city == null || target.isBaby())
                return;

            if (target.is_profession_leader && target.city != null)
            {
                target.city.removeLeader();
            }

            if (target.army != null)
            {
                FieldInfo unitsField = typeof(Army).BaseType.GetField("units", BindingFlags.NonPublic | BindingFlags.Instance);
                if (unitsField != null)
                {
                    List<Actor> units = unitsField.GetValue(target.army) as List<Actor>;
                    units?.Remove(target);
                }
                target.army = null;
            }

            if (target.kingdom.king != null)
            {
                target.kingdom.king = null;
            }

            target.kingdom.setKing(target);

            if (target.equipment != null && target.equipment.weapon.isEmpty())
            {
                City.giveItem(target, target.city.getEquipmentList(EquipmentType.Weapon), target.city);
            }

            target.setStatsDirty();
            target.startShake(0.3f, 0.1f, true, true);
            target.startColorEffect(ActorColorEffect.White);
        }
    }
}
