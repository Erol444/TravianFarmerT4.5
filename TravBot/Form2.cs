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
        // This code is to fix the old internet explorer script problems. Not sure if it's still needed since webBrowser uses edge browser now.
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
        public static List<HelperClass.Farm> ListOfFarms = new List<HelperClass.Farm>();
        public static List<HelperClass.FarmLists> NewListOfFL = new List<HelperClass.FarmLists>();
        public static List<HelperClass.FarmLists> ListOfFL = new List<HelperClass.FarmLists>();
        public static List<HelperClass.Villages> Villages = new List<HelperClass.Villages>();
        public static List<HelperClass.Villages> NewVillages = new List<HelperClass.Villages>();
        int FL_min, FL_max, FLvillnum;
        public string sel_name, sel_pass, sel_server, sel_db;
        HelperClass tools = new HelperClass();
        public Form1 main;
        public byte count = 0;

        public Form2(Form1 m)
        {
            main = m;
            UnmanagedCode.disableSound();
            InitializeComponent();
        }

        public void Login(string name, string pass, string server, string db) {
            sel_name = name;
            sel_pass = pass;
            sel_server = server;
            sel_db = db;
            Thread LoginThread = new Thread(new ThreadStart(Login_thread));
            LoginThread.Start();



            con = new SQLiteConnection(tools.DB(sel_db));
            con.Open();
            sql = "Select * from FLgeneral";
            SQLiteCommand command = new SQLiteCommand(sql, con);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                FL_max = Convert.ToInt16(reader["max"] + "");
                FL_min = Convert.ToInt16(reader["min"] + "");
                FLvillnum = Convert.ToInt16(reader["villnum"] + "");
            }
            con.Close();
        }
        private void Login_thread() {
            DoInvoke(delegate { GoTo(sel_server); });
            Thread.Sleep(tools.ReturnRandom(3500));
            DoInvoke(delegate {
                HtmlElementCollection elements = webBrowser1.Document.GetElementsByTagName("input");
                elements[1].SetAttribute("value", sel_name);
                elements[2].SetAttribute("value", sel_pass);
            });
            Thread.Sleep(tools.ReturnRandom(500));
            DoInvoke(delegate {
                webBrowser1.Document.GetElementById("s1").InvokeMember("Click");
            });
        }

        public void GoTo(string link) {
            webBrowser1.Navigate(link);
        }
        private void DoInvoke(MethodInvoker del)
        {
            if (InvokeRequired){Invoke(del);}
            else{del();}
        }

        public void Send_FL(int FL_num, bool send2, bool send3)
        {

            Read_FL();
            count++;
            richTextBox1.Text = DateTime.Now.ToLocalTime() + ": Gonna send FL, num:"+FL_num+"  ListOfFarmsCount:"+ListOfFarms.Count+"  NewListOfFLCount:"+NewListOfFL.Count+    "\n" + richTextBox1.Text;

            if (webBrowser1.Document.GetElementById("recaptcha_widget") != null || webBrowser1.Document.GetElementById("recaptcha_image") != null)
            {
                Form popup = new Form();
                popup.Show(this);
                this.Show();
                popup.Text = "CHAPTCHA DETECTED!";
                System.Media.SoundPlayer player =new System.Media.SoundPlayer();
                player.SoundLocation = "alert.wav";
                player.Load();
                player.PlayLooping();
            }

            if (FL_num >= NewListOfFL.Count) {
                richTextBox1.Text = DateTime.Now.ToLocalTime() + ": Erik Fucked up :c Did not send FL because index FL_num was out of range(better to have this message than a crash lol)\n" + richTextBox1.Text;
                Console.WriteLine("FLNUM > NEWLISTOFFL RIP");
                return;
            }
            bool SendAttack = true;
            List<string> DontFarm = new List<string>();
            con = new SQLiteConnection(tools.DB(sel_db));
            con.Open();
            sql = "Select * from DontFarm";
            SQLiteCommand command = new SQLiteCommand(sql, con);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                DontFarm.Add(reader["name"] + "");
            }
            con.Close();

            string[] Html_Words = webBrowser1.DocumentText.ToString().Split(' ');
            if (NewListOfFL.Count==0) {
                changeVillage(1);
                richTextBox1.Text = DateTime.Now.ToLocalTime() + ": Did not send FL because NewListOfFL=0(better to have this message than a crash :shrug:)\n" + richTextBox1.Text;
                return;
            }
            for (int i = NewListOfFL.ElementAt(FL_num).From; i < NewListOfFL.ElementAt(FL_num).To; i++)
            {
                if (Html_Words[i].StartsWith("id=\"slot") && !Html_Words[i].Contains("row") && !Html_Words[i].Contains("td"))
                {
                    string[] only_id = Html_Words[i].Split('"');
                    for (int j = 0; j < ListOfFarms.Count; j++)
                    {
                        if (ListOfFarms.ElementAt(j).FarmName.Contains(only_id[1]))
                        {
                            for (int k = 0; k < DontFarm.Count; k++)
                            {
                                if (ListOfFarms.ElementAt(j).Name.IndexOf(DontFarm[k], StringComparison.CurrentCultureIgnoreCase) != -1) SendAttack = false;
                            }
                            if (SendAttack) {
                                switch (ListOfFarms.ElementAt(j).LastRaid)
                                {
                                    case 0:
                                        webBrowser1.Document.GetElementById(only_id[1]).InvokeMember("Click");
                                        break;
                                    case 1:
                                        webBrowser1.Document.GetElementById(only_id[1]).InvokeMember("Click");
                                        break;
                                    case 2:
                                        if (send2){webBrowser1.Document.GetElementById(only_id[1]).InvokeMember("Click");}
                                        break;
                                    case 3:
                                        if (send3){webBrowser1.Document.GetElementById(only_id[1]).InvokeMember("Click");}
                                        break;
                                }
                            }
                            SendAttack = true;
                        }
                    }
                }
            }
            webBrowser1.Document.GetElementById(NewListOfFL.ElementAt(FL_num).FLId).FirstChild.InvokeMember("submit");
            Console.WriteLine("Should have sent the FL " + count);
        }

        public void Read_FL()
        {
            NewListOfFL.Clear();
            ListOfFarms.Clear();

            string[] Html_Words = webBrowser1.DocumentText.ToString().Split(' ');
            int FarmLists = 0, FoundFarms = 0;
            for (int i = 0; i < Html_Words.Length; i++)
            {
                if (Html_Words[i].StartsWith("id=\"list") && !Html_Words[i].Contains("row") && !Html_Words[i].Contains("td"))//FARM LIST ID
                {
                    NewListOfFL.Add(new HelperClass.FarmLists
                    {
                        FLId = Html_Words[i].Split('\"')[1],
                    });
                }
                if (Html_Words[i].StartsWith("class=\"listTitleText\">"))//FARM LIST NAME
                {
                    string name = "";
                    int j = i;
                    j += 1;
                    while (Html_Words[j] != "<img")
                    {
                        name += Html_Words[j] + " ";
                        j++;
                    }
                    name = name.Replace(" ", String.Empty);
                    NewListOfFL.ElementAt(FarmLists).FLName = name;
                    //Debug.WriteLine(name);
                }
                if (Html_Words[i].StartsWith("id=\"slot") && !Html_Words[i].Contains("row") && !Html_Words[i].Contains("td")) //FARM
                {
                    ListOfFarms.Add(new HelperClass.Farm
                    {
                        FLId = NewListOfFL.ElementAt(FarmLists).FLId,
                        FLName = NewListOfFL.ElementAt(FarmLists).FLName,
                        FarmName = Html_Words[i]
                    });
                }
                if(Html_Words[i].Contains("href=\"position_details.php?x")){
                    string Fname = "";
                    int j = 0;
                    while (!Html_Words[i + j - 1].Contains("</a>") && !Html_Words[i + j].Contains("‎&#x"))
                    {
                        Fname += " " + Html_Words[i + j];
                        j++;
                    }
                    string[] name1 = Fname.Split('>');
                    Fname = name1[1];
                    if (name1[1].Contains("<"))
                    {
                        string[] name2 = name1[1].Split('<');
                        Fname = name2[0];
                    }
                    ListOfFarms.ElementAt(FoundFarms).Name = Fname;
                }
                if (Html_Words[i].StartsWith("class=\"lastRaid\""))
                {
                    if (Html_Words[i + 2].StartsWith("class=\"iReport")) // Is the last report
                    {
                        switch (Html_Words[i + 3])
                        {
                            case "iReport1\"":
                                ListOfFarms.ElementAt(FoundFarms).LastRaid = 1;
                                break;
                            case "iReport2\"":
                                ListOfFarms.ElementAt(FoundFarms).LastRaid = 2;
                                break;
                            case "iReport3\"":
                                ListOfFarms.ElementAt(FoundFarms).LastRaid = 3;
                                break;
                        }
                    }
                    else
                    { // There is no last report
                        ListOfFarms.ElementAt(FoundFarms).LastRaid = 0;
                    }
                    FoundFarms++;
                }
                if (Html_Words[i].StartsWith("id=\"button") && Html_Words[i + 1].StartsWith("class=\"green") && Html_Words[i + 2].StartsWith("\"") && // Send button
                    (Html_Words[i - 2].StartsWith("type=\"submit") || Html_Words[i - 3].StartsWith("type=\"submit") || Html_Words[i - 4].StartsWith("type=\"submit")))
                {
                    FarmLists++;
                }
            } //End of for loop
        }

        public void Update_Villages() {
            Read_Villages();
            con = new SQLiteConnection(tools.DB(sel_db));
            con.Open();
            sql = "DELETE FROM Villages";
            SQLiteCommand command = new SQLiteCommand(sql, con);
            command.ExecuteNonQuery();
            for (int i = 0; i < NewVillages.Count; i++) {
                sql = "insert into Villages(id, name) values('"
                + NewVillages.ElementAt(i).Id + "', '"
                + NewVillages.ElementAt(i).Name + "')";
                command = new SQLiteCommand(sql, con);
                command.ExecuteNonQuery();
            }
            con.Close();
        }
        public void Read_ALL(int villnum) {
            richTextBox1.Text = DateTime.Now.ToLocalTime() + ": Reading villages\n" + richTextBox1.Text;
            FLvillnum = villnum;
            Thread ReadThread = new Thread(new ThreadStart(Read_All_Thread));
            ReadThread.Start();
        }
        public void Read_All_Thread()
        {
            DoInvoke(delegate { Update_Villages(); });
            DoInvoke(delegate { changeVillage(FLvillnum); });
            Thread.Sleep(tools.ReturnRandom(500));
            DoInvoke(delegate { GoTo("https://" + sel_server + "/build.php?tt=99&id=39"); });
            Thread.Sleep(tools.ReturnRandom(500));
            DoInvoke(delegate {
                readLists();
            });
            Thread.Sleep(tools.ReturnRandom(500));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GoTo(textBox1.Text);
        }

        public void check() {

        }
        public void switchToFl(int villnum)
        {
            richTextBox1.Text = DateTime.Now.ToLocalTime() + ": Switching to FarmList, default village: "+villnum+"\n" + richTextBox1.Text;
            FLvillnum = villnum;
            if (!webBrowser1.DocumentText.ToString().Contains("id=\"raidList\">")) {
                Thread SwitchToFL_T = new Thread(new ThreadStart(SwitchToFl_Thread));
                SwitchToFL_T.Start();
            }
        }

        public void SwitchToFl_Thread()
        {
            DoInvoke(delegate { changeVillage(FLvillnum); });
            Thread.Sleep(tools.ReturnRandom(500));
            DoInvoke(delegate { GoTo("https://" + sel_server + "/build.php?tt=99&id=39"); });
        }

        public void Read_Villages() {
            NewVillages.Clear();
            string[] Html_Words = webBrowser1.DocumentText.ToString().Split(' ');
            int FoundVillages = 0;
            for (int i = 0; i < Html_Words.Length; i++)
            {
                if (Html_Words[i].StartsWith("href=\"?newdid="))
                {
                    string[] name1 = Html_Words[i].Split('"');
                    string[] name2 = name1[1].Split('a');
                    NewVillages.Add(new HelperClass.Villages
                    {
                        Id = name2[0]
                    });
                }
                if (Html_Words[i].StartsWith("class=\"name") && !Html_Words[i - 1].Contains("</a>"))
                {
                    string name = "";
                    int j = 0;
                    while (!Html_Words[i + j - 1].Contains("</div>"))
                    {
                        name += " " + Html_Words[i + j];
                        j++;
                    }
                    string[] name1 = name.Split('>');
                    string[] name2 = name1[1].Split('<');
                    if (NewVillages.Count() != 0)
                    {
                        NewVillages.ElementAt(FoundVillages).Name = name2[0];
                        FoundVillages++;
                    }
                }
            }
        }

        public void changeVillage(int villageNUM) {
            GoTo("https://" + sel_server + "/dorf1.php" + NewVillages.ElementAt(villageNUM).Id);
        }

        public void readLists() {
            Read_FL();
            bool FoundID = true;
            con = new SQLiteConnection(tools.DB(sel_db));
            con.Open();
            sql = "select * from FL order by num";
            SQLiteCommand command = new SQLiteCommand(sql, con);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                ListOfFL.Add(new HelperClass.FarmLists
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


                    ListOfFL.Add(new HelperClass.FarmLists
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
