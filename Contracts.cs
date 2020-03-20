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
            public string text;
            public double max;
            public double min;
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
            public string NameKR;
            public string TypeKR;
            public string NameEN;
            public string TypeEN;

            public byte LangType;
            //public string Rarity;
            public string[] Inherits;
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
            internal bool SearchByType0 = false;

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
            [DataMember(Name = "rarity")]
            internal string[] Rarity = null;
            [DataMember(Name = "quality")]
            internal string[] Quality = null;
            [DataMember(Name = "sockets")]
            internal string[] Sockets = null;
            [DataMember(Name = "unidentified")]
            internal string[] Unidentified = null;
            [DataMember(Name = "max")]
            internal string[] Max = null;
            [DataMember(Name = "level")]
            internal string[] Level = null;
            [DataMember(Name = "item_level")]
            internal string[] ItemLevel = null;
            [DataMember(Name = "talisman_tier")]
            internal string[] TalismanTier = null;
            [DataMember(Name = "map_tier")]
            internal string[] MapTier = null;
            [DataMember(Name = "superior")]
            internal string[] Superior = null;
            [DataMember(Name = "vaal")]
            internal string[] Vaal = null;
            [DataMember(Name = "corrupted")]
            internal string[] Corrupted = null;
            [DataMember(Name = "metamorph")]
            internal string[] Metamorph = null;
            [DataMember(Name = "shaper_item")]
            internal string[] ShaperItem = null;
            [DataMember(Name = "elder_item")]
            internal string[] ElderItem = null;
            [DataMember(Name = "crusader_item")]
            internal string[] CrusaderItem = null;
            [DataMember(Name = "redeemer_item")]
            internal string[] RedeemerItem = null;
            [DataMember(Name = "hunter_item")]
            internal string[] HunterItem = null;
            [DataMember(Name = "warlord_item")]
            internal string[] WarlordItem = null;
            [DataMember(Name = "synthesised_item")]
            internal string[] SynthesisedItem = null;
            [DataMember(Name = "synthesised")]
            internal string[] Synthesised = null;
            [DataMember(Name = "shaped")]
            internal string[] Shaped = null;
            [DataMember(Name = "blighted")]
            internal string[] Blighted = null;
            [DataMember(Name = "monster_genus")]
            internal string[] MonsterGenus = null;
            [DataMember(Name = "monster_group")]
            internal string[] MonsterGroup = null;
            [DataMember(Name = "physical_damage")]
            internal string[] PhysicalDamage = null;
            [DataMember(Name = "elemental_damage")]
            internal string[] ElementalDamage = null;
            [DataMember(Name = "chaos_damage")]
            internal string[] ChaosDamage = null;
            [DataMember(Name = "attacks_per_second")]
            internal string[] AttacksPerSecond = null;
            [DataMember(Name = "attack_speed_incr")]
            internal string[] AttackSpeedIncr = null;
            [DataMember(Name = "physical_damage_incr")]
            internal string[] PhysicalDamageIncr = null;
            [DataMember(Name = "prophecy_item")]
            internal string[] ProphecyItem = null;
            [DataMember(Name = "entrails_item")]
            internal string[] EntrailsItem = null;

            [DataMember(Name = "currency")]
            internal ParserDictionary[] Currency = null;
            [DataMember(Name = "exchange")]
            internal ParserDictionary[] Exchange = null;

            [DataMember(Name = "checked")]
            internal ParserDictionary[] Checked = null;

            [DataMember(Name = "version")]
            internal string[] Version = null;
        }

        [DataContract]
        internal class ParserDictionary
        {
            [DataMember(Name = "id")]
            internal string ID = null;

            [DataMember(Name = "text")]
            internal string[] Text = null;

            [DataMember(Name = "hidden")]
            internal bool Hidden = false;
        }

        [DataContract]
        internal class BaseData
        {
            [DataMember(Name = "result")]
            internal BaseResult[] Result = null;
        }

        [DataContract]
        internal class BaseResult
        {
            [DataMember(Name = "data")]
            internal BaseResultData[] Data = null;
        }

        [DataContract]
        internal class BaseResultData
        {
            [DataMember(Name = "Id")]
            internal string ID = null;

            [DataMember(Name = "Name")]
            internal string NameEn = null;

            [DataMember(Name = "NameKo")]
            internal string NameKo = null;

            [DataMember(Name = "InheritsFrom")]
            internal string InheritsFrom = null;

            [DataMember(Name = "Detail")]
            internal string Detail = null;
        }

        [DataContract]
        internal class WordData
        {
            [DataMember(Name = "result")]
            public WordeResult[] Result = null;
        }

        [DataContract]
        internal class WordeResult
        {
            [DataMember(Name = "data")]
            public WordeResultData[] Data = null;
        }

        [DataContract]
        internal class WordeResultData
        {
            [DataMember(Name = "WordlistsKey")]
            internal string Key = null;

            [DataMember(Name = "Text2")]
            internal string NameEn = null;

            [DataMember(Name = "Text2Ko")]
            internal string NameKo = null;
        }

        [DataContract]
        internal class FilterData
        {
            [DataMember(Name = "result")]
            internal FilterResult[] Result = null;
        }

        [DataContract]
        internal class FilterResult
        {
            [DataMember(Name = "label")]
            internal string Label = "";

            [DataMember(Name = "entries")]
            internal FilterResultEntrie[] Entries = null;
        }

        [DataContract]
        internal class FilterResultEntrie
        {
            [DataMember(Name = "id")]
            internal string ID = "";

            [DataMember(Name = "text")]
            internal string Text = "";

            [DataMember(Name = "type")]
            internal string Type = "";

            [DataMember(Name = "part")]
            internal string Part = "";
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