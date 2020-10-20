using System;
using System.Collections.Generic;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

using AIChara;
using CharaCustom;

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
        private static SidebarToggle backlightToggle;
        private static SidebarToggle blinkingToggle;

        private static ConfigEntry<bool> DefaultLockCameralight { get; set; }
        private static ConfigEntry<bool> DefaultBacklight { get; set; }
        private static ConfigEntry<bool> DefaultBlinking { get; set; }

        private static Dictionary<string, Harmony> HooksInstance = new Dictionary<string, Harmony>();

        private void Awake()
        {
            Logger = base.Logger;

            instance = this;

            Harmony harmony = new Harmony(nameof(MakerAdditions));

            Type moreAccs = Type.GetType("MoreAccessoriesAI.Patches.ChaControl_ChaControl_Patches, MoreAccessories, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            if (moreAccs != null)
            {
                harmony.Patch(moreAccs.GetMethod("SetAccessoryPos_Prefix", AccessTools.all), null, null, new HarmonyMethod(typeof(Hooks), nameof(Hooks.ChaControl_SetAccessoryPos_ChangeLimit)));
                harmony.Patch(moreAccs.GetMethod("SetAccessoryRot_Prefix", AccessTools.all), null, null, new HarmonyMethod(typeof(Hooks), nameof(Hooks.ChaControl_SetAccessoryRot_ChangeLimit)));
            }

            harmony.PatchAll(typeof(PreserveScrollHooks));
            harmony.PatchAll(typeof(Hooks));

            MakerAPI.RegisterCustomSubCategories += RegisterCustomSubCategories;
            MakerAPI.MakerFinishedLoading += MakerFinishedLoading;
            MakerAPI.MakerExiting += (sender, e) => OnDestroy();

            DefaultLockCameralight = Config.Bind("Settings", "Default Lock Cameralight", false);
            DefaultBacklight = Config.Bind("Settings", "Default Backlight", true);
            DefaultBlinking = Config.Bind("Settings", "Default Blinking", true);
        }

        private void RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            CustomBase cbase = Singleton<CustomBase>.Instance;

            Transform camLight = GameObject.Find("CharaCustom/CustomControl/CharaCamera/Main Camera/Lights Custom/Directional Light Key").transform;
            Transform backLight = GameObject.Find("CharaCustom/CustomControl/CharaCamera/Main Camera/Lights Custom/Directional Light Back").transform;

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

                    cbase.ResetLightSetting();

                    Destroy(newParent);
                    newParent = null;
                }
            });

            backlightToggle = e.AddSidebarControl(new SidebarToggle("Toggle Backlight", DefaultBacklight.Value, this));
            backlightToggle.ValueChanged.Subscribe(b =>
            {
                backLight.gameObject.SetActive(b);
            });

            blinkingToggle = e.AddSidebarControl(new SidebarToggle("Toggle Blinking", DefaultBlinking.Value, this));
            blinkingToggle.ValueChanged.Subscribe(b =>
            {
                MakerAPI.GetCharacterControl().ChangeEyesBlinkFlag(b);
            });
        }

        private void OnDestroy()
        {
            lockCamlightToggle = null;
            backlightToggle = null;
            blinkingToggle = null;

            HooksInstance["MakerHooks"].UnpatchAll(HooksInstance["MakerHooks"].Id);
            HooksInstance["MakerHooks"] = null;
        }

        private void MakerFinishedLoading(object sender, EventArgs e)
        {
            HooksInstance["MakerHooks"] = Harmony.CreateAndPatchAll(typeof(MakerHooks));
        }

        private static class MakerHooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "ChangeEyesBlinkFlag")]
            private static void ChaControl_ChangeEyesBlinkFlag_Postfix(ChaControl __instance, bool blink)
            {
                if (blink != blinkingToggle.Value)
                    __instance.ChangeEyesBlinkFlag(blinkingToggle.Value);
            }
        }
    }
}