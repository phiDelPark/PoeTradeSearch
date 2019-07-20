using System.Collections.Generic;

namespace PoeTradeSearch
{
    internal static class ResStr
    {
        internal static string PoeClass = "POEWindowClass";
        internal static string PoeCaption = "Path of Exile";

        internal static byte ServerLang = 0;
        internal static string ServerType = "";

        internal static string[] TradeUrl = { "https://poe.game.daum.net/trade/search/", "https://www.pathofexile.com/trade/search/" };
        internal static string[] TradeApi = { "https://poe.game.daum.net/api/trade/search/", "https://www.pathofexile.com/api/trade/search/" };
        internal static string[] FetchApi = { "https://poe.game.daum.net/api/trade/fetch/", "https://www.pathofexile.com/api/trade/fetch/" };

        internal const string Rarity = "희귀도";
        internal const string Unique = "고유";
        internal const string Rare = "희귀";
        internal const string Magic = "마법";
        internal const string Normal = "보통";
        internal const string GemLv = "레벨";
        internal const string ItemLv = "아이템 레벨";
        internal const string CharmLv = "부적 등급";
        internal const string IQuality = "퀄리티";
        internal const string Unidentify = "미확인";
        internal const string Map = "지도 등급";

        //internal const string Prophecy = "예언";
        internal const string Currency = "화폐";

        internal const string DivinationCard = "점술 카드";
        internal const string Gem = "젬";
        internal const string Socket = "홈";
        internal const string Shaper = "쉐이퍼 아이템";
        internal const string Elder = "엘더 아이템";
        internal const string Corrupted = "타락";
        internal const string Vaal = "바알";
        internal const string Higher = "상급";
        internal const string formed = "형성된";
        internal const string ChkProphecy = "우클릭으로 이 예언을 캐릭터에 추가하십시오.";
        internal const string ChkFlask = "마시려면 우클릭하십시오. 허리띠에 장착 중일 때만 충전이 유지됩니다.";
        internal const string ChkMapFragment = "템플러의 실험실이나 전용 지도 장치에서";

        internal const string PhysicalDamage = "물리 피해";
        internal const string ElementalDamage = "원소 피해";
        internal const string ChaosDamage = "카오스 피해";
        internal const string AttacksPerSecond = "초당 공격 횟수";
        internal const string ChkAttackSpeedIncr = "공격 속도 #% 증가";
        internal const string ChkPhysicalDamageIncr = "물리 피해 #% 증가";

        internal static Dictionary<string, byte> lParticular = new Dictionary<string, byte>()
        {
            { "공격 속도 #% 증가", 1 }, { "정확도 #", 1 }, {"명중 시 #%의 확률로 중독", 1}, { "#~#의 카오스 피해 추가", 1 },
            { "#~#의 물리 피해 추가", 1 }, { "#~#의 번개 피해 추가", 1 },{ "#~#의 화염 피해 추가", 1 },   { "#~#의 냉기 피해 추가", 1 },
            { "에너지 보호막 최대치 #", 2 }, { "회피 #% 증가", 2 }, {"회피 #", 2}, { "방어도 #% 증가", 2}, { "방어도 #", 2 }
        };

        internal static Dictionary<string, string> lFilterType = new Dictionary<string, string>()
        {
            { "유사", "pseudo"}, { "일반", "explicit"}, { "분열", "fractured"}, { "제작", "crafted"}, { "고정", "implicit"}, { "인챈", "enchant"}
        };

        internal static Dictionary<string, string> lCategory = new Dictionary<string, string>()
        {
            { "Weapons","weapon" }, { "Quivers","armour.quiver" }, { "Armours","armour" },
            { "Amulets","accessory.amulet" }, { "Rings","accessory.ring" }, { "Belts","accessory.belt" }, /* accessory */
            { "Jewels","jewel" }, { "Flasks","flask" }, { "DivinationCards","card" }, { "Prophecies","prophecy" }, { "Gems","gem" },
            { "Currency","currency" },  { "Essences","currency" }, { "Maps","map" }, { "MapFragments","map" }, { "Scarabs","map" }
        };

        internal static Dictionary<string, string> lRarity = new Dictionary<string, string>()
        {
            { Unique, "unique"}, { Rare, "rare"}, { Magic, "magic"}/*, { Normal, "normal"}*/
        };
    }
}