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
        internal static string[] ExchangeApi = { "https://poe.game.daum.net/api/trade/exchange/", "https://www.pathofexile.com/api/trade/exchange/" };

        internal const string Rarity = "희귀도";
        internal const string Unique = "고유";
        internal const string Rare = "희귀";
        internal const string Magic = "마법";
        internal const string Normal = "보통";

        //internal const string Prophecy = "예언";
        internal const string Currency = "화폐";

        internal const string DivinationCard = "점술 카드";
        internal const string Gem = "젬";

        internal const string Higher = "상급";
        internal const string formed = "형성된";

        internal const string Quality = "퀄리티";
        internal const string Lv = "레벨";
        internal const string ItemLv = "아이템 레벨";
        internal const string CharmLv = "부적 등급";
        internal const string MaTier = "지도 등급";
        internal const string Socket = "홈";

        internal const string Shaper = "쉐이퍼 아이템";
        internal const string Elder = "엘더 아이템";
        internal const string Corrupt = "타락";
        internal const string Vaal = "바알";
        internal const string Unidentify = "미확인";

        internal const string ChkProphecy = "우클릭으로 이 예언을 캐릭터에 추가하십시오.";
        internal const string ChkMapFragment = "템플러의 실험실이나 전용 지도 장치에서";
        internal const string ChkFlask = "마시려면 우클릭하십시오. 허리띠에 장착 중일 때만 충전이 유지됩니다.";

        internal const string PhysicalDamage = "물리 피해";
        internal const string ElementalDamage = "원소 피해";
        internal const string ChaosDamage = "카오스 피해";
        internal const string AttacksPerSecond = "초당 공격 횟수";

        internal const string AttackSpeedIncr = "공격 속도 #% 증가";
        internal const string PhysicalDamageIncr = "물리 피해 #% 증가";

        internal static Dictionary<string, byte> lParticular = new Dictionary<string, byte>()
        {
            { AttackSpeedIncr, 1 }, { "정확도 #", 1 }, {"명중 시 #%의 확률로 중독", 1}, { "#~#의 카오스 피해 추가", 1 },
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
            { "Currency","currency" },  { "Essences","currency" }, { "Maps","map" }, { "MapFragments","map" }, { "Scarabs","map" },
            { "Fossil","currency" }, { "Delve","currency" }
        };

        internal static Dictionary<string, string> lRarity = new Dictionary<string, string>()
        {
            { Unique, "unique"}, { Rare, "rare"}, { Magic, "magic"}/*, { Normal, "normal"}*/
        };

        internal static Dictionary<string, bool> lIsResistance = new Dictionary<string, bool>()
        {
            { "냉기 저항 #%", true }, { "화염 저항 #%", true }, { "번개 저항 #%", true }, { "카오스 저항 #%", true },
            { "화염 및 냉기 저항 #%", true }, { "냉기 및 번개 저항 #%", true }, { "화염 및 번개 저항 #%", true }
        };        

        internal static Dictionary<string, string> lExchangeCurrency = new Dictionary<string, string>()
        {
            { "고대의 오브", "ancient-orb"}, { "기술자의 오브", "engineers-orb"}, { "기회의 오브", "chance"}, { "바알 오브", "vaal"}, { "변화의 오브", "alt"},
            { "선구자의 오브", "harbingers-orb"}, { "소멸의 오브", "orb-of-annulment"}, { "속박의 오브", "orb-of-binding"}, { "신성한 오브", "divine"}, { "색채의 오브", "chrom"},
            { "연결의 오브", "fuse"}, { "연금술의 오브", "alch"}, { "영원의 오브", "ete"}, { "엑잘티드 오브", "exa"}, { "정제의 오브", "scour"},
            { "쥬얼러 오브", "jew"}, { "지평의 오브", "orb-of-horizons"}, { "진화의 오브", "tra"},  { "제왕의 오브", "regal"},  { "축복의 오브", "blessed"},
            { "카오스 오브", "chaos"}, { "칼란드라의 거울", "mir"}, { "확장의 오브", "aug"}, { "후회의 오브", "regret"}, { "세공사의 프리즘", "gcp"}, 
            { "유리직공의 방울", "ba"}, { "실버 코인", "silver"}, { "감정 주문서", "wis"}, { "포탈 주문서", "port"}, { "지도제작자의 끌", "chisel"},
            { "수습 지도제작자의 육분의", "apprentice-sextant"}, { "숙련 지도제작자의 육분의", "journeyman-sextant"}, { "대가 지도제작자의 육분의", "master-sextant"},
            { "공허의 화석", "hollow-fossil"}, { "그을린 화석", "scorched-fossil"}, { "금속성 화석", "metallic-fossil"}, { "도금된 화석", "gilded-fossil"}, { "뒤덮인 화석", "encrusted-fossil"},
            { "부식된 화석", "corroded-fossil"}, { "분광 화석", "prismatic-fossil"}, { "분열된 화석", "fractured-fossil"},{ "뾰족한 화석", "jagged-fossil"}, { "빛나는 화석", "lucent-fossil"},
            { "상형 문자 화석", "glyphic-fossil"},    { "속박의 화석", "bound-fossil"}, { "얽혀든 화석", "tangled-fossil"},   { "연마한 화석", "faceted-fossil"}, { "온전한 화석", "pristine-fossil"},
            { "완벽한 화석", "perfect-fossil"},  { "인챈트된 화석", "enchanted-fossil"}, { "에테르 화석", "aetheric-fossil"}, { "전율의 화석", "shuddering-fossil"}, { "조밀한 화석", "dense-fossil"},
            { "차디찬 화석", "frigid-fossil"}, { "축성된 화석", "sanctified-fossil"}, { "톱니 화석", "serrated-fossil"}, { "특이한 화석", "aberrant-fossil"},{ "피얼룩 화석", "bloodstained-fossil"},    
            { "울네톨의 파편", "splinter-uul"}, { "에쉬의 파편", "splinter-esh"}, { "조프의 파편", "splinter-xoph"}, { "차율라의 파편", "splinter-chayula"}, { "툴의 파편", "splinter-tul"},
            { "울네톨의 축복", "blessing-uul-netol"}, { "에쉬의 축복", "blessing-esh"}, { "조프의 축복", "blessing-xoph"}, { "차율라의 축복", "blessing-chayula"}, { "툴의 축복", "blessing-tul"},
            { "무궁한 바알 파편", "timeless-vaal-splinter"}, { "무궁한 영원한 제국 파편", "timeless-eternal-empire-splinter"}, { "무궁한 카루이 파편", "timeless-karui-splinter"}, { "무궁한 템플러 파편", "timeless-templar-splinter"}, { "무궁한 마라케스 파편", "timeless-maraketh-splinter"},
            { "무궁한 바알 상징", "timeless-vaal-emblem"}, { "무궁한 영원한 제국 상징", "timeless-eternal-emblem"}, { "무궁한 카루이 상징", "timeless-karui-emblem"}, { "무궁한 템플러 상징", "timeless-templar-emblem"}, { "무궁한 마라케스 상징", "timeless-maraketh-emblem"},
            { "카드 묶음", "stacked-deck"}, { "페란두스 코인", "p"},
            { "대장장이의 숫돌", "whe"}, { "방어구 장인의 고철", "scr"}, { "소멸의 파편", "annulment-shard"}, { "거울 파편", "mirror-shard"}, { "엑잘티드 파편", "exalted-shard"},
            { "속박의 파편", "binding-shard"},{ "지평의 파편", "horizon-shard"}, { "선구자의 파편", "harbingers-shard"}, { "기술자의 파편", "engineers-shard"}, { "고대의 파편", "ancient-shard"},
            { "카오스 파편", "chaos-shard"},{ "제왕의 파편", "regal-shard"}
        };
    }
}