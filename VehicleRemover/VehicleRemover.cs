using ICities;
using UnityEngine;
using ColossalFramework;

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Reflection;

namespace VehicleRemover
{
    public class VehicleRemover : LoadingExtensionBase, IUserMod
    {
        #region IUserMod implementation
        public string Name
        {
            get { return "Vehicle Remover"; }
        }

        public string Description
        {
            get { return "Disable specific vehicles"; }
        }
        #endregion

        #region LoadingExtensionBase overrides
        /// <summary>
        /// Called when the level (game, map editor, asset editor) is loaded
        /// </summary>
        public override void OnLevelLoaded(LoadMode mode)
        {
            // Is it an actual game ?
            if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame)
                return;

            // Loading the configuration
            LoadConfig();
        }
        #endregion

        /// <summary>
        /// Load and apply the configuration file
        /// </summary>
        public static void LoadConfig()
        {
            if (!File.Exists("VehicleRemover.xml"))
            {
                Debug.Log("Configuration file not found. Creating new configuration file.");
                CreateConfig();
                return;
            }

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Vehicle[]));
            Vehicle[] vehicles = null;

            try
            {
                // Trying to deserialize the configuration file
                using (FileStream stream = new FileStream("VehicleRemover.xml", FileMode.Open))
                {
                    vehicles = xmlSerializer.Deserialize(stream) as Vehicle[];
                }
            }
            catch (Exception e)
            {
                // Couldn't deserialize (XML malformed?)
                Debug.LogException(e);
                return;
            }

            if (vehicles == null) return;

            List<VehicleInfo> prefabList = new List<VehicleInfo>();

            // Disable prefabs
            for (int i = 0; i < vehicles.Length; i++)
            {
                if(vehicles[i].enabled) continue;

                VehicleInfo prefab = PrefabCollection<VehicleInfo>.FindLoaded(vehicles[i].name);

                if (prefab != null)
                {
                    ItemClass itemClass = new ItemClass();
                    itemClass.m_service = ItemClass.Service.None;
                    itemClass.m_subService = ItemClass.SubService.None;
                    itemClass.m_level = ItemClass.Level.None;

                    prefab.m_class = itemClass;

                    prefabList.Add(prefab);
                }
            }

            if (prefabList.Count == 0) return;

            // Remove existing vehicles
            for (ushort i = 0; i < Singleton<VehicleManager>.instance.m_vehicles.m_size; i++)
            {
                VehicleInfo prefab = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[i].Info;
                if (prefabList.Contains(prefab))
                    Singleton<VehicleManager>.instance.ReleaseVehicle(i);
            }

            // Remove existing parked vehicles
            for (ushort i = 0; i < Singleton<VehicleManager>.instance.m_parkedVehicles.m_size; i++)
            {
                VehicleInfo prefab = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer[i].Info;
                if (prefabList.Contains(prefab))
                    Singleton<VehicleManager>.instance.ReleaseParkedVehicle(i);
            }
        }

        public static void CreateConfig()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Vehicle[]));
            List<Vehicle> list = new List<Vehicle>();

            for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
            {
                VehicleInfo prefab = PrefabCollection<VehicleInfo>.GetPrefab(i);
                list.Add(new Vehicle { name = prefab.name, enabled = true });
            }

            try
            {
                using (FileStream stream = new FileStream("VehicleRemover.xml", FileMode.Create))
                {
                    xmlSerializer.Serialize(stream, list.ToArray());
                }
            }
            catch (Exception e)
            {
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Warning,
                    "Couldn't create configuration file at \"" + Directory.GetCurrentDirectory() + "\"");
                Debug.LogException(e);
            }
        }

        public struct Vehicle
        {
            [XmlAttribute("name")]
            public string name;
            public bool enabled;
        }

    }
}
