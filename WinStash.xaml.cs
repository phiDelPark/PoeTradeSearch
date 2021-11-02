using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace PoeTradeSearch
{
    /// <summary>
    /// WinStash.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WinStash : Window
    {
        private const string _POE_DOMAIN_ = @"www.pathofexile.com";
        // client_id 만들기 귀찮아 token 사용 안함
        private const string _API_CHARACTERS_ = @"/character-window/get-characters";
        private const string _API_ACCOUNTNAME_ = @"/character-window/get-account-name-by-character?character={0}";
        private const string _API_STASH_ITEMS_ = @"/character-window/get-stash-items?tabs=1&tabIndex={0}&league={1}&accountName={2}";

        //https://poe.ninja/swagger/index.html
        private string[] @_NinjaJson = new string[6];
        private const string _NINJA_DATA_ = @"https://poe.ninja/api/data/itemoverview?league={0}&type={1}";

        public class LstItem
        {
            public string name { get; set; }
            public string value { get; set; }
        }

        [DataContract()]
        internal class NinjaData
        {
            [DataMember(Name = "lines")]
            internal NinjaLine[] lines = null;
        }

        [DataContract()]
        internal class NinjaLine
        {
            [DataMember(Name = "name")]
            internal string name = null;
            [DataMember(Name = "chaosValue")]
            internal float chaosValue = 0;
            [DataMember(Name = "exaltedValue")]
            internal float exaltedValue = 0;
        }

        [DataContract()]
        internal class Characters
        {
            [DataMember(Name = "entries")]
            internal Character[] entries = null;
        }

        [DataContract()]
        internal class Character
        {
            [DataMember(Name = "name")]
            internal string name = null;
            [DataMember(Name = "league")]
            internal string league = null;
        }

        [DataContract()]
        internal class UserAccount
        {
            [DataMember(Name = "accountName")]
            internal string name = null;
        }

        [DataContract()]
        internal class Stash
        {
            [DataMember(Name = "tabs")]
            internal StashTab[] Tabs = null;

            [DataMember(Name = "items")]
            internal StashItem[] Items = null;
        }

        [DataContract()]
        internal class StashTab
        {
            [DataMember(Name = "i")]
            internal int index = -1;

            [DataMember(Name = "type")]
            internal string type = null;
        }

        [DataContract()]
        internal class StashItem
        {
            [DataMember(Name = "typeLine")]
            internal string name = null;
        }

        private Stash @_Stash = null;
        private string @_AccountName = null;

        public WinStash()
        {
            InitializeComponent();
        }

        private void btRefresh_Click(object sender, RoutedEventArgs e)
        {
            WinMain winMain = (WinMain)Application.Current.MainWindow;
            string league = winMain.mConfig.Options.League.ToLower();

            Cookie cookie = new Cookie("POESESSID", tbSessid.Text, "/", _POE_DOMAIN_);
            Thread thread = new Thread(() =>
            {
                string u = "https://" + _POE_DOMAIN_ + _API_CHARACTERS_;
                string json = winMain.SendHTTP(null, u, 5, cookie);
                if ((json ?? "") != "")
                {
                    Characters characters = Json.Deserialize<Characters>("{\"entries\":" + json + "}");
                    foreach (Character character in characters.entries)
                    {
                        if (character.league.ToLower() == league)
                        {
                            u = "https://" + _POE_DOMAIN_ + String.Format(_API_ACCOUNTNAME_, character.name);
                            json = winMain.SendHTTP(null, u, 5, cookie);
                            if ((json ?? "") != "")
                            {
                                UserAccount account = Json.Deserialize<UserAccount>(json);
                                @_AccountName = account?.name ?? "";
                                if (@_AccountName != "")
                                {
                                    u = "https://" + _POE_DOMAIN_ + String.Format(_API_STASH_ITEMS_, -1, league, @_AccountName);
                                    json = winMain.SendHTTP(null, u, 5, cookie);
                                    if ((json ?? "") != "")
                                    {
                                        @_Stash = Json.Deserialize<Stash>(json);
                                    }
                                }
                            }

                            break;
                        }
                    }
                }
            });
            thread.Start();
            thread.Join();

            TabControl_SelectionChanged(tcStash, null);
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((e != null && !(e.Source is TabControl)) || @_Stash == null) return;

            List<LstItem> list = new List<LstItem>();
            lbStashItem.ItemsSource = null;
            (btRefresh.Parent as Grid).Visibility = Visibility.Visible;

            int index = tcStash.SelectedIndex;
            WinMain winMain = (WinMain)Application.Current.MainWindow;
            string league = winMain.mConfig.Options.League.ToLower();

            Cookie cookie = new Cookie("POESESSID", tbSessid.Text, "/", _POE_DOMAIN_);

            Thread thread = new Thread(() =>
            {
                btRefresh.BInvoke((ThreadStart)delegate ()
                {
                    btRefresh.Content = ".....";
                });

                string[] types = { "DivinationCardStash", "DelveStash", "EssenceStash" };
                StashTab stashtab = Array.Find(@_Stash.Tabs, x => x.type.Equals(types[index]));

                if (stashtab != null)
                {
                    if ((@_NinjaJson[index] ?? "") == "")
                    {
                        string[] ninjadatas = { "DivinationCard", "Fossil", "Essence" };
                        string u = String.Format(_NINJA_DATA_, winMain.mConfig.Options.League, ninjadatas[index]);
                        @_NinjaJson[index] = winMain.SendHTTP(null, u, 5, null);
                    }

                    if ((@_NinjaJson[index] ?? "") != "")
                    {
                        string u = "https://" + _POE_DOMAIN_ + String.Format(_API_STASH_ITEMS_, stashtab.index, league, @_AccountName);
                        string json = winMain.SendHTTP(null, u, 5, cookie);

                        if ((json ?? "") != "")
                        {
                            NinjaData ninja = Json.Deserialize<NinjaData>(@_NinjaJson[index]);
                            Stash stash = Json.Deserialize<Stash>(json);

                            string[] cates = { "cards", "currency", "currency" };
                            int cate_idx = Array.FindIndex(winMain.mItems[0].Result, x => x.Id.Equals(cates[index]));

                            lbStashItem.BInvoke((ThreadStart)delegate ()
                            {
                                foreach (NinjaLine line in ninja.lines)
                                {
                                    StashItem item = Array.Find(stash.Items, x => x.name.Equals(line.name));
                                    if (item != null && line.chaosValue > 1)
                                    {
                                        string tmpvalue = line.chaosValue > 200 ? line.exaltedValue.ToString() + "ex" : line.chaosValue.ToString() + "ca";

                                        int item_idx = Array.FindIndex(winMain.mItems[1].Result[cate_idx].Entries, x => (x.Text == line.name));
                                        string tmpname = item_idx == -1 ? line.name : winMain.mItems[0].Result[cate_idx].Entries[item_idx].Text;
                                        list.Add(new LstItem() { name = tmpname, value = tmpvalue });
                                    }
                                }

                                btRefresh.Content = btRefresh.Content.Equals(".....") ? "..." : ".....";
                            });
                        }
                    }
                }

                (btRefresh.Parent as Grid).BInvoke((ThreadStart)delegate ()
                {
                    (btRefresh.Parent as Grid).Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Hidden;
                    btRefresh.Content = "새로 고침";
                    lbStashItem.ItemsSource = list;
                });
            });
            thread.Start();
            //thread.Join();
        }

        private void TextBlock_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/phiDelPark/PoeTradeSearch/wiki/POESESSID");
        }
    }
}
