using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System.Reflection;
using HarmonyLib;

using ChaCustom;

using UnityEngine.UI;

namespace MakerAdditions
{
    public static class Hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(CustomAcsMoveWindow), "Start")]
        public static void CustomAcsCorrectSet_Start_CreateAccessoryAdjust(CustomAcsMoveWindow __instance, ref float[] ___movePosValue, ref float[] ___moveRotValue)
        {
            Tools.CreateAccessoryAdjust(__instance, ___movePosValue, ___moveRotValue);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CustomAcsMoveWindow), "UpdateCustomUI")]
        public static void CustomAcsCorrectSet_UpdateCustomUI_ResetAdjToggles()
        {
            foreach (Toggle toggle in Tools.toggles.Where(toggle => toggle != null))
                toggle.isOn = false;
        }

        // Increase maximum digits to two
        [HarmonyPrefix, HarmonyPatch(typeof(CustomBase), "ConvertValueFromTextLimit")]
        public static void CustomBase_ConvertValueFromTextLimit_OverrideDigit(ref int digit)
        {
            digit = 2;
        }

        // Change filter to allow two digits
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "SetAccessoryPos")]
        public static IEnumerable<CodeInstruction> ChaControl_SetAccessoryPos_ChangeLimit(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> il = instructions.ToList();

            {
                List<CodeInstruction> items = il.FindAll(instruction => instruction.opcode == OpCodes.Ldstr && (string) instruction.operand == "f1");
                foreach (int index in items.Select(item => il.IndexOf(item)).ToList())
                {
                    if (index <= 0)
                    {
                        MakerAdditions.Logger.LogMessage("Failed transpiling 'ChaControl_SetAccessoryPos_ChangeLimit' f1 index not found!");
                        MakerAdditions.Logger.LogWarning("Failed transpiling 'ChaControl_SetAccessoryPos_ChangeLimit' f1 index not found!");
                        return il;
                    }

                    il[index].operand = "f2";
                }
            }

            {
                List<CodeInstruction> items = il.FindAll(instruction => instruction.opcode == OpCodes.Call && (instruction.operand as MethodInfo)?.Name == "Clamp");
                foreach (int index in items.Select(item => il.IndexOf(item)).ToList())
                {
                    if (index <= 0)
                    {
                        MakerAdditions.Logger.LogMessage("Failed transpiling 'ChaControl_SetAccessoryPos_ChangeLimit' Clamp index not found!");
                        MakerAdditions.Logger.LogWarning("Failed transpiling 'ChaControl_SetAccessoryPos_ChangeLimit' Clamp index not found!");
                        return il;
                    }

                    il[index - 2].opcode = OpCodes.Nop;
                    il[index - 1].opcode = OpCodes.Nop;
                    il[index].opcode = OpCodes.Nop;
                }
            }

            return il;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "SetAccessoryRot")]
        public static IEnumerable<CodeInstruction> ChaControl_SetAccessoryRot_ChangeLimit(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> il = instructions.ToList();

            // Remove int cast to allow point values
            List<CodeInstruction> items = il.FindAll(instruction => instruction.opcode == OpCodes.Conv_I4);
            foreach (int index in items.Select(item => il.IndexOf(item)).ToList())
            {
                if (index <= 0)
                {
                    MakerAdditions.Logger.LogMessage("Failed transpiling 'ChaControl_SetAccessoryRot_ChangeLimit' Conv_I4 index not found!");
                    MakerAdditions.Logger.LogWarning("Failed transpiling 'ChaControl_SetAccessoryRot_ChangeLimit' Conv_I4 index not found!");
                    return il;
                }

                il[index].opcode = OpCodes.Nop;
                il[index + 1].opcode = OpCodes.Nop;
            }

            // Round to two digits
            List<CodeInstruction> items1 = il.FindAll(instruction => instruction.opcode == OpCodes.Call && (instruction.operand as MethodInfo)?.Name == "Repeat");
            foreach (int index in items1.Select(item => il.IndexOf(item)).ToList())
            {
                if (index <= 0)
                {
                    MakerAdditions.Logger.LogMessage("Failed transpiling 'ChaControl_SetAccessoryRot_ChangeLimit' Repeat index not found!");
                    MakerAdditions.Logger.LogWarning("Failed transpiling 'ChaControl_SetAccessoryRot_ChangeLimit' Repeat index not found!");
                    return il;
                }

                il[index] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Tools), nameof(Tools.NewRepeat)));
            }

            return il;
        }
    }
}