using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics; //Debug.WriteLine("Hello");
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Threading;

namespace TravBot
{
    public partial class Form1 : Form
    {
        public string sel_name, sel_pass, sel_server, sel_db;

        //Timers
        private int timerlist, timercycle;
        private int FL_min, FL_max, FL_num_sent, FLvillnum;
        private bool login_finished = true;
        private readonly Form2 bot;

        private byte count = 0;

        public List<Village> Villages = new List<Village>();
        public List<FarmList> ListOfFL = new List<FarmList>();
        //SQL
        public string sql;
        public SQLiteConnection con;

        public Form1() //startup
        {
            bot = new Form2(this);
            InitializeComponent();
            bot.Activate();
            bot.Show();
            bot.Hide();
            if (!File.Exists("Accounts.sqlite"))
            {
                SQLiteConnection.CreateFile("Accounts.sqlite;");
                con = new SQLiteConnection(Helper.DB("Accounts"));
                con.Open();
                sql = "CREATE TABLE ACC (num INTEGER PRIMARY KEY AUTOINCREMENT, server TEXT, name TEXT, pass TEXT, db TEXT)";
                SQLiteCommand command = new SQLiteCommand(sql, con);
                command.ExecuteNonQuery();
                con.Close();
            }
            else
            {
                con = new SQLiteConnection(Helper.DB("Accounts"));
                con.Open();
                sql = "select * from ACC order by num";
                SQLiteCommand command = new SQLiteCommand(sql, con);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    comboBox1.Items.Add(reader["server"] + " -> " + reader["name"]);
                }
                con.Close();
            }
            if (comboBox1.Items.Count > 0) {
                comboBox1.SelectedIndex = 0;
            }
            tabControl1.Width = Screen.PrimaryScreen.Bounds.Width - 200;
            tabControl1.Height = Screen.PrimaryScreen.Bounds.Height - 100;
        }

        private void button1_Click(object sender, EventArgs e) //AddNewAcc
        {
            string new_db = textBox3.Text.Replace('.', '_') + "-" + textBox2.Text;
            SQLiteConnection.CreateFile(new_db + ".sqlite;");
            con = new SQLiteConnection(Helper.DB(new_db));
            con.Open();
            sql = "CREATE TABLE IF NOT EXISTS FL (num INTEGER PRIMARY KEY AUTOINCREMENT, FLid TEXT, FLname TEXT, Period NUMERIC, Enabled NUMERIC, Send2 NUMERIC, Send3 NUMERIC)";
            SQLiteCommand command = new SQLiteCommand(sql, con);
            command.ExecuteNonQuery();
            sql = "CREATE TABLE IF NOT EXISTS Villages (id TEXT, name TEXT)";
            command = new SQLiteCommand(sql, con);
            command.ExecuteNonQuery();
            sql = "CREATE TABLE IF NOT EXISTS FLgeneral (max NUMERICAL, min NUMERICAL, villnum NUMERICAL)";
            command = new SQLiteCommand(sql, con);
            command.ExecuteNonQuery();
            sql = "CREATE TABLE IF NOT EXISTS DontFarm (name TEXT)";
            command = new SQLiteCommand(sql, con);
            command.ExecuteNonQuery();
            sql = "insert into FLgeneral(max, min, villnum) values('120','60','0')";
            command = new SQLiteCommand(sql, con);
            command.ExecuteNonQuery();
            sql = "insert into DontFarm (name) values('PREVZETO')";
            command = new SQLiteCommand(sql, con);
            command.ExecuteNonQuery();
            sql = "insert into DontFarm (name) values('CHIEFED')";
            command = new SQLiteCommand(sql, con);
            command.ExecuteNonQuery();
            sql = "insert into DontFarm (name) values('CONQUERED')";
            command = new SQLiteCommand(sql, con);
            command.ExecuteNonQuery();
            con.Close();
            //sql = "CREATE TABLE ACC (num INTEGER PRIMARY KEY AUTOINCREMENT, server TEXT, name TEXT, pass TEXT)";

            con = new SQLiteConnection(Helper.DB("Accounts"));
            con.Open();
            sql = "insert into ACC(server, name, pass, db) values('" + textBox3.Text + "', '" + textBox2.Text + "', '" + textBox1.Text + "', '" + new_db + "')";
            command = new SQLiteCommand(sql, con);
            command.ExecuteNonQuery();
            con.Close();
            comboBox1.Items.Add(textBox3.Text + " -> " + textBox2.Text);
            comboBox1.SelectedIndex = 0;
        }

        private void button2_Click(object sender, EventArgs e) //---------------------login-------------------
        {
            if (comboBox1.SelectedIndex == -1) { return; }
            int i = 0;
            con = new SQLiteConnection(Helper.DB("Accounts"));
            con.Open();
            sql = "select * from ACC order by num";
            SQLiteCommand command = new SQLiteCommand(sql, con);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (i == comboBox1.SelectedIndex)
                {
                    sel_server = reader["server"] + "";
                    sel_name = reader["name"] + "";
                    sel_pass = reader["pass"] + "";
                    sel_db = reader["db"] + "";
                }
                i++;
            }
            con.Close();

            Thread GetThread = new Thread(new ThreadStart(Get));
            GetThread.Start();

            con = new SQLiteConnection(Helper.DB(sel_db));
            con.Open();
            sql = "Select * from FLgeneral";
            command = new SQLiteCommand(sql, con);
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                FL_max = Convert.ToInt16(reader["max"] + "");
                FL_min = Convert.ToInt16(reader["min"] + "");
                FLvillnum = Convert.ToInt16(reader["villnum"] + "");
            }
            numericUpDown1.Value = FL_min;
            numericUpDown2.Value = FL_max;
            sql = "Select * from DontFarm";
            command = new SQLiteCommand(sql, con);
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                richTextBox2.AppendText(reader["name"] + "\n");
            }
            con.Close();
            Village_update();
            FL_box_update();
            if (Villages.Count != 0)
            {
                VIllNameSwitch.Text = Villages.ElementAt(FLvillnum).Name;
                comboBox2.SelectedIndex = FLvillnum;
            }
            login_finished = false;
        }//------------------------------END OF LOGIN----------------------------------------------

        private void button4_Click(object sender, EventArgs e) //togle webbrowser
        {
            if (bot.Visible)bot.Hide();
            else bot.Show();
        }
        public void Get() {
            DoInvoke(delegate { bot.Login(sel_name, sel_pass, sel_server, sel_db); });
        }
        private void button3_Click(object sender, EventArgs e) //logout
        {
            // TODO: add
        }

        public void button5_Click(object sender, EventArgs e) //READ FL
        {
            ReadAll();
        }

        private void button6_Click(object sender, EventArgs e) //GO TO FL
        {
            bot.GoTo("https://" + sel_server + "/build.php?tt=99&id=39");
        }


        public void checkBox2_CheckedChanged(object sender, EventArgs e) //ENABLE/DISABLE FARM
        {
            if (listBox1.SelectedIndex == -1) return;
            ListOfFL.ElementAt(listBox1.SelectedIndex).Enabled = checkBox2.Checked;
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e) //PERIOD OF SENDING
        {
            ListOfFL.ElementAt(listBox1.SelectedIndex).Period = Convert.ToInt32(numericUpDown3.Value);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) //LISTBOX OF FLS
        {
            try
            {
                checkBox2.Enabled = true;
                checkBox3.Enabled = true;
                checkBox4.Enabled = true;
                numericUpDown3.Enabled = true;
                label10.Text = ListOfFL.ElementAt(listBox1.SelectedIndex).FLName;
                numericUpDown3.Value = ListOfFL.ElementAt(listBox1.SelectedIndex).Period;
                checkBox2.Checked = ListOfFL.ElementAt(listBox1.SelectedIndex).Enabled;
                checkBox3.Checked = ListOfFL.ElementAt(listBox1.SelectedIndex).Send2;
                checkBox4.Checked = ListOfFL.ElementAt(listBox1.SelectedIndex).Send3;
            }
            catch (Exception) {
                richTextBox1.Text = DateTime.Now.ToLocalTime() + ": ERROR AT LISTBOX1 INDEXCHANGE\n" + richTextBox1.Text;
            }

        }
        private void Timer_Cycle_Tick(object sender, EventArgs e)
        {
            LabelNextCycle.Text = "Next Cycle in:" + timercycle;
            if (timercycle == 5) bot.Read_Villages();
            if (timercycle == 3) bot.SwitchToFl(FLvillnum);
            if (timercycle <= 0) {
                Timer_List.Start();
                Timer_Cycle.Stop();
                FL_num_sent = ListOfFL.Count;
                bot.Read_Villages();

            }
            timercycle--;
        }

        private void Timer_List_Tick_1(object sender, EventArgs e)
        {
            Label_FL_Count.Text = "FL to send: " + FL_num_sent;
            Label_Period.Text = "timerlist: " + timerlist;
            int indexNum = ListOfFL.Count - FL_num_sent;
            if (timerlist > Helper.ReturnSec(2))
            {
                if (ListOfFL.ElementAt(indexNum).Enabled)
                {
                    Label_Period.Text = "Period number of " + ListOfFL.ElementAt(indexNum).FLName + " is " + ListOfFL.ElementAt(indexNum).Period_num;
                    if (ListOfFL.ElementAt(indexNum).Period <= ListOfFL.ElementAt(indexNum).Period_num)
                    {
                        try
                        {
                            count++;
                            Console.WriteLine("Sending FL " + indexNum+"   Count "+count);
                            ListOfFL.ElementAt(indexNum).Period_num = 1;
                            richTextBox1.Text = DateTime.Now.ToLocalTime() + ": Will send FL num: " + indexNum + " Total FL objects:" + ListOfFL.Count + "\n" + richTextBox1.Text;
                            bot.SendFarmlist(indexNum, ListOfFL.ElementAt(indexNum).Send2, ListOfFL.ElementAt(indexNum).Send3);
                        }
                        catch (Exception) {
                            richTextBox1.Text = DateTime.Now.ToLocalTime() + ": ERROR AT SENDING FL\n" + richTextBox1.Text;
                        }
                    }
                    else ListOfFL.ElementAt(indexNum).Period_num++;
                }
                FL_num_sent--;
                Label_FL_Count.Text = "FL to send: " + FL_num_sent;
                timerlist = 0;
            }

            if (FL_num_sent == 0)
            {
                timercycle = Helper.ReturnRandom(FL_min, FL_max);
                Timer_List.Stop();
                Timer_Cycle.Start();
            }
            timerlist++;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e) //SEND2
        {
            ListOfFL.ElementAt(listBox1.SelectedIndex).Send2 = checkBox3.Checked;
        }
        private void numericUpDown1_ValueChanged(object sender, EventArgs e) //min Fl period
        {
            FL_min = Convert.ToInt32(numericUpDown1.Value);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            ReadAll();
        }

        private void Start_Click(object sender, EventArgs e)
        {
            timercycle = 7;
            Timer_Cycle.Start();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            FLvillnum = comboBox2.SelectedIndex;
            VIllNameSwitch.Text = Villages.ElementAt(FLvillnum).Name;
        }


        private void button7_Click(object sender, EventArgs e)
        {
            Timer_Cycle.Stop();
            Timer_List.Stop();
            Label_FL_Count.Text = "FL count: ";
            LabelNextCycle.Text = "Next Cycle in: ";
        }
        private void numericUpDown2_ValueChanged(object sender, EventArgs e) //max FL period
        {
            FL_max = Convert.ToInt32(numericUpDown2.Value);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            string imena = richTextBox2.Text.Replace(" ", String.Empty);
            string[] text = richTextBox2.Text.Split('\n');
            con = new SQLiteConnection(Helper.DB(sel_db));
            con.Open();
            sql = "DELETE FROM DontFarm";
            SQLiteCommand command = new SQLiteCommand(sql, con);
            command.ExecuteNonQuery();
            for (int i = 0; i < text.Length; i++)
            {
                if (!(text[i] == String.Empty) && !(text[i] == " "))
                {
                    sql = "insert into DontFarm(name) values('" + text[i] + "')";
                    command = new SQLiteCommand(sql, con);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            con = new SQLiteConnection(Helper.DB(sel_db));
            con.Open();
            sql = "DELETE FROM FL";
            SQLiteCommand command = new SQLiteCommand(sql, con);
            command.ExecuteNonQuery();
            con.Close();
        }

        private void button12_Click(object sender, EventArgs e) //Delete all accs
        {
            con = new SQLiteConnection(Helper.DB("Accounts"));
            con.Open();
            sql = "DELETE FROM ACC";
            SQLiteCommand command = new SQLiteCommand(sql, con);
            command.ExecuteNonQuery();
            con.Close();
            comboBox1.Items.Clear();
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e) //SEND3
        {
            ListOfFL.ElementAt(listBox1.SelectedIndex).Send3 = checkBox4.Checked;
        }

        public void ReadAll() {
            bot.Read_ALL(FLvillnum);
            Thread ReadThread = new Thread(new ThreadStart(Read_All_Thread));
            ReadThread.Start();
        }
        public void Read_All_Thread()
        {
            Thread.Sleep(Helper.ReturnRandom(4000));
            DoInvoke(delegate {
                FL_box_update();
                Village_update();
            });

        }


        private void DoInvoke(MethodInvoker del)
        {
            if (InvokeRequired) { Invoke(del); }
            else { del(); }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) //ON CLOSING
        {
            if (!login_finished)
            {
                con = new SQLiteConnection(Helper.DB(sel_db));
                con.Open();
                sql = "DELETE FROM FLgeneral";
                SQLiteCommand command = new SQLiteCommand(sql, con);
                command.ExecuteNonQuery();
                sql = "DELETE FROM FL";
                command = new SQLiteCommand(sql, con);
                command.ExecuteNonQuery();
                for (int i = 0; i < ListOfFL.Count; i++)
                {
                    sql = "insert into FL(period, enabled, FLid, FLName, Send2, Send3) values('"
                        + ListOfFL.ElementAt(i).Period + "', '"
                        + Convert.ToByte(ListOfFL.ElementAt(i).Enabled) + "', '"
                        + ListOfFL.ElementAt(i).FLId + "', '"
                        + ListOfFL.ElementAt(i).FLName + "', '"
                        + Convert.ToByte(ListOfFL.ElementAt(i).Send2) + "', '"
                        + Convert.ToByte(ListOfFL.ElementAt(i).Send3) + "')";
                    command = new SQLiteCommand(sql, con);
                    command.ExecuteNonQuery();
                }

                sql = "insert into FLgeneral(max, min, villnum) values('" + FL_max + "','" + FL_min + "','"+ FLvillnum + "')";
                command = new SQLiteCommand(sql, con);
                command.ExecuteNonQuery();
                con.Close();
            }
        }

        public void FL_box_update()
        {
            listBox1.Items.Clear();
            ListOfFL.Clear();
            con = new SQLiteConnection(Helper.DB(sel_db));
            con.Open();
            sql = "select * from FL";
            SQLiteCommand command = new SQLiteCommand(sql, con);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                listBox1.Items.Add("->"+reader["FLname"] + "");
                ListOfFL.Add(new FarmList
                {
                    Period = Convert.ToInt32(reader["Period"] + ""),
                    Enabled = Convert.ToBoolean(reader["Enabled"]),
                    Send2 = Convert.ToBoolean(reader["Send2"]),
                    Send3 = Convert.ToBoolean(reader["Send3"]),
                    FLId = reader["FLid"] + "",
                    FLName = reader["FLname"] + "",
                    Period_num = 1,
                });
            }
            con.Close();
        }

        public void Village_update() {
            Villages.Clear();
            comboBox2.Items.Clear();
            con = new SQLiteConnection(Helper.DB(sel_db));
            con.Open();
            sql = "select * from Villages";
            SQLiteCommand command = new SQLiteCommand(sql, con);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                comboBox2.Items.Add(reader["name"] + "");
                Villages.Add(new Village
                {
                    Id = reader["id"]+"",
                    Name = reader["name"]+"",
                });
            }
            con.Close();
        }
    }
}