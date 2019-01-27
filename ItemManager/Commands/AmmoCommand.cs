using System.Collections.Generic;
using System.Linq;
using ItemManager.Utilities;
using RemoteAdmin;
using Smod2.API;
using Smod2.Commands;
using UnityEngine;

namespace ItemManager.Commands
{
	public class AmmoCommand : ICommandHandler
	{
		private readonly ImPlugin plugin;

		public AmmoCommand(ImPlugin plugin)
		{
			this.plugin = plugin;
		}

		private static int ParseAll(IReadOnlyList<string> args, out int[] argInts)
		{
			argInts = new int[args.Count];

			for (int i = 0; i < args.Count; i++)
			{
				if (!int.TryParse(args[i], out argInts[i]))
				{
					return i;
				}
			}

			return -1;
		}

		public string[] OnCall(ICommandSender sender, string[] args)
		{
			Player player;
			if (!(sender is Server) && (player = sender as Player) != null && !plugin.AmmoRanks.Contains(player.GetRankName()))
			{
				return new[]
				{
					"Your rank does not have the permission to create custom ammo."
				};
			}

			int errorArg = ParseAll(args, out int[] intArgs);
			if (errorArg != -1)
			{
				return new[]
				{
					$"Invalid number in argument {errorArg + 1}."
				};
			}

			ICustomWeaponHandler handler;
			Vector3 position;
			int? count = null;
			string targetName;
			
			if (args.Length > 1)
			{
				GameObject target = PlayerManager.singleton.players.FirstOrDefault(x => intArgs[0] == x.GetComponent<QueryProcessor>().PlayerId);
				if (target == null)
				{
					return new[]
					{
						"Invalid target player ID."
					};
				}
				
				targetName = target.GetComponent<NicknameSync>().myNick;
				position = target.transform.position;

				if (!Items.Handlers.ContainsKey(intArgs[1]))
				{
					return new[]
					{
						"Invalid psuedo ID."
					};
				}

				handler = Items.Handlers[intArgs[1]] as ICustomWeaponHandler;
				if (handler == null)
				{
					return new[]
					{
						"Psuedo ID of the specified item is not a weapon."
					};
				}

				if (args.Length > 2)
				{
					count = intArgs[2];
				}
			}
			else
			{
				return new[]
				{
					"Invalid number of arguments."
				};
			}

			if (count == null)
			{
				count = handler.DroppedAmmoCount ?? Items.DefaultDroppedAmmoCount;
			}

			handler.CreateAmmo(position, Quaternion.Euler(0, 0, 0), count.Value); // Drop ammo with command specified count -> item default count -> global default count

			return new[]
			{
				$"Dropped {count.Value} {handler.AmmoName} onto player \"{targetName}\"."
			};
		}

		public string GetUsage()
		{
			return "imammo <player ID> <psuedo ID> <ammo count (optional)>";
		}

		public string GetCommandDescription()
		{
			return "Drops an ammo pickup of the specified item on the player (self if not specified) with the an ammo count (default if not specified)";
		}
	}
}
