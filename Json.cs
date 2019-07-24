using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace PoeTradeSearch
{
    [DataContract]
    public class WordData
    {
        public string id { get; set; }
        public string kr { get; set; }
        public string en { get; set; }
        public string detail { get; set; }
    }

    [DataContract]
    public class FilterData
    {
        public string id { get; set; }
        public string text { get; set; }
        public string type { get; set; }
        public string force { get; set; }
        public string default_position { get; set; }
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
        
        [DataMember(Name = "price")]
        internal PriceData Price = new PriceData();
    }

    [DataContract]
    internal class FetchDataInfo
    {
        [DataMember(Name = "id")]
        internal string Id = "";

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
        internal string Id = "";

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
        internal q_Type_filters_filters type_filters_filters = new q_Type_filters_filters();
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
        internal q_Socket_filters_filters socket_filters_filters = new q_Socket_filters_filters();
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

        [DataMember(Name = "elder_item")]
        internal q_Option Elder = new q_Option();

        [DataMember(Name = "shaper_item")]
        internal q_Option Shaper = new q_Option();        
    }

    [DataContract]
    internal class q_Misc_filters
    {
        [DataMember(Name = "disabled")]
        internal bool Disabled = false;

        [DataMember(Name = "filters")]
        internal q_Misc_filters_filters misc_filters_filters = new q_Misc_filters_filters();
    }

    [DataContract]
    internal class q_Trade_filters_filters
    {
        [DataMember(Name = "indexed")]
        internal q_Option Indexed = new q_Option();
    }

    [DataContract]
    internal class q_Trade_filters
    {
        [DataMember(Name = "disabled")]
        internal bool Disabled = false;

        [DataMember(Name = "filters")]
        internal q_Trade_filters_filters trade_filters_filters = new q_Trade_filters_filters();
    }

    [DataContract]
    internal class q_Filters
    {
        [DataMember(Name = "type_filters")]
        internal q_Type_filters Type_filters = new q_Type_filters();

        [DataMember(Name = "misc_filters")]
        internal q_Misc_filters Misc_filters = new q_Misc_filters();

        [DataMember(Name = "socket_filters")]
        internal q_Socket_filters Socket_filters;

        [DataMember(Name = "trade_filters")]
        internal q_Trade_filters Trade_filters;
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

    public static class Json
    {
        public static string Serialize<T>(object obj) where T : class
        {
            DataContractJsonSerializer dcsJson = new DataContractJsonSerializer(typeof(T));
            MemoryStream mS = new MemoryStream();
            dcsJson.WriteObject(mS, obj);
            var json = mS.ToArray();
            mS.Close();
            return Encoding.UTF8.GetString(json, 0, json.Length);
        }

        public static T Deserialize<T>(string strData) where T : class
        {
            DataContractJsonSerializer dcsJson = new DataContractJsonSerializer(typeof(T));
            byte[] byteArray = Encoding.UTF8.GetBytes(strData);
            MemoryStream mS = new MemoryStream(byteArray);
            T tRet = dcsJson.ReadObject(mS) as T;
            mS.Dispose();
            return (tRet);
        }
    }
}
