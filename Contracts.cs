using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows;

namespace PoeTradeSearch
{
    public partial class WinMain : Window
    {
        [DataContract]
        public class Itemfilter
        {
            public string id;
            public string type;
            public string text;
            public double max;
            public double min;
            public string flag;
            public bool disabled;
            public bool isNull = false;
        }

        [DataContract]
        public class ItemOption
        {
            public byte RarityAt;
            public byte Corrupt;
            public byte Influence1;
            public byte Influence2;
            public byte AltQuality;
            public bool ByType;
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
            public List<Itemfilter> itemfilters = new List<Itemfilter>();
        }

        [DataContract]
        public class ItemBaseName
        {
            public string[] Ids;
            public string NameKR;
            public string TypeKR;
            public string NameEN;
            public string TypeEN;
            public byte LangType;
        }

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
            internal string League = null;

            [DataMember(Name = "server")]
            internal string Server = null;

            [DataMember(Name = "server_timeout")]
            internal int ServerTimeout = 0;

            [DataMember(Name = "server_redirect")]
            internal bool ServerRedirect = false;

            [DataMember(Name = "search_price_min")]
            internal decimal SearchPriceMin = 0;

            [DataMember(Name = "search_price_count")]
            internal decimal SearchPriceCount = 20;

            [DataMember(Name = "search_before_day")]
            internal int SearchBeforeDay = 0;

            [DataMember(Name = "search_by_type")]
            internal bool SearchByType = false;

            [DataMember(Name = "auto_search_delay")]
            internal int AutoSearchDelay = 10;

            [DataMember(Name = "auto_check_unique")]
            internal bool AutoCheckUnique = false;

            [DataMember(Name = "auto_check_totalres")]
            internal bool AutoCheckTotalres = false;

            [DataMember(Name = "auto_select_pseudo")]
            internal bool AutoSelectPseudo = false;

            [DataMember(Name = "auto_select_corrupt")]
            internal string AutoSelectCorrupt = "";

            [DataMember(Name = "auto_select_bytype")]
            internal string AutoSelectByType = "";

            [DataMember(Name = "ctrl_wheel")]
            internal bool CtrlWheel = false;

            [DataMember(Name = "check_updates")]
            internal bool CheckUpdates = false;
        }

        [DataContract(Name = "shortcuts")]
        internal class ConfigShortcut
        {
            [DataMember(Name = "keycode")]
            internal int Keycode = 0;

            [DataMember(Name = "value")]
            internal string Value = null;

            [DataMember(Name = "position")]
            internal string Position = null;

            [DataMember(Name = "ctrl")]
            internal bool Ctrl = false;
        }

        [DataContract()]
        internal class ParserData
        {
            [DataMember(Name = "category")]
            internal ParserEntries Category = null;
            [DataMember(Name = "rarity")]
            internal ParserEntries Rarity = null;
            [DataMember(Name = "quality")]
            internal ParserEntries Quality = null;
            [DataMember(Name = "sockets")]
            internal ParserEntries Sockets = null;
            [DataMember(Name = "unidentified")]
            internal ParserEntries Unidentified = null;
            [DataMember(Name = "max")]
            internal ParserEntries Max = null;
            [DataMember(Name = "level")]
            internal ParserEntries Level = null;
            [DataMember(Name = "item_level")]
            internal ParserEntries ItemLevel = null;
            [DataMember(Name = "talisman_tier")]
            internal ParserEntries TalismanTier = null;
            [DataMember(Name = "map_tier")]
            internal ParserEntries MapTier = null;
            [DataMember(Name = "map_ultimatum")]
            internal ParserEntries MapUltimatum = null;
            [DataMember(Name = "superior")]
            internal ParserEntries Superior = null;
            [DataMember(Name = "vaal")]
            internal ParserEntries Vaal = null;
            [DataMember(Name = "corrupted")]
            internal ParserEntries Corrupted = null;
            [DataMember(Name = "metamorph")]
            internal ParserEntries Metamorph = null;
            [DataMember(Name = "shaper_item")]
            internal ParserEntries ShaperItem = null;
            [DataMember(Name = "elder_item")]
            internal ParserEntries ElderItem = null;
            [DataMember(Name = "crusader_item")]
            internal ParserEntries CrusaderItem = null;
            [DataMember(Name = "redeemer_item")]
            internal ParserEntries RedeemerItem = null;
            [DataMember(Name = "hunter_item")]
            internal ParserEntries HunterItem = null;
            [DataMember(Name = "warlord_item")]
            internal ParserEntries WarlordItem = null;
            [DataMember(Name = "synthesised_item")]
            internal ParserEntries SynthesisedItem = null;
            [DataMember(Name = "synthesised")]
            internal ParserEntries Synthesised = null;
            [DataMember(Name = "shaped")]
            internal ParserEntries Shaped = null;
            [DataMember(Name = "blighted")]
            internal ParserEntries Blighted = null;
            [DataMember(Name = "monster_genus")]
            internal ParserEntries MonsterGenus = null;
            [DataMember(Name = "monster_group")]
            internal ParserEntries MonsterGroup = null;
            [DataMember(Name = "physical_damage")]
            internal ParserEntries PhysicalDamage = null;
            [DataMember(Name = "elemental_damage")]
            internal ParserEntries ElementalDamage = null;
            [DataMember(Name = "chaos_damage")]
            internal ParserEntries ChaosDamage = null;
            [DataMember(Name = "attacks_per_second")]
            internal ParserEntries AttacksPerSecond = null;
            [DataMember(Name = "attack_speed_incr")]
            internal ParserEntries AttackSpeedIncr = null;
            [DataMember(Name = "physical_damage_incr")]
            internal ParserEntries PhysicalDamageIncr = null;
            [DataMember(Name = "prophecy_item")]
            internal ParserEntries ProphecyItem = null;
            [DataMember(Name = "entrails_item")]
            internal ParserEntries EntrailsItem = null;
            [DataMember(Name = "unstack_items")]
            internal ParserEntries UnstackItems = null;
            [DataMember(Name = "gems")]
            internal ParserEntries Gems = null;
            [DataMember(Name = "currency")]
            internal ParserEntries Currency = null;
            [DataMember(Name = "exchange")]
            internal ParserEntries Exchange = null;
            [DataMember(Name = "cluster")]
            internal ParserEntries Cluster = null;
            [DataMember(Name = "heist")]
            internal ParserEntries Heist = null;
        }

        [DataContract()]
        internal class CheckedData
        {
            [DataMember(Name = "checked")]
            internal ParserEntries Checked = null;
        }

        [DataContract]
        internal class ParserEntries
        {
            [DataMember(Name = "text")]
            internal string[] Text = null;

            [DataMember(Name = "entries")]
            internal ParserDictionary[] Entries = null;
        }

        [DataContract]
        internal class ParserDictionary
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

        [DataContract]
        internal class PoeData
        {
            [DataMember(Name = "result")]
            internal DataResult[] Result = null;

            [DataMember(Name = "upddate")]
            internal string Upddate = null;
        }

        [DataContract]
        internal class DataResult
        {
            [DataMember(Name = "id")]
            internal string Id = "";

            [DataMember(Name = "label")]
            internal string Label = "";

            [DataMember(Name = "entries")]
            internal DataEntrie[] Entries = null;
        }

        [DataContract]
        internal class DataEntrie
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

            [DataMember(Name = "flags")]
            internal DataFlags[] Flags = null;
        }

        [DataContract]
        internal class DataFlags
        {
            [DataMember(Name = "unique")]
            internal bool Unique = false;
        }

        [DataContract]
        internal class AccountData
        {
            [DataMember(Name = "name")]
            internal string Name = "";
        }

        [DataContract]
        internal class PriceData
        {
            [DataMember(Name = "type")]
            internal string Type = "";

            [DataMember(Name = "amount")]
            internal double Amount = 0;

            [DataMember(Name = "currency")]
            internal string Currency = "";
        }

        [DataContract]
        internal class FetchDataListing
        {
            [DataMember(Name = "indexed")]
            internal string Indexed = "";

            [DataMember(Name = "account")]
            internal AccountData Account = new AccountData();

            [DataMember(Name = "price")]
            internal PriceData Price = new PriceData();
        }

        [DataContract]
        internal class FetchDataInfo
        {
            [DataMember(Name = "id")]
            internal string ID = "";

            [DataMember(Name = "listing")]
            internal FetchDataListing Listing = new FetchDataListing();
        }

        [DataContract]
        internal class FetchData
        {
            [DataMember(Name = "result")]
            internal FetchDataInfo[] Result;
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
    }

    public class FilterEntrie
    {
        public string ID { get; set; }
        public string Name { get; set; }

        public FilterEntrie(string id, string name)
        {
            this.ID = id;
            this.Name = name;
        }
    }
}