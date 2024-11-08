using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using System.Xml;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;
using System.Web.Configuration;
using StackExchange.Redis;
using JsWebServer_CP.Scripts;

namespace JsWebServer_CP.datas
{
    public partial class ljdata : System.Web.UI.Page
    {
        QuestManager Qm = new QuestManager();
        static Random Ran = new Random();
        static MySqlCommand scom = new MySqlCommand();
        static string cst = "redis-hfh9.cdb.ntruss.com:6379";

        static string conn = "localhost";
        static Random Rd = new Random();

        static ConfigurationOptions option = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            EndPoints = { conn }
        };

        ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(conn);

        IDatabase cache;

        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            return ConnectionMultiplexer.Connect(option);
        });

        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }

        protected virtual void DoTask()
        {
            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper_test"].ConnectionString))
            {
                scon.Open();
                try
                {
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                    string User_Code = Request["User_Code"];
                    string Auth_Type = Request["Auth_Type"];
                    XmlTextWriter writer =
                    new XmlTextWriter(this.Response.OutputStream, System.Text.Encoding.UTF8);
                    MySqlCommand cmd = new MySqlCommand("Get_User_Info", scon);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("User_Code", User_Code);
                    cmd.Parameters.AddWithValue("Auth_Type", Auth_Type);

                    writer.Formatting = System.Xml.Formatting.Indented;
                    writer.WriteStartDocument();
                    writer.WriteStartElement("user_data");

                    MySqlDataReader rd = cmd.ExecuteReader();
                    if (!rd.HasRows)
                    {
                        if (rd != null)
                            rd.Close();

                        MySqlCommand Sign_User = new MySqlCommand();
                        Sign_User.Connection = scon;
                        Sign_User.CommandText = "Sign_User";
                        Sign_User.CommandType = System.Data.CommandType.StoredProcedure;
                        Sign_User.Parameters.AddWithValue("?User_Code", User_Code);
                        //Sign_User.Parameters.AddWithValue("@Auth_Type", Auth_Type);

                        for (int i = 0; i < 3; i++)
                        {
                            JObject B_Quest = Qm.Base_Quest(i);
                            Sign_User.Parameters.AddWithValue("?v_Quest" + i, JsonConvert.SerializeObject(B_Quest));
                        }

                        int Sign_Usered = Sign_User.ExecuteNonQuery();

                        if (Sign_Usered == 0)
                        {
                            Response.Write("이미 가입된 유저");
                            rd.Close();
                            return;
                        }
                    }

                    if (!rd.IsClosed)
                        rd.Close();

                    bool Today_Refresh = false;

                    MySqlCommand User_Info = new MySqlCommand();
                    User_Info.Connection = scon;
                    User_Info.CommandText = "Get_User_Info";
                    User_Info.CommandType = System.Data.CommandType.StoredProcedure;
                    User_Info.Parameters.AddWithValue("User_Code", User_Code);
                    User_Info.Parameters["User_Code"].Direction = System.Data.ParameterDirection.Input;

                    MySqlDataReader Info_Rd = User_Info.ExecuteReader();
                    while (Info_Rd.Read())
                    {
                        writer.WriteStartElement("User_Info");
                        for (int j = 0; j < Info_Rd.FieldCount; j++)
                        {
                            writer.WriteAttributeString(Info_Rd.GetName(j), Info_Rd[j].ToString());
                            if (j == Info_Rd.FieldCount - 3)
                            {
                                int bools = System.Convert.ToInt32(Info_Rd[j].ToString());
                                if (bools == 0)
                                {
                                    Today_Refresh = false;
                                }
                                else
                                {
                                    Today_Refresh = true;
                                }

                            }
                        }
                        string userid = Auth_Type + ":" + User_Code + "*";
                        cache = connection.GetDatabase();
                        for (int i = 1; i < 5; i++)
                        {
                            IEnumerable<SortedSetEntry> My_List = cache.SortedSetScan(i + "Theme", userid);

                            List<string> Rank_Datas = new List<string>();

                            foreach (SortedSetEntry d in My_List)
                            {
                                Rank_Datas.Add(d.Element.ToString() + "/" + d.Score);
                            }

                            if (Rank_Datas.Count >= 1)
                            {
                                JObject MyRanking = new JObject();
                                string[] top_data = Rank_Datas[Rank_Datas.Count - 1].Split('/');

                                string user_key = top_data[0];

                                long? s = cache.SortedSetRank(i + "Theme", user_key, Order.Descending) + 1;

                                string[] user_data = user_key.Split(':');

                                MyRanking.Add("Auth_Type", user_data[0]);
                                MyRanking.Add("User_Nick", cache.HashGet("Nickname", user_data[1]).ToString());
                                MyRanking.Add("Id", user_data[1]);
                                MyRanking.Add("Char_Id", user_data[2]);
                                MyRanking.Add("Score", top_data[1]);
                                MyRanking.Add("Rank", s);

                                writer.WriteAttributeString("Theme_" + i + "Rank", JsonConvert.SerializeObject(MyRanking));
                            }
                        }

                        writer.WriteEndElement();
                    }
                    connection.Close();
                    Info_Rd.Close();

                    MySqlCommand Char_Info = new MySqlCommand();
                    Char_Info.Connection = scon;
                    Char_Info.CommandText = "Get_Char_Info";
                    Char_Info.CommandType = System.Data.CommandType.StoredProcedure;
                    Char_Info.Parameters.AddWithValue("User_Code", User_Code);
                    Char_Info.Parameters["User_Code"].Direction = System.Data.ParameterDirection.Input;

                    MySqlDataReader Char_Info_Rd = Char_Info.ExecuteReader();
                    while (Char_Info_Rd.Read())
                    {
                        writer.WriteStartElement("Char_Info");
                        for (int j = 0; j < Char_Info_Rd.FieldCount; j++)
                        {
                            writer.WriteAttributeString(Char_Info_Rd.GetName(j), Char_Info_Rd[j].ToString());
                        }
                        writer.WriteEndElement();
                    }
                    Char_Info_Rd.Close();

                    MySqlCommand Effect_Info = new MySqlCommand();
                    Effect_Info.Connection = scon;
                    Effect_Info.CommandText = "Get_Effect_Info";
                    Effect_Info.CommandType = System.Data.CommandType.StoredProcedure;
                    Effect_Info.Parameters.AddWithValue("User_Code", User_Code);
                    Effect_Info.Parameters["User_Code"].Direction = System.Data.ParameterDirection.Input;

                    MySqlDataReader Effect_Info_Rd = Effect_Info.ExecuteReader();
                    while (Effect_Info_Rd.Read())
                    {
                        writer.WriteStartElement("Effect_Info");
                        for (int j = 0; j < Effect_Info_Rd.FieldCount; j++)
                        {
                            writer.WriteAttributeString(Effect_Info_Rd.GetName(j), Effect_Info_Rd[j].ToString());
                        }
                        writer.WriteEndElement();
                    }
                    Effect_Info_Rd.Close();


                    //---------------------------------------------------//

                    MySqlCommand Quest_Ask = new MySqlCommand();
                    Quest_Ask.Connection = scon;
                    Quest_Ask.CommandText = "Get_Quest_Info";
                    Quest_Ask.CommandType = System.Data.CommandType.StoredProcedure;
                    Quest_Ask.Parameters.AddWithValue("User_Code", User_Code);
                    Quest_Ask.Parameters["User_Code"].Direction = System.Data.ParameterDirection.Input;

                    MySqlDataReader Quest_rd = Quest_Ask.ExecuteReader();
                    JObject Results = new JObject();

                    JArray Quests = new JArray();

                    while (Quest_rd.Read())
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            JObject Quest = new JObject();
                            string info = Quest_rd["Quest" + j].ToString();
                            if (info.Equals(""))
                            {
                                Quest = Qm.Base_Quest(j);
                            }
                            else
                            {
                                Quest = JObject.Parse(info);
                            }
                            Quests.Add(Quest);
                        }
                    }

                    Quest_rd.Close();

                    DateTime Today = DateTime.Now;

                    MySqlCommand Quest_Set_Cmd = new MySqlCommand();
                    Quest_Set_Cmd.Connection = scon;
                    Quest_Set_Cmd.CommandText = "Today_Refresh_Quest";
                    Quest_Set_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Quest_Set_Cmd.Parameters.AddWithValue("?User_Code", User_Code);
                    Quest_Set_Cmd.Parameters["?User_Code"].Direction = System.Data.ParameterDirection.Input;

                    for (int i = 0; i < 3; i++)
                    {

                        DateTime Cleared_Time;
                        string CDate = Quests[i]["Cleared_Date"].ToString();

                        if (CDate.Equals("0"))
                        {
                            Cleared_Time = DateTime.MinValue;
                        }
                        else
                        {
                            Cleared_Time = System.Convert.ToDateTime(CDate);
                        }

                        string RDate = Quests[i]["Date"].ToString();
                        DateTime Received_Time;
                        if (RDate.Equals("0"))
                        {
                            Received_Time = DateTime.MinValue;
                        }
                        else
                        {
                            Received_Time = System.Convert.ToDateTime(RDate);
                        }

                        if (Today_Refresh)
                        {
                            Quests[i] = Qm.Quest_Generate(i, User_Code);
                        }

                        string Pa_name = "?v_Quest" + i;

                        Quest_Set_Cmd.Parameters.AddWithValue(Pa_name, JsonConvert.SerializeObject(Quests[i]));
                    }

                    MySqlDataReader Quest_Infos = Quest_Set_Cmd.ExecuteReader();

                    while (Quest_Infos.Read())
                    {
                        writer.WriteStartElement("Quest");
                        for (int j = 0; j < Quest_Infos.FieldCount; j++)
                        {
                            //노드와 값 설정
                            writer.WriteAttributeString(Quest_Infos.GetName(j), Quest_Infos[j].ToString());
                        }
                        writer.WriteEndElement();
                    }

                    Quest_Infos.Close();

                    
                    /////////////////////////////////////////////////////////////////////////
                    MySqlCommand WQuest_Ask = new MySqlCommand();
                    WQuest_Ask.Connection = scon;
                    WQuest_Ask.CommandText = "Get_WQuest_Info";
                    WQuest_Ask.CommandType = System.Data.CommandType.StoredProcedure;
                    WQuest_Ask.Parameters.AddWithValue("User_Code", User_Code);
                    WQuest_Ask.Parameters["User_Code"].Direction = System.Data.ParameterDirection.Input;

                    MySqlDataReader WQuest_rd = WQuest_Ask.ExecuteReader();
                    JObject WResults = new JObject();

                    JArray WQuests = new JArray();


                    while (WQuest_rd.Read())
                    {
                        writer.WriteStartElement("WQuest");

                        for (int j = 0; j < WQuest_rd.FieldCount; j++)
                        {
                            writer.WriteAttributeString(WQuest_rd.GetName(j), WQuest_rd[j].ToString());
                        }
                        writer.WriteEndElement();
                    }

                    WQuest_rd.Close();
                    
                    //---------------------------------------------------//
                    scon.Close();

                    writer.WriteEndDocument();
                    Response.ContentEncoding = System.Text.Encoding.UTF8;
                    Response.ContentType = "text/xml";
                    writer.Flush();
                    writer.Close();
                    if (rd != null)
                        rd.Close();
                }
                catch (Exception e)
                {
                    scon.Close();
                    connection.Close();
                    Response.Write(e.Message + "|" + e.ToString());
                }
            }
        }


        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Clear();

            Action action = new Action(DoTask);
            var task = Task.Factory.FromAsync(action.BeginInvoke, action.EndInvoke, null);
            Thread.Sleep(10);
            task.Wait();
        }

        public void Base_Char()
        {
            JObject Results = new JObject();
            JArray Ja = new JArray();

            for (int i = 0; i < 1; i++)
            {
                int char_id = 1;
                var cha_info = new JObject();
                cha_info.Add("char_id", char_id);
                cha_info.Add("add_date", DateTime.Now);

                Ja.Add(cha_info);
            }

            Results.Add("Characters", Ja);

            Response.Write(JsonConvert.SerializeObject(Results));
        }
    }
}