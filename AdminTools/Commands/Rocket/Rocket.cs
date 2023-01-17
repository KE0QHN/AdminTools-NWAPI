﻿using CommandSystem;
using MEC;
using PlayerRoles;
using PluginAPI.Core;
using System;
using System.Linq;

namespace AdminTools.Commands.Rocket
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public sealed class Rocket : ParentCommand
    {
        public Rocket() => LoadGeneratedCommands();

        public override string Command => "rocket";

        public override string[] Aliases { get; } = { };

        public override string Description => "Sends players high in the sky and explodes them";

        public override void LoadGeneratedCommands() { }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!((CommandSender)sender).CheckPermission(PlayerPermissions.ForceclassWithoutRestrictions))
            {
                response = "You do not have permission to use this command";
                return false;
            }

            if (arguments.Count < 2)
            {
                response = "Usage: rocket ((player id / name) or (all / *)) (speed)";
                return false;
            }

            switch (arguments.At(0))
            {
                case "*":
                case "all":
                    if (!float.TryParse(arguments.At(1), out float speed) && speed <= 0)
                    {
                        response = $"Speed argument invalid: {arguments.At(1)}";
                        return false;
                    }

                    foreach (Player ply in Player.GetPlayers())
                        Timing.RunCoroutine(EventHandlers.DoRocket(ply, speed));

                    response = "Everyone has been rocketed into the sky (We're going on a trip, in our favorite rocketship)";
                    return true;
                default:
                    Player pl = int.TryParse(arguments.At(0), out int id) ? Player.GetPlayers().FirstOrDefault(x => x.PlayerId == id) : Player.GetByName(arguments.At(0));

                    if (pl == null)
                    {
                        response = $"Player not found: {arguments.At(0)}";
                        return false;
                    }
                    if (pl.Role is RoleTypeId.Spectator or RoleTypeId.None)
                    {
                        response = $"Player {pl.Nickname} is not a valid class to rocket";
                        return false;
                    }

                    if (!float.TryParse(arguments.At(1), out float spd) && spd <= 0)
                    {
                        response = $"Speed argument invalid: {arguments.At(1)}";
                        return false;
                    }

                    Timing.RunCoroutine(EventHandlers.DoRocket(pl, spd));
                    response = $"Player {pl.Nickname} has been rocketed into the sky (We're going on a trip, in our favorite rocketship)";
                    return true;
            }
        }
    }
}
