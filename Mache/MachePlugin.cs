﻿using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Il2CppInterop.Runtime;
using UniverseLib;
using Sons.Items.Core;
using Sons.Inventory;
using Sons.Gameplay;
using TheForest.Items.Inventory;
using TheForest.Modding.Bridge;
using UniverseLib.UI;
using Mache.UI;
using Mache.Utils;
using System;
using System.IO;
using System.Text;

namespace Mache
{
    [BepInPlugin(ModId, ModName, Version)]
    [BepInProcess("SonsOfTheForest.exe")]
    internal class MachePlugin : BasePlugin
    {
        public const string ModId = "com.willis.sotf.mache";
        public const string ModName = "Mache";
        public const string Version = "0.0.1";

        internal static MachePlugin Instance { get; private set; }

        public override void Load()
        {
            Instance = this;
            AddComponent<Mache>();
        }

        public static IEnumerable<T> FindObjectsOfType<T>() where T : Component
        {
            return GameObject.FindObjectsOfType(Il2CppType.Of<T>()).Select(i => i.Cast<T>());
        }
        public static T FindObjectOfType<T>() where T : Component
        {
            return GameObject.FindObjectOfType(Il2CppType.Of<T>()).Cast<T>();
        }
    }

    public class Mache : MonoBehaviour
    {
        private static MacheOverlay Overlay { get; set; }

        private static List<Func<ModDetails>> pendingModRegistrations = new List<Func<ModDetails>>();
        private static bool overlayReady = false;


        public static void RegisterMod(Func<ModDetails> detailsProvider)
        {
            if (detailsProvider == null) return;
            if (overlayReady)
            {
                Overlay.RegisterMod(detailsProvider.Invoke());
                return;
            }
            pendingModRegistrations.Add(detailsProvider);
        }

        public void Awake()
        {
            UniverseLib.Config.UniverseLibConfig config = new UniverseLib.Config.UniverseLibConfig()
            {
                Disable_EventSystem_Override = true,
                Force_Unlock_Mouse = true,
                Unhollowed_Modules_Folder = Path.Combine(Paths.BepInExRootPath, "interop")
            };

            Universe.Init(3f, UniverseInitialized, Log, config);
        }
        private static void Log(object message, LogType logType)
        {
            string text = ((message != null) ? message.ToString() : null) ?? "";
            switch (logType)
            {
                case LogType.Error:
                case LogType.Exception:
                    MachePlugin.Instance.Log.LogError(message);
                    break;
                case LogType.Assert:
                case LogType.Log:
                    MachePlugin.Instance.Log.LogInfo(text);
                    break;
                case LogType.Warning:
                    MachePlugin.Instance.Log.LogWarning(text);
                    break;
            }
        }

        public void Start()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Mache is a modding framework designed for Sons of the Forest. As a utility it provides support for modders, allowing simple creation of new game content and functions to tweak existing elements of the game. Mache provides access to a unified set of tools, menus, and actions that make the process of creating and implementing mods easier and more streamlined.");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("<b><size=20>Contact</size></b>");
            sb.AppendLine();
            sb.AppendLine("If you encounter issues or have suggestions, feel free to contact me on Discord at <i>Willis#8400</i>");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("<i><color=orange>This project is a work-in-progress, and undergoing active development</color></i>");
            var description = sb.ToString();
            Mache.RegisterMod(() => new ModDetails
            {
                Name = "Mache",
                Id = MachePlugin.ModId,
                Version = MachePlugin.Version,
                Description = description,
                OnMenuShow = () => { MachePlugin.Instance.Log.LogInfo("Opening Mache menu!"); }
            });
        }

        private void UniverseInitialized()
        {
            var uiBase = UniversalUI.RegisterUI(MachePlugin.ModId, MacheMenuUpdate);
            Overlay = new MacheOverlay(uiBase);

            overlayReady = true;

            foreach (var detailsProvider in pendingModRegistrations)
            {
                Overlay.RegisterMod(detailsProvider.Invoke());
            }
            pendingModRegistrations.Clear();
        }

        private void MacheMenuUpdate(){ }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Overlay.Toggle();
            }

            /*
            if (Input.GetKeyDown(KeyCode.F1))
            {
                testUi.Toggle();
            }
            else if (Input.GetKeyDown(KeyCode.Keypad0))
            {
                LogAllItemData();
            }
            else if (Input.GetKeyDown(KeyCode.Keypad1))
            {
                GiveItem(ItemDatabase.GoldenArmourId);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad2))
            {
                var layerMask = LayerMask.GetMask("Terrain");
                var player = Mache.FindObjectOfType<PlayerInventory>().gameObject;
                var camera = Camera.main;
                if (Physics.Raycast(camera.transform.position, camera.transform.forward, out var hit, 100, layerMask))
                {
                    player.transform.position = hit.point + (player.transform.position - camera.transform.position);
                }
                Mache.Instance.Log.LogInfo("Blink!");
            }
            */
        }

        private void GiveItem(int itemId)
        {
            MainSceneBridge.LocalPlayer.AddItem(itemId);
        }

        private void LogAllItemData()
        {
            MachePlugin.Instance.Log.LogInfo("All pickups:");
            //var items = GameObject.FindObjectsOfType(Il2CppType.Of<PickUp>()).Select(i => i.Cast<PickUp>());
            var items = MachePlugin.FindObjectsOfType<PickUp>();
            foreach (var item in items)
            {
                LogItemData(item.ItemInstance);
            }
        }

        private void LogItemData(ItemInstance item)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine();
            sb.AppendLine($" Name: {item.Data.name}");
            sb.AppendLine($"Item Type: {item.Data._type}");
            sb.AppendLine($"Item ID: {item.Data.Id}");
            sb.AppendLine($"Perishable: {item.Data.IsPerishable}");
            if (item.Data.IsPerishable)
            {
                var perishableData = item.Data.PerishableData;
                if (perishableData.StatesData != null && perishableData.StatesData.Count > 0)
                {
                    sb.AppendLine($"Perisable states:");
                    foreach (var state in perishableData.StatesData)
                    {
                        sb.AppendLine($"\tState: {state.State}");
                        sb.AppendLine($"\tDuration Game Days: {state.DurationGameDays}");
                        if (state.StatEffects != null && state.StatEffects.Count > 0)
                        {
                            sb.AppendLine($"\tEffects:");
                            foreach (var effect in state.StatEffects)
                            {
                                sb.AppendLine($"\t\t(type)'{effect._type}' (amount)'{effect._amount}' (buff)'{effect._buff}'");
                            }
                        }
                    }
                }
            }
            sb.AppendLine($"Is Limb: {item.Data.IsDismemberedLimb}");
            sb.AppendLine($"Is GPS Locator: {item.Data.IsGPSLocator}");
            sb.AppendLine($"Is Structure Element: {item.Data.IsStructureElement}");
            sb.AppendLine();

            MachePlugin.Instance.Log.LogInfo(sb.ToString());
        }

    }
}