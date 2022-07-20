using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MelonLoader;
using System.IO;
using UnityEngine;
using VRC;
using VRC.SDKBase;
using VRC.Playables;
using UnityEngine.Animations;
using System.Reflection;

namespace DexProtect
{
    public class DexProtectUnlock : MelonMod
    {
        public static MelonLogger.Instance log = new MelonLogger.Instance("DexProtect");
        public static AvatarPlayableController playerAnim;
        public static AnimatorControllerPlayable playerFX;
        public static List<string> keyParams;
        public static List<bool> keyBools;
        public static List<bool> roundVals = new List<bool>() { false, false, false, true };
        public static List<bool> lastVals = new List<bool>() { false, false, false, true };
        public static List<string> roundParams;
        public static bool inLockedAvatar;
        public static int triggerReset = 0;
        public static Il2CppSystem.Collections.Generic.Dictionary<int, AvatarParameter> allParams;
        public static Dictionary<string, int> paramKeys;
        public static List<(string, float)> storedParams;
        public static string lastAvatarId;
        private static void OnAvatarInstantiate(Player player, GameObject avatar, VRC_AvatarDescriptor descriptor)
        {
            try
            {
                if (player._vrcplayer == VRCPlayer.field_Internal_Static_VRCPlayer_0)
                {
                    var id = player._vrcplayer.field_Private_ApiAvatar_0.id;
                    if (File.Exists("UserData/DexProtect/" + id + ".key"))
                    {
                        log.Msg("Key detected for avatar ID " + id + ", unlocking...");
                        var keyString = File.ReadAllText("UserData/DexProtect/" + id + ".key", Encoding.Unicode).Split('|').ToList();
                        keyParams = keyString.Skip(5).Select(x => new string(x.Skip(1).ToArray())).ToList();
                        keyBools = keyString.Skip(5).Select(x => x[0] == '1').ToList();
                        roundParams = keyString.GetRange(1, 4);
                        var roundSplit = keyString[0];
                        playerAnim = player._vrcplayer.field_Private_AnimatorControllerManager_0.field_Private_AvatarAnimParamController_0.field_Private_AvatarPlayableController_0;
                        playerFX = playerAnim.field_Private_AvatarAnimLayer_7.field_Private_AnimatorControllerPlayable_0;
                        allParams = playerAnim.field_Private_Dictionary_2_Int32_AvatarParameter_0;
                        SetParams();
                        playerFX.SetBool(roundSplit, true);
                        if (triggerReset == 2 && lastAvatarId != id) triggerReset = 0;
                        lastAvatarId = id;
                        inLockedAvatar = true;
                    }
                    else
                    {
                        log.Msg("No key detected for this avatar ID, not unlocking");
                        inLockedAvatar = false;
                        lastAvatarId = id;
                        return;
                    }
                }
            }
            catch { }
        }
        private static void SetParams()
        {
            for (var i = 0; i < playerFX.GetParameterCount(); i++)
            {
                var param = playerFX.GetParameter(i);
                var ind = keyParams.IndexOf(param.name);
                if (ind != -1)
                {
                    playerFX.SetBool(param.name, keyBools[ind]);
                }
            }
        }
        private static void SetParamsLate()
        {
            for (var i = 0; i < keyParams.Count; i++)
            {
                allParams[paramKeys[keyParams[i]]].prop_Int32_1 = Convert.ToInt32(keyBools[i]);
            }
        }
        private static void StoreParams()
        {
            storedParams = new List<(string, float)>();
            foreach (var param in allParams)
            {
                storedParams.Add((param.Value.field_Private_String_0, param.Value.prop_Single_0));
            }
        }
        private static void RestoreParams()
        {
            foreach (var storedParam in storedParams)
            {
                allParams[paramKeys[storedParam.Item1]].prop_Single_0 = storedParam.Item2;
            }
        }
        public override void OnUpdate()
        {
            if (!inLockedAvatar) return;
            if (playerAnim == null) return;
            var currentVals = new List<bool>();
            for (var i = 0; i < 4; i++) currentVals.Add(playerFX.GetBool(roundParams[i]));
            if (currentVals[0] == roundVals[0] && currentVals[1] == roundVals[1] && currentVals[2] == roundVals[2] && currentVals[3] == roundVals[3] &&
                (currentVals[0] != lastVals[0] || currentVals[1] != lastVals[1] || currentVals[2] != lastVals[2] || currentVals[3] != lastVals[3]) &&
                !(lastVals[0] == false && lastVals[1] == false && lastVals[2] == false && lastVals[3] == false))
            {
                if (triggerReset != 2)
                {
                    paramKeys = new Dictionary<string, int>();
                    foreach (var entry in allParams) paramKeys.Add(entry.Value.field_Private_String_0, entry.Key);
                    if (triggerReset == 0)
                    {
                        triggerReset = 1;
                        StoreParams();
                        ResetAvatar();
                        return;
                    }
                    else
                    {
                        triggerReset = 2;
                        RestoreParams();
                    }
                }
                SetParamsLate();
            }
            lastVals = currentVals;
        }
        public override void OnApplicationStart()
        {
            VRCAvatarManager.field_Private_Static_Action_3_Player_GameObject_VRC_AvatarDescriptor_0 = Il2CppSystem.Delegate.Combine(
                (Il2CppSystem.Action<Player, GameObject, VRC_AvatarDescriptor>)OnAvatarInstantiate,
                VRCAvatarManager.field_Private_Static_Action_3_Player_GameObject_VRC_AvatarDescriptor_0
            ).Cast<Il2CppSystem.Action<Player, GameObject, VRC_AvatarDescriptor>>();
        }
        private static void ResetAvatar() => VRCPlayer.field_Internal_Static_VRCPlayer_0.Method_Public_Void_PDM_1();
    }
}
