﻿using CommandSystem;
using PluginAPI.Core;
using System;

namespace AdminTools.Commands.Unmute
{
    public class RoundStart : ICommand
    {
        public string Command => "roundstart";

        public string[] Aliases { get; } = { "rs" };

        public string Description => "Unmutes everyone from speaking until the round starts.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!((CommandSender)sender).CheckPermission(PlayerPermissions.PlayersManagement))
            {
                response = "You do not have permission to use this command";
                return false;
            }

            if (arguments.Count != 0)
            {
                response = "Usage: pumute roundstart";
                return false;
            }

            foreach (Player player in Plugin.RoundStartMutes)
            {
                player.Unmute(true);
            }
            Plugin.RoundStartMutes.Clear();

            response = "All non-staff players that were muted until round start have been unmuted.";
            return true;
        }
    }
}
