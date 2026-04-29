using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ai.behaviours;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace WorldBoxMod
{
    public partial class WorldBoxMod : MonoBehaviour
    {
        private static bool _initialized = false;
        private static bool _AreIconsReady = true; //if no icons drawn. When loading should use placeholder.png
        public static WorldTile _lastTile = null;
        public static string _lastSelectedBuildingID = null;

        void Update()
        {
            if (!_initialized)
            {
                _initialized = true;
                Debug.Log("[WorldBoxMod] Initializing Advanced Civilization Toolkit(CCT) Mod functions...");
                Init();
            }
        }

        void Init()
        {
            InitializeReflection();      
            RegisterGodPowers();
            Debug.Log("[WorldBoxMod] Advanced Civilization Toolkit(CCT) Mod loaded successfully!");
            LocalizationFix.Apply();
        }


        

 

        // The rest of the implementation has been split into partial files for clarity.
    }
   
}
