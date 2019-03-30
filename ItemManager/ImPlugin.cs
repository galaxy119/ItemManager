using System;

using scp4aiur;
using ItemManager.Commands;

using Smod2;
using Smod2.API;
using Smod2.Config;
using Smod2.Attributes;

namespace ItemManager
{
	[Flags]
	public enum HeldSetting
	{
		None = 0,
		Custom = 1,
		Vanilla = 2,
		CurrentCustom = 3,
		CurrentVanilla = 4,
		CurrentAll = CurrentCustom | CurrentVanilla,
		All = Custom | Vanilla
	}

	[PluginDetails(
		author = "4aiur",
		name = "Item Manager",
		description = "Allows other plugins to register custom item associations.",
		id = "4aiur.custom.itemmanager",
		version = "1.0.3",
		SmodMajor = 3,
		SmodMinor = 2,
		SmodRevision = 2)]
	public class ImPlugin : Plugin
	{
		internal const ItemType DefaultDropAmmoType = ItemType.COIN;
		internal const int DefaultDropAmmoCount = 15;

		public HeldSetting HeldItems { get; private set; }
		public string[] GiveRanks { get; private set; }
		public string[] AmmoRanks { get; private set; }

		public override void Register()
		{
			AddConfig(new ConfigSetting("im_helditems", (int)HeldSetting.All, SettingType.NUMERIC, true, "Whether or not ItemManager will take held items into account in 914. 0 for none, 1 for only custom items, 2 for only normal items, 3 for all items."));
			AddConfig(new ConfigSetting("im_give_ranks", new[]
			{
				"owner",
				"admin"
			}, SettingType.LIST, true, "Rank names that should be allowed to use the give command."));
			AddConfig(new ConfigSetting("im_ammo_ranks", new[]
			{
				"owner",
				"admin"
			}, SettingType.LIST, true, "Rank names that should be allowed to use the ammo command."));
			AddConfig(new ConfigSetting("im_ammo_type", (int)DefaultDropAmmoType, SettingType.NUMERIC, true, "Default item ID of custom ammo pickups."));
			AddConfig(new ConfigSetting("im_ammo_count", DefaultDropAmmoCount, SettingType.NUMERIC, true, "Default amount of ammo in custom ammo pickups."));

			Timing.Init(this);
			AddEventHandlers(new EventHandlers(this));
			AddCommand("imgive", new GiveCommand(this));
			AddCommand("imammo", new AmmoCommand(this));
		}

		public void RefreshConfig()
		{
			HeldItems = (HeldSetting)GetConfigInt("im_helditems");
			GiveRanks = GetConfigList("im_give_ranks");
			AmmoRanks = GetConfigList("im_ammo_ranks");
			Items.DefaultDroppedAmmoType = (ItemType)GetConfigInt("im_ammo_type");
			Items.DefaultDroppedAmmoCount = GetConfigInt("im_ammo_count");
		}

		public override void OnEnable()
		{
			Info("Enabled Item Manager");
		}

		public override void OnDisable()
		{
			Info("Disabled Item Manager");
		}
	}
}
