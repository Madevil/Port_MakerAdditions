using System;
using System.Collections.Generic;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

using KKAPI.Maker;
using KKAPI.Maker.UI.Sidebar;

using UniRx;
using UnityEngine;

namespace MakerAdditions
{
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class MakerAdditions : BaseUnityPlugin
    {
        public const string GUID = Constants.Prefix + "_MakerAdditions";
        public const string PluginName = Constants.Prefix + "_MakerAdditions";
        public const string Version = Constants.Version;

        internal new static ManualLogSource Logger;

        internal static MakerAdditions instance;

        private static GameObject oldParent;
        private static GameObject newParent;

        private static SidebarToggle lockCamlightToggle;

        private static ConfigEntry<bool> DefaultLockCameralight { get; set; }

        private void Awake()
        {
            Logger = base.Logger;

            instance = this;

            Harmony harmony = new Harmony(nameof(MakerAdditions));

            Type moreAccsPos = Type.GetType("MoreAccessoriesKOI.ChaControl_SetAccessoryPos_Patches, MoreAccessories, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            Type moreAccsRot = Type.GetType("MoreAccessoriesKOI.ChaControl_SetAccessoryRot_Patches, MoreAccessories, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

            if (moreAccsPos != null && moreAccsRot != null)
            {
                harmony.Patch(moreAccsPos.GetMethod("Prefix", AccessTools.all), null, null, new HarmonyMethod(typeof(Hooks), nameof(Hooks.ChaControl_SetAccessoryPos_ChangeLimit)));
                harmony.Patch(moreAccsRot.GetMethod("Prefix", AccessTools.all), null, null, new HarmonyMethod(typeof(Hooks), nameof(Hooks.ChaControl_SetAccessoryRot_ChangeLimit)));
            }

            harmony.PatchAll(typeof(Hooks));

            MakerAPI.RegisterCustomSubCategories += RegisterCustomSubCategories;
            MakerAPI.MakerExiting += (sender, e) => OnDestroy();

            DefaultLockCameralight = Config.Bind("Settings", "Default Lock Cameralight", false);
        }

        private void RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            Transform camLight = GameObject.Find("CustomScene/CamBase/Camera/Directional Light").transform;

            lockCamlightToggle = e.AddSidebarControl(new SidebarToggle("Lock Cameralight", DefaultLockCameralight.Value, this));
            lockCamlightToggle.ValueChanged.Subscribe(x =>
            {
                if (camLight == null)
                    return;

                if (x)
                {
                    oldParent = camLight.parent.gameObject;

                    newParent = new GameObject("CamLightLock");
                    newParent.transform.position = oldParent.transform.position;
                    newParent.transform.eulerAngles = oldParent.transform.eulerAngles;

                    camLight.parent = newParent.transform;
                }
                else if (oldParent != null)
                {
                    camLight.parent = oldParent.transform;

                    camLight.transform.localEulerAngles = new Vector3(0, 3, 0);

                    Destroy(newParent);
                    newParent = null;
                }
            });
        }

        private void OnDestroy()
        {
            lockCamlightToggle = null;
        }
    }
}