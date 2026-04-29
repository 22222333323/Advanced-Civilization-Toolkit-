using System;
using System.Reflection;
using UnityEngine;

namespace WorldBoxMod
{
    public partial class WorldBoxMod : MonoBehaviour
    {
        private static MethodInfo _setProfessionMethod;
        private static MethodInfo _addBuildingMethod;

        private static void InitializeReflection()
        {
            if (_setProfessionMethod == null)
            {
                _setProfessionMethod = typeof(Actor).GetMethod("setProfession",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null, new Type[] { typeof(UnitProfession), typeof(bool) }, null);
            }

            if (_addBuildingMethod == null)
            {
                _addBuildingMethod = typeof(BuildingManager).GetMethod("addBuilding",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null, new Type[] { typeof(string), typeof(WorldTile), typeof(bool), typeof(bool), typeof(BuildPlacingType) }, null);
            }
        }
    }
}
