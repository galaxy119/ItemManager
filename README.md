# ItemManager [![Build status](https://ci.appveyor.com/api/projects/status/gojb9ucyfmdjn28p?svg=true)](https://ci.appveyor.com/project/probe4aiur/itemmanager)
An SCP: Secret Laboratory plugin for easily implementing custom items.

# Installation
**[Smod2](https://github.com/Grover-c13/Smod2) must be installed for this to work.**  
**914 held items ("914inhand.dll") must be uninstalled or people with custom items in their inventories going through 914 will possibly break their own inventories.**  
If you need held items, look to the config below.

Place the "ItemManager.dll" file in your sm_plugins folder.

# Commands
| Command        | Description |
| :-------------: | :------ |
| imgive [player ID] [psuedo ID] | Gives the specified player the custom weapon with said psuedo ID. |

| Parameter | Value Type | Description |
| :-------: | :--------: | :---------- |
| player ID | Integer | The ID of a player, shown as the number to the left in remote admin. |
| psuedo ID | Integer | The ID of a custom item. Look at their own documentation to figure out what it is. |

# Configs
| Config        | Value Type | Default | Description |
| :-------------: | :---------: | :---------: |:------ |
| im_helditems | Integer | 3 | Whether or not to support held items. Set to 0 for none, 1 for only custom, 2 for only vanilla, or 3 for all items|
| im_give_ranks | String List | owner, admin | Ranks allowed to use the `imgive` command. |

# Attention Developers!
If you are making a custom item, **please add your item psuedo ID to the wiki** so people in the future do not use the same ID and cause conflicts.  
Thank you!
