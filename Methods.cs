using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PoeTradeSearch
{
    public partial class MainWindow : Window
    {
        private bool Setting()
        {
#if DEBUG
            string path = System.IO.Path.GetFullPath(@"..\..\") + "_POE_Data\\";
#else
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            path = path.Remove(path.Length - 4) + "Data\\";
#endif
            FileStream fs = null;
            try
            {
                fs = new FileStream(path + "Config.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mConfigData = Json.Deserialize<ConfigData>(json);
                }

                if (mConfigData.Options.SearchPriceCount > 80)
                    mConfigData.Options.SearchPriceCount = 80;

                //-----------------------------

                if (mCreateDatabase)
                {
                    File.Delete(path + "Bases.txt");
                    File.Delete(path + "Words.txt");
                    File.Delete(path + "Prophecies.txt"); ;
                    File.Delete(path + "Monsters.txt");
                    File.Delete(path + "FiltersKO.txt");
                    File.Delete(path + "FiltersEN.txt");

                    if (!BaseDataUpdates(path) || !FilterDataUpdates(path))
                        throw new UnauthorizedAccessException("failed to create database");
                }

                fs = new FileStream(path + "Bases.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    BaseData data = Json.Deserialize<BaseData>(json);
                    mBaseDatas = new List<BaseResultData>();
                    mBaseDatas.AddRange(data.Result[0].Data);
                }

                fs = new FileStream(path + "Words.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    WordData data = Json.Deserialize<WordData>(json);
                    mWordDatas = new List<WordeResultData>();
                    mWordDatas.AddRange(data.Result[0].Data);
                }

                fs = new FileStream(path + "Prophecies.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    BaseData data = Json.Deserialize<BaseData>(json);
                    mProphecyDatas = new List<BaseResultData>();
                    mProphecyDatas.AddRange(data.Result[0].Data);
                }

                fs = new FileStream(path + "Monsters.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    BaseData data = Json.Deserialize<BaseData>(json);
                    mMonsterDatas = new List<BaseResultData>();
                    mMonsterDatas.AddRange(data.Result[0].Data);
                }

                fs = new FileStream(path + "FiltersKO.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mFilterData[0] = Json.Deserialize<FilterData>(json);
                }

                fs = new FileStream(path + "FiltersEN.txt", FileMode.Open);
                using (StreamReader reader = new StreamReader(fs))
                {
                    fs = null;
                    string json = reader.ReadToEnd();
                    mFilterData[1] = Json.Deserialize<FilterData>(json);
                }
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

        private void ForegroundMessage(string message, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            MessageBox.Show(Application.Current.MainWindow, message, caption, button, icon);
            Native.SetForegroundWindow(Native.FindWindow(RS.PoeClass, RS.PoeCaption));
        }

        private void SetSearchButtonText(bool is_kor)
        {
            bool isExchange = bdExchange.Visibility == Visibility.Visible && (cbOrbs.SelectedIndex > 0 || cbSplinters.SelectedIndex > 0);
            btnSearch.Content = "거래소에서 " + (isExchange ? "대량 " : "") + "찾기 (" + (is_kor ? "한글" : "영어") + ")";
        }

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

            cbAiiCheck.IsChecked = false;
            ckLv.IsChecked = false;
            ckQuality.IsChecked = false;
            ckSocket.IsChecked = false;

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

            ckLv.Content = RS.Lv[0];
            ckLv.FontWeight = FontWeights.Normal;
            ckLv.Foreground = Synthesis.Foreground;
            ckLv.BorderBrush = Synthesis.BorderBrush;
            ckQuality.FontWeight = FontWeights.Normal;
            ckQuality.Foreground = Synthesis.Foreground;
            ckQuality.BorderBrush = Synthesis.BorderBrush;
            lbSocketBackground.Visibility = Visibility.Hidden;

            lbDPS.Content = "옵션";
            Synthesis.Content = "결합";

            cbRarity.Items.Clear();
            cbRarity.Items.Add(RS.All[0]);
            cbRarity.Items.Add(RS.lRarity["Normal"]);
            cbRarity.Items.Add(RS.lRarity["Magic"]);
            cbRarity.Items.Add(RS.lRarity["Rare"]);
            cbRarity.Items.Add(RS.lRarity["Unique"]);

            tabControl1.SelectedIndex = 0;
            cbPriceListCount.SelectedIndex = (int)Math.Ceiling(mConfigData.Options.SearchPriceCount / 20) - 1;
            tbPriceFilterMin.Text = mConfigData.Options.SearchPriceMin > 0 ? mConfigData.Options.SearchPriceMin.ToString() : "";

            for (int i = 0; i < 10; i++)
            {
                ((TextBox)this.FindName("tbOpt" + i)).Text = "";
                ((TextBox)this.FindName("tbOpt" + i)).Background = SystemColors.WindowBrush;
                ((TextBox)this.FindName("tbOpt" + i + "_0")).Text = "";
                ((TextBox)this.FindName("tbOpt" + i + "_1")).Text = "";
                ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsEnabled = true;
                ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = false;
                ((CheckBox)this.FindName("tbOpt" + i + "_3")).IsChecked = false;
                ((CheckBox)this.FindName("tbOpt" + i + "_3")).Visibility = Visibility.Hidden;
                ((TextBox)this.FindName("tbOpt" + i)).BorderBrush = SystemColors.ActiveBorderBrush;
                ((TextBox)this.FindName("tbOpt" + i + "_0")).BorderBrush = SystemColors.ActiveBorderBrush;
                ((TextBox)this.FindName("tbOpt" + i + "_1")).BorderBrush = SystemColors.ActiveBorderBrush;
                ((CheckBox)this.FindName("tbOpt" + i + "_2")).BorderBrush = SystemColors.ActiveBorderBrush;
                ((CheckBox)this.FindName("tbOpt" + i + "_3")).BorderBrush = SystemColors.ActiveBorderBrush;

                ((ComboBox)this.FindName("cbOpt" + i)).Items.Clear();
                // ((ComboBox)this.FindName("cbOpt" + i)).ItemsSource = new List<FilterEntrie>();
                ((ComboBox)this.FindName("cbOpt" + i)).DisplayMemberPath = "Name";
                ((ComboBox)this.FindName("cbOpt" + i)).SelectedValuePath = "Name";
            }
        }

        private void ItemTextParser(string itemText, bool isWinShow = true)
        {
            string itemName = "";
            string itemType = "";
            string itemRarity = "";
            string itemInherits = "";
            string itemID = "";

            try
            {
                string[] asData = (itemText ?? "").Trim().Split(new string[] { "--------" }, StringSplitOptions.None);

                if (asData.Length > 1 && (asData[0].IndexOf(RS.Rarity[0] + ": ") == 0 || asData[0].IndexOf(RS.Rarity[1] + ": ") == 0))
                {
                    byte z = (byte)(asData[0].IndexOf(RS.Rarity[0] + ": ") == 0 ? 0 : 1);
                    if (mConfigData.Options.Server != "en" && mConfigData.Options.Server != "ko") RS.ServerLang = z;

                    ResetControls();
                    mItemBaseName = new ItemBaseName();
                    mItemBaseName.LangType = z;

                    string[] asOpt = asData[0].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

                    itemRarity = asOpt[0].Split(':')[1].Trim();
                    itemName = Regex.Replace(asOpt[1] ?? "", @"<<set:[A-Z]+>>", "");
                    itemType = asOpt.Length > 2 && asOpt[2] != "" ? Regex.Replace(asOpt[2] ?? "", @"<<set:[A-Z]+>>", "") : itemName;

                    if (asOpt.Length == 2) itemName = "";
                    if (z == 1 && RS.lRarity.ContainsKey(itemRarity)) itemRarity = RS.lRarity[itemRarity];

                    int k = 0, baki = 0, notImpCnt = 0;
                    double attackSpeedIncr = 0, PhysicalDamageIncr = 0;
                    bool is_prophecy = false, is_map_fragment = false, is_met_entrails = false;

                    List<Itemfilter> itemfilters = new List<Itemfilter>();

                    Dictionary<string, string> lItemOption = new Dictionary<string, string>()
                    {
                        { RS.Quality[z], "" }, { RS.Lv[z], "" }, { RS.ItemLv[z], "" }, { RS.CharmLv[z], "" }, { RS.MaTier[z], "" }, { RS.Socket[z], "" },
                        { RS.PhysicalDamage[z], "" }, { RS.ElementalDamage[z], "" }, { RS.ChaosDamage[z], "" }, { RS.AttacksPerSecond[z], "" },
                        { RS.Shaper[z], "" }, { RS.Elder[z], "" }, { RS.Crusader[z], "" }, { RS.Redeemer[z], "" }, { RS.Hunter[z], "" }, { RS.Warlord[z], "" },
                        { RS.Synthesis[z], "" }, { RS.Corrupt[z], "" }, { RS.Unidentify[z], "" }, { RS.Vaal[z], "" }, { RS.Genus[z], "" }, { RS.Group[z], "" }
                    };

                    for (int i = 1; i < asData.Length; i++)
                    {
                        asOpt = asData[i].Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None);

                        for (int j = 0; j < asOpt.Length; j++)
                        {
                            if (asOpt[j].Trim() == "") continue;

                            string[] asTmp = asOpt[j].Split(':');

                            if (lItemOption.ContainsKey(asTmp[0]))
                            {
                                if (lItemOption[asTmp[0]] == "")
                                    lItemOption[asTmp[0]] = asTmp.Length > 1 ? asTmp[1] : "_TRUE_";
                            }
                            else
                            {
                                if (itemRarity == RS.lRarity["Gem"] && (RS.Vaal[z] + " " + itemType) == asTmp[0])
                                    lItemOption[RS.Vaal[z]] = "_TRUE_";
                                else if (!is_prophecy && asTmp[0].IndexOf(RS.ChkProphecy[z]) == 0)
                                    is_prophecy = true;
                                else if (!is_map_fragment && asTmp[0].IndexOf(RS.ChkMapFragment[z]) == 0)
                                    is_map_fragment = true;
                                else if (!is_met_entrails && asTmp[0].IndexOf(RS.ChkMetEntrails[z]) == 0)
                                    is_met_entrails = true;
                                else if (lItemOption[RS.ItemLv[z]] != "" && k < 10)
                                {
                                    double min = 99999, max = 99999;
                                    bool resistance = false;
                                    bool crafted = asOpt[j].IndexOf("(crafted)") > -1;

                                    string input = Regex.Replace(asOpt[j], @" \([a-zA-Z]+\)", "");
                                    input = Regex.Escape(Regex.Replace(input, @"[+-]?[0-9]+\.[0-9]+|[+-]?[0-9]+", "#"));
                                    input = Regex.Replace(input, @"\\#", "[+-]?([0-9]+\\.[0-9]+|[0-9]+|\\#)");
                                    //input = input + (is_captured_beast ? "\\(" + RS.Captured[z] + "\\)" : "");

                                    FilterResultEntrie filter = null;
                                    Regex rgx = new Regex("^" + input + "$", RegexOptions.IgnoreCase);

                                    foreach (FilterResult filterResult in mFilterData[z].Result)
                                    {
                                        FilterResultEntrie[] entries = Array.FindAll(filterResult.Entries, x => rgx.IsMatch(x.Text));
                                        if (entries.Length > 0)
                                        {
                                            MatchCollection matches1 = Regex.Matches(asOpt[j], @"[-]?[0-9]+\.[0-9]+|[-]?[0-9]+");
                                            foreach (FilterResultEntrie entrie in entries)
                                            {
                                                // 장비 옵션 (특정) 이 겹칠경우 (특정) 대신 일반 옵션 값 사용 (후에 json 만들때 다시 검사함)
                                                if (entries.Length > 1 && entrie.Part != null)
                                                    continue;

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
                                                    ((ComboBox)this.FindName("cbOpt" + k)).Items.Add(new FilterEntrie(entrie.ID, filterResult.Label));

                                                    if (filter == null)
                                                    {
                                                        string[] id_split = entrie.ID.Split('.');
                                                        resistance = id_split.Length == 2 && RS.lResistance.ContainsKey(id_split[1]);
                                                        filter = entrie;

                                                        MatchCollection matches = Regex.Matches(asOpt[j], @"[-]?[0-9]+\.[0-9]+|[-]?[0-9]+");
                                                        min = isMin && matches.Count > idxMin ? StrToDouble(((Match)matches[idxMin]).Value, 99999) : 99999;
                                                        max = isMax && idxMin < idxMax && matches.Count > idxMax ? StrToDouble(((Match)matches[idxMax]).Value, 99999) : 99999;
                                                    }

                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    if (filter != null)
                                    {
                                        ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = RS.lFilterType["crafted"];
                                        int selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;

                                        if (crafted && selidx > -1)
                                        {
                                            ((TextBox)this.FindName("tbOpt" + k)).BorderBrush = System.Windows.Media.Brushes.Blue;
                                            ((TextBox)this.FindName("tbOpt" + k + "_0")).BorderBrush = System.Windows.Media.Brushes.Blue;
                                            ((TextBox)this.FindName("tbOpt" + k + "_1")).BorderBrush = System.Windows.Media.Brushes.Blue;
                                            ((CheckBox)this.FindName("tbOpt" + k + "_2")).BorderBrush = System.Windows.Media.Brushes.Blue;
                                            ((CheckBox)this.FindName("tbOpt" + k + "_3")).BorderBrush = System.Windows.Media.Brushes.Blue;
                                            ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex = selidx;
                                        }
                                        else
                                        {
                                            ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = RS.lFilterType["pseudo"];
                                            selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;

                                            if (selidx == -1 && ((ComboBox)this.FindName("cbOpt" + k)).Items.Count > 0)
                                            {
                                                FilterEntrie filterEntrie = (FilterEntrie)((ComboBox)this.FindName("cbOpt" + k)).Items[0];
                                                string[] id_split = filterEntrie.ID.Split('.');
                                                if (id_split.Length == 2 && RS.lPseudo.ContainsKey(id_split[1]))
                                                {
                                                    ((ComboBox)this.FindName("cbOpt" + k)).Items.Add(new FilterEntrie("pseudo." + RS.lPseudo[id_split[1]], RS.lFilterType["pseudo"]));
                                                }
                                            }

                                            selidx = -1;

                                            /* if (is_captured_beast)
                                            {
                                                ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = RS.lFilterType["monster"];
                                                selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;
                                            }
                                            else */
                                            
                                            if (mConfigData.Options.AutoSelectPseudo)
                                            {
                                                ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = RS.lFilterType["pseudo"];
                                                selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;
                                            }

                                            if (selidx == -1)
                                            {
                                                ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = RS.lFilterType["explicit"];
                                                selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;
                                            }

                                            if (selidx == -1)
                                            {
                                                ((ComboBox)this.FindName("cbOpt" + k)).SelectedValue = RS.lFilterType["fractured"];
                                                selidx = ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex;
                                            }

                                            if (selidx == -1 && ((ComboBox)this.FindName("cbOpt" + k)).Items.Count == 1)
                                            {
                                                selidx = 0;
                                            }

                                            ((ComboBox)this.FindName("cbOpt" + k)).SelectedIndex = selidx;
                                        }

                                        if (i != baki)
                                        {
                                            baki = i;
                                            notImpCnt = 0;
                                        }

                                        ((TextBox)this.FindName("tbOpt" + k)).Text = filter.Text;
                                        ((CheckBox)this.FindName("tbOpt" + k + "_3")).Visibility = resistance ? Visibility.Visible : Visibility.Hidden;

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
                                            string[] split = filter.ID.Split('.');
                                            bool defMaxPosition = split.Length == 2 && RS.lDefaultPosition.ContainsKey(split[1]);
                                            if ((defMaxPosition && min > 0 && max == 99999) || (!defMaxPosition && min < 0 && max == 99999))
                                            {
                                                max = min;
                                                min = 99999;
                                            }
                                        }

                                        ((TextBox)this.FindName("tbOpt" + k + "_0")).Text = min == 99999 ? "" : min.ToString();
                                        ((TextBox)this.FindName("tbOpt" + k + "_1")).Text = max == 99999 ? "" : max.ToString();

                                        Itemfilter itemfilter = new Itemfilter
                                        {
                                            id = filter.Type,
                                            text = filter.Text,
                                            max = max,
                                            min = min,
                                            disabled = true
                                        };

                                        itemfilters.Add(itemfilter);

                                        if (filter.Text == RS.AttackSpeedIncr[z] && min > 0 && min < 999)
                                        {
                                            attackSpeedIncr += min;
                                        }
                                        else if (filter.Text == RS.PhysicalDamageIncr[z] && min > 0 && min < 9999)
                                        {
                                            PhysicalDamageIncr += min;
                                        }

                                        k++;
                                        notImpCnt++;
                                    }
                                }
                            }
                        }
                    }

                    bool is_blight = false;
                    bool is_unIdentify = lItemOption[RS.Unidentify[z]] == "_TRUE_";
                    bool is_map = lItemOption[RS.MaTier[z]] != "";
                    bool is_gem = itemRarity == RS.lRarity["Gem"];
                    bool is_currency = itemRarity == RS.lRarity["Currency"];
                    bool is_divinationCard = itemRarity == RS.lRarity["Divination Card"];
                    bool is_captured_beast = lItemOption[RS.Genus[z]] != "" && lItemOption[RS.Group[z]] != "";

                    if (is_map || is_currency) is_map_fragment = false;
                    bool is_detail = is_gem || is_currency || is_divinationCard || is_prophecy || is_map_fragment;

                    if (lItemOption[RS.Socket[z]] != "")
                    {
                        string socket = lItemOption[RS.Socket[z]];
                        int sckcnt = socket.Replace(" ", "-").Split('-').Length - 1;
                        string[] scklinks = socket.Split(' ');

                        int lnkcnt = 0;
                        for (int s = 0; s < scklinks.Length; s++)
                        {
                            if (lnkcnt < scklinks[s].Length)
                                lnkcnt = scklinks[s].Length;
                        }

                        int link = lnkcnt < 3 ? 0 : lnkcnt - (int)Math.Ceiling((double)lnkcnt / 2) + 1;
                        tbSocketMin.Text = sckcnt.ToString();
                        tbLinksMin.Text = link > 0 ? link.ToString() : "";
                        ckSocket.IsChecked = link > 4;
                    }

                    BaseResultData tmpBaseType = null;

                    if (is_met_entrails)
                    {
                        itemID = itemInherits = "Entrailles/Entrails";
                        string[] tmp = itemType.Split(' ');
                        itemType = RS.Metamorph[z] + " " + tmp[tmp.Length - 1];
                    }
                    else if (is_prophecy)
                    {
                        itemRarity = RS.lRarity["Prophecy"];
                        tmpBaseType = mProphecyDatas.Find(x => (z == 1 ? x.NameEn : x.NameKo) == itemType);
                        if (tmpBaseType != null)
                        {
                            itemID = tmpBaseType.ID;
                            itemInherits = tmpBaseType.InheritsFrom;
                        }
                    }
                    else if (is_captured_beast)
                    {
                        tmpBaseType = mMonsterDatas.Find(x => (z == 1 ? x.NameEn : x.NameKo) == itemType);
                        if (tmpBaseType != null)
                        {
                            itemID = tmpBaseType.ID;
                            itemInherits = tmpBaseType.InheritsFrom;
                        }
                    }
                    else
                    {
                        if (is_gem && lItemOption[RS.Corrupt[z]] == "_TRUE_" && lItemOption[RS.Vaal[z]] == "_TRUE_")
                        {
                            tmpBaseType = mBaseDatas.Find(x => (z == 1 ? x.NameEn : x.NameKo) == RS.Vaal[z] + " " + itemType);
                            if (tmpBaseType != null)
                                itemType = z == 1 ? tmpBaseType.NameEn : tmpBaseType.NameKo;
                        }

                        if (!is_unIdentify && itemRarity == RS.lRarity["Magic"])
                            itemType = itemType.Split(new string[] { z == 1 ? " of " : " - " }, StringSplitOptions.None)[0].Trim();

                        if ((is_unIdentify || itemRarity == RS.lRarity["Normal"]) && itemType.Length > 4 && itemType.IndexOf(RS.Higher[z] + " ") == 0)
                            itemType = itemType.Substring(z == 1 ? 9 : 3);

                        if (is_map && itemType.Length > 5)
                        {
                            if (itemType.IndexOf(RS.Blighted[z] + " ") == 0)
                            {
                                is_blight = true;
                                itemType = itemType.Substring(z == 1 ? 9 : 6);
                            }

                            if (itemType.Substring(0, z == 1 ? 7 : 4) == RS.Shaped[z] + " ")
                                itemType = itemType.Substring(z == 1 ? 7 : 4);
                        }
                        else if (lItemOption[RS.Synthesis[z]] == "_TRUE_")
                        {
                            if (itemType.Substring(0, z == 1 ? 12 : 4) == RS.Synthesised[z] + " ")
                                itemType = itemType.Substring(z == 1 ? 12 : 4);
                        }

                        if (!is_unIdentify && itemRarity == RS.lRarity["Magic"])
                        {
                            string[] tmp = itemType.Split(' ');

                            if (tmp.Length > 1)
                            {
                                for (int i = 0; i < tmp.Length - 1; i++)
                                {
                                    tmp[i] = "";
                                    string tmp2 = string.Join(" ", tmp).Trim();

                                    tmpBaseType = mBaseDatas.Find(x => (z == 1 ? x.NameEn : x.NameKo) == tmp2);
                                    if (tmpBaseType != null)
                                    {
                                        itemType = z == 1 ? tmpBaseType.NameEn : tmpBaseType.NameKo;
                                        itemID = tmpBaseType.ID;
                                        itemInherits = tmpBaseType.InheritsFrom;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (itemInherits == "")
                    {
                        tmpBaseType = mBaseDatas.Find(x => (z == 1 ? x.NameEn : x.NameKo) == itemType);
                        if (tmpBaseType != null)
                        {
                            itemID = tmpBaseType.ID;
                            itemInherits = tmpBaseType.InheritsFrom;
                        }
                    }

                    mItemBaseName.Inherits = itemInherits.Split('/');
                    string inherit = mItemBaseName.Inherits[0];
                    string sub_inherit = mItemBaseName.Inherits.Length > 1 ? mItemBaseName.Inherits[1] : "";
                    string item_quality = Regex.Replace(lItemOption[RS.Quality[z]].Trim(), "[^0-9]", "");

                    bool is_essences = inherit == "Currency" && itemID.IndexOf("Currency/CurrencyEssence") == 0;
                    bool is_incubations = inherit == "Legion" && sub_inherit == "Incubator";
                    bool by_type = inherit == "Weapons" || inherit == "Quivers" || inherit == "Armours" || inherit == "Amulets" || inherit == "Rings" || inherit == "Belts";
                    is_detail = is_detail || is_incubations || (!is_detail && (inherit == "MapFragments" || inherit == "UniqueFragments" || inherit == "Labyrinth"));

                    mItemBaseName.NameEN = "";
                    mItemBaseName.NameKR = "";

                    if (is_detail)
                    {
                        tmpBaseType = is_prophecy   ? mProphecyDatas.Find(x => (z == 1 ? x.NameEn : x.NameKo) == itemType)
                                                    : mBaseDatas.Find(x => (z == 1 ? x.NameEn : x.NameKo) == itemType);

                        mItemBaseName.TypeEN = tmpBaseType == null ? itemType : tmpBaseType.NameEn;
                        mItemBaseName.TypeKR = tmpBaseType == null ? itemType : tmpBaseType.NameKo;
                    }
                    else
                    {
                        tmpBaseType = is_captured_beast ? mMonsterDatas.Find(x => (z == 1 ? x.NameEn : x.NameKo) == itemType)
                                                        : mBaseDatas.Find(x => (z == 1 ? x.NameEn : x.NameKo) == itemType);

                        mItemBaseName.TypeEN = tmpBaseType == null ? itemType : tmpBaseType.NameEn;
                        mItemBaseName.TypeKR = tmpBaseType == null ? itemType : (is_captured_beast ? tmpBaseType.NameEn : tmpBaseType.NameKo);

                        if (!is_captured_beast)
                        {
                            WordeResultData wordData = mWordDatas.Find(x => (z == 1 ? x.NameEn : x.NameKo) == itemName);
                            mItemBaseName.NameEN = wordData == null ? itemName : wordData.NameEn;
                            mItemBaseName.NameKR = wordData == null ? itemName : wordData.NameKo;

                            if (wordData == null && itemRarity == RS.lRarity["Rare"])
                            {
                                string[] tmp = itemName.Split(' ');
                                if (tmp.Length > 1)
                                {
                                    int idx = 0;
                                    string tmp2 = "";

                                    for (int i = 0; i < tmp.Length; i++)
                                    {
                                        tmp2 += " " + tmp[i];
                                        tmp2 = tmp2.TrimStart();
                                        wordData = mWordDatas.Find(x => (z == 1 ? x.NameEn : x.NameKo) == tmp2);
                                        if (wordData != null)
                                        {
                                            idx = i + 1;
                                            mItemBaseName.NameEN = wordData.NameEn;
                                            mItemBaseName.NameKR = wordData.NameKo;
                                            break;
                                        }
                                    }

                                    tmp2 = "";
                                    for (int i = idx; i < tmp.Length; i++)
                                    {
                                        tmp2 += " " + tmp[i];
                                        wordData = mWordDatas.Find(x => (z == 1 ? x.NameEn : x.NameKo) == tmp2);
                                        if (wordData != null)
                                        {
                                            mItemBaseName.NameEN += wordData.NameEn;
                                            mItemBaseName.NameKR += wordData.NameKo;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (is_detail)
                    {
                        try
                        {
                            if (is_essences || is_incubations || inherit == "Gems" || inherit == "UniqueFragments" || inherit == "Labyrinth")
                            {
                                int i = inherit == "Gems" ? 3 : 1;
                                tkDetail.Text = asData.Length > 2 ? ((inherit == "Gems" || inherit == "Labyrinth" ? asData[i] : "") + asData[i + 1]) : "";
                            }
                            else
                            {
                                if ((tmpBaseType?.Detail ?? "") != "")
                                    tkDetail.Text = "세부사항:" + '\n' + '\n' + tmpBaseType.Detail.Replace("\\n", "" + '\n');
                                else
                                {
                                    int i = inherit == "Delve" ? 3 : (is_divinationCard || inherit == "Currency" ? 2 : 1);
                                    tkDetail.Text = asData.Length > (i + 1) ? asData[i] + asData[i + 1] : asData[asData.Length - 1];

                                    if (asData.Length > (i + 1))
                                    {
                                        int v = asData[i - 1].TrimStart().IndexOf(z == 1 ? "Apply: " : "적용: ");
                                        tkDetail.Text += v > -1 ? "" + '\n' + '\n' + (asData[i - 1].TrimStart().Split('\n')[v == 0 ? 0 : 1].TrimEnd()) : "";
                                    }
                                }
                            }

                            tkDetail.Text = Regex.Replace(
                                tkDetail.Text.Replace(RS.SClickSplitItem[z], ""), 
                                "<(uniqueitem|prophecy|divination|gemitem|magicitem|rareitem|whiteitem|corrupted|default|normal|augmented|size:[0-9]+)>", 
                                ""
                            );
                        }
                        catch { }
                    }
                    else
                    {
                        int Imp_cnt = itemfilters.Count - ((itemRarity == RS.lRarity["Normal"] || is_unIdentify) ? 0 : notImpCnt);

                        for (int i = 0; i < itemfilters.Count; i++)
                        {
                            Itemfilter ifilter = itemfilters[i];

                            if (i < Imp_cnt)
                            {
                                ((TextBox)this.FindName("tbOpt" + i)).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                ((TextBox)this.FindName("tbOpt" + i + "_0")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                ((TextBox)this.FindName("tbOpt" + i + "_1")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                ((CheckBox)this.FindName("tbOpt" + i + "_2")).BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = false;
                                ((CheckBox)this.FindName("tbOpt" + i + "_3")).BorderBrush = System.Windows.Media.Brushes.DarkRed;

                                itemfilters[i].disabled = true;

                                ((ComboBox)this.FindName("cbOpt" + i)).SelectedValue = RS.lFilterType["enchant"];
                                if (((ComboBox)this.FindName("cbOpt" + i)).SelectedIndex == -1)
                                    ((ComboBox)this.FindName("cbOpt" + i)).SelectedValue = RS.lFilterType["implicit"];
                            }
                            else if (inherit != "" && (string)((ComboBox)this.FindName("cbOpt" + i)).SelectedValue != RS.lFilterType["crafted"])
                            {
                                if (
                                    (mConfigData.Options.AutoCheckUnique && itemRarity == RS.lRarity["Unique"])
                                    || (Array.Find(mConfigData.Checked, x => x.Text == ifilter.text && x.ID.IndexOf(inherit + "/") > -1) != null)
                                )
                                {
                                    ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = true;
                                    itemfilters[i].disabled = false;
                                }
                            }
                        }

                        // DPS 계산 POE-TradeMacro 참고
                        if (!is_unIdentify && inherit == "Weapons")
                        {
                            double PhysicalDPS = DamageToDPS(lItemOption[RS.PhysicalDamage[z]]);
                            double ElementalDPS = DamageToDPS(lItemOption[RS.ElementalDamage[z]]);
                            double ChaosDPS = DamageToDPS(lItemOption[RS.ChaosDamage[z]]);

                            double quality20Dps = item_quality == "" ? 0 : StrToDouble(item_quality, 0);
                            double attacksPerSecond = StrToDouble(Regex.Replace(lItemOption[RS.AttacksPerSecond[z]], @"\([a-zA-Z]+\)", "").Trim(), 0);

                            if (attackSpeedIncr > 0)
                            {
                                double baseAttackSpeed = attacksPerSecond / (attackSpeedIncr / 100 + 1);
                                double modVal = baseAttackSpeed % 0.05;
                                baseAttackSpeed += modVal > 0.025 ? (0.05 - modVal) : -modVal;
                                attacksPerSecond = baseAttackSpeed * (attackSpeedIncr / 100 + 1);
                            }

                            PhysicalDPS = (PhysicalDPS / 2) * attacksPerSecond;
                            ElementalDPS = (ElementalDPS / 2) * attacksPerSecond;
                            ChaosDPS = (ChaosDPS / 2) * attacksPerSecond;

                            //20 퀄리티 보다 낮을땐 20 퀄리티 기준으로 계산
                            quality20Dps = quality20Dps < 20 ? PhysicalDPS * (PhysicalDamageIncr + 120) / (PhysicalDamageIncr + quality20Dps + 100) : 0;
                            PhysicalDPS = quality20Dps > 0 ? quality20Dps : PhysicalDPS;

                            lbDPS.Content = "DPS: P." + Math.Round(PhysicalDPS, 2).ToString() +
                                            " + E." + Math.Round(ElementalDPS, 2).ToString() +
                                            " = T." + Math.Round(PhysicalDPS + ElementalDPS + ChaosDPS, 2).ToString();
                        }
                    }
                    
                    if (RS.ServerLang == 1)
                        cbName.Content = (mItemBaseName.NameEN + " " + mItemBaseName.TypeEN).Trim();
                    else
                        cbName.Content = (Regex.Replace(itemName, @"\([a-zA-Z\s']+\)$", "") + " " + Regex.Replace(itemType, @"\([a-zA-Z\s']+\)$", "")).Trim();

                    cbName.IsChecked = (itemRarity != RS.lRarity["Magic"] && itemRarity != RS.lRarity["Rare"]) || !(by_type && mConfigData.Options.SearchByType);

                    cbRarity.SelectedValue = itemRarity;
                    if (cbRarity.SelectedIndex == -1)
                    {
                        cbRarity.Items.Clear();
                        cbRarity.Items.Add(itemRarity);
                        cbRarity.SelectedIndex = 0;
                    }
                    else if ((string)cbRarity.SelectedValue == RS.lRarity["Normal"])
                    {
                        cbRarity.SelectedIndex = 0;
                    }

                    bool IsExchangeCurrency = inherit == "Currency" && RS.lExchangeCurrency[z].ContainsKey(itemType);
                    bdExchange.Visibility = !is_gem && (is_detail || IsExchangeCurrency) ? Visibility.Visible : Visibility.Hidden;
                    bdExchange.IsEnabled = IsExchangeCurrency;

                    if (bdExchange.Visibility == Visibility.Hidden)
                    {
                        tbLvMin.Text = Regex.Replace(lItemOption[is_gem ? RS.Lv[z] : RS.ItemLv[z]].Trim(), "[^0-9]", "");
                        tbQualityMin.Text = item_quality;

                        string[] Influences = { RS.Shaper[z], RS.Elder[z], RS.Crusader[z], RS.Redeemer[z], RS.Hunter[z], RS.Warlord[z] };
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

                        if (lItemOption[RS.Corrupt[z]] == "_TRUE_")
                        {
                            cbCorrupt.BorderThickness = new Thickness(2);
                            cbCorrupt.FontWeight = FontWeights.Bold;
                            cbCorrupt.Foreground = System.Windows.Media.Brushes.DarkRed;
                        }

                        Synthesis.IsChecked = (is_map && is_blight) || lItemOption[RS.Synthesis[z]] == "_TRUE_";

                        if (cbInfluence1.SelectedIndex > 0) cbInfluence1.BorderThickness = new Thickness(2);
                        if (cbInfluence2.SelectedIndex > 0) cbInfluence2.BorderThickness = new Thickness(2);

                        if (is_map)
                        {
                            tbLvMin.Text = tbLvMax.Text = lItemOption[RS.MaTier[z]];
                            ckLv.Content = "등급";
                            ckLv.IsChecked = true;
                            Synthesis.Content = "역병";
                        }
                        else if (is_gem)
                        {
                            ckLv.IsChecked = lItemOption[RS.Lv[z]].IndexOf(" (" + RS.Max[z]) > 0;
                            ckQuality.IsChecked = ckLv.IsChecked == true && item_quality != "" && int.Parse(item_quality) > 19;
                        }
                        else if (by_type || inherit == "Flasks")
                        {
                            if (tbQualityMin.Text != "" && int.Parse(tbQualityMin.Text) > 20)
                            {
                                ckQuality.FontWeight = FontWeights.Bold;
                                ckQuality.Foreground = System.Windows.Media.Brushes.DarkRed;
                                ckQuality.BorderBrush = System.Windows.Media.Brushes.DarkRed;
                            }

                            if (by_type)
                            {
                                if (tbLvMin.Text != "" && int.Parse(tbLvMin.Text) > 82)
                                {
                                    ckLv.FontWeight = FontWeights.Bold;
                                    ckLv.Foreground = System.Windows.Media.Brushes.DarkRed;
                                    ckLv.BorderBrush = System.Windows.Media.Brushes.DarkRed;
                                }

                                cbCorrupt.SelectedIndex = mConfigData.Options.AutoSelectCorrupt == "no" ? 2 : (mConfigData.Options.AutoSelectCorrupt == "yes" ? 1 : 0);
                            }
                        }
                    }

                    bdDetail.Visibility = is_detail ? Visibility.Visible : Visibility.Hidden;
                    lbSocketBackground.Visibility = by_type ? Visibility.Hidden : Visibility.Visible;

                    if (isWinShow || this.Visibility == Visibility.Visible)
                    {
                        PriceUpdateThreadWorker(GetItemOptions(), null);
                        SetSearchButtonText(RS.ServerLang == 0);

                        tkPriceInfo1.Foreground = tkPriceInfo2.Foreground = System.Windows.SystemColors.WindowTextBrush;
                        tkPriceCount1.Foreground = tkPriceCount2.Foreground = System.Windows.SystemColors.WindowTextBrush;

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

            itemOption.Influence1 = (byte)cbInfluence1.SelectedIndex;
            itemOption.Influence2 = (byte)cbInfluence2.SelectedIndex;

            // 영향은 첫번째 값이 우선 순위여야 함
            if (itemOption.Influence1 == 0 && itemOption.Influence2 != 0)
            {
                itemOption.Influence1 = itemOption.Influence2;
                itemOption.Influence2 = 0;
            }

            itemOption.Corrupt = (byte)cbCorrupt.SelectedIndex;
            itemOption.Synthesis = Synthesis.IsChecked == true;
            itemOption.ChkSocket = ckSocket.IsChecked == true;
            itemOption.ChkQuality = ckQuality.IsChecked == true;
            itemOption.ChkLv = ckLv.IsChecked == true;
            itemOption.ByType = cbName.IsChecked != true;

            itemOption.SocketMin = StrToDouble(tbSocketMin.Text, 99999);
            itemOption.SocketMax = StrToDouble(tbSocketMax.Text, 99999);
            itemOption.LinkMin = StrToDouble(tbLinksMin.Text, 99999);
            itemOption.LinkMax = StrToDouble(tbLinksMax.Text, 99999);
            itemOption.QualityMin = StrToDouble(tbQualityMin.Text, 99999);
            itemOption.QualityMax = StrToDouble(tbQualityMax.Text, 99999);
            itemOption.LvMin = StrToDouble(tbLvMin.Text, 99999);
            itemOption.LvMax = StrToDouble(tbLvMax.Text, 99999);

            itemOption.PriceMin = tbPriceFilterMin.Text == "" ? 0 : StrToDouble(tbPriceFilterMin.Text, 99999);
            itemOption.RarityAt = (byte)(cbRarity.Items.Count > 1 ? cbRarity.SelectedIndex : 0);

            int total_res_idx = -1;

            for (int i = 0; i < 10; i++)
            {
                Itemfilter itemfilter = new Itemfilter();
                ComboBox comboBox = (ComboBox)this.FindName("cbOpt" + i);

                if (comboBox.SelectedIndex > -1)
                {
                    itemfilter.text = ((TextBox)this.FindName("tbOpt" + i)).Text.Trim();
                    itemfilter.disabled = ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked != true;
                    itemfilter.min = StrToDouble(((TextBox)this.FindName("tbOpt" + i + "_0")).Text, 99999);
                    itemfilter.max = StrToDouble(((TextBox)this.FindName("tbOpt" + i + "_1")).Text, 99999);

                    if (itemfilter.text == RS.TotalResistance)
                    {
                        if (total_res_idx == -1)
                            total_res_idx = itemOption.itemfilters.Count;
                        else
                        {
                            itemOption.itemfilters[total_res_idx].min += itemfilter.min == 99999 ? 0 : itemfilter.min;
                            itemOption.itemfilters[total_res_idx].max += itemfilter.max == 99999 ? 0 : itemfilter.max;
                            continue;
                        }

                        itemfilter.id = "pseudo.pseudo_total_resistance";
                    }
                    else
                    {
                        itemfilter.id = ((FilterEntrie)comboBox.SelectedItem).ID;
                    }

                    itemOption.itemfilters.Add(itemfilter);
                }
            }

            return itemOption;
        }

        private string CreateJson(ItemOption itemOptions, bool useSaleType)
        {
            string BeforeDayToString(int day)
            {
                if (day < 3)
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

                JQ.Name = RS.ServerLang == 1 ? mItemBaseName.NameEN : mItemBaseName.NameKR;
                JQ.Type = RS.ServerLang == 1 ? mItemBaseName.TypeEN : mItemBaseName.TypeKR;

                byte lang_type = mItemBaseName.LangType;
                string Inherit = mItemBaseName.Inherits.Length > 0 ? mItemBaseName.Inherits[0] : "";

                JQ.Stats = new q_Stats[0];
                JQ.Status.Option = "online";

                jsonData.Sort.Price = "asc";

                JQ.Filters.Type.Filters.Category.Option = "any";
                JQ.Filters.Type.Filters.Rarity.Option = itemOptions.RarityAt > 0 ? RS.lRarity.ElementAt(itemOptions.RarityAt - 1).Key.ToLower() : "any";

                JQ.Filters.Trade.Disabled = mConfigData.Options.SearchBeforeDay == 0;
                JQ.Filters.Trade.Filters.Indexed.Option = mConfigData.Options.SearchBeforeDay == 0 ? "any" : BeforeDayToString(mConfigData.Options.SearchBeforeDay);
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

                JQ.Filters.Misc.Filters.Ilvl.Min = itemOptions.ChkLv != true || Inherit == "Gems" || Inherit == "Maps" ? 99999 : itemOptions.LvMin;
                JQ.Filters.Misc.Filters.Ilvl.Max = itemOptions.ChkLv != true || Inherit == "Gems" || Inherit == "Maps" ? 99999 : itemOptions.LvMax;
                JQ.Filters.Misc.Filters.Gem_level.Min = itemOptions.ChkLv == true && Inherit == "Gems" ? itemOptions.LvMin : 99999;
                JQ.Filters.Misc.Filters.Gem_level.Max = itemOptions.ChkLv == true && Inherit == "Gems" ? itemOptions.LvMax : 99999;

                JQ.Filters.Misc.Filters.Shaper.Option = Inherit != "Maps" && (itemOptions.Influence1 == 1 || itemOptions.Influence2 == 1) ? "true" : "any";
                JQ.Filters.Misc.Filters.Elder.Option = Inherit != "Maps" && (itemOptions.Influence1 == 2 || itemOptions.Influence2 == 2) ? "true" : "any";
                JQ.Filters.Misc.Filters.Crusader.Option = Inherit != "Maps" && (itemOptions.Influence1 == 3 || itemOptions.Influence2 == 3) ? "true" : "any";
                JQ.Filters.Misc.Filters.Redeemer.Option = Inherit != "Maps" && (itemOptions.Influence1 == 4 || itemOptions.Influence2 == 4) ? "true" : "any";
                JQ.Filters.Misc.Filters.Hunter.Option = Inherit != "Maps" && (itemOptions.Influence1 == 5 || itemOptions.Influence2 == 5) ? "true" : "any";
                JQ.Filters.Misc.Filters.Warlord.Option = Inherit != "Maps" && (itemOptions.Influence1 == 6 || itemOptions.Influence2 == 6) ? "true" : "any";

                JQ.Filters.Misc.Filters.Synthesis.Option = Inherit != "Maps" && itemOptions.Synthesis == true ? "true" : "any";
                JQ.Filters.Misc.Filters.Corrupted.Option = itemOptions.Corrupt == 1 ? "true" : (itemOptions.Corrupt == 2 ? "false" : "any");

                JQ.Filters.Misc.Disabled = !(
                    itemOptions.ChkQuality == true || (Inherit != "Maps" && itemOptions.Influence1 != 0) || itemOptions.Corrupt != 0
                    || (Inherit != "Maps" && itemOptions.ChkLv == true) || (Inherit != "Maps" && itemOptions.Synthesis == true)
                );

                JQ.Filters.Map.Disabled = !(
                    Inherit == "Maps" && (itemOptions.ChkLv == true || itemOptions.Synthesis == true || itemOptions.Influence1 != 0)
                );

                JQ.Filters.Map.Filters.Tier.Min = itemOptions.ChkLv == true && Inherit == "Maps" ? itemOptions.LvMin : 99999;
                JQ.Filters.Map.Filters.Tier.Max = itemOptions.ChkLv == true && Inherit == "Maps" ? itemOptions.LvMax : 99999;
                JQ.Filters.Map.Filters.Shaper.Option = Inherit == "Maps" && itemOptions.Influence1 == 1 ? "true" : "any";
                JQ.Filters.Map.Filters.Elder.Option = Inherit == "Maps" && itemOptions.Influence1 == 2 ? "true" : "any";
                JQ.Filters.Map.Filters.Blight.Option = Inherit == "Maps" && itemOptions.Synthesis == true ? "true" : "any";

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
                        string type = itemOptions.itemfilters[i].id.Split('.')[0];

                        if (input.Trim() != "")
                        {
                            string type_name = RS.lFilterType[type];
                            bool isPseudo = type_name == RS.lFilterType["pseudo"];

                            FilterResultEntrie filter = null;
                            FilterResult filterResult = Array.Find(mFilterData[lang_type].Result, x => x.Label == type_name);

                            input = Regex.Escape(input).Replace("\\+\\#", "[+]?\\#");

                            // 무기에 경우 pseudo_adds_[a-z]+_damage 옵션은 공격 시 가 붙음
                            if (isPseudo && Inherit == "Weapons" && Regex.IsMatch(id, @"^pseudo.pseudo_adds_[a-z]+_damage$"))
                            {
                                id = id + "_to_attacks";
                            }
                            else if (!isPseudo && (Inherit == "Weapons" || Inherit == "Armours"))
                            {
                                // 장비 전용 옵션 (특정) 인 것인가 검사
                                Regex rgx = new Regex("^" + input + "$", RegexOptions.IgnoreCase);
                                FilterResultEntrie[] tmp_filters = Array.FindAll(filterResult.Entries, x => rgx.IsMatch(x.Text) && x.Type == type && x.Part == Inherit);
                                if (tmp_filters.Length > 0) filter = tmp_filters[0];
                            }

                            if (filter == null)
                            {
                                filter = Array.Find(filterResult.Entries, x => x.ID == id && x.Type == type && x.Part == null);
                            }

                            JQ.Stats[0].Filters[idx] = new q_Stats_filters();
                            JQ.Stats[0].Filters[idx].Value = new q_Min_And_Max();

                            if (filter != null && filter.ID != null && filter.ID.Trim() != "")
                            {
                                JQ.Stats[0].Filters[idx].Disabled = itemOptions.itemfilters[i].disabled == true;
                                JQ.Stats[0].Filters[idx].Value.Min = itemOptions.itemfilters[i].min;
                                JQ.Stats[0].Filters[idx].Value.Max = itemOptions.itemfilters[i].max;
                                JQ.Stats[0].Filters[idx++].Id = filter.ID;
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

                /*
                if (!ckSocket.Dispatcher.CheckAccess())
                else if (ckSocket.Dispatcher.CheckAccess())
                */

                if (RS.lInherit.ContainsKey(Inherit))
                {
                    string option = RS.lInherit[Inherit];

                    if (itemOptions.ByType && Inherit == "Weapons" || Inherit == "Armours")
                    {
                        string[] tmp = mItemBaseName.Inherits;

                        if (tmp.Length > 2)
                        {
                            string tmp2 = tmp[Inherit == "Armours" ? 1 : 2].ToLower();

                            if (Inherit == "Weapons")
                            {
                                tmp2 = tmp2.Replace("hand", "");
                                tmp2 = tmp2.Remove(tmp2.Length - 1);
                                if (tmp2 == "stave" && tmp.Length == 4)
                                {
                                    if (tmp[3] == "AbstractWarstaff")
                                        tmp2 = "warstaff";
                                    else if (tmp[3] == "AbstractStaff")
                                        tmp2 = "staff";
                                }
                            }
                            else if (Inherit == "Armours" && (tmp2 == "shields" || tmp2 == "helmets" || tmp2 == "bodyarmours"))
                            {
                                if (tmp2 == "bodyarmours")
                                    tmp2 = "chest";
                                else
                                    tmp2 = tmp2.Remove(tmp2.Length - 1);
                            }

                            option += "." + tmp2;
                        }
                    }

                    JQ.Filters.Type.Filters.Category.Option = option;
                }

                string sEntity = Json.Serialize<JsonData>(jsonData);

                if (itemOptions.ByType || JQ.Name == "" || JQ.Filters.Type.Filters.Rarity.Option != "unique")
                {
                    sEntity = sEntity.Replace("\"name\":\"" + JQ.Name + "\",", "");

                    if (Inherit == "Jewels" || itemOptions.ByType)
                        sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "");
                    else if (Inherit == "Prophecies")
                        sEntity = sEntity.Replace("\"type\":\"" + JQ.Type + "\",", "\"name\":\"" + JQ.Type + "\",");
                }

                sEntity = sEntity.Replace("{\"max\":99999,\"min\":99999}", "{}");
                sEntity = sEntity.Replace("{\"max\":99999,", "{");
                sEntity = sEntity.Replace(",\"min\":99999}", "}");

                sEntity = sEntity.Replace(",{\"disabled\":true,\"id\":\"temp_ids\",\"value\":{}}", "");
                sEntity = sEntity.Replace("[{\"disabled\":true,\"id\":\"temp_ids\",\"value\":{}}", "[");
                sEntity = sEntity.Replace("[,", "[");

                sEntity = Regex.Replace(sEntity, "\"(sale_type|rarity|category|corrupted|synthesised_item|shaper_item|elder_item|crusader_item|redeemer_item|hunter_item|warlord_item|map_shaped|map_elder|map_blighted)\":{\"option\":\"any\"},?", "");
                sEntity = sEntity.Replace("},}", "}}");

                if (error_filter)
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                        (ThreadStart)delegate ()
                        {
                            for (int i = 0; i < itemOptions.itemfilters.Count; i++)
                            {
                                if (itemOptions.itemfilters[i].isNull)
                                {
                                    ((TextBox)this.FindName("tbOpt" + i)).Background = System.Windows.Media.Brushes.Red;
                                    ((TextBox)this.FindName("tbOpt" + i + "_0")).Text = "error";
                                    ((TextBox)this.FindName("tbOpt" + i + "_1")).Text = "error";
                                    ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsChecked = false;
                                    ((CheckBox)this.FindName("tbOpt" + i + "_2")).IsEnabled = false;
                                    ((CheckBox)this.FindName("tbOpt" + i + "_3")).Visibility = Visibility.Hidden;
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

        private void PriceUpdate(string[] entity, int listCount)
        {
            string result = "정보가 없습니다";
            string result2 = "";
            string urlString = "";
            string sEentity;

            if (entity.Length > 1)
            {
                sEentity = String.Format(
                        "{{\"exchange\":{{\"status\":{{\"option\":\"online\"}},\"have\":[\"{0}\"],\"want\":[\"{1}\"]}}}}",
                        entity[0],
                        entity[1]
                    );
                urlString = RS.ExchangeApi[RS.ServerLang];
            }
            else
            {
                sEentity = entity[0];
                urlString = RS.TradeApi[RS.ServerLang];
            }

            if (sEentity != null && sEentity != "")
            {
                try
                {
                    string sResult = SendHTTP(sEentity, urlString + RS.ServerType, mConfigData.Options.ServerTimeout);
                    result = "거래소 접속이 원활하지 않습니다";

                    if (sResult != null)
                    {
                        ResultData resultData = Json.Deserialize<ResultData>(sResult);
                        Dictionary<string, int> currencys = new Dictionary<string, int>();

                        int total = 0;
                        int resultCount = resultData.Result.Length;

                        if (resultData.Result.Length > 0)
                        {
                            string ents0 = "", ents1 = "";

                            if (entity.Length > 1)
                            {
                                listCount = listCount + 2;
                                ents0 = Regex.Replace(entity[0], @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");
                                ents1 = Regex.Replace(entity[1], @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");
                            }

                            for (int x = 0; x < listCount; x++)
                            {
                                string[] tmp = new string[5];
                                int cnt = x * 5;
                                int length = 0;

                                if (cnt >= resultData.Result.Length)
                                    break;

                                for (int i = 0; i < 5; i++)
                                {
                                    if (i + cnt >= resultData.Result.Length)
                                        break;

                                    tmp[i] = resultData.Result[i + cnt];
                                    length++;
                                }

                                string jsonResult = "";
                                string url = RS.FetchApi[RS.ServerLang] + string.Join(",", tmp) + "?query=" + resultData.ID;
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
                                request.Timeout = 10000;

                                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                                using (StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                                {
                                    jsonResult = streamReader.ReadToEnd();
                                }

                                if (jsonResult != "")
                                {
                                    FetchData fetchData = new FetchData();
                                    fetchData.Result = new FetchDataInfo[5];

                                    fetchData = Json.Deserialize<FetchData>(jsonResult);

                                    for (int i = 0; i < fetchData.Result.Length; i++)
                                    {
                                        if (fetchData.Result[i] == null)
                                            break;

                                        if (fetchData.Result[i].Listing.Price != null && fetchData.Result[i].Listing.Price.Amount > 0)
                                        {
                                            string account = fetchData.Result[i].Listing.Account.Name;
                                            string key = fetchData.Result[i].Listing.Price.Currency;
                                            double amount = fetchData.Result[i].Listing.Price.Amount;
                                            string keyName = RS.lExchangeCurrency[0].ContainsValue(key) ? RS.lExchangeCurrency[0].FirstOrDefault(o => o.Value == key).Key : key;

                                            liPrice.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                                (ThreadStart)delegate ()
                                                {
                                                    if (entity.Length > 1)
                                                    {
                                                        string tName2 = RS.lExchangeCurrency[0].ContainsValue(entity[1])
                                                                        ? RS.lExchangeCurrency[0].FirstOrDefault(o => o.Value == entity[1]).Key : entity[1];
                                                        liPrice.Items.Add(Math.Round(1 / amount, 4) + " " + tName2 + " <-> " + Math.Round(amount, 4) + " " + keyName + " [" + account + "]");
                                                    }
                                                    else
                                                        liPrice.Items.Add(amount + " " + keyName + " [" + account + "]");
                                                }
                                            );

                                            if (entity.Length > 1)
                                                key = amount < 1 ? Math.Round(1 / amount, 1) + " " + ents1 : Math.Round(amount, 1) + " " + ents0;
                                            else
                                                key = Math.Round(amount - 0.1) + " " + key;

                                            if (currencys.ContainsKey(key))
                                                currencys[key]++;
                                            else
                                                currencys.Add(key, 1);

                                            total++;
                                        }
                                    }
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
                                    result2 += myList[i].Key + "[" + myList[i].Value + "], ";
                                }

                                result = Regex.Replace(first + " ~ " + last, @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");
                                result2 = Regex.Replace(result2.TrimEnd(',', ' '), @"(timeless-)?([a-z]{3})[a-z\-]+\-([a-z]+)", @"$3`$2");

                                if (result2 == "")
                                    result2 = "가장 많은 수 없음";
                            }
                        }

                        cbPriceListTotal.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            (ThreadStart)delegate ()
                            {
                                cbPriceListTotal.Text = total + "/" + resultCount + " 검색";
                            }
                        );

                        tkPriceCount1.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            (ThreadStart)delegate ()
                            {
                                tkPriceCount1.Text = total > 0 ? total + (resultCount > total ? "+" : ".") : "";
                            }
                        );

                        tkPriceCount2.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            (ThreadStart)delegate ()
                            {
                                tkPriceCount2.Text = total > 0 ? total + (resultCount > total ? "+" : ".") : "";
                            }
                        );

                        if (resultData.Total == 0 || currencys.Count == 0)
                        {
                            result = "해당 물품의 거래가 없습니다";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            tkPriceInfo1.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (ThreadStart)delegate ()
                {
                    tkPriceInfo1.Text = result + (result2 != "" ? " = " + result2 : "");
                }
            );

            tkPriceInfo2.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (ThreadStart)delegate ()
                {
                    tkPriceInfo2.Text = result + (result2 != "" ? " = " + result2 : "");
                }
            );

            liPrice.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (ThreadStart)delegate ()
                {
                    if (liPrice.Items.Count == 0)
                        liPrice.Items.Add(result + (result2 != "" ? " = " + result2 : ""));
                }
            );
        }

        private Thread priceThread = null;

        private void PriceUpdateThreadWorker(ItemOption itemOptions, string[] exchange)
        {
            tkPriceInfo1.Text = tkPriceInfo2.Text = "시세 확인중...";
            tkPriceCount1.Text = tkPriceCount2.Text = "";
            cbPriceListTotal.Text = "0/0 검색";
            liPrice.Items.Clear();

            int listCount = (cbPriceListCount.SelectedIndex + 1) * 4;

            priceThread?.Interrupt();
            priceThread?.Abort();
            priceThread = new Thread(() => PriceUpdate(
                    exchange != null ? exchange : new string[1] { CreateJson(itemOptions, true) },
                    listCount
                ));
            priceThread.Start();
        }

        private string GetClipText(bool isUnicode)
        {
            return Clipboard.GetText(isUnicode ? TextDataFormat.UnicodeText : TextDataFormat.Text);
        }

        private void SetClipText(string text, TextDataFormat textDataFormat)
        {
            var ClipboardThread = new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        Clipboard.SetText(text, textDataFormat);
                        return;
                    }
                    catch { }
                    Thread.Sleep(10);
                }
            });
            ClipboardThread.SetApartmentState(ApartmentState.STA);
            ClipboardThread.IsBackground = false;
            ClipboardThread.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (Native.GetForegroundWindow().Equals(Native.FindWindow(RS.PoeClass, RS.PoeCaption)))
            {
                if (!mIsHotKey) InstallRegisterHotKey();

                if (!mIsPause && mConfigData.Options.CtrlWheel)
                {
                    TimeSpan dateDiff = Convert.ToDateTime(DateTime.Now) - MouseHookCallbackTime;
                    if (dateDiff.Ticks > 3000000000) // 5분간 마우스 움직임이 없으면 훜이 풀렸을 수 있어 다시...
                    {
                        MouseHookCallbackTime = Convert.ToDateTime(DateTime.Now);
                        MouseHook.Start();
                    }
                }
            }
            else
            {
                if (mIsHotKey) RemoveRegisterHotKey();
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
            mIsHotKey = true;

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
            mIsHotKey = false;

            for (int i = 0; i < mConfigData.Shortcuts.Length; i++)
            {
                ConfigShortcut shortcut = mConfigData.Shortcuts[i];
                if (shortcut.Keycode > 0 && (shortcut.Value ?? "") != "")
                    Native.UnregisterHotKey(mMainHwnd, 10001 + i);
            }
        }

        private bool mHotkeyProcBlock = false;
        private bool mClipboardBlock = false;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Native.WM_DRAWCLIPBOARD)
            {
                IntPtr findHwnd = Native.FindWindow(RS.PoeClass, RS.PoeCaption);

                if (!mIsPause && !mClipboardBlock && !Native.GetForegroundWindow().Equals(findHwnd))
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
            else if (!mHotkeyProcBlock && msg == (int)0x312) //WM_HOTKEY
            {
                mHotkeyProcBlock = true;

                IntPtr findHwnd = Native.FindWindow(RS.PoeClass, RS.PoeCaption);

                if (Native.GetForegroundWindow().Equals(findHwnd))
                {
                    int keyIdx = wParam.ToInt32();

                    string popWinTitle = "이곳을 잡고 이동, 이미지 클릭시 닫힘";
                    ConfigShortcut shortcut = mConfigData.Shortcuts[keyIdx - 10001];

                    if (shortcut != null && shortcut.Value != null)
                    {
                        string valueLower = shortcut.Value.ToLower();

                        try
                        {
                            if (valueLower == "{pause}")
                            {
                                mIsPause = !mIsPause;

                                if (mIsPause)
                                {
                                    if (mConfigData.Options.CtrlWheel)
                                        MouseHook.Stop();

                                    MessageBox.Show(Application.Current.MainWindow, "프로그램 동작을 일시 중지합니다." + '\n' +
                                        "다시 시작하려면 일시 중지 단축키를 한번더 누르세요.", "POE 거래소 검색");
                                }
                                else
                                {
                                    if (mConfigData.Options.CtrlWheel)
                                        MouseHook.Start();

                                    MessageBox.Show(Application.Current.MainWindow, "프로그램 동작을 다시 시작합니다.", "POE 거래소 검색");
                                }

                                Native.SetForegroundWindow(findHwnd);
                            }
                            else if (valueLower == "{close}")
                            {
                                IntPtr pHwnd = Native.FindWindow(null, popWinTitle);

                                if (this.Visibility == Visibility.Hidden && pHwnd.ToInt32() == 0)
                                {
                                    Native.SendMessage(findHwnd, 0x0101, new IntPtr(shortcut.Keycode), IntPtr.Zero);
                                }
                                else
                                {
                                    if (pHwnd.ToInt32() != 0)
                                        Native.SendMessage(pHwnd, /* WM_CLOSE = */ 0x10, IntPtr.Zero, IntPtr.Zero);

                                    if (this.Visibility == Visibility.Visible)
                                        Close();
                                }
                            }
                            else if (!mIsPause)
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
                                else if (valueLower.IndexOf(".jpg") > 0)
                                {
                                    IntPtr pHwnd = Native.FindWindow(null, popWinTitle);
                                    if (pHwnd.ToInt32() != 0)
                                        Native.SendMessage(pHwnd, /* WM_CLOSE = */ 0x10, IntPtr.Zero, IntPtr.Zero);

                                    PopWindow popWindow = new PopWindow(shortcut.Value);

                                    if ((shortcut.Position ?? "") != "")
                                    {
                                        string[] strs = shortcut.Position.ToLower().Split('x');
                                        popWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                                        popWindow.Left = double.Parse(strs[0]);
                                        popWindow.Top = double.Parse(strs[1]);
                                    }

                                    popWindow.Title = popWinTitle;
                                    popWindow.Show();
                                }
                            }
                        }
                        catch (Exception)
                        {
                            ForegroundMessage("잘못된 단축키 명령입니다.", "단축키 에러", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    handled = true;
                }

                mHotkeyProcBlock = false;
            }

            return IntPtr.Zero;
        }
    }
}
