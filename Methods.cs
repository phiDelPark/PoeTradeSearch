using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PoeTradeSearch
{
    public partial class WinMain : Window
    {
        private void ResetControls()
        {
            tbLinksMin.Text = "";
            tbSocketMin.Text = "";
            tbLinksMax.Text = "";
            tbSocketMax.Text = "";
            tbLvMin.Text = "";
            tbLvMax.Text = "";
            tbQualityMin.Text = "";
            tbQualityMax.Text = "";
            tkDetail.Text = "";

            lbDPS.Content = "옵션";
            Synthesis.Content = "결합";

            cbRarity.Items.Clear();
            cbRarity.Items.Add("모두");
            cbRarity.Items.Add(mParserData.Rarity.Entries[0].Text[0]);
            cbRarity.Items.Add(mParserData.Rarity.Entries[1].Text[0]);
            cbRarity.Items.Add(mParserData.Rarity.Entries[2].Text[0]);
            cbRarity.Items.Add(mParserData.Rarity.Entries[3].Text[0]);

            cbAiiCheck.IsChecked = false;
            ckLv.IsChecked = false;
            ckQuality.IsChecked = false;
            ckSocket.IsChecked = false;
            Synthesis.IsChecked = false;

            cbAltQuality.Items.Clear();
            cbInfluence1.SelectedIndex = 0;
            cbInfluence2.SelectedIndex = 0;
            cbInfluence1.BorderThickness = new Thickness(1);
            cbInfluence2.BorderThickness = new Thickness(1);

            cbCorrupt.SelectedIndex = 0;
            cbCorrupt.BorderThickness = new Thickness(1);
            cbCorrupt.FontWeight = FontWeights.Normal;
            cbCorrupt.Foreground = cbInfluence1.Foreground;

            cbOrbs.SelectionChanged -= CbOrbs_SelectionChanged;
            cbSplinters.SelectionChanged -= CbOrbs_SelectionChanged;
            cbOrbs.SelectedIndex = 0;
            cbSplinters.SelectedIndex = 0;
            cbOrbs.SelectionChanged += CbOrbs_SelectionChanged;
            cbSplinters.SelectionChanged += CbOrbs_SelectionChanged;

            cbOrbs.FontWeight = FontWeights.Normal;
            cbSplinters.FontWeight = FontWeights.Normal;

            ckLv.Content = mParserData.Level.Text[0];
            ckLv.FontWeight = FontWeights.Normal;
            ckLv.Foreground = Synthesis.Foreground;
            ckLv.BorderBrush = Synthesis.BorderBrush;
            ckQuality.FontWeight = FontWeights.Normal;
            ckQuality.Foreground = Synthesis.Foreground;
            ckQuality.BorderBrush = Synthesis.BorderBrush;
            lbSocketBackground.Visibility = Visibility.Hidden;

            tabControl1.SelectedIndex = 0;
            cbPriceListCount.SelectedIndex = (int)Math.Ceiling(mConfigData.Options.SearchPriceCount / 20) - 1;
            tbPriceFilterMin.Text = mConfigData.Options.SearchPriceMin > 0 ? mConfigData.Options.SearchPriceMin.ToString() : "";

            tkPriceCount.Text = "";
            tkPriceInfo.Text = (string)tkPriceInfo.Tag;
            cbPriceListTotal.Text = "0/0 검색";

            for (int i = 0; i < 10; i++)
            {
                ((ComboBox)FindName("cbOpt" + i)).Items.Clear();
                // ((ComboBox)FindName("cbOpt" + i)).ItemsSource = new List<FilterEntrie>();
                ((ComboBox)FindName("cbOpt" + i)).DisplayMemberPath = "Name";
                ((ComboBox)FindName("cbOpt" + i)).SelectedValuePath = "Name";

                ((TextBox)FindName("tbOpt" + i)).Text = "";
                ((TextBox)FindName("tbOpt" + i)).Tag = null;
                ((TextBox)FindName("tbOpt" + i)).Background = SystemColors.WindowBrush;
                ((TextBox)FindName("tbOpt" + i + "_0")).Text = "";
                ((TextBox)FindName("tbOpt" + i + "_1")).Text = "";
                ((TextBox)FindName("tbOpt" + i + "_0")).IsEnabled = true;
                ((TextBox)FindName("tbOpt" + i + "_1")).IsEnabled = true;
                ((TextBox)FindName("tbOpt" + i + "_0")).Background = SystemColors.WindowBrush;
                ((TextBox)FindName("tbOpt" + i + "_0")).Foreground = ((TextBox)FindName("tbOpt" + i)).Foreground;
                ((CheckBox)FindName("tbOpt" + i + "_2")).IsEnabled = true;
                ((CheckBox)FindName("tbOpt" + i + "_2")).IsChecked = false;
                ((CheckBox)FindName("tbOpt" + i + "_3")).IsChecked = false;
                ((CheckBox)FindName("tbOpt" + i + "_3")).Visibility = Visibility.Hidden;
                SetFilterObjectColor(i, SystemColors.ActiveBorderBrush);
            }
        }

        private void SetFilterObjectColor(int index, System.Windows.Media.SolidColorBrush colorBrush)
        {
            ((Control)FindName("tbOpt" + index)).BorderBrush = colorBrush;
            ((Control)FindName("tbOpt" + index + "_0")).BorderBrush = colorBrush;
            ((Control)FindName("tbOpt" + index + "_1")).BorderBrush = colorBrush;
            ((Control)FindName("tbOpt" + index + "_2")).BorderBrush = colorBrush;
            ((Control)FindName("tbOpt" + index + "_3")).BorderBrush = colorBrush;
        }

        private void SetSearchButtonText(bool is_kor)
        {
            bool isExchange = bdExchange.Visibility == Visibility.Visible && (cbOrbs.SelectedIndex > 0 || cbSplinters.SelectedIndex > 0);
            btnSearch.Content = "거래소에서 " + (isExchange ? "대량 " : "") + "찾기 (" + (is_kor ? "한국" : "영국") + ")";
        }

        private void setDPS(string physical, string elemental, string chaos, string quality, string perSecond, double phyDmgIncr, double speedIncr)
        {
            // DPS 계산 POE-TradeMacro 참고
            double physicalDPS = DamageToDPS(physical);
            double elementalDPS = DamageToDPS(elemental);
            double chaosDPS = DamageToDPS(chaos);

            double quality20Dps = quality == "" ? 0 : quality.ToDouble(0);
            double attacksPerSecond = Regex.Replace(perSecond, "[^0-9.]", "").ToDouble(0);

            if (speedIncr > 0)
            {
                double baseAttackSpeed = attacksPerSecond / (speedIncr / 100 + 1);
                double modVal = baseAttackSpeed % 0.05;
                baseAttackSpeed += modVal > 0.025 ? (0.05 - modVal) : -modVal;
                attacksPerSecond = baseAttackSpeed * (speedIncr / 100 + 1);
            }

            physicalDPS = (physicalDPS / 2) * attacksPerSecond;
            elementalDPS = (elementalDPS / 2) * attacksPerSecond;
            chaosDPS = (chaosDPS / 2) * attacksPerSecond;

            //20 퀄리티 보다 낮을땐 20 퀄리티 기준으로 계산
            quality20Dps = quality20Dps < 20 ? physicalDPS * (phyDmgIncr + 120) / (phyDmgIncr + quality20Dps + 100) : 0;
            physicalDPS = quality20Dps > 0 ? quality20Dps : physicalDPS;

            lbDPS.Content = "DPS: P." + Math.Round(physicalDPS, 2).ToString() +
                            " + E." + Math.Round(elementalDPS, 2).ToString() +
                            " = T." + Math.Round(physicalDPS + elementalDPS + chaosDPS, 2).ToString();
        }

        private void Deduplicationfilter(List<Itemfilter> itemfilters)
        {
            for (int i = 0; i < itemfilters.Count; i++)
            {
                string txt = ((TextBox)FindName("tbOpt" + i)).Text;
                if (((CheckBox)FindName("tbOpt" + i + "_2")).IsEnabled == false) continue;

                for (int j = 0; j < itemfilters.Count; j++)
                {
                    if (i == j) continue;

                    CheckBox tmpCcheckBox2 = (CheckBox)FindName("tbOpt" + j + "_2");
                    if (((TextBox)FindName("tbOpt" + j)).Text == txt)
                    {
                        tmpCcheckBox2.IsChecked = false;
                        tmpCcheckBox2.IsEnabled = false;
                        itemfilters[j].disabled = true;
                    }
                }
            }
        }

        private void ItemTextParser(string itemText, bool isWinShow = true)
        {
            int[] SocketParser(string socket)
            {
                int sckcnt = socket.Replace(" ", "-").Split('-').Length;
                string[] scklinks = socket.Split(' ');

                int lnkcnt = 0;
                for (int s = 0; s < scklinks.Length; s++)
                {
                    if (lnkcnt < scklinks[s].Length) lnkcnt = scklinks[s].Length;
                }

                return new int[] { sckcnt, lnkcnt < 3 ? 0 : lnkcnt - (int)Math.Ceiling((double)lnkcnt / 2) + 1 };
            }

            string item_category = "";
            string item_name = "";
            string item_type = "";
            string item_rarity = "";
            string map_influenced = "";
            ParserData PS = mParserData;

            try
            {
                string[] asData = (itemText ?? "").Trim().Split(new string[] { "--------" }, StringSplitOptions.None);

                if (asData.Length > 1 && (asData[0].IndexOf(PS.Category.Text[0] + ": ") == 0 || asData[0].IndexOf(PS.Category.Text[1] + ": ") == 0))
                {
                    byte z = (byte)(asData[0].IndexOf(PS.Category.Text[0] + ": ") == 0 ? 0 : 1);
                    if (mConfigData.Options.Server != "en" && mConfigData.Options.Server != "ko") RS.ServerLang = z;

                    ResetControls();
                    mItemBaseName = new ItemBaseName();
                    mItemBaseName.LangType = z;

                    string[] asOpt = asData[0].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

                    item_category = asOpt[0].Split(':')[1].Trim();
                    item_rarity = asOpt[1].Split(':')[1].Trim();

                    item_name = Regex.Replace(asOpt[2] ?? "", @"<<set:[A-Z]+>>", "");
                    if (asOpt.Length > 3 && asOpt[3] != "")
                    {
                        item_type = Regex.Replace(asOpt[3] ?? "", @"<<set:[A-Z]+>>", "");
                    }
                    else
                    {
                        item_type = item_name;
                        item_name = "";
                    }

                    ParserDictionary category = Array.Find(PS.Category.Entries, x => x.Text[z] == item_category);
                    string[] cate_ids = category != null ? category.Id.Split('.') : new string[] { "" };

                    ParserDictionary rarity = Array.Find(PS.Rarity.Entries, x => x.Text[z] == item_rarity);
                    string rarity_id = rarity != null ? rarity.Id : "";
                    item_rarity = rarity != null ? rarity.Text[0] : item_rarity;

                    int k = 0, baki = 0, notImpCnt = 0;
                    double attackSpeedIncr = 0, PhysicalDamageIncr = 0;

                    List<Itemfilter> itemfilters = new List<Itemfilter>();

                    Dictionary<string, string> lItemOption = new Dictionary<string, string>()
                    {
                        { PS.Quality.Text[z], "" }, { PS.Level.Text[z], "" }, { PS.ItemLevel.Text[z], "" }, { PS.TalismanTier.Text[z], "" }, { PS.MapTier.Text[z], "" },
                        { PS.Sockets.Text[z], "" }, { PS.Heist.Text[z], "" }, { PS.MapUltimatum.Text[z], "" }, { PS.RewardUltimatum.Text[z], "" },
                        { PS.MonsterGenus.Text[z], "" }, { PS.MonsterGroup.Text[z], "" },
                        { PS.PhysicalDamage.Text[z], "" }, { PS.ElementalDamage.Text[z], "" }, { PS.ChaosDamage.Text[z], "" }, { PS.AttacksPerSecond.Text[z], "" },
                        { PS.ShaperItem.Text[z], "" }, { PS.ElderItem.Text[z], "" }, { PS.CrusaderItem.Text[z], "" }, { PS.RedeemerItem.Text[z], "" },
                        { PS.HunterItem.Text[z], "" }, { PS.WarlordItem.Text[z], "" }, { PS.SynthesisedItem.Text[z], "" },
                        { PS.Corrupted.Text[z], "" }, { PS.Unidentified.Text[z], "" }, { PS.ProphecyItem.Text[z], "" }, { PS.Vaal.Text[z] + " " + item_type, "" }
                    };

                    for (int i = 1; i < asData.Length; i++)
                    {
                        asOpt = asData[i].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

                        for (int j = 0; j < asOpt.Length; j++)
                        {
                            if (asOpt[j].Trim().IsEmpty()) continue;

                            string[] asLocal = Regex.Replace(asOpt[j], @" \([\w\s]+\)\: ", ": ").Split(':');

                            if (lItemOption.ContainsKey(asLocal[0]))
                            {
                                if (lItemOption[asLocal[0]] == "") lItemOption[asLocal[0]] = asLocal.Length > 1 ? asLocal[1].Trim() : "_TRUE_";
                            }
                            else if (k < 10 && (!lItemOption[PS.ItemLevel.Text[z]].IsEmpty() || !lItemOption[PS.MapUltimatum.Text[z]].IsEmpty()))
                            {
                                string cluster_jewel = "";
                                double min = 99999, max = 99999;
                                bool resistance = false;
                                bool crafted = asOpt[j].IndexOf("(crafted)") > -1;
                                bool implicit_ = asOpt[j].IndexOf("(implicit)") > -1;

                                if (asLocal.Length == 2)
                                {
                                    asLocal[1] = Regex.Replace(asLocal[1], @" \([a-zA-Z]+\)", "").Trim();
                                    ParserDictionary Cluster = Array.Find(PS.Cluster.Entries, x => x.Text[z] == asLocal[1]);
                                    if (Cluster != null)
                                    {
                                        cluster_jewel = Cluster.Text[z];
                                        asOpt[j] = asLocal[0] + ": " + Cluster.Id;
                                    }
                                }

                                string input = Regex.Replace(asOpt[j], @" \([a-zA-Z]+\)", "");

                                if (implicit_ && cate_ids.Length == 1 && cate_ids[0] == "map")
                                {
                                    string pats = "";
                                    foreach (ParserDictionary item in PS.MapTier.Entries)
                                    {
                                        pats += item.Text[z] + "|";
                                    }
                                    Match match = Regex.Match(input.Trim(), "(.+) (" + pats + "_none_)(.*)");
                                    if (match.Success)
                                    {
                                        map_influenced = match.Groups[2] + "";
                                        input = match.Groups[1] + " #" + match.Groups[3];
                                    }
                                    continue;
                                }

                                input = Regex.Escape(Regex.Replace(input, @"[+-]?[0-9]+\.[0-9]+|[+-]?[0-9]+", "#"));
                                input = Regex.Replace(input, @"\\#", "[+-]?([0-9]+\\.[0-9]+|[0-9]+|\\#)");

                                bool local_exists = false;
                                DataEntrie filter = null;
                                Regex rgx = new Regex("^" + input + "$", RegexOptions.IgnoreCase);

                                foreach (DataResult data_result in mFilterData[z].Result)
                                {
                                    DataEntrie[] entries = Array.FindAll(data_result.Entries, x => rgx.IsMatch(x.Text));

                                    // 2개 이상 같은 옵션이 있을때 장비 옵션 (특정) 만 추출
                                    if (entries.Length > 1)
                                    {
                                        DataEntrie[] entries_tmp = Array.FindAll(entries, x => x.Part == cate_ids[0]);
                                        if (entries_tmp.Length > 0)
                                        {
                                            local_exists = true;
                                            entries = entries_tmp;
                                        }
                                        else
                                        {
                                            entries = Array.FindAll(entries, x => x.Part == null);
                                        }
                                    }

                                    if (entries.Length > 0)
                                    {
                                        Array.Sort(entries, delegate (DataEntrie entrie1, DataEntrie entrie2)
                                        {
                                            return (entrie2.Part ?? "").CompareTo(entrie1.Part ?? "");
                                        });

                                        MatchCollection matches1 = Regex.Matches(asOpt[j], @"[-]?[0-9]+\.[0-9]+|[-]?[0-9]+");
                                        foreach (DataEntrie entrie in entries)
                                        {
                                            int idxMin = 0, idxMax = 0;
                                            bool isMin = false, isMax = false;
                                            bool isBreak = true;

                                            MatchCollection matches2 = Regex.Matches(entrie.Text, @"[-]?[0-9]+\.[0-9]+|[-]?[0-9]+|#");

                                            for (int t = 0; t < matches2.Count; t++)
                                            {
                                                if (matches2[t].Value == "#")
                                                {
                                                    if (!isMin)
                                                    {
                                                        isMin = true;
                                                        idxMin = t;
                                                    }
                                                    else if (!isMax)
                                                    {
                                                        isMax = true;
                                                        idxMax = t;
                                                    }
                                                }
                                                else if (matches1[t].Value != matches2[t].Value)
                                                {
                                                    isBreak = false;
                                                    break;
                                                }
                                            }

                                            if (isBreak)
                                            {
                                                ((ComboBox)FindName("cbOpt" + k)).Items.Add(new FilterEntrie(entrie.Id, data_result.Label));

                                                if (filter == null)
                                                {
                                                    string[] id_split = entrie.Id.Split('.');
                                                    resistance = id_split.Length == 2 && RS.lResistance.ContainsKey(id_split[1]);
                                                    filter = entrie;

                                                    MatchCollection matches = Regex.Matches(asOpt[j], @"[-]?[0-9]+\.[0-9]+|[-]?[0-9]+");
                                                    min = isMin && matches.Count > idxMin ? ((Match)matches[idxMin]).Value.ToDouble(99999) : 99999;
                                                    max = isMax && idxMin < idxMax && matches.Count > idxMax ? ((Match)matches[idxMax]).Value.ToDouble(99999) : 99999;
                                                }

                                                break;
                                            }
                                        }
                                    }
                                }

                                if (filter != null)
                                {
                                    ((ComboBox)FindName("cbOpt" + k)).SelectedValue = RS.lFilterType["crafted"];
                                    int selidx = ((ComboBox)FindName("cbOpt" + k)).SelectedIndex;

                                    if (crafted && selidx > -1)
                                    {
                                        SetFilterObjectColor(k, System.Windows.Media.Brushes.Blue);
                                        ((ComboBox)FindName("cbOpt" + k)).SelectedIndex = selidx;
                                    }
                                    else
                                    {
                                        ((ComboBox)FindName("cbOpt" + k)).SelectedValue = RS.lFilterType["pseudo"];
                                        selidx = ((ComboBox)FindName("cbOpt" + k)).SelectedIndex;

                                        if (selidx == -1 && ((ComboBox)FindName("cbOpt" + k)).Items.Count > 0)
                                        {
                                            FilterEntrie filterEntrie = (FilterEntrie)((ComboBox)FindName("cbOpt" + k)).Items[0];
                                            string[] id_split = filterEntrie.ID.Split('.');
                                            if (id_split.Length == 2 && RS.lPseudo.ContainsKey(id_split[1]))
                                            {
                                                ((ComboBox)FindName("cbOpt" + k)).Items.Add(new FilterEntrie("pseudo." + RS.lPseudo[id_split[1]], RS.lFilterType["pseudo"]));
                                            }
                                        }

                                        selidx = ((ComboBox)FindName("cbOpt" + k)).Items.Count == 1 ? 0 : -1;

                                        // 인첸트, 제작은 다른 곳에서 다시 체크함
                                        string[] tmps = { !local_exists && mConfigData.Options.AutoSelectPseudo ? "pseudo" : "explicit", "explicit", "fractured" };
                                        foreach (string tmp in tmps)
                                        {
                                            ((ComboBox)FindName("cbOpt" + k)).SelectedValue = RS.lFilterType[tmp];
                                            if (((ComboBox)FindName("cbOpt" + k)).SelectedIndex > -1)
                                            {
                                                selidx = ((ComboBox)FindName("cbOpt" + k)).SelectedIndex;
                                                break;
                                            }
                                        }

                                        ((ComboBox)FindName("cbOpt" + k)).SelectedIndex = selidx;
                                    }

                                    if (i != baki)
                                    {
                                        baki = i;
                                        notImpCnt = 0;
                                    }

                                    ((TextBox)FindName("tbOpt" + k)).Text = filter.Text;
                                    ((CheckBox)FindName("tbOpt" + k + "_3")).Visibility = resistance ? Visibility.Visible : Visibility.Hidden;
                                    if (((CheckBox)FindName("tbOpt" + k + "_3")).Visibility == Visibility.Visible && mConfigData.Options.AutoCheckTotalres)
                                        ((CheckBox)FindName("tbOpt" + k + "_3")).IsChecked = true;

                                    if (min != 99999 && max != 99999)
                                    {
                                        if (filter.Text.IndexOf("#~#") > -1)
                                        {
                                            min += max;
                                            min = Math.Truncate(min / 2 * 10) / 10;
                                            max = 99999;
                                        }
                                    }
                                    else if (min != 99999 || max != 99999)
                                    {
                                        string[] split = filter.Id.Split('.');
                                        bool defMaxPosition = split.Length == 2 && RS.lDefaultPosition.ContainsKey(split[1]);
                                        if (((defMaxPosition && min > 0) || (!defMaxPosition && min < 0)) && max == 99999)
                                        {
                                            max = min;
                                            min = 99999;
                                        }
                                    }

                                    itemfilters.Add(new Itemfilter
                                    {
                                        id = filter.Id,
                                        type = filter.Type,
                                        text = filter.Text,
                                        max = max,
                                        min = min,
                                        disabled = true
                                    });

                                    if (cluster_jewel != "")
                                    {
                                        ((TextBox)FindName("tbOpt" + k + "_0")).IsEnabled = false;
                                        ((TextBox)FindName("tbOpt" + k + "_1")).IsEnabled = false;
                                        ((TextBox)FindName("tbOpt" + k + "_0")).Background = SystemColors.WindowBrush;
                                        ((TextBox)FindName("tbOpt" + k + "_0")).Foreground = SystemColors.WindowBrush;
                                        ((TextBox)FindName("tbOpt" + k)).Text = cluster_jewel;
                                        ((TextBox)FindName("tbOpt" + k)).Tag = "CLUSTER";
                                    }

                                    ((TextBox)FindName("tbOpt" + k + "_0")).Text = min == 99999 ? "" : min.ToString();
                                    ((TextBox)FindName("tbOpt" + k + "_1")).Text = max == 99999 ? "" : max.ToString();

                                    attackSpeedIncr += filter.Text == PS.AttackSpeedIncr.Text[z] && min.WithIn(1, 999) ? min : 0;
                                    PhysicalDamageIncr += filter.Text == PS.PhysicalDamageIncr.Text[z] && min.WithIn(1, 9999) ? min : 0;

                                    k++;
                                    notImpCnt++;
                                }
                            }
                        }
                    }

                    int alt_quality = 0;
                    bool is_blight = false;

                    /*
                    //if (is_map || is_currency) is_map_fragment = false;
                    */
                    bool is_map = cate_ids[0] == "map"; // || lItemOption[PS.MapTier.Text[z]] != "";
                    bool is_map_fragment = cate_ids.Length > 1 && cate_ids.Join(".") == "map.fragment";
                    bool is_map_ultimatum = lItemOption[PS.MapUltimatum.Text[z]] != "";
                    bool is_prophecy = lItemOption[PS.ProphecyItem.Text[z]] == "_TRUE_";
                    bool is_currency = rarity_id == "currency";
                    bool is_divination_card = rarity_id == "card";
                    bool is_gem = rarity_id == "gem";
                    bool is_vaal_gem = is_gem && lItemOption[PS.Vaal.Text[z] + " " + item_type] == "_TRUE_";
                    bool is_heist = lItemOption[PS.Heist.Text[z]] != "";
                    bool is_unIdentify = lItemOption[PS.Unidentified.Text[z]] == "_TRUE_";
                    bool is_detail = is_gem || is_map_fragment || (!is_map_ultimatum && is_currency) || is_divination_card || is_prophecy;

                    int item_idx = -1;
                    int cate_idx = category != null ? Array.FindIndex(mItemsData[z].Result, x => x.Id.Equals(category.Key)) : -1;

                    if (is_prophecy)
                    {
                        cate_ids = new string[] { "prophecy" };
                        item_rarity = Array.Find(PS.Category.Entries, x => x.Id == "prophecy").Text[z];
                        item_idx = Array.FindIndex(mItemsData[z].Result[cate_idx].Entries, x => x.Type == item_type);
                    }
                    if (is_map_fragment || is_map_ultimatum)
                    {
                        item_rarity = is_map_ultimatum ? "결전" : Array.Find(PS.Category.Entries, x => x.Id == "map.fragment").Text[z];
                        item_idx = Array.FindIndex(mItemsData[z].Result[cate_idx].Entries, x => x.Type == item_type);
                    }
                    else if (lItemOption[PS.MonsterGenus.Text[z]] != "" && lItemOption[PS.MonsterGroup.Text[z]] != "")
                    {
                        cate_ids = new string[] { "monster", "beast" };
                        cate_idx = Array.FindIndex(mItemsData[z].Result, x => x.Id.Equals("monsters"));
                        item_idx = Array.FindIndex(mItemsData[z].Result[cate_idx].Entries, x => x.Text == item_type);
                        item_rarity = Array.Find(PS.Category.Entries, x => x.Id == "monster.beast").Text[z];
                        item_type = z == 1 || item_idx == -1 ? item_type : mItemsData[1].Result[cate_idx].Entries[item_idx].Type;
                        item_idx = -1; // 야수는 영어로만 검색됨...
                    }
                    else if (cate_idx > -1)
                    {
                        DataResult data = mItemsData[z].Result[cate_idx];

                        if ((is_unIdentify || rarity_id == "normal") && item_type.Length > 4 && item_type.IndexOf(PS.Superior.Text[z] + " ") == 0)
                        {
                            item_type = item_type.Substring(z == 1 ? 9 : 3);
                        }
                        else if (rarity_id == "magic")
                        {
                            item_type = item_type.Split(new string[] { z == 1 ? " of " : " - " }, StringSplitOptions.None)[0].Trim();
                        }

                        if (is_gem)
                        {
                            for (int i = 0; i < PS.Gems.Entries.Length; i++)
                            {
                                int pos = item_type.IndexOf(PS.Gems.Entries[i].Text[z] + " ");
                                if (pos == 0)
                                {
                                    alt_quality = i + 1;
                                    item_type = item_type.Substring(PS.Gems.Entries[i].Text[z].Length + 1);
                                }
                            }

                            if (is_vaal_gem && lItemOption[PS.Corrupted.Text[z]] == "_TRUE_")
                            {
                                DataEntrie entries = Array.Find(data.Entries, x => x.Text.Equals(PS.Vaal.Text[z] + " " + item_type));
                                if (entries != null) item_type = entries.Type;
                            }
                        }
                        else if (is_map && item_type.Length > 5)
                        {
                            if (item_type.IndexOf(PS.Blighted.Text[z] + " ") == 0)
                            {
                                is_blight = true;
                                item_type = item_type.Substring(PS.Blighted.Text[z].Length + 1);
                            }

                            if (item_type.IndexOf(PS.Shaped.Text[z] + " ") == 0)
                                item_type = item_type.Substring(PS.Shaped.Text[z].Length + 1);
                        }
                        else if (lItemOption[PS.SynthesisedItem.Text[z]] == "_TRUE_")
                        {
                            string[] tmp = PS.SynthesisedItem.Text[z].Split(' ');
                            if (item_type.IndexOf(tmp[0] + " ") == 0)
                                item_type = item_type.Substring(tmp[0].Length + 1);
                        }

                        if (!is_unIdentify && rarity_id == "magic")
                        {
                            string[] tmp = item_type.Split(' ');

                            if (data != null && tmp.Length > 1)
                            {
                                for (int i = 0; i < tmp.Length - 1; i++)
                                {
                                    tmp[i] = "";
                                    string tmp2 = tmp.Join(" ").Trim();

                                    DataEntrie entries = Array.Find(data.Entries, x => x.Type.Equals(tmp2));
                                    if (entries != null)
                                    {
                                        item_type = entries.Type;
                                        break;
                                    }
                                }
                            }
                        }

                        item_idx = Array.FindIndex(mItemsData[z].Result[cate_idx].Entries, x => (x.Type == item_type && (rarity_id != "unique" || x.Name == item_name)));
                    }

                    mItemBaseName.Ids = cate_ids;
                    mItemBaseName.NameEN = z == 1 || cate_idx == -1 || item_idx == -1 || rarity_id != "unique" ? item_name : mItemsData[1].Result[cate_idx].Entries[item_idx].Name;
                    mItemBaseName.NameKR = z == 0 || cate_idx == -1 || item_idx == -1 || rarity_id != "unique" ? item_name : mItemsData[0].Result[cate_idx].Entries[item_idx].Name;
                    mItemBaseName.TypeEN = z == 1 || cate_idx == -1 || item_idx == -1 ? item_type : mItemsData[1].Result[cate_idx].Entries[item_idx].Type;
                    mItemBaseName.TypeKR = z == 0 || cate_idx == -1 || item_idx == -1 ? item_type : mItemsData[0].Result[cate_idx].Entries[item_idx].Type;

                    string item_quality = Regex.Replace(lItemOption[PS.Quality.Text[z]], "[^0-9]", "");
                    bool by_type = cate_ids.Length > 1 && cate_ids[0].WithIn(new string[] { "weapon", "armour", "accessory" });

                    if (is_detail || is_map_fragment)
                    {
                        try
                        {
                            int i = is_map_fragment ? 1 : (is_gem ? 3 : 2);
                            tkDetail.Text = asData.Length > (i + 1) ? asData[i] + asData[i + 1] : asData[asData.Length - 1];

                            tkDetail.Text = Regex.Replace(
                                tkDetail.Text.Replace(PS.UnstackItems.Text[z], ""),
                                "<(uniqueitem|prophecy|divination|gemitem|magicitem|rareitem|whiteitem|corrupted|default|normal|augmented|size:[0-9]+)>",
                                ""
                            );
                        }
                        catch { }
                    }
                    else
                    {
                        int Imp_cnt = itemfilters.Count - ((rarity_id == "normal" || is_unIdentify) ? 0 : notImpCnt);

                        for (int i = 0; i < itemfilters.Count; i++)
                        {
                            Itemfilter ifilter = itemfilters[i];
                            ComboBox tmpComboBox = (ComboBox)FindName("cbOpt" + i);
                            CheckBox tmpCheckBox = (CheckBox)FindName("tbOpt" + i + "_2");

                            if (i < Imp_cnt)
                            {
                                SetFilterObjectColor(i, System.Windows.Media.Brushes.Blue);
                                tmpComboBox.SelectedValue = RS.lFilterType["enchant"];
                                if (tmpComboBox.SelectedIndex == -1)
                                {
                                    SetFilterObjectColor(i, System.Windows.Media.Brushes.DarkRed);
                                    tmpComboBox.SelectedValue = RS.lFilterType["implicit"];
                                }
                                tmpCheckBox.IsChecked = ((string)((TextBox)FindName("tbOpt" + i)).Tag ?? "") == "CLUSTER";
                                itemfilters[i].disabled = true;
                            }
                            else if (cate_ids[0] != "" && tmpComboBox.SelectedIndex > -1)
                            {
                                if ((string)tmpComboBox.SelectedValue != RS.lFilterType["crafted"] && ((mConfigData.Options.AutoCheckUnique && rarity_id == "unique")
                                    || (Array.Find(mCheckedData.Checked.Entries, x => x.Text[z] == ifilter.text && x.Id.IndexOf(cate_ids[0] + "/") > -1) != null)))
                                {
                                    tmpCheckBox.IsChecked = true;
                                    itemfilters[i].disabled = false;
                                }
                            }

                            if (RS.lDisable.ContainsKey(ifilter.id.Split('.')[1]))
                            {
                                tmpCheckBox.IsChecked = false;
                                tmpCheckBox.IsEnabled = false;
                                itemfilters[i].disabled = true;
                            }
                        }

                        // 장기는 중복 옵션 제거
                        if (cate_ids.Join(".") == "monster.sample")
                        {
                            Deduplicationfilter(itemfilters);
                        }

                        if (!is_unIdentify && cate_ids[0] == "weapon")
                        {
                            setDPS(
                                    lItemOption[PS.PhysicalDamage.Text[z]], lItemOption[PS.ElementalDamage.Text[z]], lItemOption[PS.ChaosDamage.Text[z]],
                                    item_quality, lItemOption[PS.AttacksPerSecond.Text[z]], PhysicalDamageIncr, attackSpeedIncr
                                );
                        }
                    }

                    cbName.SelectionChanged -= cbName_SelectionChanged;
                    cbName.Items.Clear();
                    cbName.Items.Add((Regex.Replace(mItemBaseName.NameKR, @"\([a-zA-Z\,\s']+\)$", "") + " " + Regex.Replace(mItemBaseName.TypeKR, @"\([a-zA-Z\,\s']+\)$", "")).Trim());
                    cbName.Items.Add((mItemBaseName.NameEN + " " + mItemBaseName.TypeEN).Trim());
                    cbName.Items.Add((RS.ServerLang == 1 ? "영국서버 - " : "한국서버 - ") + "아이템 유형으로 검색합니다");
                    cbName.SelectedIndex = RS.ServerLang == 1 ? 1 : 0;

                    if (by_type && rarity_id.WithIn(new string[] { "magic", "rare" }))
                    {
                        string[] bys = mConfigData.Options.AutoSelectByType.ToLower().Split(',');
                        if (Array.IndexOf(bys, cate_ids[0]) > -1) cbName.SelectedIndex = 2;
                    }

                    cbName.SelectionChanged += cbName_SelectionChanged;
                    cbRarity.SelectedValue = item_rarity;

                    if (cbRarity.SelectedIndex == -1)
                    {
                        cbRarity.Items.Clear();
                        cbRarity.Items.Add(item_rarity);
                        cbRarity.SelectedIndex = 0;
                    }
                    else if ((string)cbRarity.SelectedValue == "normal")
                    {
                        cbRarity.SelectedIndex = 0;
                    }

                    bdExchange.IsEnabled = cate_ids[0] == "currency" && GetExchangeItem(z, item_type) != null;
                    bdExchange.Visibility = !is_gem && (is_detail || bdExchange.IsEnabled) ? Visibility.Visible : Visibility.Hidden;

                    if (bdExchange.Visibility == Visibility.Hidden)
                    {
                        tbLvMin.Text = Regex.Replace(lItemOption[is_gem ? PS.Level.Text[z] : PS.ItemLevel.Text[z]], "[^0-9]", "");
                        tbQualityMin.Text = item_quality;

                        string[] Influences = { PS.ShaperItem.Text[z], PS.ElderItem.Text[z], PS.CrusaderItem.Text[z], PS.RedeemerItem.Text[z], PS.HunterItem.Text[z], PS.WarlordItem.Text[z] };
                        for (int i = 0; i < Influences.Length; i++)
                        {
                            if (lItemOption[Influences[i]] == "_TRUE_")
                                cbInfluence1.SelectedIndex = i + 1;
                        }

                        for (int i = 0; i < Influences.Length; i++)
                        {
                            if (cbInfluence1.SelectedIndex != (i + 1) && lItemOption[Influences[i]] == "_TRUE_")
                                cbInfluence2.SelectedIndex = i + 1;
                        }

                        if (lItemOption[PS.Corrupted.Text[z]] == "_TRUE_")
                        {
                            cbCorrupt.BorderThickness = new Thickness(2);
                            cbCorrupt.FontWeight = FontWeights.Bold;
                            cbCorrupt.Foreground = System.Windows.Media.Brushes.DarkRed;
                        }

                        if (is_heist || is_gem || is_map)
                        {
                            cbAltQuality.Items.Add(is_heist ? "모든 강탈 가치" : (is_gem ? "모든 젬" : (is_map_ultimatum ? "모든 보상" : "영향 없음")));

                            foreach (ParserDictionary item in (is_heist ? PS.Heist : (is_gem ? PS.Gems : (is_map_ultimatum ? PS.RewardUltimatum : PS.MapTier))).Entries)
                            {
                                cbAltQuality.Items.Add(item.Text[z]);
                            }

                            if (is_gem)
                            {
                                ckLv.IsChecked = lItemOption[PS.Level.Text[z]].IndexOf(" (" + PS.Max.Text[z]) > 0;
                                ckQuality.IsChecked = item_quality.ToInt(0) > 20;
                                cbAltQuality.SelectedIndex = alt_quality;
                            }
                            else if (is_heist)
                            {
                                //string tmp = Regex.Replace(lItemOption[PS.Heist.Text[z]], @".+ \(([^\)]+)\)$", "$1");
                                cbAltQuality.SelectedIndex = 0; // SelectedValue = tmp;
                            }
                            else if (is_map || is_map_ultimatum)
                            {
                                Synthesis.Content = "역병";

                                if (is_map_ultimatum)
                                {
                                    cbAltQuality.SelectedValue = lItemOption[PS.RewardUltimatum.Text[z]];
                                    if (cbAltQuality.SelectedIndex == -1)
                                    {
                                        cbAltQuality.Items[cbAltQuality.Items.Count - 1] = lItemOption[PS.RewardUltimatum.Text[z]];
                                        cbAltQuality.SelectedIndex = cbAltQuality.Items.Count - 1;
                                    }
                                }
                                else
                                {
                                    ckLv.IsChecked = true;
                                    ckLv.Content = "등급";
                                    tbLvMin.Text = tbLvMax.Text = lItemOption[PS.MapTier.Text[z]];
                                    cbAltQuality.SelectedValue = map_influenced != "" ? map_influenced : "영향 없음";
                                }
                            }
                        }
                        else if (by_type || cate_ids[0] == "flask")
                        {
                            if (tbQualityMin.Text.ToInt(0) > (cate_ids[0] == "accessory" ? 4 : 20))
                            {
                                ckQuality.FontWeight = FontWeights.Bold;
                                ckQuality.Foreground = System.Windows.Media.Brushes.DarkRed;
                                ckQuality.BorderBrush = System.Windows.Media.Brushes.DarkRed;
                            }

                            if (by_type)
                            {
                                if (tbLvMin.Text.ToInt(0) > 82)
                                {
                                    ckLv.FontWeight = FontWeights.Bold;
                                    ckLv.Foreground = System.Windows.Media.Brushes.DarkRed;
                                    ckLv.BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                }

                                cbCorrupt.SelectedIndex = mConfigData.Options.AutoSelectCorrupt == "no" ? 2 : (mConfigData.Options.AutoSelectCorrupt == "yes" ? 1 : 0);
                            }
                        }
                    }

                    if (lItemOption[PS.Sockets.Text[z]] != "")
                    {
                        int[] socket = SocketParser(lItemOption[PS.Sockets.Text[z]]);
                        tbSocketMin.Text = socket[0].ToString();
                        tbLinksMin.Text = socket[1] > 0 ? socket[1].ToString() : "";
                        ckSocket.IsChecked = socket[1] > 4;
                    }

                    if (isWinShow || this.Visibility == Visibility.Visible)
                    {
                        Synthesis.IsChecked = (is_map && is_blight) || lItemOption[PS.SynthesisedItem.Text[z]] == "_TRUE_";
                        lbSocketBackground.Visibility = by_type ? Visibility.Hidden : Visibility.Visible;
                        cbAltQuality.Visibility = by_type ? Visibility.Hidden : Visibility.Visible;
                        bdDetail.Visibility = is_detail ? Visibility.Visible : Visibility.Hidden;

                        cbInfluence1.Visibility = cbAltQuality.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                        cbInfluence2.Visibility = cbAltQuality.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                        if (cbInfluence1.SelectedIndex > 0) cbInfluence1.BorderThickness = new Thickness(2);
                        if (cbInfluence2.SelectedIndex > 0) cbInfluence2.BorderThickness = new Thickness(2);

                        tkPriceInfo.Foreground = tkPriceCount.Foreground = SystemColors.WindowTextBrush;

                        mLockUpdatePrice = false;

                        if (mConfigData.Options.AutoSearchDelay > 0 && mAutoSearchTimerCount < 1)
                        {
                            UpdatePriceThreadWorker(GetItemOptions(), null);
                        }
                        else
                        {
                            liPrice.Items.Clear();
                        }

                        SetSearchButtonText(RS.ServerLang == 0);
                        this.ShowActivated = false;
                        this.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                ForegroundMessage(String.Format("{0} 에러:  {1}\r\n\r\n{2}\r\n\r\n", ex.Source, ex.Message, ex.StackTrace), "에러", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ItemOption GetItemOptions()
        {
            ItemOption itemOption = new ItemOption();

            itemOption.Influence1 = cbInfluence1.SelectedIndex;
            itemOption.Influence2 = cbInfluence2.SelectedIndex;

            // 영향은 첫번째 값이 우선 순위여야 함
            if (itemOption.Influence1 == 0 && itemOption.Influence2 != 0)
            {
                itemOption.Influence1 = itemOption.Influence2;
                itemOption.Influence2 = 0;
            }

            itemOption.Corrupt = cbCorrupt.SelectedIndex;
            itemOption.Synthesis = Synthesis.IsChecked == true;
            itemOption.ChkSocket = ckSocket.IsChecked == true;
            itemOption.ChkQuality = ckQuality.IsChecked == true;
            itemOption.ChkLv = ckLv.IsChecked == true;
            itemOption.ByType = cbName.SelectedIndex == 2;

            itemOption.SocketMin = tbSocketMin.Text.ToDouble(99999);
            itemOption.SocketMax = tbSocketMax.Text.ToDouble(99999);
            itemOption.LinkMin = tbLinksMin.Text.ToDouble(99999);
            itemOption.LinkMax = tbLinksMax.Text.ToDouble(99999);
            itemOption.QualityMin = tbQualityMin.Text.ToDouble(99999);
            itemOption.QualityMax = tbQualityMax.Text.ToDouble(99999);
            itemOption.LvMin = tbLvMin.Text.ToDouble(99999);
            itemOption.LvMax = tbLvMax.Text.ToDouble(99999);

            itemOption.AltQuality = cbAltQuality.SelectedIndex;
            itemOption.RarityAt = (cbRarity.Items.Count > 1 ? cbRarity.SelectedIndex : 0);
            itemOption.Flags = (string)(cbRarity.SelectedValue ?? "") == "결전" ? "결전|" + cbAltQuality.SelectedValue : "";
            itemOption.PriceMin = tbPriceFilterMin.Text == "" ? 0 : tbPriceFilterMin.Text.ToDouble(99999);


            int total_res_idx = -1;

            for (int i = 0; i < 10; i++)
            {
                Itemfilter itemfilter = new Itemfilter();
                ComboBox comboBox = (ComboBox)FindName("cbOpt" + i);

                if (comboBox.SelectedIndex > -1)
                {
                    itemfilter.text = ((TextBox)FindName("tbOpt" + i)).Text.Trim();
                    itemfilter.flag = (string)((TextBox)FindName("tbOpt" + i)).Tag;
                    itemfilter.disabled = ((CheckBox)FindName("tbOpt" + i + "_2")).IsChecked != true;
                    itemfilter.min = ((TextBox)FindName("tbOpt" + i + "_0")).Text.ToDouble(99999);
                    itemfilter.max = ((TextBox)FindName("tbOpt" + i + "_1")).Text.ToDouble(99999);

                    if (itemfilter.disabled == false && ((CheckBox)FindName("tbOpt" + i + "_3")).IsChecked == true)
                    {
                        if (total_res_idx == -1)
                        {
                            total_res_idx = itemOption.itemfilters.Count;
                            itemfilter.id = "pseudo.pseudo_total_resistance";
                        }
                        else
                        {
                            double min = itemOption.itemfilters[total_res_idx].min;
                            itemOption.itemfilters[total_res_idx].min = (min == 99999 ? 0 : min) + (itemfilter.min == 99999 ? 0 : itemfilter.min);
                            double max = itemOption.itemfilters[total_res_idx].max;
                            itemOption.itemfilters[total_res_idx].max = (max == 99999 ? 0 : max) + (itemfilter.max == 99999 ? 0 : itemfilter.max);
                            continue;
                        }
                    }
                    else
                    {
                        itemfilter.id = ((FilterEntrie)comboBox.SelectedItem).ID;
                    }

                    itemfilter.type = itemfilter.id.Split('.')[0];
                    itemOption.itemfilters.Add(itemfilter);
                }
            }

            // 총 저항은 min 값만 필요
            if (total_res_idx > -1)
            {
                double min = itemOption.itemfilters[total_res_idx].min;
                double max = itemOption.itemfilters[total_res_idx].max;
                itemOption.itemfilters[total_res_idx].min = (min == 99999 ? 0 : min) + (max == 99999 ? 0 : max);
                itemOption.itemfilters[total_res_idx].max = 99999;
            }

            return itemOption;
        }

        private string CreateJson(ItemOption itemOptions, bool useSaleType)
        {
            string BeforeDayToString(int day)
            {
                if (day < 1)
                    return "any";
                else if (day < 3)
                    return "1day";
                else if (day < 7)
                    return "3days";
                else if (day < 14)
                    return "1week";
                return "2weeks";
            }

            try
            {
                JsonData jsonData = new JsonData();
                jsonData.Query = new q_Query();
                q_Query JQ = jsonData.Query;

                jsonData.Sort.Price = "asc";

                byte lang_type = mItemBaseName.LangType;
                string Inherit = mItemBaseName.Ids.Length > 0 ? mItemBaseName.Ids[0] : "any";
                string[] flags = itemOptions.Flags.Split('|');

                JQ.Name = RS.ServerLang == 1 ? mItemBaseName.NameEN : mItemBaseName.NameKR;
                JQ.Type = RS.ServerLang == 1 ? mItemBaseName.TypeEN : mItemBaseName.TypeKR;

                JQ.Stats = new q_Stats[0];
                JQ.Status.Option = "online";

                JQ.Filters.Type.Filters.Category.Option = Inherit == "jewel" ? Inherit : mItemBaseName.Ids.Join(".");
                JQ.Filters.Type.Filters.Rarity.Option = itemOptions.RarityAt > 0 ? (mParserData.Rarity.Entries[itemOptions.RarityAt - 1].Id) : "any";
                //JQ.Filters.Type.Filters.Rarity.Option = itemOptions.RarityAt > 0 ? RS.lRarity.ElementAt(itemOptions.RarityAt - 1).Key.ToLower() : "any";

                JQ.Filters.Trade.Disabled = mConfigData.Options.SearchBeforeDay == 0;
                JQ.Filters.Trade.Filters.Indexed.Option = BeforeDayToString(mConfigData.Options.SearchBeforeDay);
                JQ.Filters.Trade.Filters.SaleType.Option = useSaleType ? "priced" : "any";
                JQ.Filters.Trade.Filters.Price.Min = 99999;
                JQ.Filters.Trade.Filters.Price.Max = 99999;

                if (itemOptions.PriceMin > 0)
                {
                    JQ.Filters.Trade.Filters.Price.Min = itemOptions.PriceMin;
                }

                JQ.Filters.Socket.Disabled = itemOptions.ChkSocket != true;

                JQ.Filters.Socket.Filters.Links.Min = itemOptions.LinkMin;
                JQ.Filters.Socket.Filters.Links.Max = itemOptions.LinkMax;
                JQ.Filters.Socket.Filters.Sockets.Min = itemOptions.SocketMin;
                JQ.Filters.Socket.Filters.Sockets.Max = itemOptions.SocketMax;

                JQ.Filters.Misc.Filters.Quality.Min = itemOptions.ChkQuality == true ? itemOptions.QualityMin : 99999;
                JQ.Filters.Misc.Filters.Quality.Max = itemOptions.ChkQuality == true ? itemOptions.QualityMax : 99999;

                JQ.Filters.Misc.Filters.Ilvl.Min = itemOptions.ChkLv != true || Inherit == "gem" || Inherit == "map" ? 99999 : itemOptions.LvMin;
                JQ.Filters.Misc.Filters.Ilvl.Max = itemOptions.ChkLv != true || Inherit == "gem" || Inherit == "map" ? 99999 : itemOptions.LvMax;
                JQ.Filters.Misc.Filters.Gem_level.Min = itemOptions.ChkLv == true && Inherit == "gem" ? itemOptions.LvMin : 99999;
                JQ.Filters.Misc.Filters.Gem_level.Max = itemOptions.ChkLv == true && Inherit == "gem" ? itemOptions.LvMax : 99999;
                JQ.Filters.Misc.Filters.AlternateQuality.Option = Inherit == "gem" && itemOptions.AltQuality > 0 ? itemOptions.AltQuality.ToString() : "any";

                JQ.Filters.Misc.Filters.Shaper.Option = Inherit != "map" && (itemOptions.Influence1 == 1 || itemOptions.Influence2 == 1) ? "true" : "any";
                JQ.Filters.Misc.Filters.Elder.Option = Inherit != "map" && (itemOptions.Influence1 == 2 || itemOptions.Influence2 == 2) ? "true" : "any";
                JQ.Filters.Misc.Filters.Crusader.Option = Inherit != "map" && (itemOptions.Influence1 == 3 || itemOptions.Influence2 == 3) ? "true" : "any";
                JQ.Filters.Misc.Filters.Redeemer.Option = Inherit != "map" && (itemOptions.Influence1 == 4 || itemOptions.Influence2 == 4) ? "true" : "any";
                JQ.Filters.Misc.Filters.Hunter.Option = Inherit != "map" && (itemOptions.Influence1 == 5 || itemOptions.Influence2 == 5) ? "true" : "any";
                JQ.Filters.Misc.Filters.Warlord.Option = Inherit != "map" && (itemOptions.Influence1 == 6 || itemOptions.Influence2 == 6) ? "true" : "any";

                JQ.Filters.Misc.Filters.Synthesis.Option = Inherit != "map" && itemOptions.Synthesis == true ? "true" : "any";
                JQ.Filters.Misc.Filters.Corrupted.Option = itemOptions.Corrupt == 1 ? "true" : (itemOptions.Corrupt == 2 ? "false" : "any");

                JQ.Filters.Heist.Filters.HeistObjective.Option = "any";
                if (Inherit == "heistmission" && itemOptions.AltQuality > 0)
                {
                    string[] tmp = new string[] { "moderate", "high", "precious", "priceless" };
                    JQ.Filters.Heist.Filters.HeistObjective.Option = tmp[itemOptions.AltQuality - 1];
                }

                JQ.Filters.Heist.Disabled = JQ.Filters.Heist.Filters.HeistObjective.Option == "any";

                JQ.Filters.Misc.Disabled = !(
                    itemOptions.ChkQuality == true || itemOptions.Corrupt > 0 || (itemOptions.AltQuality != 0xff && itemOptions.AltQuality > 0)
                    || (Inherit != "map" && (itemOptions.Influence1 != 0 || itemOptions.ChkLv == true || itemOptions.Synthesis == true))
                );

                JQ.Filters.Ultimatum.Disabled = !(itemOptions.AltQuality > 0 && flags.Length > 1 && flags[0] == "결전");
                JQ.Filters.Map.Disabled = !(
                    Inherit == "map" && (itemOptions.AltQuality > 0 || itemOptions.ChkLv == true || itemOptions.Synthesis == true || itemOptions.Influence1 != 0)
                );

                JQ.Filters.Map.Filters.Tier.Min = itemOptions.ChkLv == true && Inherit == "map" ? itemOptions.LvMin : 99999;
                JQ.Filters.Map.Filters.Tier.Max = itemOptions.ChkLv == true && Inherit == "map" ? itemOptions.LvMax : 99999;
                JQ.Filters.Map.Filters.Shaper.Option = Inherit == "map" && itemOptions.Influence1 == 1 ? "true" : "any";
                JQ.Filters.Map.Filters.Elder.Option = Inherit == "map" && itemOptions.Influence1 == 2 ? "true" : "any";
                JQ.Filters.Map.Filters.Blight.Option = Inherit == "map" && itemOptions.Synthesis == true ? "true" : "any";
                if (JQ.Filters.Ultimatum.Disabled && Inherit == "map" && itemOptions.AltQuality > 0)
                {
                    Itemfilter itemfilter = new Itemfilter();
                    itemfilter.id = "implicit.stat_1792283443";
                    DataResult filterResult = Array.Find(mFilterData[lang_type].Result, x => x.Label == RS.lFilterType["implicit"]);
                    DataEntrie filter = Array.Find(filterResult.Entries, x => x.Id == itemfilter.id);
                    if (filter != null)
                    {
                        itemfilter.text = filter.Text;
                        itemfilter.type = "implicit";
                        itemfilter.flag = "INFLUENCED";
                        itemfilter.disabled = false;
                        itemfilter.min = itemOptions.AltQuality;
                        itemfilter.max = 99999;
                        itemOptions.itemfilters.Add(itemfilter);
                    }
                }
                else if (!JQ.Filters.Ultimatum.Disabled && flags.Length > 1 && itemOptions.AltQuality > 0)
                {
                    JQ.Filters.Ultimatum.Filters.Reward.Option = mParserData.RewardUltimatum.Entries[itemOptions.AltQuality - 1].Id;
                    JQ.Filters.Ultimatum.Filters.Output.Option = itemOptions.AltQuality == mParserData.RewardUltimatum.Entries.Length ? flags[1] : "any";
                }

                bool error_filter = false;

                if (itemOptions.itemfilters.Count > 0)
                {
                    JQ.Stats = new q_Stats[1];
                    JQ.Stats[0] = new q_Stats();
                    JQ.Stats[0].Type = "and";
                    JQ.Stats[0].Filters = new q_Stats_filters[itemOptions.itemfilters.Count];

                    int idx = 0;

                    for (int i = 0; i < itemOptions.itemfilters.Count; i++)
                    {
                        string input = itemOptions.itemfilters[i].text;
                        string id = itemOptions.itemfilters[i].id;
                        string type = itemOptions.itemfilters[i].type;

                        if (input.Trim() != "" && RS.lFilterType.ContainsKey(type))
                        {
                            string type_name = RS.lFilterType[type];

                            DataResult filterResult = Array.Find(mFilterData[lang_type].Result, x => x.Label == type_name);

                            if (filterResult != null)
                            {
                                // 무기에 경우 pseudo_adds_[a-z]+_damage 옵션은 공격 시 가 붙음
                                if (type == "pseudo" && Inherit == "weapon" && Regex.IsMatch(id, @"^pseudo.pseudo_adds_[a-z]+_damage$"))
                                {
                                    id = id + "_to_attacks";
                                }

                                DataEntrie filter = Array.Find(filterResult.Entries, x => x.Id == id && x.Type == type);

                                JQ.Stats[0].Filters[idx] = new q_Stats_filters();
                                JQ.Stats[0].Filters[idx].Value = new q_Min_And_Max();

                                if (filter != null && (filter.Id ?? "").Trim() != "")
                                {
                                    JQ.Stats[0].Filters[idx].Disabled = itemOptions.itemfilters[i].disabled == true;
                                    JQ.Stats[0].Filters[idx].Value.Min = itemOptions.itemfilters[i].min;
                                    JQ.Stats[0].Filters[idx].Value.Max = itemOptions.itemfilters[i].max;
                                    if ((itemOptions.itemfilters[i].flag ?? "") == "CLUSTER")
                                    {
                                        JQ.Stats[0].Filters[idx].Value.Option = itemOptions.itemfilters[i].min;
                                        JQ.Stats[0].Filters[idx].Value.Min = 99999;
                                    }
                                    else if ((itemOptions.itemfilters[i].flag ?? "") == "INFLUENCED")
                                    {
                                        JQ.Stats[0].Filters[idx].Value.Option = itemOptions.itemfilters[i].min.ToString();
                                        JQ.Stats[0].Filters[idx].Value.Min = 99999;
                                    }
                                    JQ.Stats[0].Filters[idx++].Id = filter.Id;
                                }
                                else
                                {
                                    error_filter = true;
                                    itemOptions.itemfilters[i].isNull = true;

                                    // 오류 방지를 위해 널값시 아무거나 추가 
                                    JQ.Stats[0].Filters[idx].Disabled = true;
                                    JQ.Stats[0].Filters[idx].Value.Min = 99999;
                                    JQ.Stats[0].Filters[idx].Value.Max = 99999;
                                    JQ.Stats[0].Filters[idx++].Id = "temp_ids";
                                }
                            }
                        }
                    }
                }

                //if (!ckSocket.Dispatcher.CheckAccess())
                //else if (ckSocket.Dispatcher.CheckAccess())

                string sEntity = Json.Serialize<JsonData>(jsonData);

                if (itemOptions.ByType || JQ.Name == "" || JQ.Filters.Type.Filters.Rarity.Option != "unique")
                {
                    sEntity = sEntity.Replace("\"name\":\"" + JQ.Name + "\",", "");

                    if (Inherit == "jewel" || itemOptions.ByType)
                        sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "");
                    else if (Inherit == "prophecy")
                        sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "\"name\":\"" + JQ.Type + "\",");
                    else if (JQ.Filters.Type.Filters.Category.Option == "monster.sample")
                        sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "\"term\":\"" + JQ.Type + "\",");
                }

                sEntity = Regex.Replace(sEntity.Replace("{[a-z\":,]+\"temp_ids\"[a-z\":,]+{[a-z0-9\":,]*}}", ""), "\"(min|max)\":99999|\"option\":0", "");
                sEntity = Regex.Replace(sEntity, "\"("
                    + "sale_type|rarity|category|corrupted|synthesised_item|shaper_item|elder_item|crusader_item|redeemer_item|hunter_item|warlord_"
                    + "item|map_shaped|map_elder|map_blighted|heist_objective_value|ultimatum_reward|ultimatum_output" + ")\":{\"option\":(\"any\"|null)},?", ""
                );
                sEntity = Regex.Replace(Regex.Replace(Regex.Replace(sEntity, ",{2,}", ","), "({),{1,}", "$1"), ",{1,}(}|])", "$1");

                if (error_filter)
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                        (ThreadStart)delegate ()
                        {
                            for (int i = 0; i < itemOptions.itemfilters.Count; i++)
                            {
                                if (itemOptions.itemfilters[i].isNull)
                                {
                                    ((TextBox)FindName("tbOpt" + i)).Background = System.Windows.Media.Brushes.Red;
                                    ((TextBox)FindName("tbOpt" + i + "_0")).Text = "error";
                                    ((TextBox)FindName("tbOpt" + i + "_1")).Text = "error";
                                    ((CheckBox)FindName("tbOpt" + i + "_2")).IsChecked = false;
                                    ((CheckBox)FindName("tbOpt" + i + "_2")).IsEnabled = false;
                                    ((CheckBox)FindName("tbOpt" + i + "_3")).Visibility = Visibility.Hidden;
                                }
                            }
                        }
                    );
                }

                return sEntity;
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                ForegroundMessage(String.Format("{0} 에러:  {1}\r\n\r\n{2}\r\n\r\n", ex.Source, ex.Message, ex.StackTrace), "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                return "";
            }
        }

        private void UpdatePrice(string[] entity, int listCount)
        {
            string url_string = "";
            string json_entity = "";
            string msg = "정보가 없습니다";
            string msg_2 = "";

            try
            {
                if (entity.Length > 0 && !string.IsNullOrEmpty(entity[0]))
                {
                    if (entity.Length == 1)
                    {
                        json_entity = entity[0];
                        url_string = RS.TradeApi[RS.ServerLang] + RS.ServerType;
                    }
                    else
                    {
                        url_string = RS.ExchangeApi[RS.ServerLang] + RS.ServerType;
                        json_entity = "{\"exchange\":{\"status\":{\"option\":\"online\"},\"have\":[\"" + entity[0] + "\"],\"want\":[\"" + entity[1] + "\"]}}";
                    }
                    string request_result = SendHTTP(json_entity, url_string, mConfigData.Options.ServerTimeout);
                    msg = "거래소 접속이 원활하지 않습니다";

                    if (request_result != null)
                    {
                        ResultData resultData = Json.Deserialize<ResultData>(request_result);
                        Dictionary<string, int> currencys = new Dictionary<string, int>();

                        int total = 0;
                        int resultCount = resultData.Result.Length;

                        if (resultData.Result.Length > 0)
                        {
                            string ents0 = "", ents1 = "";

                            if (entity.Length > 1)
                            {
                                //listCount = listCount + 2;
                                ents0 = Regex.Replace(entity[0], @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");
                                ents1 = Regex.Replace(entity[1], @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");
                            }

                            for (int x = 0; x < listCount; x++)
                            {
                                string[] tmp = new string[10];
                                int cnt = x * 10;
                                int length = 0;

                                if (cnt >= resultData.Result.Length)
                                    break;

                                for (int i = 0; i < 10; i++)
                                {
                                    if (i + cnt >= resultData.Result.Length)
                                        break;

                                    tmp[i] = resultData.Result[i + cnt];
                                    length++;
                                }

                                string json_result = "";
                                string url = RS.FetchApi[RS.ServerLang] + tmp.Join(",") + "?query=" + resultData.ID;
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
                                request.CookieContainer = new CookieContainer();
                                request.UserAgent = RS.UserAgent;
                                request.Timeout = mConfigData.Options.ServerTimeout * 1000;
                                //request.UseDefaultCredentials = true;

                                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                                using (StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                                {
                                    json_result = streamReader.ReadToEnd();
                                }

                                if (json_result != "")
                                {
                                    FetchData fetchData = new FetchData();
                                    fetchData.Result = new FetchDataInfo[10];

                                    fetchData = Json.Deserialize<FetchData>(json_result);

                                    for (int i = 0; i < fetchData.Result.Length; i++)
                                    {
                                        if (fetchData.Result[i] == null)
                                            break;

                                        if (fetchData.Result[i].Listing.Price != null && fetchData.Result[i].Listing.Price.Amount > 0)
                                        {
                                            string key = "";
                                            string indexed = fetchData.Result[i].Listing.Indexed;
                                            string account = fetchData.Result[i].Listing.Account.Name;
                                            string currency = fetchData.Result[i].Listing.Price.Currency;
                                            double amount = fetchData.Result[i].Listing.Price.Amount;

                                            liPrice.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                                            {
                                                ParserDictionary item = GetExchangeItem(currency);
                                                string keyName = item != null ? item.Text[0] : currency;

                                                if (entity.Length > 1)
                                                {
                                                    item = GetExchangeItem(entity[1]);
                                                    string tName2 = item != null ? item.Text[0] : entity[1];
                                                    liPrice.Items.Add(Math.Round(1 / amount, 4) + " " + tName2 + " <-> " + Math.Round(amount, 4) + " " + keyName + " [" + account + "]");
                                                }
                                                else
                                                {
                                                    liPrice.Items.Add((
                                                        String.Format(
                                                            "{0} {1} [{2}]",
                                                            GetLapsedTime(indexed).PadRight(10, '\u2000'), (amount + " " + keyName).PadRight(12, '\u2000'), account)
                                                        )
                                                    );
                                                }
                                            });

                                            if (entity.Length > 1)
                                                key = amount < 1 ? Math.Round(1 / amount, 1) + " " + ents1 : Math.Round(amount, 1) + " " + ents0;
                                            else
                                                key = Math.Round(amount - 0.1) + " " + currency;

                                            if (currencys.ContainsKey(key))
                                                currencys[key]++;
                                            else
                                                currencys.Add(key, 1);

                                            total++;
                                        }
                                    }
                                }

                                if (!mLockUpdatePrice)
                                {
                                    currencys.Clear();
                                    break;
                                }
                            }

                            if (currencys.Count > 0)
                            {
                                List<KeyValuePair<string, int>> myList = new List<KeyValuePair<string, int>>(currencys);
                                string first = ((KeyValuePair<string, int>)myList[0]).Key;
                                string last = ((KeyValuePair<string, int>)myList[myList.Count - 1]).Key;

                                myList.Sort(
                                    delegate (KeyValuePair<string, int> firstPair,
                                    KeyValuePair<string, int> nextPair)
                                    {
                                        return -1 * firstPair.Value.CompareTo(nextPair.Value);
                                    }
                                );

                                for (int i = 0; i < myList.Count; i++)
                                {
                                    if (i == 2) break;
                                    if (myList[i].Value < 2) continue;
                                    msg_2 += myList[i].Key + "[" + myList[i].Value + "], ";
                                }

                                msg = Regex.Replace(first + " ~ " + last, @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");
                                msg_2 = Regex.Replace(msg_2.TrimEnd(',', ' '), @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");

                                if (msg_2 == "") msg_2 = "가장 많은 수 없음";
                            }
                        }

                        cbPriceListTotal.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                        {
                            cbPriceListTotal.Text = total + "/" + resultCount + " 검색";
                        });

                        if (resultData.Total == 0 || currencys.Count == 0)
                        {
                            msg = mLockUpdatePrice ? "해당 물품의 거래가 없습니다" : "검색 실패: 클릭하여 다시 시도해주세요";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                mLockUpdatePrice = false;

                tkPriceCount.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                {
                    if (tkPriceCount.Text == ".") tkPriceCount.Text = ""; // 값 . 이면 읽는중 표시 끝나면 처리
                });

                tkPriceInfo.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                {
                    tkPriceInfo.Text = msg + (msg_2 != "" ? " = " + msg_2 : "");
                });

                liPrice.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                {
                    if (liPrice.Items.Count == 0)
                    {
                        liPrice.Items.Add(msg + (msg_2 != "" ? " = " + msg_2 : ""));
                    }
                    else
                    {
                        liPrice.ScrollIntoView(liPrice.Items[0]);
                    }
                });
            }
        }

        private Thread priceThread = null;
        private void UpdatePriceThreadWorker(ItemOption itemOptions, string[] exchange)
        {
            if (!mLockUpdatePrice)
            {
                mLockUpdatePrice = true;

                int listCount = (cbPriceListCount.SelectedIndex + 1) * 2;

                mAutoSearchTimer.Stop();
                liPrice.Items.Clear();

                tkPriceCount.Text = ".";
                tkPriceInfo.Text = "시세 확인중...";
                cbPriceListTotal.Text = "0/0 검색";

                priceThread?.Interrupt();
                priceThread?.Abort();
                priceThread = new Thread(() =>
                {
                    UpdatePrice(
                            exchange != null ? exchange : new string[1] { CreateJson(itemOptions, true) },
                            listCount
                        );

                    if (mConfigData.Options.AutoSearchDelay > 0)
                    {
                        mAutoSearchTimer.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
                        {
                            mAutoSearchTimerCount = mConfigData.Options.AutoSearchDelay;
                            mAutoSearchTimer.Start();
                        });
                    }
                });
                priceThread.Start();
            }
        }

        private int mAutoSearchTimerCount;
        private void AutoSearchTimer_Tick(object sender, EventArgs e)
        {
            tkPriceInfo.Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)delegate ()
            {
                if (mAutoSearchTimerCount < 1)
                {
                    mAutoSearchTimer.Stop();
                    if (liPrice.Items.Count == 0 && tkPriceCount.Text != ".")
                        tkPriceInfo.Text = (string)tkPriceInfo.Tag;
                }
                else
                {
                    mAutoSearchTimerCount--;
                    if (liPrice.Items.Count == 0 && tkPriceCount.Text != ".")
                        tkPriceInfo.Text = (string)tkPriceInfo.Tag + " (" + mAutoSearchTimerCount + ")";
                }
            });
        }

        private ParserDictionary GetExchangeItem(string id)
        {
            ParserDictionary item = Array.Find(mParserData.Currency.Entries, x => x.Id == id);
            if (item == null)
                item = Array.Find(mParserData.Exchange.Entries, x => x.Id == id);

            return item;
        }

        private ParserDictionary GetExchangeItem(int index, string text)
        {
            ParserDictionary item = Array.Find(mParserData.Currency.Entries, x => x.Text[index] == text);
            if (item == null)
                item = Array.Find(mParserData.Exchange.Entries, x => x.Text[index] == text);

            return item;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (Native.GetForegroundWindow().Equals(Native.FindWindow(RS.PoeClass, RS.PoeCaption)))
            {
                if (!mInstalledHotKey)
                    InstallRegisterHotKey();

                if (!mPausedHotKey && mConfigData.Options.CtrlWheel)
                {
                    TimeSpan dateDiff = Convert.ToDateTime(DateTime.Now) - mMouseHookCallbackTime;
                    if (dateDiff.Ticks > 3000000000) // 5분간 마우스 움직임이 없으면 훜이 풀렸을 수 있어 다시...
                    {
                        mMouseHookCallbackTime = Convert.ToDateTime(DateTime.Now);
                        MouseHook.Start();
                    }
                }
            }
            else
            {
                if (mInstalledHotKey)
                    RemoveRegisterHotKey();
            }
        }

        private void MouseEvent(object sender, EventArgs e)
        {
            if (!mHotkeyProcBlock)
            {
                mHotkeyProcBlock = true;

                try
                {
                    int zDelta = ((MouseHook.MouseEventArgs)e).zDelta;
                    if (zDelta != 0)
                        System.Windows.Forms.SendKeys.SendWait(zDelta > 0 ? "{Left}" : "{Right}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                mHotkeyProcBlock = false;
            }
        }

        private void InstallRegisterHotKey()
        {
            mInstalledHotKey = true;

            // 0x0: None, 0x1: Alt, 0x2: Ctrl, 0x3: Shift
            for (int i = 0; i < mConfigData.Shortcuts.Length; i++)
            {
                ConfigShortcut shortcut = mConfigData.Shortcuts[i];
                if (shortcut.Keycode > 0 && (shortcut.Value ?? "") != "")
                    Native.RegisterHotKey(mMainHwnd, 10001 + i, (uint)(shortcut.Ctrl ? 0x2 : 0x0), (uint)Math.Abs(shortcut.Keycode));
            }
        }

        private void RemoveRegisterHotKey()
        {
            for (int i = 0; i < mConfigData.Shortcuts.Length; i++)
            {
                ConfigShortcut shortcut = mConfigData.Shortcuts[i];
                if (shortcut.Keycode > 0 && (shortcut.Value ?? "") != "")
                    Native.UnregisterHotKey(mMainHwnd, 10001 + i);
            }

            mInstalledHotKey = false;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Native.WM_DRAWCLIPBOARD)
            {
                if (!mPausedHotKey && !mClipboardBlock)
                {
#if DEBUG
                    test123(); //무언가 테스트...
                    if (Native.GetForegroundWindow().Equals(Native.FindWindow(RS.PoeClass, RS.PoeCaption)))
#else
                    if (Native.GetForegroundWindow().Equals(Native.FindWindow(RS.PoeClass, RS.PoeCaption)))
#endif
                    {
                        try
                        {
                            if (Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text))
                                ItemTextParser(GetClipText(Clipboard.ContainsText(TextDataFormat.UnicodeText)));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
            else if (!mHotkeyProcBlock && msg == (int)0x312) //WM_HOTKEY
            {
                mHotkeyProcBlock = true;

                IntPtr findHwnd = Native.FindWindow(RS.PoeClass, RS.PoeCaption);

                if (Native.GetForegroundWindow().Equals(findHwnd))
                {
                    int key_idx = wParam.ToInt32() - 10001;

                    try
                    {
                        ConfigShortcut shortcut = mConfigData.Shortcuts[key_idx];

                        if (shortcut != null && shortcut.Value != null)
                        {
                            string valueLower = shortcut.Value.ToLower();
                            string popWinTitle = "이곳을 잡고 이동, 닫기는 클릭 혹은 ESC";

                            if (valueLower == "{pause}")
                            {
                                mPausedHotKey = !mPausedHotKey;

                                if (mPausedHotKey)
                                {
                                    if (mConfigData.Options.CtrlWheel) MouseHook.Stop();

                                    MessageBox.Show(Application.Current.MainWindow, "프로그램 동작을 일시 중지합니다." + '\n'
                                                    + "다시 시작하려면 일시 중지 단축키를 한번더 누르세요.", "POE 거래소 검색");
                                }
                                else
                                {
                                    if (mConfigData.Options.CtrlWheel) MouseHook.Start();

                                    MessageBox.Show(Application.Current.MainWindow, "프로그램 동작을 다시 시작합니다.", "POE 거래소 검색");
                                }

                                Native.SetForegroundWindow(findHwnd);
                            }
                            else if (valueLower == "{close}")
                            {
                                IntPtr pHwnd = Native.FindWindow(null, popWinTitle);
                                IntPtr pHwnd2 = Native.FindWindow(null, Title + " - " + "{grid:stash}");
                                if (pHwnd.ToInt32() != 0 || pHwnd2.ToInt32() != 0)
                                {
                                    if (pHwnd.ToInt32() != 0)
                                        Native.SendMessage(pHwnd, /* WM_CLOSE = */ 0x10, IntPtr.Zero, IntPtr.Zero);
                                    if (pHwnd2.ToInt32() != 0)
                                        Native.SendMessage(pHwnd2, /* WM_CLOSE = */ 0x10, IntPtr.Zero, IntPtr.Zero);
                                }
                                else if (this.Visibility == Visibility.Hidden)
                                {
                                    Native.SendMessage(findHwnd, 0x0101, new IntPtr(shortcut.Keycode), IntPtr.Zero);
                                }
                                else if (this.Visibility == Visibility.Visible)
                                {
                                    Close();
                                }
                            }
                            else if (!mPausedHotKey)
                            {
                                if (valueLower == "{run}" || valueLower == "{wiki}")
                                {
                                    mClipboardBlock = true;

                                    System.Windows.Forms.SendKeys.SendWait("^{c}");
                                    Thread.Sleep(300);

                                    try
                                    {
                                        if (Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Text))
                                        {
                                            ItemTextParser(GetClipText(Clipboard.ContainsText(TextDataFormat.UnicodeText)), valueLower != "{wiki}");

                                            if (valueLower == "{wiki}")
                                                Button_Click_4(null, new RoutedEventArgs());
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }

                                    mClipboardBlock = false;
                                }
                                else if (valueLower.IndexOf("{enter}") == 0)
                                {
                                    Regex regex = new Regex(@"{enter}", RegexOptions.IgnoreCase);
                                    string tmp = regex.Replace(shortcut.Value, "" + '\n');
                                    string[] strs = tmp.Trim().Split('\n');

                                    for (int i = 0; i < strs.Length; i++)
                                    {
                                        SetClipText(strs[i], TextDataFormat.UnicodeText);
                                        Thread.Sleep(300);
                                        System.Windows.Forms.SendKeys.SendWait("{enter}");
                                        System.Windows.Forms.SendKeys.SendWait("^{a}");
                                        System.Windows.Forms.SendKeys.SendWait("^{v}");
                                        System.Windows.Forms.SendKeys.SendWait("{enter}");
                                    }
                                }
                                else if (valueLower.IndexOf("{link}") == 0)
                                {
                                    Regex regex = new Regex(@"{link}", RegexOptions.IgnoreCase);
                                    string tmp = regex.Replace(shortcut.Value, "" + '\n');
                                    string[] strs = tmp.Trim().Split('\n');
                                    if (strs.Length > 0) Process.Start(strs[0]);
                                }
                                else if (valueLower.IndexOf("{grid:quad}") == 0 || valueLower.IndexOf("{grid:stash}") == 0)
                                {
                                    IntPtr pHwnd = Native.FindWindow(null, Title + " - " + "{grid:stash}");
                                    if (pHwnd.ToInt32() != 0)
                                    {
                                        Native.SendMessage(pHwnd, /* WM_CLOSE = */ 0x10, IntPtr.Zero, IntPtr.Zero);
                                    }
                                    else
                                    {
                                        WinGrid winGrid = new WinGrid(valueLower.IndexOf("{grid:quad}") == 0, findHwnd);
                                        winGrid.Title = Title + " - " + "{grid:stash}";
                                        winGrid.Show();
                                    }
                                }
                                else if (valueLower.IndexOf(".jpg") > 0)
                                {
                                    IntPtr pHwnd = Native.FindWindow(null, popWinTitle);
                                    if (pHwnd.ToInt32() != 0)
                                        Native.SendMessage(pHwnd, /* WM_CLOSE = */ 0x10, IntPtr.Zero, IntPtr.Zero);

                                    WinPopup winPopup = new WinPopup(shortcut.Value);

                                    if ((shortcut.Position ?? "") != "")
                                    {
                                        string[] strs = shortcut.Position.ToLower().Split('x');
                                        winPopup.WindowStartupLocation = WindowStartupLocation.Manual;
                                        winPopup.Left = double.Parse(strs[0]);
                                        winPopup.Top = double.Parse(strs[1]);
                                    }

                                    winPopup.Title = popWinTitle;
                                    winPopup.Show();
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        ForegroundMessage("잘못된 단축키 명령입니다.", "단축키 에러", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    handled = true;
                }

                mHotkeyProcBlock = false;
            }

            return IntPtr.Zero;
        }
    }
}
