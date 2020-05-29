using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Data.SQLite;
using System.Runtime.InteropServices;

namespace TravBot
{
    public static class UnmanagedCode
    {
        // This code is to disable the clicking sound of the bot. Maybe there's an easier solution now.
        private const int FEATURE_DISABLE_NAVIGATION_SOUNDS = 21;
        private const int SET_FEATURE_ON_THREAD = 0x00000001;
        private const int SET_FEATURE_ON_PROCESS = 0x00000002;
        private const int SET_FEATURE_IN_REGISTRY = 0x00000004;
        private const int SET_FEATURE_ON_THREAD_LOCALMACHINE = 0x00000008;
        private const int SET_FEATURE_ON_THREAD_INTRANET = 0x00000010;
        private const int SET_FEATURE_ON_THREAD_TRUSTED = 0x00000020;
        private const int SET_FEATURE_ON_THREAD_INTERNET = 0x00000040;
        private const int SET_FEATURE_ON_THREAD_RESTRICTED = 0x00000080;
        [DllImport("urlmon.dll")]
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Error)]
        public static extern int CoInternetSetFeatureEnabled(
            int FeatureEntry,
            [MarshalAs(UnmanagedType.U4)] int dwFlags,
            bool fEnable);
        public static int disableSound()
        {
            UnmanagedCode.CoInternetSetFeatureEnabled(FEATURE_DISABLE_NAVIGATION_SOUNDS, SET_FEATURE_ON_THREAD, true);
            UnmanagedCode.CoInternetSetFeatureEnabled(FEATURE_DISABLE_NAVIGATION_SOUNDS, SET_FEATURE_ON_PROCESS, true);
            UnmanagedCode.CoInternetSetFeatureEnabled(FEATURE_DISABLE_NAVIGATION_SOUNDS, SET_FEATURE_IN_REGISTRY, true);
            UnmanagedCode.CoInternetSetFeatureEnabled(FEATURE_DISABLE_NAVIGATION_SOUNDS, SET_FEATURE_ON_THREAD_LOCALMACHINE, true);
            UnmanagedCode.CoInternetSetFeatureEnabled(FEATURE_DISABLE_NAVIGATION_SOUNDS, SET_FEATURE_ON_THREAD_INTRANET, true);
            UnmanagedCode.CoInternetSetFeatureEnabled(FEATURE_DISABLE_NAVIGATION_SOUNDS, SET_FEATURE_ON_THREAD_TRUSTED, true);
            UnmanagedCode.CoInternetSetFeatureEnabled(FEATURE_DISABLE_NAVIGATION_SOUNDS, SET_FEATURE_ON_THREAD_INTERNET, true);
            UnmanagedCode.CoInternetSetFeatureEnabled(FEATURE_DISABLE_NAVIGATION_SOUNDS, SET_FEATURE_ON_THREAD_RESTRICTED, true);
            return 1;
        }
    }

    public partial class Form2 : Form
    {
        string sql;
        SQLiteConnection con;
        public List<FarmList> NewListOfFL = new List<FarmList>();
        public List<FarmList> ListOfFL = new List<FarmList>();
        public List<Village> Villages = new List<Village>();
        public List<Village> NewVillages = new List<Village>();
        int FLvillnum;
        public string sel_name, sel_pass, sel_server, sel_db;
        public Form1 main;
        public byte count = 0;

        public Form2(Form1 m)
        {
            main = m;
            UnmanagedCode.disableSound();
            InitializeComponent();
        }

        public void Login(string name, string pass, string server, string db)
        {
            sel_name = name;
            sel_pass = pass;
            sel_server = server;
            sel_db = db;
            Thread LoginThread = new Thread(new ThreadStart(Login_thread));
            LoginThread.Start();

            con = new SQLiteConnection(Helper.DB(sel_db));
            con.Open();
            sql = "Select * from FLgeneral";
            SQLiteCommand command = new SQLiteCommand(sql, con);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                FLvillnum = Convert.ToInt16(reader["villnum"] + "");
            }
            con.Close();
        }
        private void Login_thread()
        {
            DoInvoke(delegate { GoTo(sel_server); });
            Thread.Sleep(Helper.ReturnRandom(3500));
            DoInvoke(delegate
            {
                HtmlElementCollection elements = webBrowser1.Document.GetElementsByTagName("input");
                elements[1].SetAttribute("value", sel_name);
                elements[2].SetAttribute("value", sel_pass);
            });
            Thread.Sleep(Helper.ReturnRandom(500));
            DoInvoke(delegate
            {
                webBrowser1.Document.GetElementById("s1").InvokeMember("Click");
            });
        }

        public void GoTo(string link)
        {
            webBrowser1.Navigate(link);
        }
        private void DoInvoke(MethodInvoker del)
        {
            if (InvokeRequired) { Invoke(del); }
            else { del(); }
        }

        public void SendFarmlist(int FL_num, bool send2, bool send3)
        {
            ReadFarmlist();
            count++;
            richTextBox1.Text = DateTime.Now.ToLocalTime() + ": Will send FL num:" + FL_num + "  NewListOfFLCount:" + NewListOfFL.Count + "\n" + richTextBox1.Text;

            if (webBrowser1.Document.GetElementById("recaptcha_widget") != null || webBrowser1.Document.GetElementById("recaptcha_image") != null)
            {
                Form popup = new Form();
                popup.Show(this);
                this.Show();
                popup.Text = "CHAPTCHA DETECTED!";
                System.Media.SoundPlayer soundPlayer = new System.Media.SoundPlayer();
                soundPlayer.SoundLocation = "alert.wav";
                soundPlayer.Load();
                soundPlayer.PlayLooping();
            }

            if (FL_num >= NewListOfFL.Count)
            {
                richTextBox1.Text = DateTime.Now.ToLocalTime() + ": Did not send FL because index FL_num was out of range(better to have this message than a crash)\n" + richTextBox1.Text;
                Console.WriteLine("FLNUM > NEWLISTOFFL RIP");
                return;
            }
            List<string> dontFarm = new List<string>();
            con = new SQLiteConnection(Helper.DB(sel_db));
            con.Open();
            sql = "Select * from DontFarm";
            SQLiteCommand command = new SQLiteCommand(sql, con);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                dontFarm.Add(reader["name"] + "");
            }
            con.Close();

            HtmlAgilityPack.HtmlDocument html = new HtmlAgilityPack.HtmlDocument();
            html.LoadHtml(webBrowser1.DocumentText);

            if (NewListOfFL.Count == 0)
            {
                ChangeVillage(1);
                richTextBox1.Text = DateTime.Now.ToLocalTime() + ": Village does not have rally point built. Switching village\n" + richTextBox1.Text;
                return;
            }

            var raidLists = html.GetElementbyId("raidList").ChildNodes.Where(x => x.HasClass("listEntry"));

            var selectedList = raidLists.ElementAt(FL_num);

            var farms = selectedList.Descendants("tr").Where(x => x.HasClass("slotRow"));
            foreach (var farm in farms)
            {
                //<a href="position_details.php?x=67&amp;y=-80">New village</a>
                var villName = farm.ChildNodes.FirstOrDefault(x => x.HasClass("village")).ChildNodes.FirstOrDefault(x => x.Name == "a").InnerText;
                if (dontFarm.Any(x => villName.ToLower().StartsWith(x.ToLower()))) continue;

                //<img src="img/x.gif" class="iReport iReport1" alt="Won as attacker without losses.">
                // iReport1 => Green sword, no loss
                // iReport2 => Yellow sword, losses
                // iReport3 => Red sword, all losses
                // No (image ) element => No last raid
                var lastRaidImg = farm.Descendants("img").FirstOrDefault(x => x.HasClass("iReport"));
                if (lastRaidImg != null)
                {
                    if (!send2 && lastRaidImg.HasClass("iReport2")) continue;
                    if (!send3 && lastRaidImg.HasClass("iReport3")) continue;
                }

                var checkbox = farm.Descendants("input").FirstOrDefault(x => x.HasClass("markSlot"));
                webBrowser1.Document.GetElementById(checkbox.Id).InvokeMember("Click");
            }

            //<button type="submit" value="Start raid" id="button5ed0cc65b55d1" class="textButtonV1 green " version="textButtonV1">Start raid</ button >
            webBrowser1.Document.GetElementById(selectedList.Id).FirstChild.InvokeMember("submit");

            Console.WriteLine("Should have sent the FL " + count);
        }

        public void ReadFarmlist()
        {
            NewListOfFL.Clear();

            HtmlAgilityPack.HtmlDocument html = new HtmlAgilityPack.HtmlDocument();
            html.LoadHtml(webBrowser1.DocumentText);

            var raidLists = html.GetElementbyId("raidList").ChildNodes.Where(x => x.HasClass("listEntry"));

            foreach (var raidList in raidLists)
            {
                NewListOfFL.Add(new FarmList
                {
                    FLId = raidList.GetAttributeValue("id", ""),
                    FLName = raidList.Descendants("div").FirstOrDefault(x => x.HasClass("listTitleText")).InnerText.Trim(),
                });
            }
        }

        public void UpdateVillages()
        {
            Read_Villages();
            con = new SQLiteConnection(Helper.DB(sel_db));
            con.Open();
            sql = "DELETE FROM Villages";
            SQLiteCommand command = new SQLiteCommand(sql, con);
            command.ExecuteNonQuery();
            for (int i = 0; i < NewVillages.Count; i++)
            {
                sql = "insert into Villages(id, name) values('"
                + NewVillages.ElementAt(i).Id + "', '"
                + NewVillages.ElementAt(i).Name + "')";
                command = new SQLiteCommand(sql, con);
                command.ExecuteNonQuery();
            }
            con.Close();
        }
        public void Read_ALL(int villnum)
        {
            richTextBox1.Text = DateTime.Now.ToLocalTime() + ": Reading villages\n" + richTextBox1.Text;
            FLvillnum = villnum;
            Thread ReadThread = new Thread(new ThreadStart(ReadAllThread));
            ReadThread.Start();
        }
        public void ReadAllThread()
        {
            DoInvoke(delegate { UpdateVillages(); });
            DoInvoke(delegate { ChangeVillage(FLvillnum); });
            Thread.Sleep(Helper.ReturnRandom(500));
            DoInvoke(delegate { GoTo("https://" + sel_server + "/build.php?tt=99&id=39"); });
            Thread.Sleep(Helper.ReturnRandom(500));
            DoInvoke(delegate
            {
                ReadLists();
            });
            Thread.Sleep(Helper.ReturnRandom(500));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GoTo(textBox1.Text);
        }

        public void SwitchToFl(int villnum)
        {
            richTextBox1.Text = DateTime.Now.ToLocalTime() + ": Switching to FarmList, default village: " + villnum + "\n" + richTextBox1.Text;
            FLvillnum = villnum;
            if (!webBrowser1.DocumentText.ToString().Contains("id=\"raidList\">"))
            {
                Thread SwitchToFL_T = new Thread(new ThreadStart(SwitchToFl_Thread));
                SwitchToFL_T.Start();
            }
        }

        public void SwitchToFl_Thread()
        {
            DoInvoke(delegate { ChangeVillage(FLvillnum); });
            Thread.Sleep(Helper.ReturnRandom(500));
            DoInvoke(delegate { GoTo("https://" + sel_server + "/build.php?tt=99&id=39"); });
        }

        public void Read_Villages()
        {
            NewVillages.Clear();

            HtmlAgilityPack.HtmlDocument html = new HtmlAgilityPack.HtmlDocument();
            html.LoadHtml(webBrowser1.DocumentText);

            var villBox = html.GetElementbyId("sidebarBoxVillagelist");
            var vills = villBox
                .ChildNodes
                .FirstOrDefault(x => x.HasClass("content"))
                .ChildNodes
                .FirstOrDefault(x => x.Name == "ul")
                .ChildNodes
                .Where(x => x.Name == "li");

            foreach (var vill in vills)
            {
                /*
                 <span class="coordinatesGrid" data-x="123" data-y="123" data-did="123123" data-villagename="FooBar">
                 */
                var span = vill.Descendants("span").FirstOrDefault(x => x.HasClass("coordinatesGrid"));

                NewVillages.Add(new Village
                {
                    Id = span.GetAttributeValue("data-did", ""),
                    Name = span.GetAttributeValue("data-villagename", ""),
                });
            }
        }

        public void ChangeVillage(int villageNUM)
        {
            GoTo("https://" + sel_server + "/dorf1.php" + NewVillages.ElementAt(villageNUM).Id);
        }

        public void ReadLists()
        {
            ReadFarmlist();
            bool FoundID = true;
            con = new SQLiteConnection(Helper.DB(sel_db));
            con.Open();
            sql = "select * from FL order by num";
            SQLiteCommand command = new SQLiteCommand(sql, con);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                ListOfFL.Add(new FarmList
                {
                    FLId = reader["FLid"] + ""
                });
            }
            for (int i = 0; i < NewListOfFL.Count; i++)
            {
                for (int j = 0; j < ListOfFL.Count; j++) { if (ListOfFL.ElementAt(j).FLId == NewListOfFL.ElementAt(i).FLId) { FoundID = false; } }
                if (FoundID)
                {
                    sql = "insert into FL(period, enabled, FLid, FLName, Send2, Send3) values('1','"
                        + Convert.ToByte(NewListOfFL.ElementAt(i).Enabled) + "', '"
                        + NewListOfFL.ElementAt(i).FLId + "', '"
                        + NewListOfFL.ElementAt(i).FLName + "','0','0')";
                    command = new SQLiteCommand(sql, con);
                    command.ExecuteNonQuery();


                    ListOfFL.Add(new FarmList
                    {
                        Period = NewListOfFL.ElementAt(i).Period,
                        Enabled = NewListOfFL.ElementAt(i).Enabled,
                        FLId = NewListOfFL.ElementAt(i).FLId,
                        FLName = NewListOfFL.ElementAt(i).FLName
                    });
                }
                FoundID = true;
            }
            con.Close();
        }

        private void TaskTimer_Tick(object sender, EventArgs e)
        {

        }
    }
}
