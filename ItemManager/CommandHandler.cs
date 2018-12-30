using System.Linq;
using RemoteAdmin;
using Smod2.API;
using Smod2.Commands;
using UnityEngine;

namespace ItemManager
{
    public class CommandHandler : ICommandHandler
    {
        public string[] OnCall(ICommandSender sender, string[] args)
        {
            if (!(sender is Server) && sender is Player player && !Plugin.giveRanks.Contains(player.GetRankName()))
            {
                return new[]
                {
                    "Your rank does not have the permission to give custom items."
                };
            }

            if (args.Length < 1)
            {
                return new[]
                {
                    "Please specify a player ID."
                };
            }

            GameObject target;
            if (!int.TryParse(args[0], out int playerId) || (target = PlayerManager.singleton.players.FirstOrDefault(x => playerId == x.GetComponent<QueryProcessor>().PlayerId)) == null)
            {
                return new[]
                {
                    "Invalid target player ID."
                };
            }

            if (args.Length < 2)
            {
                return new[]
                {
                    "Please specify an item psuedo ID."
                };
            }

            if (!int.TryParse(args[1], out int psuedoId) || !Items.registeredItems.ContainsKey(psuedoId))
            {
                return new[]
                {
                    "Invalid psuedo ID."
                };
            }

            Items.GiveItem(target, psuedoId);

            return new[]
            {
                "Added item successfully."
            };
        }

        public string GetUsage()
        {
            return "imgive <psuedo ID>";
        }

        public string GetCommandDescription()
        {
            return "Gives a player a custom item that has that psuedo ID.";
        }
    }
}
