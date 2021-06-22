﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace PoeTradeSearch
{
    internal static class RS
    {
        internal static string PoeClass = "POEWindowClass";
        internal static string PoeCaption = "Path of Exile";

        internal static string LeaguesApi = "https://www.pathofexile.com/api/trade/data/leagues";
        internal static string[] TradeUrl = { "https://poe.game.daum.net/trade/search/", "https://www.pathofexile.com/trade/search/" };
        internal static string[] TradeApi = { "https://poe.game.daum.net/api/trade/search/", "https://www.pathofexile.com/api/trade/search/" };
        internal static string[] FetchApi = { "https://poe.game.daum.net/api/trade/fetch/", "https://www.pathofexile.com/api/trade/fetch/" };
        internal static string[] ExchangeUrl = { "https://poe.game.daum.net/trade/exchange/", "https://www.pathofexile.com/trade/exchange/" };
        internal static string[] ExchangeApi = { "https://poe.game.daum.net/api/trade/exchange/", "https://www.pathofexile.com/api/trade/exchange/" };

        internal static string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.97 Safari/537.36";

        internal static Dictionary<string, string> lFilterType = new Dictionary<string, string>()
        {
            { "pseudo", "유사"}, { "explicit", "일반"}, { "implicit", "고정"}, { "fractured", "분열"}, { "enchant", "인챈"},
            { "crafted", "제작"}, { "veiled", "장막"}, { "monster", "야수"}, { "delve", "탐광"}, { "ultimatum", "결전" }
        };

        internal static Dictionary<string, bool> lDisable = new Dictionary<string, bool>()
        {
            { "stat_1001829678", true}, { "stat_1778298516", true}, { "stat_2881111359", true},  { "stat_561307714", true}, { "stat_57434274", true},
            { "stat_3666934677", true}, { "stat_723388324", true}
        };

        internal static Dictionary<string, bool> lResistance = new Dictionary<string, bool>()
        {
            { "stat_4220027924", true }, { "stat_3372524247", true }, { "stat_1671376347", true }, { "stat_2923486259", true },
            { "stat_2915988346", true }, { "stat_4277795662", true }, { "stat_3441501978", true }
        };

        internal static Dictionary<string, string> lPseudo = new Dictionary<string, string>()
        {
            { "stat_4220027924", "pseudo_total_cold_resistance" }, { "stat_3372524247", "pseudo_total_fire_resistance" }, { "stat_1671376347", "pseudo_total_lightning_resistance" }, { "stat_2923486259", "pseudo_total_chaos_resistance" },
            { "stat_3299347043", "pseudo_total_life" }, { "stat_1050105434", "pseudo_total_mana" }, { "stat_3489782002", "pseudo_total_energy_shield" }, { "stat_2482852589", "pseudo_increased_energy_shield" },
            { "stat_4080418644", "pseudo_total_strength" }, { "stat_3261801346", "pseudo_total_dexterity" }, { "stat_328541901", "pseudo_total_intelligence" },
            { "stat_681332047", "pseudo_total_attack_speed" }, { "stat_2891184298", "pseudo_total_cast_speed" }, { "stat_2250533757", "pseudo_increased_movement_speed" },
            { "stat_587431675", "pseudo_global_critical_strike_chance" }, { "stat_3556824919", "pseudo_global_critical_strike_multiplier" }, { "stat_737908626", "pseudo_critical_strike_chance_for_spells" },
            { "stat_1509134228", "pseudo_increased_physical_damage" }, { "stat_2974417149", "pseudo_increased_spell_damage" }, { "stat_3141070085", "pseudo_increased_elemental_damage" },
            { "stat_2231156303", "pseudo_increased_lightning_damage" }, { "stat_3291658075", "pseudo_increased_cold_damage" }, { "stat_3962278098", "pseudo_increased_fire_damage" },
            { "stat_4208907162", "pseudo_increased_lightning_damage_with_attack_skills" }, { "stat_860668586", "pseudo_increased_cold_damage_with_attack_skills" }, { "stat_2468413380", "pseudo_increased_fire_damage_with_attack_skills" }, { "stat_387439868", "pseudo_increased_elemental_damage_with_attack_skills" },
            { "stat_960081730", "pseudo_adds_physical_damage" }, { "stat_1334060246", "pseudo_adds_lightning_damage" }, { "stat_2387423236", "pseudo_adds_cold_damage" }, { "stat_321077055", "pseudo_adds_fire_damage" }, { "stat_3531280422", "pseudo_adds_chaos_damage" },
            { "stat_3032590688", "pseudo_adds_physical_damage_to_attacks" }, { "stat_1754445556", "pseudo_adds_lightning_damage_to_attacks" }, { "stat_4067062424", "pseudo_adds_cold_damage_to_attacks" }, { "stat_1573130764", "pseudo_adds_fire_damage_to_attacks" }, { "stat_674553446", "pseudo_adds_chaos_damage_to_attacks" },
            { "stat_2435536961", "pseudo_adds_physical_damage_to_spells" }, { "stat_2831165374", "pseudo_adds_lightning_damage_to_spells" }, { "stat_2469416729", "pseudo_adds_cold_damage_to_spells" }, { "stat_1133016593", "pseudo_adds_fire_damage_to_spells" }, { "stat_2300399854", "pseudo_adds_chaos_damage_to_spells" },
            { "stat_3325883026", "pseudo_total_life_regen" }, { "stat_836936635", "pseudo_percent_life_regen" }, { "stat_789117908", "pseudo_increased_mana_regen" }
        };

        internal static ParserEntries lRadius = new ParserEntries()
        {
            Entries = new ParserDictionary[4]
            {
                new ParserDictionary { Text = new string[2] { "좁은 반경", "Small Ring" } },
                new ParserDictionary { Text = new string[2] { "중간 반경", "Medium Ring" } },
                new ParserDictionary { Text = new string[2] { "넓은 반경", "Large Ring" } },
                new ParserDictionary { Text = new string[2] { "아주 넓은 반경", "Very Large Ring" } }
            }
        };
    }

    public partial class WinMain : Window
    {
        internal ConfigData mConfigData;
        private ParserData mParserData;
        private CheckedData mCheckedData;

        internal PoeData[] mFilterData = new PoeData[2];
        private PoeData[] mItemsData = new PoeData[2];
        private PoeData[] mStaticData = new PoeData[2];

        internal bool Setting()
        {
            string path = (string)Application.Current.Properties["DataPath"];
            FileStream fs = null;

            try
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                if (!File.Exists(path + "Config.txt"))
                {
                    if (!BasicDataUpdate(path, "Config.txt"))
                        throw new UnauthorizedAccessException("Config 파일 생성 실패");
                }
                fs = new FileStream(path + "Config.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mConfigData = Json.Deserialize<ConfigData>(json);
                }

                if (mConfigData.Options.SearchListCount > 80)
                    mConfigData.Options.SearchListCount = 80;

                if (!File.Exists(path + "Parser.txt"))
                {
                    if (!BasicDataUpdate(path, "Parser.txt"))
                        throw new UnauthorizedAccessException("Parser 파일 생성 실패");
                }
                fs = new FileStream(path + "Parser.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mParserData = Json.Deserialize<ParserData>(json);
                }

                if (!File.Exists(path + "Checked.txt"))
                {
                    if (!BasicDataUpdate(path, "Checked.txt"))
                        throw new UnauthorizedAccessException("checked 파일 생성 실패");
                }
                fs = new FileStream(path + "Checked.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mCheckedData = Json.Deserialize<CheckedData>(json);
                }

                if (mCheckedData.Entries == null)
                    mCheckedData.Entries = new List<ParserDictionary>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, ex.Message, "에러");
                return false;
            }
            finally
            {
                if (fs != null)
                    fs.Dispose();
            }

            return true;
        }

        private bool LoadData(out string outString)
        {
            string path = (string)Application.Current.Properties["DataPath"];
            FileStream fs = null;
            string s = "";
            try
            {
                if (!File.Exists(path + "FiltersKO.txt") || !File.Exists(path + "FiltersEN.txt"))
                {
                    mTrayIcon.BalloonTipText = "데이터를 업데이트 하는중...";
                    mTrayIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
                    mTrayIcon.ShowBalloonTip(5000);

                    string[] items = { "FiltersKO", "FiltersEN", "ItemsKO", "ItemsEN", "StaticKO", "StaticEN" };
                    foreach (string item in items) File.Delete(path + item + ".txt");

                    if (!FilterDataUpdate(path) || !ItemDataUpdate(path) || !StaticDataUpdate(path))
                    {
                        s = "생성 실패";
                        throw new UnauthorizedAccessException("Database 파일 생성 실패");
                    }
                }

                s = "FiltersKO.txt";
                fs = new FileStream(path + s, FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mFilterData[0] = Json.Deserialize<PoeData>(json);
                    reader.Close();
                }

                s = "FiltersEN.txt";
                fs = new FileStream(path + s, FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mFilterData[1] = Json.Deserialize<PoeData>(json);
                    reader.Close();
                }

                s = "ItemsKO.txt";
                fs = new FileStream(path + s, FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mItemsData[0] = Json.Deserialize<PoeData>(json);
                    reader.Close();
                }

                s = "ItemsEN.txt";
                fs = new FileStream(path + s, FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mItemsData[1] = Json.Deserialize<PoeData>(json);
                    reader.Close();
                }

                s = "StaticKO.txt";
                fs = new FileStream(path + s, FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mStaticData[0] = Json.Deserialize<PoeData>(json);
                    reader.Close();
                }

                s = "StaticEN.txt";
                fs = new FileStream(path + s, FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mStaticData[1] = Json.Deserialize<PoeData>(json);
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                outString = s;
                MessageBox.Show(Application.Current.MainWindow, ex.Message, "에러");
                return false;
            }
            finally
            {
                if (fs != null) fs.Dispose();
            }

            outString = s;
            return true;
        }
    }
}