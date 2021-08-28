using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Windows;

namespace PoeTradeSearch
{
    [DataContract()]
    internal class ConfigData
    {
        [DataMember(Name = "options")]
        internal ConfigOption Options = null;

        [DataMember(Name = "shortcuts")]
        internal ConfigShortcut[] Shortcuts = null;
    }

    [DataContract(Name = "options")]
    internal class ConfigOption
    {
        [DataMember(Name = "league")]
        internal string League = "";

        [DataMember(Name = "server_type")]
        internal int ServerType = 0;

        [DataMember(Name = "server_timeout")]
        internal int ServerTimeout = 5;

        [DataMember(Name = "server_redirect")]
        internal bool ServerRedirect = false;

        [DataMember(Name = "search_auto_delay")]
        internal int SearchAutoDelay = 0;

        [DataMember(Name = "search_list_count")]
        internal int SearchListCount = 20;

        [DataMember(Name = "search_price_minimum")]
        internal int SearchPriceMinimum = 0;

        [DataMember(Name = "search_before_day")]
        internal int SearchBeforeDay = 7;

        [DataMember(Name = "auto_check_unique")]
        internal bool AutoCheckUnique = true;

        [DataMember(Name = "auto_check_totalres")]
        internal bool AutoCheckTotalres = true;

        [DataMember(Name = "auto_select_pseudo")]
        internal bool AutoSelectPseudo = true;

        [DataMember(Name = "auto_select_corrupt")]
        internal string AutoSelectCorrupt = "";

        [DataMember(Name = "auto_select_bytype")]
        internal string AutoSelectByType = "";

        [DataMember(Name = "check_updates")]
        internal bool AutoCheckUpdates = true;

        [DataMember(Name = "use_ctrl_wheel")]
        internal bool UseCtrlWheel = false;
    }

    [DataContract(Name = "shortcuts")]
    internal class ConfigShortcut
    {
        [DataMember(Name = "modifiers")]
        internal int Modifiers = 0;

        [DataMember(Name = "keycode")]
        internal int Keycode = 0;

        [DataMember(Name = "value")]
        internal string Value = null;
    }

    [DataContract()]
    internal class ParserData
    {
        [DataMember(Name = "local")]
        internal ParserDict Local = null;
        [DataMember(Name = "disable")]
        internal ParserDict Disable = null;
        [DataMember(Name = "position")]
        internal ParserDict Position = null;
        [DataMember(Name = "category")]
        internal ParserDict Category = null;
        [DataMember(Name = "rarity")]
        internal ParserDict Rarity = null;
        [DataMember(Name = "quality")]
        internal ParserDict Quality = null;
        [DataMember(Name = "sockets")]
        internal ParserDict Sockets = null;
        [DataMember(Name = "radius")]
        internal ParserDict Radius = null;
        [DataMember(Name = "unidentified")]
        internal ParserDict Unidentified = null;
        [DataMember(Name = "max")]
        internal ParserDict Max = null;
        [DataMember(Name = "level")]
        internal ParserDict Level = null;
        [DataMember(Name = "item_level")]
        internal ParserDict ItemLevel = null;
        [DataMember(Name = "talisman_tier")]
        internal ParserDict TalismanTier = null;
        [DataMember(Name = "option_tier")]
        internal ParserDict OptionTier = null;
        [DataMember(Name = "map_tier")]
        internal ParserDict MapTier = null;
        [DataMember(Name = "map_ultimatum")]
        internal ParserDict MapUltimatum = null;
        [DataMember(Name = "reward_ultimatum")]
        internal ParserDict RewardUltimatum = null;
        [DataMember(Name = "superior")]
        internal ParserDict Superior = null;
        [DataMember(Name = "vaal")]
        internal ParserDict Vaal = null;
        [DataMember(Name = "corrupted")]
        internal ParserDict Corrupted = null;
        [DataMember(Name = "metamorph")]
        internal ParserDict Metamorph = null;
        [DataMember(Name = "shaper_item")]
        internal ParserDict ShaperItem = null;
        [DataMember(Name = "elder_item")]
        internal ParserDict ElderItem = null;
        [DataMember(Name = "crusader_item")]
        internal ParserDict CrusaderItem = null;
        [DataMember(Name = "redeemer_item")]
        internal ParserDict RedeemerItem = null;
        [DataMember(Name = "hunter_item")]
        internal ParserDict HunterItem = null;
        [DataMember(Name = "warlord_item")]
        internal ParserDict WarlordItem = null;
        [DataMember(Name = "synthesised_item")]
        internal ParserDict SynthesisedItem = null;
        [DataMember(Name = "shaped")]
        internal ParserDict Shaped = null;
        [DataMember(Name = "blighted")]
        internal ParserDict Blighted = null;
        [DataMember(Name = "delirium_reward")]
        internal ParserDict DeliriumReward = null;
        [DataMember(Name = "monster_genus")]
        internal ParserDict MonsterGenus = null;
        [DataMember(Name = "monster_group")]
        internal ParserDict MonsterGroup = null;
        [DataMember(Name = "physical_damage")]
        internal ParserDict PhysicalDamage = null;
        [DataMember(Name = "elemental_damage")]
        internal ParserDict ElementalDamage = null;
        [DataMember(Name = "chaos_damage")]
        internal ParserDict ChaosDamage = null;
        [DataMember(Name = "attacks_per_second")]
        internal ParserDict AttacksPerSecond = null;
        [DataMember(Name = "attack_speed_incr")]
        internal ParserDict AttackSpeedIncr = null;
        [DataMember(Name = "physical_damage_incr")]
        internal ParserDict PhysicalDamageIncr = null;
        [DataMember(Name = "prophecy_item")]
        internal ParserDict ProphecyItem = null;
        [DataMember(Name = "entrails_item")]
        internal ParserDict EntrailsItem = null;
        [DataMember(Name = "unstack_items")]
        internal ParserDict UnstackItems = null;
        [DataMember(Name = "gems")]
        internal ParserDict Gems = null;
        [DataMember(Name = "currency")]
        internal ParserDict Currency = null;
        [DataMember(Name = "exchange")]
        internal ParserDict Exchange = null;
        [DataMember(Name = "cluster")]
        internal ParserDict Cluster = null;
        [DataMember(Name = "heist")]
        internal ParserDict Heist = null;
        [DataMember(Name = "logbook")]
        internal ParserDict Logbook = null;
    }

    [DataContract]
    internal class ParserDict
    {
        [DataMember(Name = "text")]
        internal string[] Text = null;

        [DataMember(Name = "entries")]
        internal ParserDictItem[] Entries = null;
    }

    [DataContract]
    internal class ParserDictItem
    {
        [DataMember(Name = "id")]
        internal string Id = null;

        [DataMember(Name = "key")]
        internal string Key = null;

        [DataMember(Name = "text")]
        internal string[] Text = null;

        [DataMember(Name = "hidden")]
        internal bool Hidden = false;
    }

    [DataContract()]
    internal class ModsDict
    {
        [DataMember(Name = "entries")]
        internal List<ModsDictItem> Entries = null;
    }


    [DataContract]
    public class ModsDictItem
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "level")]
        public int Level { get; set; }

        [DataMember(Name = "fix")]
        public string Fix { get; set; }

        [DataMember(Name = "name")]
        public string[] Name { get; set; }

        [DataMember(Name = "tags")]
        public string Tags { get; set; }

        [DataMember(Name = "min")]
        public string Min { get; set; }

        [DataMember(Name = "max")]
        public string Max { get; set; }
    }

    [DataContract()]
    internal class CheckedDict
    {
        [DataMember(Name = "entries")]
        internal List<CheckedDictItem> Entries = null;

        [DataMember(Name = "bases")]
        internal List<CheckedDictItem> bases = null;
    }

    [DataContract]
    public class CheckedDictItem
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "key")]
        public string Key { get; set; }

        public string Text
        {
            get
            {
                FilterDictItem dictItem = null;
                WinMain winMain = (WinMain)Application.Current.MainWindow;

                foreach (FilterDict data_result in winMain.mFilter[0].Result)
                {
                    dictItem = Array.Find(data_result.Entries, x => x.Id.IndexOf("." + this.Id) > 1);
                    if (dictItem != null) break;
                }

                return dictItem?.Text ?? this.Id;
            }
        }
    }

    [DataContract]
    internal class FilterData
    {
        [DataMember(Name = "result")]
        internal FilterDict[] Result = null;

        [DataMember(Name = "update")]
        internal string Update = null;
    }

    [DataContract]
    internal class FilterDict
    {
        [DataMember(Name = "id")]
        internal string Id = "";

        [DataMember(Name = "label")]
        internal string Label = "";

        [DataMember(Name = "entries")]
        internal FilterDictItem[] Entries = null;
    }

    [DataContract]
    internal class FilterDictItem
    {
        [DataMember(Name = "id")]
        internal string Id = "";

        [DataMember(Name = "name")]
        internal string Name = "";

        [DataMember(Name = "text")]
        internal string Text = "";

        [DataMember(Name = "type")]
        internal string Type = "";

        [DataMember(Name = "part")]
        internal string Part = "";
    }

    [DataContract]
    internal class FetchData
    {
        [DataMember(Name = "result")]
        internal FetchInfo[] Result;
    }

    [DataContract]
    internal class FetchInfo
    {
        [DataMember(Name = "id")]
        internal string ID = "";

        [DataMember(Name = "listing")]
        internal FetchListing Listing = new FetchListing();
    }

    [DataContract]
    internal class FetchListing
    {
        [DataMember(Name = "indexed")]
        internal string Indexed = "";

        [DataMember(Name = "account")]
        internal FetchAccount Account = new FetchAccount();

        [DataMember(Name = "price")]
        internal FetchPrice Price = new FetchPrice();
    }

    [DataContract]
    internal class FetchAccount
    {
        [DataMember(Name = "name")]
        internal string Name = "";
    }

    [DataContract]
    internal class FetchPrice
    {
        [DataMember(Name = "type")]
        internal string Type = "";

        [DataMember(Name = "amount")]
        internal double Amount = 0;

        [DataMember(Name = "currency")]
        internal string Currency = "";
    }

    [DataContract]
    internal class ResultData
    {
        [DataMember(Name = "result")]
        internal string[] Result = null;

        [DataMember(Name = "id")]
        internal string ID = "";

        [DataMember(Name = "total")]
        internal int Total = 0;
    }

    /*
    [DataContract]
    internal class DataFlags
    {
        [DataMember(Name = "unique")]
        internal bool Unique = false;
    }
    */

    [DataContract]
    internal class q_Option
    {
        [DataMember(Name = "option")]
        internal string Option;
    }

    [DataContract]
    internal class q_Min_And_Max
    {
        [DataMember(Name = "min")]
        internal double Min;

        [DataMember(Name = "max")]
        internal double Max;

        [DataMember(Name = "option")]
        internal object Option;
    }

    [DataContract]
    internal class q_Type_filters_filters
    {
        [DataMember(Name = "category")]
        internal q_Option Category = new q_Option();

        [DataMember(Name = "rarity")]
        internal q_Option Rarity = new q_Option();
    }

    [DataContract]
    internal class q_Type_filters
    {
        [DataMember(Name = "filters")]
        internal q_Type_filters_filters Filters = new q_Type_filters_filters();
    }

    [DataContract]
    internal class q_Socket_filters_filters
    {
        [DataMember(Name = "sockets")]
        internal q_Min_And_Max Sockets = new q_Min_And_Max();

        [DataMember(Name = "links")]
        internal q_Min_And_Max Links = new q_Min_And_Max();
    }

    [DataContract]
    internal class q_Socket_filters
    {
        [DataMember(Name = "disabled")]
        internal bool Disabled = false;

        [DataMember(Name = "filters")]
        internal q_Socket_filters_filters Filters = new q_Socket_filters_filters();
    }

    [DataContract]
    internal class q_Misc_filters_filters
    {
        [DataMember(Name = "quality")]
        internal q_Min_And_Max Quality = new q_Min_And_Max();

        [DataMember(Name = "ilvl")]
        internal q_Min_And_Max Ilvl = new q_Min_And_Max();

        [DataMember(Name = "gem_level")]
        internal q_Min_And_Max Gem_level = new q_Min_And_Max();

        [DataMember(Name = "corrupted")]
        internal q_Option Corrupted = new q_Option();

        [DataMember(Name = "shaper_item")]
        internal q_Option Shaper = new q_Option();

        [DataMember(Name = "elder_item")]
        internal q_Option Elder = new q_Option();

        [DataMember(Name = "crusader_item")]
        internal q_Option Crusader = new q_Option();

        [DataMember(Name = "redeemer_item")]
        internal q_Option Redeemer = new q_Option();

        [DataMember(Name = "hunter_item")]
        internal q_Option Hunter = new q_Option();

        [DataMember(Name = "warlord_item")]
        internal q_Option Warlord = new q_Option();

        [DataMember(Name = "synthesised_item")]
        internal q_Option Synthesis = new q_Option();

        [DataMember(Name = "gem_alternate_quality")]
        internal q_Option AlternateQuality = new q_Option();
    }

    [DataContract]
    internal class q_Misc_filters
    {
        [DataMember(Name = "disabled")]
        internal bool Disabled = false;

        [DataMember(Name = "filters")]
        internal q_Misc_filters_filters Filters = new q_Misc_filters_filters();
    }

    [DataContract]
    internal class q_Map_filters_filters
    {
        [DataMember(Name = "map_tier")]
        internal q_Min_And_Max Tier = new q_Min_And_Max();

        [DataMember(Name = "map_shaped")]
        internal q_Option Shaper = new q_Option();

        [DataMember(Name = "map_elder")]
        internal q_Option Elder = new q_Option();

        [DataMember(Name = "map_blighted")]
        internal q_Option Blight = new q_Option();
    }

    [DataContract]
    internal class q_Map_filters
    {
        [DataMember(Name = "disabled")]
        internal bool Disabled = false;

        [DataMember(Name = "filters")]
        internal q_Map_filters_filters Filters = new q_Map_filters_filters();
    }

    [DataContract]
    internal class q_Heist_filters_filters
    {
        [DataMember(Name = "heist_objective_value")]
        internal q_Option HeistObjective = new q_Option();
    }

    [DataContract]
    internal class q_Heist_filters
    {
        [DataMember(Name = "disabled")]
        internal bool Disabled = false;

        [DataMember(Name = "filters")]
        internal q_Heist_filters_filters Filters = new q_Heist_filters_filters();
    }

    [DataContract]
    internal class q_ultimatum_filters_filters
    {
        [DataMember(Name = "ultimatum_reward")]
        internal q_Option Reward = new q_Option();

        [DataMember(Name = "ultimatum_output")]
        internal q_Option Output = new q_Option();
    }

    [DataContract]
    internal class q_ultimatum_filters
    {
        [DataMember(Name = "disabled")]
        internal bool Disabled = false;

        [DataMember(Name = "filters")]
        internal q_ultimatum_filters_filters Filters = new q_ultimatum_filters_filters();
    }

    [DataContract]
    internal class q_Trade_filters_filters
    {
        [DataMember(Name = "indexed")]
        internal q_Option Indexed = new q_Option();

        [DataMember(Name = "sale_type")]
        internal q_Option SaleType = new q_Option();

        [DataMember(Name = "price")]
        internal q_Min_And_Max Price = new q_Min_And_Max();
    }

    [DataContract]
    internal class q_Trade_filters
    {
        [DataMember(Name = "disabled")]
        internal bool Disabled = false;

        [DataMember(Name = "filters")]
        internal q_Trade_filters_filters Filters = new q_Trade_filters_filters();
    }

    [DataContract]
    internal class q_Filters
    {
        [DataMember(Name = "type_filters")]
        internal q_Type_filters Type = new q_Type_filters();
        [DataMember(Name = "socket_filters")]
        internal q_Socket_filters Socket = new q_Socket_filters();
        [DataMember(Name = "map_filters")]
        internal q_Map_filters Map = new q_Map_filters();
        [DataMember(Name = "heist_filters")]
        internal q_Heist_filters Heist = new q_Heist_filters();
        [DataMember(Name = "ultimatum_filters")]
        internal q_ultimatum_filters Ultimatum = new q_ultimatum_filters();
        [DataMember(Name = "misc_filters")]
        internal q_Misc_filters Misc = new q_Misc_filters();
        [DataMember(Name = "trade_filters")]
        internal q_Trade_filters Trade = new q_Trade_filters();
        [DataMember(Name = "weapon_filters")]
        internal q_Disabled_filters Weapon = new q_Disabled_filters();
        [DataMember(Name = "armour_filters")]
        internal q_Disabled_filters Armour = new q_Disabled_filters();
        [DataMember(Name = "req_filters")]
        internal q_Disabled_filters Req = new q_Disabled_filters();
    }

    [DataContract]
    internal class q_Disabled_filters
    {
        [DataMember(Name = "disabled")]
        internal bool Disabled = true;
    }

    [DataContract]
    internal class q_Stats_filters
    {
        [DataMember(Name = "id")]
        internal string Id;

        [DataMember(Name = "value")]
        internal q_Min_And_Max Value;

        [DataMember(Name = "disabled")]
        internal bool Disabled;
    }

    [DataContract]
    internal class q_Stats
    {
        [DataMember(Name = "type")]
        internal string Type;

        [DataMember(Name = "filters")]
        internal q_Stats_filters[] Filters;
    }

    [DataContract]
    internal class q_Sort
    {
        [DataMember(Name = "price")]
        internal string Price;
    }

    [DataContract]
    internal class q_Query
    {
        [DataMember(Name = "status", Order = 0)]
        internal q_Option Status = new q_Option();

        [DataMember(Name = "name")]
        internal string Name;

        [DataMember(Name = "type")]
        internal string Type;

        [DataMember(Name = "stats")]
        internal q_Stats[] Stats;

        [DataMember(Name = "filters")]
        internal q_Filters Filters = new q_Filters();
    }

    [DataContract]
    internal class JsonData
    {
        [DataMember(Name = "query")]
        internal q_Query Query;

        [DataMember(Name = "sort")]
        internal q_Sort Sort = new q_Sort();
    }

    public class FilterEntrie
    {
        public string Key { get; set; }
        public string Stat { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }

        public FilterEntrie(string key, string type, string stat, string name)
        {
            this.Key = key;
            this.Type = type;
            this.Stat = stat;
            this.Name = name;
        }
    }

    public class ItemNames
    {
        public string Text { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }

        public ItemNames(string name, string type)
        {
            this.Name = name;
            this.Type = type;
            this.Text = string.Format("{0} {1}",
                    Regex.Replace(name, @"\([a-zA-Z\,\s']+\)$", ""),
                    Regex.Replace(type, @"\([a-zA-Z\,\s']+\)$", "")
                ).Trim();
        }
    }

    [DataContract]
    public class ItemOption
    {
        public int LangIndex;
        public string Name;
        public string Type;
        public string[] Inherits;
        public int RarityAt;
        public int Corrupt;
        public int Influence1;
        public int Influence2;
        public int AltQuality;
        public bool ByCategory;
        public bool ChkLv;
        public bool ChkSocket;
        public bool ChkQuality;
        public bool Synthesis;
        public double SocketMin;
        public double SocketMax;
        public double LinkMin;
        public double LinkMax;
        public double QualityMin;
        public double QualityMax;
        public double LvMin;
        public double LvMax;
        public double PriceMin;
        public string Flags;
        public List<Itemfilter> itemfilters = new List<Itemfilter>();
    }

    [DataContract]
    public class Itemfilter
    {
        public string stat;
        public string type;
        public string text;
        public double max;
        public double min;
        public object option;
        public string flag;
        public bool disabled;
        public bool isNull = false;
    }
}