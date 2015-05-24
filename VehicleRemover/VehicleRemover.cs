using ICities;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.Plugins;

using System;
using System.Reflection;
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

        private const string modPrefix = "[Vehicle Remover] ";

        public static void Message(string message)
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, modPrefix + message);
        }

        public static void Warning(string message)
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning, modPrefix + message);
        }

        public static void Log(string message)
        {
            Debug.Log(modPrefix + message);
        }


        /// <summary>
        /// Load and apply the configuration file
        /// </summary>
        public static void LoadConfig()
        {
            if (!File.Exists("VehicleRemover.xml"))
            {
                Log("Configuration file not found. Creating new configuration file.");
                CreateConfig();
                return;
            }

            Log("Loading configuration...");

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
                Warning("Couldn't load configuration (XML malformed?)");
                Debug.LogException(e);
                return;
            }

            if (vehicles == null)
            {
                Warning("Couldn't load configuration (vehicle list is null)");
                return;
            }

            List<VehicleInfo> prefabList = new List<VehicleInfo>();

            // Instead of destroying the prefab, service is removed.
            ItemClass itemClass = new ItemClass();
            itemClass.m_service = ItemClass.Service.None;
            itemClass.m_subService = ItemClass.SubService.None;
            itemClass.m_level = ItemClass.Level.None;

            // Disable prefabs
            for (int i = 0; i < vehicles.Length; i++)
            {
                if(vehicles[i].enabled) continue;

                VehicleInfo prefab = PrefabCollection<VehicleInfo>.FindLoaded(vehicles[i].name);

                if (prefab != null)
                {
                    prefab.m_class = itemClass;
                    prefabList.Add(prefab);
                    Log("Disabled vehicle: " + vehicles[i].name);
                }
                else
                {
                    Log("Couldn't disable vehicle: " + vehicles[i].name);
                }
            }

            if (prefabList.Count == 0) return;

            // IPT compatibility fix
            typeof(VehicleManager).GetField("m_transferVehiclesDirty", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(
                Singleton<VehicleManager>.instance, true);


            // Remove existing vehicle instances
            for (ushort i = 0; i < Singleton<VehicleManager>.instance.m_vehicles.m_size; i++)
            {
                VehicleInfo prefab = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[i].Info;
                if (prefabList.Contains(prefab))
                    Singleton<VehicleManager>.instance.ReleaseVehicle(i);
            }

            // Remove existing parked vehicle instances
            for (ushort i = 0; i < Singleton<VehicleManager>.instance.m_parkedVehicles.m_size; i++)
            {
                VehicleInfo prefab = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer[i].Info;
                if (prefabList.Contains(prefab))
                    Singleton<VehicleManager>.instance.ReleaseParkedVehicle(i);
            }

            Message("Configuration loaded (" + prefabList.Count + " vehicle(s) disabled)");
        }

        public static void CreateConfig()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Vehicle[]));
            List<Vehicle> list = new List<Vehicle>();

            for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
            {
                VehicleInfo prefab = PrefabCollection<VehicleInfo>.GetPrefab(i);
                if(prefab != null)
                    list.Add(new Vehicle { name = prefab.name, enabled = true });
            }

            // The list shouldn't be empty
            if(list.Count == 0)
            {
                Warning("Couldn't create configuration (PrefabCollection is empty)");
                return;
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
                Warning("Couldn't create configuration file at \"" + Directory.GetCurrentDirectory() + "\"");
                Debug.LogException(e);
            }

            Message("Configuration file created at \"" + Directory.GetCurrentDirectory() + "\"");
        }

        public struct Vehicle
        {
            [XmlAttribute("name")]
            public string name;
            public bool enabled;
        }

    }
}
