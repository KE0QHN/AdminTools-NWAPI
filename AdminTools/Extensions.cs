using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AdminTools
{
    public static class Extensions
    {
        public static void InvokeStaticMethod(this Type type, string methodName, object[] param)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.Public;
            MethodInfo info = type.GetMethod(methodName, flags);
            info?.Invoke(null, param);
        }

        // Not used.
        // public static void OldRefreshPlyModel(this CharacterClassManager ccm, GameObject player, RoleTypeId classId = RoleTypeId.None)
        // {
        // 	ReferenceHub hub = ReferenceHub.GetHub(player);
        // 	hub.GetComponent<AnimationController>().OnChangeClass();
        // 	if (ccm.MyModel != null)
        // 	{
        // 		UnityEngine.Object.Destroy(ccm.MyModel);
        // 	}
        // 	Role role = ccm.Classes.SafeGet((classId < RoleTypeId.Scp173) ? ccm.CurClass : classId);
        // 	if (role.team != Team.RIP)
        // 	{
        // 		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(role.model_player, ccm.gameObject.transform, true);
        // 		gameObject.transform.localPosition = role.model_offset.position;
        // 		gameObject.transform.localRotation = Quaternion.Euler(role.model_offset.rotation);
        // 		gameObject.transform.localScale = role.model_offset.scale;
        // 		ccm.MyModel = gameObject;
        // 		AnimationController component = hub.GetComponent<AnimationController>();
        // 		if (ccm.MyModel.GetComponent<Animator>() != null)
        // 		{
        // 			component.animator = ccm.MyModel.GetComponent<Animator>();
        // 		}
        // 		FootstepSync component2 = ccm.GetComponent<FootstepSync>();
        // 		FootstepHandler component3 = ccm.MyModel.GetComponent<FootstepHandler>();
        // 		if (component2 != null)
        // 		{
        // 			component2.FootstepHandler = component3;
        // 		}
        // 		if (component3 != null)
        // 		{
        // 			component3.FootstepSync = component2;
        // 			component3.AnimationController = component;
        // 		}
        // 		if (ccm.isLocalPlayer)
        // 		{
        // 			if (ccm.MyModel.GetComponent<Renderer>() != null)
        // 			{
        // 				ccm.MyModel.GetComponent<Renderer>().enabled = false;
        // 			}
        // 			Renderer[] componentsInChildren = ccm.MyModel.GetComponentsInChildren<Renderer>();
        // 			for (int i = 0; i < componentsInChildren.Length; i++)
        // 			{
        // 				componentsInChildren[i].enabled = false;
        // 			}
        // 			foreach (Collider collider in ccm.MyModel.GetComponentsInChildren<Collider>())
        // 			{
        // 				if (collider.name != "LookingTarget")
        // 				{
        // 					collider.enabled = false;
        // 				}
        // 			}
        // 		}
        // 	}
        // 	ccm.GetComponent<CapsuleCollider>().enabled = (role.team != Team.RIP);
        // }
        public static List<AtPlayer> Players => Player.GetPlayers<AtPlayer>();
        public static AtPlayer GetPlayer(string arg) => int.TryParse(arg, out int id) ? Extensions.Players.FirstOrDefault(x => x.PlayerId == id) : Player.GetByName<AtPlayer>(arg);
    }
}
