using AdminTools.Enums;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Firearms.Attachments;
using MEC;
using Mirror;
using NorthwoodLib.Pools;
using PlayerRoles;
using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AdminTools
{

    public sealed class EventHandlers
    {
        public static Plugin Plugin;
        public EventHandlers(Plugin plugin) => Plugin = plugin;

        [PluginEvent(ServerEventType.PlayerInteractDoor)]
        public void OnDoorOpen(AtPlayer player, DoorVariant door, bool canOpen)
        {
            if (player.PryGateEnabled)
                door.TryPryOpen(player);
        }

        public static string FormatArguments(ArraySegment<string> sentence, int index)
        {
            StringBuilder sb = StringBuilderPool.Shared.Rent();
            foreach (string word in sentence.Segment(index))
            {
                sb.Append(word);
                sb.Append(" ");
            }
            string msg = sb.ToString();
            StringBuilderPool.Shared.Return(sb);
            return msg;
        }

        public static void SpawnDummyModel(Player ply, Vector3 position, Quaternion rotation, RoleTypeId role, float x, float y, float z, out int dummyIndex)
        {
            dummyIndex = 0;
            GameObject obj = Object.Instantiate(NetworkManager.singleton.playerPrefab);
            CharacterClassManager ccm = obj.GetComponent<CharacterClassManager>();
            ccm._hub.roleManager.ServerSetRole(role, RoleChangeReason.RemoteAdmin);
            ccm.GodMode = true;
            obj.GetComponent<NicknameSync>().Network_myNickSync = "Dummy";
            obj.GetComponent<QueryProcessor>()._hub.Network_playerId = new RecyclablePlayerId(9999);
            obj.transform.localScale = new Vector3(x, y, z);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            NetworkServer.Spawn(obj);
            List<GameObject> objs = Plugin.DumHubs.GetOrAdd(ply, GameObjectListFactory);
            objs.Add(obj);
            dummyIndex = objs.Count;
            if (dummyIndex != 1)
                dummyIndex = objs.Count;
        }
        private static List<GameObject> GameObjectListFactory() => new();

        public static IEnumerator<float> SpawnBodies(Player player, RoleTypeId role, int count)
        {
            if (!PlayerRoleLoader.AllRoles.TryGetValue(role, out PlayerRoleBase roleBase) || roleBase is not IRagdollRole currentRole)
                yield break;
            for (int i = 0; i < count; i++)
            {

                GameObject gameObject = Object.Instantiate(currentRole.Ragdoll.gameObject, player.Position, player.Camera.rotation);
                if (gameObject.TryGetComponent(out BasicRagdoll component))
                {
                    Transform transform = currentRole.Ragdoll.transform;
                    UniversalDamageHandler handler = new(0.0f, DeathTranslations.Unknown);
                    component.NetworkInfo = new RagdollData(ReferenceHub._hostHub, handler, transform.localPosition, transform.localRotation);
                }

                NetworkServer.Spawn(gameObject);

                RagdollManager.ServerSpawnRagdoll(ReferenceHub._hostHub,
                    new UniversalDamageHandler(0.0f, DeathTranslations.Unknown));
                yield return Timing.WaitForOneFrame;
            }
        }

        [PluginEvent(ServerEventType.PlayerLeft)]
        public void OnPlayerDestroyed(Player player, Player attacker, DamageHandlerBase damageHandler)
        {
            if (!Plugin.RoundStartMutes.Contains(player))
                return;
            player.Unmute(true);
            Plugin.RoundStartMutes.Remove(player);
        }

        public static void SpawnWorkbench(Player ply, Vector3 position, Vector3 rotation, Vector3 size, out int benchIndex)
        {
            try
            {
                Log.Debug("Spawning workbench");
                benchIndex = 0;
                GameObject bench = Object.Instantiate(NetworkManager.singleton.spawnPrefabs.Find(p => p.gameObject.name == "Work Station"));
                rotation.x += 180;
                rotation.z += 180;
                Offset offset = new()
                {
                    position = position,
                    rotation = rotation,
                    scale = Vector3.one
                };
                bench.gameObject.transform.localScale = size;
                NetworkServer.Spawn(bench);
                List<GameObject> objs = Plugin.BchHubs.GetOrAdd(ply, GameObjectListFactory);
                objs.Add(bench);
                benchIndex = Plugin.BchHubs[ply].Count;

                if (benchIndex != 1)
                    benchIndex = objs.Count;
                bench.transform.localPosition = offset.position;
                bench.transform.localRotation = Quaternion.Euler(offset.rotation);
                bench.AddComponent<WorkstationController>();
            }
            catch (Exception e)
            {
                Log.Error($"{nameof(SpawnWorkbench)}: {e}");
                benchIndex = -1;
            }
        }

        public static void SetPlayerScale(GameObject target, float x, float y, float z)
        {
            try
            {
                NetworkIdentity identity = target.GetComponent<NetworkIdentity>();
                target.transform.localScale = new Vector3(1 * x, 1 * y, 1 * z);

                ObjectDestroyMessage destroyMessage = new()
                {
                    netId = identity.netId
                };

                foreach (Player player in Player.GetPlayers())
                {
                    NetworkConnection playerCon = player.Connection;
                    if (player.GameObject != target)
                        playerCon.Send(destroyMessage);

                    object[] parameters = { identity, playerCon };
                    typeof(NetworkServer).InvokeStaticMethod("SendSpawnMessage", parameters);
                }
            }
            catch (Exception e)
            {
                Log.Info($"Set Scale error: {e}");
            }
        }

        public static void SetPlayerScale(GameObject target, float scale)
        {
            try
            {
                NetworkIdentity identity = target.GetComponent<NetworkIdentity>();
                target.transform.localScale = Vector3.one * scale;

                ObjectDestroyMessage destroyMessage = new()
                {
                    netId = identity.netId
                };

                foreach (Player player in Player.GetPlayers())
                {
                    if (player.GameObject == target)
                        continue;

                    NetworkConnection connection = player.Connection;
                    connection.Send(destroyMessage);

                    object[] parameters = { identity, connection };
                    typeof(NetworkServer).InvokeStaticMethod("SendSpawnMessage", parameters);
                }
            }
            catch (Exception e)
            {
                Log.Info($"Set Scale error: {e}");
            }
        }

        public static IEnumerator<float> DoRocket(Player player, float speed)
        {
            const int maxAmnt = 50;
            int amnt = 0;
            while (player.Role != RoleTypeId.Spectator)
            {
                player.Position += Vector3.up * speed;
                amnt++;
                if (amnt >= maxAmnt)
                {
                    player.IsGodModeEnabled = false;
                    Handlers.CreateThrowable(ItemType.GrenadeHE).SpawnActive(player.Position, .5f, player);
                    player.Kill("Went on a trip in their favorite rocket ship.");
                }

                yield return Timing.WaitForOneFrame;
            }
        }

        public static IEnumerator<float> DoJail(Player player, bool skipAdd = false)
        {
            Dictionary<AmmoType, ushort> ammo = player.Ammo().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            List<ItemType> items = player.ReferenceHub.inventory.UserInventory.Items.Select(x => x.Value.ItemTypeId).ToList();
            if (!skipAdd)
            {
                Plugin.JailedPlayers.Add(new Jailed
                {
                    Health = player.Health,
                    Position = player.Position,
                    Items = items,
                    Role = player.Role,
                    UserId = player.UserId,
                    CurrentRound = true,
                    Ammo = ammo
                });
            }

            if (player.IsOverwatchEnabled)
                player.IsOverwatchEnabled = false;
            yield return Timing.WaitForSeconds(1f);

            player.SetRole(RoleTypeId.Tutorial);
            player.Position = new Vector3(38f, 1020f, -32f);
        }

        public static IEnumerator<float> DoUnJail(Player player)
        {
            Jailed jail = Plugin.JailedPlayers.Find(j => j.UserId == player.UserId);
            if (jail.CurrentRound)
            {
                player.SetRole(jail.Role);
                yield return Timing.WaitForSeconds(0.5f);
                try
                {
                    player.ResetInventory(jail.Items);
                    player.Health = jail.Health;
                    player.Position = jail.Position;
                    foreach (KeyValuePair<AmmoType, ushort> kvp in jail.Ammo)
                        player.AddAmmo(kvp.Key.GetItemType(), kvp.Value);
                }
                catch (Exception e)
                {
                    Log.Error($"{nameof(DoUnJail)}: {e}");
                }
            }
            else
            {
                player.SetRole(RoleTypeId.Spectator);
            }
            Plugin.JailedPlayers.Remove(jail);
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        public void OnPlayerVerified(Player player)
        {
            try
            {
                if (Plugin.JailedPlayers.Any(j => j.UserId == player.UserId))
                    Timing.RunCoroutine(DoJail(player, true));

                if (File.ReadAllText(Plugin.OverwatchFilePath).Contains(player.UserId))
                {
                    Log.Debug($"Putting {player.UserId} into overwatch.");
                    Timing.CallDelayed(1, () => player.IsOverwatchEnabled = true);
                }

                if (File.ReadAllText(Plugin.HiddenTagsFilePath).Contains(player.UserId))
                {
                    Log.Debug($"Hiding {player.UserId}'s tag.");
                    Timing.CallDelayed(1, () => player.SetBadgeHidden(true));
                }

                if (Plugin.RoundStartMutes.Count == 0 || player.ReferenceHub.serverRoles.RemoteAdmin ||
                    Plugin.RoundStartMutes.Contains(player)) return;

                Log.Debug($"Muting {player.UserId} (no RA).");
                player.Mute();
                Plugin.RoundStartMutes.Add(player);
            }
            catch (Exception e)
            {
                Log.Error($"Player Join: {e}");
            }
        }

        [PluginEvent(ServerEventType.RoundStart)]
        public void OnRoundStart()
        {
            foreach (Player ply in Plugin.RoundStartMutes.Where(ply => ply != null))
                ply.Unmute(true);

            Plugin.RoundStartMutes.Clear();
        }

        [PluginEvent(ServerEventType.RoundEnd)]
        public void OnRoundEnd(RoundSummary.LeadingTeam leadingTeam)
        {
            try
            {
                List<string> overwatchRead = File.ReadAllLines(Plugin.OverwatchFilePath).ToList();
                List<string> tagsRead = File.ReadAllLines(Plugin.HiddenTagsFilePath).ToList();

                foreach (Player player in Player.GetPlayers())
                {
                    string userId = player.UserId;

                    if (player.IsOverwatchEnabled && !overwatchRead.Contains(userId))
                        overwatchRead.Add(userId);
                    else if (!player.IsOverwatchEnabled && overwatchRead.Contains(userId))
                        overwatchRead.Remove(userId);

                    if (player.IsBadgeHidden() && !tagsRead.Contains(userId))
                        tagsRead.Add(userId);
                    else if (!player.IsBadgeHidden() && tagsRead.Contains(userId))
                        tagsRead.Remove(userId);
                }

                foreach (string s in overwatchRead)
                    Log.Debug($"{s} is in overwatch.");
                foreach (string s in tagsRead)
                    Log.Debug($"{s} has their tag hidden.");
                File.WriteAllLines(Plugin.OverwatchFilePath, overwatchRead);
                File.WriteAllLines(Plugin.HiddenTagsFilePath, tagsRead);

                // Update all the jails that it is no longer the current round, so when they are unjailed they don't teleport into the void.
                foreach (Jailed jail in Plugin.JailedPlayers.Where(jail => jail.CurrentRound))
                    jail.CurrentRound = false;
            }
            catch (Exception e)
            {
                Log.Error($"Round End: {e}");
            }

            if (!Plugin.RestartOnEnd)
                return;
            Log.Info("Restarting server....");
            Round.Restart(false, true);
        }

        [PluginEvent(ServerEventType.PlayerInteractDoor)]
        public void OnPlayerInteractingDoor(AtPlayer player, DoorVariant door, bool canOpen)
        {
            if (player.BreakDoorsEnabled)
                door.BreakDoor();
        }
    }
}
