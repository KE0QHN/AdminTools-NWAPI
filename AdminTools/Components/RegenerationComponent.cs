﻿using MEC;
using PluginAPI.Core;
using System.Collections.Generic;
using UnityEngine;

namespace AdminTools.Components
{
    public class RegenerationComponent : MonoBehaviour
    {
        private Player _player;
        private CoroutineHandle _handle;
        public void Awake()
        {
            _player = Player.Get(gameObject);
            _handle = Timing.RunCoroutine(HealHealth(_player));
            Plugin.RgnHubs.Add(_player, this);
        }

        public void OnDestroy()
        {
            Timing.KillCoroutines(_handle);
            Plugin.RgnHubs.Remove(_player);
        }

        public static IEnumerator<float> HealHealth(Player ply)
        {
            while (true)
            {
                if (ply.Health < ply.MaxHealth)
                    ply.Health += Plugin.HealthGain;
                else
                    ply.Health = ply.MaxHealth;

                yield return Timing.WaitForSeconds(Plugin.HealthInterval);
            }
        }
    }
}
