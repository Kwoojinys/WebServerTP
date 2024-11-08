using System;
using System.Data;
using System.Web.Mvc;
using JsWebServer_CP.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;
using System.Web.Configuration;
using StackExchange.Redis;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Collections.Specialized;
using System.Web.Script.Serialization;
using Microsoft.Owin.Logging;
using WebGrease;
using System.Globalization;
using System.Threading;
using JsWebServer_CP.Scripts;

namespace JsWebServer_CP.Controllers
{
    public class LJ_UserController : AsyncController
    {
        QuestManager Qm = new QuestManager();

        #region Setting
        private ApplicationDbContext db = new ApplicationDbContext();
        static MySqlCommand scom = new MySqlCommand();
        static Random Ran = new Random();

        //static string cst = "redis-hfh9.cdb.ntruss.com:6379";
        static string cst = "redis-nj2f.cdb.ntruss.com:6379";
        static string new_cst = "redis-nj2f.cdb.ntruss.com:6379";

        static string conn = "localhost";
        static Random Rd = new Random();

        static ConfigurationOptions option = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            EndPoints = { cst }
        };

        ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(option);

        IDatabase cache;
        IDatabase new_cache;

        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            ConfigurationOptions option = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                EndPoints = { cst },
            };
            return ConnectionMultiplexer.Connect(option);
        });

        private static Lazy<ConnectionMultiplexer> lazyConnections = new Lazy<ConnectionMultiplexer>(() =>
        {
            ConfigurationOptions option = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                EndPoints = { cst },
            };
            return ConnectionMultiplexer.Connect(option);
        });

        public static ConnectionMultiplexer Connections
        {
            get
            {
                return lazyConnections.Value;
            }
        }


        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }

        private static Lazy<ConnectionMultiplexer> New_lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            ConfigurationOptions option = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                EndPoints = { new_cst }
            };
            return ConnectionMultiplexer.Connect(option);
        });


        public static ConnectionMultiplexer New_Connection
        {
            get
            {
                return New_lazyConnection.Value;
            }
        }
        #endregion

        // GET: UserSet
        public void Conn_TestAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string Result = "접속 테스트 성공";
            AsyncManager.Parameters["Result"] = Result;
            AsyncManager.OutstandingOperations.Decrement();
        }

        public string Conn_TestCompleted(string Result)
        {
            return Result;
        }

        public void Ranking_BackUp()
        {
            cache = connection.GetDatabase();
            new_cache = New_Connection.GetDatabase();

            HashEntry[] ranks2 = cache.HashGetAll("Nickname");
            for (int i = 0; i < ranks2.Length; i++)
            {
                new_cache.HashSet("Nickname", new HashEntry[] { new HashEntry(ranks2[i].Name, ranks2[i].Value) });
            }

            Response.Write("Migrate Complete! Count : " + ranks2.Length);
        }

        #region Quest
        public void Quest_ClearAsync(/*string User_Code, string In_id*/) // 퀘스트 클리어
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            //int Target_Id = System.Convert.ToInt32(Request.Form["Target_Ids"]);

            int In_id = System.Convert.ToInt32(Request.Form["In_ids"]);

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    MySqlCommand Check_String_Cmd = new MySqlCommand();

                    Check_String_Cmd.Connection = scon;
                    Check_String_Cmd.CommandText = "Get_Quest_Check";
                    Check_String_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Check_String_Cmd.Parameters.AddWithValue("?UserCode", User_Code);
                    Check_String_Cmd.Parameters.AddWithValue("?I_id", In_id);
                    for (int i = 0; i < Check_String_Cmd.Parameters.Count; i++)
                    {
                        Check_String_Cmd.Parameters[i].Direction = ParameterDirection.Input;
                    }

                    MySqlDataReader Status_Reader = Check_String_Cmd.ExecuteReader();

                    JObject Results = new JObject();
                    JObject Quest_Info = new JObject();

                    int Id = 0;
                    int Condition_Value = 0;
                    int Current_Value = 0;
                    DateTime R_Date = System.DateTime.MinValue;
                    int I_id = 0;

                    if (Status_Reader.Read())
                    {
                        string info = Status_Reader["current_value"].ToString();
                        Quest_Info = JObject.Parse(info);
                        Id = Convert.ToInt32(Quest_Info["Id"]);
                        I_id = Convert.ToInt32(Quest_Info["I_id"]);
                        Current_Value = Convert.ToInt32(Quest_Info["Value"]);
                        R_Date = Convert.ToDateTime(Quest_Info["Date"]);
                    }

                    Status_Reader.Close();

                    JObject New_Quest = new JObject();
                    New_Quest.Add("I_id", In_id);
                    New_Quest.Add("Id", 0);
                    New_Quest.Add("Date", R_Date);
                    New_Quest.Add("Cleared_Date", System.DateTime.Now);
                    New_Quest.Add("Value", "0");

                    string Cleared = JsonConvert.SerializeObject(New_Quest);

                    MySqlCommand Clear_String_Cmd = new MySqlCommand();
                    Clear_String_Cmd.Connection = scon;
                    Clear_String_Cmd.CommandText = "Clear_Quest";
                    Clear_String_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Clear_String_Cmd.Parameters.AddWithValue("?User_Code", User_Code);
                    Clear_String_Cmd.Parameters.AddWithValue("?v_Quest", Cleared);
                    Clear_String_Cmd.Parameters.AddWithValue("?Internal_id", In_id);
                    Clear_String_Cmd.Parameters.AddWithValue("?Q_id", Id);
                    Clear_String_Cmd.Parameters.AddWithValue("?q_value", Current_Value);
                    for (int i = 0; i < Clear_String_Cmd.Parameters.Count; i++)
                    {
                        Clear_String_Cmd.Parameters[i].Direction = ParameterDirection.Input;
                    }

                    MySqlDataReader Clear_Reader = Clear_String_Cmd.ExecuteReader();
                    JArray Quest_List = new JArray();
                    JObject User_info = new JObject();
                    string[] Quest_Infos = new string[3];
                    if (Clear_Reader.Read())
                    {
                        for (int i = 0; i < Quest_Infos.Length; i++)
                        {
                            Quest_Infos[i] = Clear_Reader["Quest" + i].ToString();
                            JObject Quests = new JObject();

                            if (Quest_Infos[i].Equals(""))
                            {
                                Quest_Info = Qm.Quest_Base(i);
                            }
                            else
                            {
                                Quests = JObject.Parse(Quest_Infos[i]);
                            }

                            Quest_List.Add(Quests);
                        }

                        User_info.Add("UserGold", Clear_Reader["UserGold"].ToString());

                        Results.Add("Quests", Quest_List);
                        Results.Add("User_Info", User_info);
                        AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    }
                    else
                    {
                        AsyncManager.Parameters["Result"] = "-1";
                    }

                    Clear_Reader.Close();
                    scon.Close();
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Quest_ClearCompleted(string Result)
        {
            return Result;
        }

        public void Test_Get2Async(/*string User_Code, int isAd, int In_id*/) // 퀘스트 받기
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            int isAd = System.Convert.ToInt32(Request.Form["isAd"]);
            int In_id = System.Convert.ToInt32(Request.Form["In_id"]);
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    DateTime Last_Ref_Time = DateTime.Now;
                    MySqlCommand Quest_Set_Cmd = new MySqlCommand();
                    Quest_Set_Cmd.Connection = scon;
                    Quest_Set_Cmd.CommandText = "Get_Q_Status";
                    Quest_Set_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Quest_Set_Cmd.Parameters.AddWithValue("?User_Code", User_Code);
                    for (int i = 0; i < Quest_Set_Cmd.Parameters.Count; i++)
                    {
                        Quest_Set_Cmd.Parameters[i].Direction = ParameterDirection.Input;
                    }

                    MySqlDataReader Quest_Set_Reader = Quest_Set_Cmd.ExecuteReader();

                    JObject Results = new JObject();
                    JArray Quest_List = new JArray();
                    string[] Quest_Infos = new string[3];

                    if (Quest_Set_Reader.Read())
                    {
                        for (int i = 0; i < Quest_Infos.Length; i++)
                        {
                            Quest_Infos[i] = Quest_Set_Reader["Quest" + i].ToString();
                            JObject Quest_Info = new JObject();

                            if (Quest_Infos[i].Equals(""))
                            {
                                Quest_Info = Qm.Quest_Base(i);
                            }
                            else
                            {
                                Quest_Info = JObject.Parse(Quest_Infos[i]);
                            }

                            Quest_List.Add(Quest_Info);
                        }

                        string Time = Quest_Set_Reader["Ref_Time"].ToString();
                        if (!Time.Equals(""))
                        {
                            Last_Ref_Time = System.Convert.ToDateTime(Time);
                        }
                    }

                    Quest_Set_Reader.Close();

                    if (Last_Ref_Time > DateTime.Now)
                    {
                        AsyncManager.Parameters["Result"] = "Don't be evil.";
                        scon.Close();
                        AsyncManager.OutstandingOperations.Decrement();
                        return;
                    }

                    int[] Received_Quest_Ids = new int[3];

                    for (int i = 0; i < Received_Quest_Ids.Length; i++)
                    {
                        Received_Quest_Ids[i] = System.Convert.ToInt32(Quest_List[i]["Id"]);
                    }

                    MySqlCommand Refresh_Quest_Cmd = new MySqlCommand();
                    Refresh_Quest_Cmd.Connection = scon;
                    Refresh_Quest_Cmd.CommandText = "Ref_Time_Quests";
                    Refresh_Quest_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Refresh_Quest_Cmd.Parameters.AddWithValue("?User_Code", User_Code);
                    Refresh_Quest_Cmd.Parameters.AddWithValue("?Next_Time", DateTime.Now.AddMinutes(10));

                    for (int i = 0; i < Quest_List.Count; i++)
                    {
                        if (i == In_id)
                        {
                            if (!Quest_List[i]["Cleared_Date"].ToString().Equals("0"))
                            {
                                DateTime Cleared_Date = System.Convert.ToDateTime(Quest_List[i]["Cleared_Date"]);
                                if (Cleared_Date.AddHours(4) > System.DateTime.Now)
                                {
                                    if (isAd == 0)
                                    {
                                        AsyncManager.Parameters["Result"] = "Don't be evil.";
                                        scon.Close();
                                        AsyncManager.OutstandingOperations.Decrement();
                                        return;
                                    }
                                    else
                                    {
                                        Quest_List[i] = Qm.Quest_Generate2(i, User_Code, Received_Quest_Ids);
                                        Received_Quest_Ids[i] = System.Convert.ToInt32(Quest_List[i]["Id"]);
                                    }
                                }
                                else
                                {
                                    if (isAd == 0)
                                    {
                                        AsyncManager.Parameters["Result"] = "Don't be evil.";
                                        scon.Close();
                                        AsyncManager.OutstandingOperations.Decrement();
                                        return;
                                    }
                                    else
                                    {
                                        Quest_List[i] = Qm.Quest_Generate2(i, User_Code, Received_Quest_Ids);
                                        Received_Quest_Ids[i] = System.Convert.ToInt32(Quest_List[i]["Id"]);
                                    }
                                }
                            }
                        }

                        Refresh_Quest_Cmd.Parameters.AddWithValue("?v_Quest" + i, JsonConvert.SerializeObject(Quest_List[i]));
                    }

                    int Refreshed = Refresh_Quest_Cmd.ExecuteNonQuery();
                    if (Refreshed >= 1)
                    {
                        Results.Add("Quests", Quest_List);
                        Results.Add("Next_Time", DateTime.Now.AddMinutes(10));
                        AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    }
                    else
                    {
                        AsyncManager.Parameters["Result"] = "Fuck You";
                    }

                    scon.Close();
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Test_Get2Completed(string Result)
        {
            return Result;
        }

        public void Test_GetAsync(/*string User_Code, int isAd, int In_id*/) // 퀘스트 받기
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            int isAd = System.Convert.ToInt32(Request.Form["isAd"]);
            int In_id = System.Convert.ToInt32(Request.Form["In_id"]);
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    DateTime Last_Ref_Time = DateTime.Now;
                    MySqlCommand Quest_Set_Cmd = new MySqlCommand();
                    Quest_Set_Cmd.Connection = scon;
                    Quest_Set_Cmd.CommandText = "Get_Q_Status";
                    Quest_Set_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Quest_Set_Cmd.Parameters.AddWithValue("?User_Code", User_Code);
                    for (int i = 0; i < Quest_Set_Cmd.Parameters.Count; i++)
                    {
                        Quest_Set_Cmd.Parameters[i].Direction = ParameterDirection.Input;
                    }

                    MySqlDataReader Quest_Set_Reader = Quest_Set_Cmd.ExecuteReader();

                    JObject Results = new JObject();
                    JArray Quest_List = new JArray();
                    string[] Quest_Infos = new string[3];

                    if (Quest_Set_Reader.Read())
                    {
                        for (int i = 0; i < Quest_Infos.Length; i++)
                        {
                            Quest_Infos[i] = Quest_Set_Reader["Quest" + i].ToString();
                            JObject Quest_Info = new JObject();

                            if (Quest_Infos[i].Equals(""))
                            {
                                Quest_Info = Qm.Quest_Base(i);
                            }
                            else
                            {
                                Quest_Info = JObject.Parse(Quest_Infos[i]);
                            }

                            Quest_List.Add(Quest_Info);
                        }

                        string Time = Quest_Set_Reader["Ref_Time"].ToString();
                        if (!Time.Equals(""))
                        {
                            Last_Ref_Time = System.Convert.ToDateTime(Time);
                        }
                    }

                    Quest_Set_Reader.Close();

                    if (Last_Ref_Time > DateTime.Now)
                    {
                        AsyncManager.Parameters["Result"] = "Don't be evil.";
                        scon.Close();
                        AsyncManager.OutstandingOperations.Decrement();
                        return;
                    }

                    int[] Received_Quest_Ids = new int[3];

                    for (int i = 0; i < Received_Quest_Ids.Length; i++)
                    {
                        Received_Quest_Ids[i] = System.Convert.ToInt32(Quest_List[i]["Id"]);
                    }

                    MySqlCommand Refresh_Quest_Cmd = new MySqlCommand();
                    Refresh_Quest_Cmd.Connection = scon;
                    Refresh_Quest_Cmd.CommandText = "Ref_Time_Quests";
                    Refresh_Quest_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Refresh_Quest_Cmd.Parameters.AddWithValue("?User_Code", User_Code);
                    Refresh_Quest_Cmd.Parameters.AddWithValue("?Next_Time", DateTime.Now.AddMinutes(10));

                    for (int i = 0; i < Quest_List.Count; i++)
                    {
                        if (i == In_id)
                        {
                            if (!Quest_List[i]["Cleared_Date"].ToString().Equals("0"))
                            {
                                DateTime Cleared_Date = System.Convert.ToDateTime(Quest_List[i]["Cleared_Date"]);
                                if (Cleared_Date.AddHours(4) > System.DateTime.Now)
                                {
                                    if (isAd == 0)
                                    {
                                        AsyncManager.Parameters["Result"] = "Don't be evil.";
                                        scon.Close();
                                        AsyncManager.OutstandingOperations.Decrement();
                                        return;
                                    }
                                    else
                                    {
                                        Quest_List[i] = Qm.Quest_Generate2(i, User_Code, Received_Quest_Ids);
                                        Received_Quest_Ids[i] = System.Convert.ToInt32(Quest_List[i]["Id"]);
                                    }
                                }
                                else
                                {
                                    if (isAd == 0)
                                    {
                                        AsyncManager.Parameters["Result"] = "Don't be evil.";
                                        scon.Close();
                                        AsyncManager.OutstandingOperations.Decrement();
                                        return;
                                    }
                                    else
                                    {
                                        Quest_List[i] = Qm.Quest_Generate2(i, User_Code, Received_Quest_Ids);
                                        Received_Quest_Ids[i] = System.Convert.ToInt32(Quest_List[i]["Id"]);
                                    }
                                }
                            }
                        }

                        Refresh_Quest_Cmd.Parameters.AddWithValue("?v_Quest" + i, JsonConvert.SerializeObject(Quest_List[i]));
                    }

                    int Refreshed = Refresh_Quest_Cmd.ExecuteNonQuery();
                    if (Refreshed >= 1)
                    {
                        Results.Add("Quests", Quest_List);
                        Results.Add("Next_Time", DateTime.Now.AddMinutes(10));
                        AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    }
                    else
                    {
                        AsyncManager.Parameters["Result"] = "Fuck You";
                    }

                    scon.Close();
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Test_GetCompleted(string Result)
        {
            return Result;
        }

        public void Refresh_QuestAsync(/*string User_Code, string Quest_Info*/)
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            string Quest_Info = Request.Form["Quest_Info"];
            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    MySqlCommand Quest_Set_Cmd = new MySqlCommand();
                    Quest_Set_Cmd.Connection = scon;
                    Quest_Set_Cmd.CommandText = "Refresh_Quest";
                    Quest_Set_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Quest_Set_Cmd.Parameters.AddWithValue("?User_Code", User_Code);

                    JObject Results = new JObject();
                    JObject Receive_Data = JObject.Parse(Quest_Info);

                    JArray Receive_Quest = JArray.Parse(Receive_Data["Quests"].ToString()) as JArray;


                    for (int i = 0; i < Receive_Quest.Count; i++)
                    {
                        Quest_Set_Cmd.Parameters.AddWithValue("?v_Quest" + i, JsonConvert.SerializeObject(Receive_Quest[i]));
                    }

                    for (int i = 0; i < Quest_Set_Cmd.Parameters.Count; i++)
                    {
                        Quest_Set_Cmd.Parameters[i].Direction = ParameterDirection.Input;
                    }

                    Results.Add("Quests", Receive_Quest);
                    int Query_Result = Quest_Set_Cmd.ExecuteNonQuery();

                    if (Query_Result >= 1)
                    {
                        AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    }
                    else
                    {
                        AsyncManager.Parameters["Result"] = "-1";
                    }

                    scon.Close();
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Refresh_QuestCompleted(string Result)
        {
            return Result;
        }

        public void User_QListAsync() // 퀘스트 리스트 전송, 퀘스트 클리어하고 난뒤 4시간이지났다면 퀘스트교체
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    string Quest_Set = "Get_Quest_Status ?UserCode";
                    MySqlCommand Quest_Set_Cmd = new MySqlCommand();
                    Quest_Set_Cmd.Connection = scon;
                    Quest_Set_Cmd.CommandText = "Get_Quest_Status";
                    Quest_Set_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Quest_Set_Cmd.Parameters.AddWithValue("?User_Code", User_Code);
                    for (int i = 0; i < Quest_Set_Cmd.Parameters.Count; i++)
                    {
                        Quest_Set_Cmd.Parameters[i].Direction = ParameterDirection.Input;
                    }
                    MySqlDataReader Quest_Set_Reader = Quest_Set_Cmd.ExecuteReader();

                    JObject Results = new JObject();
                    JArray Quest_List = new JArray();
                    JObject User_info = new JObject();
                    string[] Quest_Infos = new string[3];

                    if (Quest_Set_Reader.Read())
                    {
                        for (int i = 0; i < Quest_Infos.Length; i++)
                        {
                            Quest_Infos[i] = Quest_Set_Reader["Quest" + i].ToString();
                            JObject Quest_Info = new JObject();

                            if (Quest_Infos[i].Equals(""))
                            {
                                Quest_Info = Qm.Quest_Base(i);
                            }
                            else
                            {
                                Quest_Info = JObject.Parse(Quest_Infos[i]);
                            }

                            Quest_List.Add(Quest_Info);
                        }


                        User_info.Add("UserGold", Quest_Set_Reader["UserGold"].ToString());
                    }

                    Quest_Set_Reader.Close();

                    //bool isReceived = false;

                    MySqlCommand Refresh_Quest_Cmd = new MySqlCommand();
                    Refresh_Quest_Cmd.Connection = scon;
                    Refresh_Quest_Cmd.CommandText = "Refresh_Quest";
                    Refresh_Quest_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Refresh_Quest_Cmd.Parameters.AddWithValue("?User_Code", User_Code);

                    for (int i = 0; i < Quest_List.Count; i++)
                    {
                        DateTime Cleared_Date = System.Convert.ToDateTime(Quest_List[i]["Cleared_Date"]);
                        string Id = Quest_List[i]["Id"].ToString();
                        if (Id.Equals("0"))
                        {
                            if (!Quest_List[i]["Cleared_Date"].ToString().Equals("0"))
                            {
                                if (Cleared_Date.AddHours(4) < System.DateTime.Now)
                                {
                                    Quest_List[i] = Qm.Quest_Generate(i, User_Code);
                                }
                            }
                            else
                            {
                                Quest_List[i] = Qm.Quest_Generate(i, User_Code);
                            }

                            Refresh_Quest_Cmd.Parameters.AddWithValue("?v_Quest" + i, JsonConvert.SerializeObject(Quest_List[i]));
                        }
                        else
                        {
                            if (!Quest_List[i]["Cleared_Date"].ToString().Equals("0"))
                            {
                                if (Cleared_Date.AddHours(4) < System.DateTime.Now)
                                {
                                    Quest_List[i] = Qm.Quest_Generate(i, User_Code);
                                }
                            }

                            Refresh_Quest_Cmd.Parameters.AddWithValue("?v_Quest" + i, JsonConvert.SerializeObject(Quest_List[i]));
                        }
                    }

                    for (int i = 0; i < Refresh_Quest_Cmd.Parameters.Count; i++)
                    {
                        Refresh_Quest_Cmd.Parameters[i].Direction = ParameterDirection.Input;
                    }

                    int Refreshed = Refresh_Quest_Cmd.ExecuteNonQuery();
                    if (Refreshed >= 1)
                    {
                        Results.Add("Quests", Quest_List);
                        Results.Add("User_Info", User_info);
                        AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    }
                    else
                    {
                        AsyncManager.Parameters["Result"] = "Don't be evil.";
                    }

                    scon.Close();
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string User_QListCompleted(string Result)
        {
            return Result;
        }

        public void User_WQListAsync() // 퀘스트 리스트 전송, 퀘스트 클리어하고 난뒤 4시간이지났다면 퀘스트교체
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    MySqlCommand Quest_Set_Cmd = new MySqlCommand();
                    Quest_Set_Cmd.Connection = scon;
                    Quest_Set_Cmd.CommandText = "Get_WQuest_Status";
                    Quest_Set_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Quest_Set_Cmd.Parameters.AddWithValue("?User_Code", User_Code);
                    for (int i = 0; i < Quest_Set_Cmd.Parameters.Count; i++)
                    {
                        Quest_Set_Cmd.Parameters[i].Direction = ParameterDirection.Input;
                    }
                    MySqlDataReader Quest_Set_Reader = Quest_Set_Cmd.ExecuteReader();

                    JObject Results = new JObject();
                    JArray Quest_List = new JArray();
                    JObject User_info = new JObject();
                    string[] Quest_Infos = new string[3];
                    int Weekly_Refresh = 0;

                    if (Quest_Set_Reader.Read())
                    {
                        for (int i = 0; i < Quest_Infos.Length; i++)
                        {
                            Quest_Infos[i] = Quest_Set_Reader["Quest" + i].ToString();
                            JObject Quest_Info = new JObject();

                            if (Quest_Infos[i].Equals(""))
                            {
                                Quest_Info = Qm.Quest_Base(i);
                            }
                            else
                            {
                                Quest_Info = JObject.Parse(Quest_Infos[i]);
                            }

                            Quest_List.Add(Quest_Info);
                        }

                        Weekly_Refresh = System.Convert.ToInt32(Quest_Set_Reader["Weekly_Q_Refresh"]);
                        User_info.Add("UserGold", Quest_Set_Reader["UserGold"].ToString());
                    }

                    Quest_Set_Reader.Close();

                    //bool isReceived = false;
                    DataTable Available_Quests = new DataTable();

                    if (Weekly_Refresh == 1)
                    {
                        MySqlCommand Available_Quest_List = new MySqlCommand();
                        Available_Quest_List.Connection = scon;
                        Available_Quest_List.CommandText = "Get_Available_Weekly_Quest";
                        Available_Quest_List.CommandType = System.Data.CommandType.StoredProcedure;
                        Available_Quest_List.Parameters.AddWithValue("?User_Code", User_Code);
                        MySqlDataReader Aql = Available_Quest_List.ExecuteReader();
                        if (Aql.Read())
                        {
                            Available_Quests.Load(Aql);
                        }
                        Aql.Close();
                    }

                    MySqlCommand Refresh_Quest_Cmd = new MySqlCommand();
                    Refresh_Quest_Cmd.Connection = scon;
                    Refresh_Quest_Cmd.CommandText = "Refresh_WQuest";
                    Refresh_Quest_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Refresh_Quest_Cmd.Parameters.AddWithValue("?User_Code", User_Code);

                    for (int i = 0; i < Quest_List.Count; i++)
                    {
                        if (Weekly_Refresh == 1)
                        {
                            Quest_List[i] = Qm.WQuest_Generate(i, User_Code, Available_Quests);
                        }
                            Refresh_Quest_Cmd.Parameters.AddWithValue("?v_Quest" + i, JsonConvert.SerializeObject(Quest_List[i]));
                    }

                    for (int i = 0; i < Refresh_Quest_Cmd.Parameters.Count; i++)
                    {
                        Refresh_Quest_Cmd.Parameters[i].Direction = ParameterDirection.Input;
                    }

                    int Refreshed = Refresh_Quest_Cmd.ExecuteNonQuery();
                    if (Refreshed >= 1)
                    {
                        Results.Add("Quests", Quest_List);
                        Results.Add("User_Info", User_info);
                        AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    }
                    else
                    {
                        AsyncManager.Parameters["Result"] = "Don't be evil.";
                    }

                    scon.Close();
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string User_WQListCompleted(string Result)
        {
            return Result;
        }
        #endregion

        #region Shop


        public void Change_AuthAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"]; // 구글/페이스북등에서 얻은 코드
            string Auth_Type = Request.Form["Auth_Type"];
            string User_Email = Request.Form["User_Mail"];
            string Unique_Code = Request.Form["Unique_Code"];

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    JObject Results = new JObject();

                    MySqlCommand Search_Cmd = new MySqlCommand();
                    Search_Cmd.Connection = scon;
                    Search_Cmd.CommandText = "Search_Mail";
                    Search_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Search_Cmd.Parameters.AddWithValue("?User_Code", User_Code);
                    Search_Cmd.Parameters.AddWithValue("?Auth_Types", Auth_Type);

                    MySqlDataReader uic = Search_Cmd.ExecuteReader();
                    if (!uic.HasRows)
                    {
                        uic.Close();

                        MySqlCommand Change_Auth_Cmd = new MySqlCommand();
                        Change_Auth_Cmd.Connection = scon;
                        Change_Auth_Cmd.CommandText = "Change_Auth";
                        Change_Auth_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        Change_Auth_Cmd.Parameters.AddWithValue("?User_Code", User_Code);
                        Change_Auth_Cmd.Parameters.AddWithValue("?Unique_Code", Unique_Code);
                        Change_Auth_Cmd.Parameters.AddWithValue("?User_Mail", User_Email);
                        Change_Auth_Cmd.Parameters.AddWithValue("?Auth_Types", Auth_Type);

                        MySqlDataReader Cac = Change_Auth_Cmd.ExecuteReader();
                        if (Cac.Read())
                        {
                            Results.Add("AuthComplete", 1);
                        }

                        Cac.Close();
                    }
                    else
                    {
                        Results.Add("AuthComplete", 1);
                    }

                    scon.Close();
                    AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Change_AuthCompleted(string Result)
        {
            return Result;
        }

        public void Box_OpensAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            string Request_Char_Id = Request.Form["Char_Id"];

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {

                    MySqlCommand User_Info_Cmd = new MySqlCommand();
                    User_Info_Cmd.Connection = scon;
                    User_Info_Cmd.CommandText = "Get_User_Info";
                    User_Info_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    User_Info_Cmd.Parameters.AddWithValue("?User_Code", User_Code);

                    MySqlDataReader uic = User_Info_Cmd.ExecuteReader();
                    int gold = 0;
                    int require_gold = 300;

                    if (uic.Read())
                    {
                        gold = System.Convert.ToInt32(uic["Gold"]);
                    }

                    uic.Close();

                    if (gold < require_gold)
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "Do not evil";
                        AsyncManager.OutstandingOperations.Decrement();
                        return;
                    }

                    MySqlCommand Box_Buy_Cmd = new MySqlCommand();
                    Box_Buy_Cmd.Connection = scon;
                    Box_Buy_Cmd.CommandText = "Box_Buy";
                    Box_Buy_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Box_Buy_Cmd.Parameters.AddWithValue("?User_Code", User_Code);

                    MySqlDataReader bbc = Box_Buy_Cmd.ExecuteReader();

                    if (bbc.Read())
                    {

                    }

                    bbc.Close();


                    MySqlCommand Get_User_Status = new MySqlCommand();
                    Get_User_Status.Connection = scon;
                    Get_User_Status.CommandText = "Get_Box_Open_Status";
                    Get_User_Status.CommandType = System.Data.CommandType.StoredProcedure;
                    Get_User_Status.Parameters.AddWithValue("?User_Code", User_Code);
                    MySqlDataReader usr = Get_User_Status.ExecuteReader();

                    int box_count = 0;
                    JObject Results = new JObject();
                    JObject Effect_Results = new JObject();
                    JArray Ja = null;
                    JArray Effect_Array = null;
                    JObject Characters = null;
                    JObject Effects = null;

                    if (usr.Read())
                    {
                        box_count = System.Convert.ToInt32(usr["Box_count"]);
                        Characters = JObject.Parse(usr["info"].ToString());
                        Effects = JObject.Parse(usr["effect_info"].ToString());
                    }

                    if (box_count < 1)
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "Do not evil.";
                        AsyncManager.OutstandingOperations.Decrement();
                        return;
                    }

                    Ja = JArray.Parse(Characters["Characters"].ToString());
                    Effect_Array = JArray.Parse(Effects["Effects"].ToString());

                    int isChar = Ran.Next(0, 10);
                    JObject char_info = Char_info();
                    JObject Effect_infos = Effect_info();
                    bool already_got = false;
                    int Add_gold = 0;

                    bool isEff = false;

                    if (isChar < 6) // 캐릭터뽑
                    {
                        for (int i = 0; i < Ja.Count; i++)
                        {
                            string this_id = Ja[i]["char_id"].ToString();
                            string get_id = char_info["char_id"].ToString();

                            if (this_id.Equals(get_id))
                            {
                                already_got = true;
                                break;
                            }
                        }

                        if (already_got)
                        {
                            Add_gold += 50;
                        }
                        else
                        {
                            Ja.Add(char_info);
                        }
                    }
                    else // 이펙트뽑
                    {
                        isEff = true;
                        for (int i = 0; i < Effect_Array.Count; i++)
                        {
                            string this_id = Effect_Array[i]["eff_id"].ToString();
                            string get_id = Effect_infos["eff_id"].ToString();

                            if (this_id.Equals(get_id))
                            {
                                already_got = true;
                                break;
                            }
                        }

                        if (already_got)
                        {
                            Add_gold += 50;
                        }
                        else
                        {
                            Effect_Array.Add(Effect_infos);
                        }
                    }

                    Results.Add("Characters", Ja);
                    Effect_Results.Add("Effects", Effect_Array);

                    usr.Close();
                    MySqlCommand Set_User_Status = new MySqlCommand();
                    Set_User_Status.Connection = scon;
                    Set_User_Status.CommandText = "Set_Inven_Info";
                    Set_User_Status.CommandType = System.Data.CommandType.StoredProcedure;
                    Set_User_Status.Parameters.AddWithValue("?User_Code", User_Code);
                    Set_User_Status.Parameters.AddWithValue("?Char_Info", JsonConvert.SerializeObject(Results));
                    Set_User_Status.Parameters.AddWithValue("?Effect_Info", JsonConvert.SerializeObject(Effect_Results));
                    if (isEff)
                    {
                        Set_User_Status.Parameters.AddWithValue("?Char_id", 0);
                    }
                    else
                    {
                        Set_User_Status.Parameters.AddWithValue("?Char_id", char_info["char_id"]);
                    }
                    Set_User_Status.Parameters.AddWithValue("?Golds", Add_gold);
                    MySqlDataReader sus = Set_User_Status.ExecuteReader();
                    if (sus.Read())
                    {
                        Results.Add("Gold", sus["Gold"].ToString());
                        if (isEff)
                        {
                            Results.Add("New_Effect", Effect_infos["eff_id"]);
                        }
                        else
                        {
                            Results.Add("New_Char", char_info["char_id"]);
                        }
                        Results.Add("Box_Count", sus["Box_Count"].ToString());
                    }
                    Results.Add("Effects", Effect_Array);

                    sus.Close();

                    scon.Close();
                    AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Box_OpensCompleted(string Result)
        {
            return Result;
        }

        public void Gold_UpAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    JObject Results = new JObject();

                    MySqlCommand User_Info_Cmd = new MySqlCommand();
                    User_Info_Cmd.Connection = scon;
                    User_Info_Cmd.CommandText = "Cheat_Gold";
                    User_Info_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    User_Info_Cmd.Parameters.AddWithValue("?User_Code", User_Code);

                    MySqlDataReader uic = User_Info_Cmd.ExecuteReader();
                    int gold = 0;

                    if (uic.Read())
                    {
                        gold = System.Convert.ToInt32(uic["Gold"]);
                        Results.Add("UserGold", gold);
                    }

                    uic.Close();
                    scon.Close();
                    AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Gold_UpCompleted(string Result)
        {
            return Result;
        }

        public void L_BonusAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    JObject Results = new JObject();

                    MySqlCommand User_Info_Cmd = new MySqlCommand();
                    User_Info_Cmd.Connection = scon;
                    User_Info_Cmd.CommandText = "Check_Login_Bonus";
                    User_Info_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    User_Info_Cmd.Parameters.AddWithValue("?User_Code", User_Code);
                    MySqlDataReader uic = User_Info_Cmd.ExecuteReader();

                    int gold = 0;
                    int d_bonus = 0;
                    int can_bonus = 0;

                    if(uic.Read())
                    {
                        can_bonus = Convert.ToInt32(uic["ds"]);
                    }

                    uic.Close();

                    if (can_bonus == 0)
                    {
                            scon.Close();
                            AsyncManager.Parameters["Result"] = "-37";
                            AsyncManager.OutstandingOperations.Decrement();
                            return;
                    }

                    MySqlCommand Daily_Cmd = new MySqlCommand();
                    Daily_Cmd.Connection = scon;
                    Daily_Cmd.CommandText = "Execute_Login_Bonus";
                    Daily_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Daily_Cmd.Parameters.AddWithValue("?User_Code", User_Code);
                    MySqlDataReader Daily_Reader = Daily_Cmd.ExecuteReader();
                    if (Daily_Reader.Read())
                    {
                        gold = System.Convert.ToInt32(Daily_Reader["UserGold"]);
                        d_bonus = System.Convert.ToInt32(Daily_Reader["L_Bonus"]);

                        Results.Add("UserGold", gold);
                        Results.Add("L_Bonus", d_bonus);
                    }
                    Daily_Reader.Close();

                    scon.Close();
                    AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string L_BonusCompleted(string Result)
        {
            return Result;
        }

        public JObject Char_info()
        {
            JObject Results = new JObject();
            int char_id = Ran.Next(1, 40);
            var cha_info = new JObject();
            Results.Add("char_id", char_id);

            return Results;
        }

        public JObject Effect_info()
        {
            JObject Results = new JObject();
            int char_id = Ran.Next(1, 18);
            var cha_info = new JObject();
            Results.Add("eff_id", char_id);

            return Results;
        }

        public void Char_BuyAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            int Request_Char_Id = System.Convert.ToInt32(Request.Form["R_CharCode"]);
            string receipt = Request.Form["Receipt"];
            var receipt_data = JObject.Parse(receipt.ToString());

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    var payload_data = JObject.Parse(receipt_data["Payload"].ToString());
                    var real_data = JObject.Parse(payload_data["json"].ToString());
                    string packageName = real_data["packageName"].ToString();
                    string product_id = real_data["productId"].ToString();
                    //int purchase_State = System.Convert.ToInt32(real_data["purchaseState"]);
                    string token = real_data["purchaseToken"].ToString();

                    //string currentPath = System.IO.Directory.GetCurrentDirectory();

                    string MapPath = Server.MapPath("~/TestGotYou-4c593160e77f.p12");
                    var auth = GoogleJsonWebToken.GetAccessToken("billingvail@testgotyou-95045285.iam.gserviceaccount.com", MapPath, GoogleJsonWebToken.SCOPE_AUTH_ANDROIDPUBLISHER);

                    String URL = "https://www.googleapis.com/androidpublisher/v1.1/applications/" + packageName + "/inapp/" + product_id + "/purchases/" + token + "?access_token=" + auth["access_token"];

                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(URL);
                    req.Method = "GET";
                    req.Accept = "application/json";
                    WebResponse res = req.GetResponse();
                    StreamReader reader = new StreamReader(res.GetResponseStream(), Encoding.UTF8);
                    string result = reader.ReadToEnd();

                    var Vailed_Data = JObject.Parse(result);

                    int Purchase_State = System.Convert.ToInt32(Vailed_Data["purchaseState"]);

                    if (Purchase_State != 0) // 구매실패,환불등
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-37";
                        AsyncManager.OutstandingOperations.Decrement();
                        return;
                    }

                    MySqlCommand Get_User_Status = new MySqlCommand();
                    Get_User_Status.Connection = scon;
                    Get_User_Status.CommandText = "Get_Char_Status";
                    Get_User_Status.CommandType = System.Data.CommandType.StoredProcedure;
                    Get_User_Status.Parameters.AddWithValue("?User_Code", User_Code);
                    MySqlDataReader usr = Get_User_Status.ExecuteReader();

                    JObject Results = new JObject();
                    JArray Ja = null;
                    JObject Characters = null;

                    if (usr.Read())
                    {
                        Characters = JObject.Parse(usr["info"].ToString());
                    }

                    Ja = JArray.Parse(Characters["Characters"].ToString());

                    JObject char_info = new JObject();
                    char_info.Add("char_id", Request_Char_Id);
                    Ja.Add(char_info);

                    Results.Add("Characters", Ja);

                    usr.Close();
                    MySqlCommand Set_User_Status = new MySqlCommand();
                    Set_User_Status.Connection = scon;
                    Set_User_Status.CommandText = "Buy_Char";
                    Set_User_Status.CommandType = System.Data.CommandType.StoredProcedure;
                    Set_User_Status.Parameters.AddWithValue("?User_Code", User_Code);
                    Set_User_Status.Parameters.AddWithValue("?Char_Info", JsonConvert.SerializeObject(Results));
                    Set_User_Status.Parameters.AddWithValue("?Char_id", Request_Char_Id);
                    MySqlDataReader sus = Set_User_Status.ExecuteReader();

                    if (sus.Read())
                    {
                        Results.Add("Gold", sus["Gold"].ToString());
                        Results.Add("New_Char", char_info["char_id"]);
                    }

                    sus.Close();

                    scon.Close();
                    AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Char_BuyCompleted(string Result)
        {
            return Result;
        }

        public void Package_BuyAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            string receipt = Request.Form["Receipt"];
            //Response.Write(receipt);
            var receipt_data = JObject.Parse(receipt.ToString());
            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    JObject Results = new JObject();

                    var payload_data = JObject.Parse(receipt_data["Payload"].ToString());
                    var real_data = JObject.Parse(payload_data["json"].ToString());
                    string packageName = real_data["packageName"].ToString();
                    string product_id = real_data["productId"].ToString();
                    //int purchase_State = System.Convert.ToInt32(real_data["purchaseState"]);
                    string token = real_data["purchaseToken"].ToString();

                    //string currentPath = System.IO.Directory.GetCurrentDirectory();

                    string MapPath = Server.MapPath("~/TestGotYou-4c593160e77f.p12");
                    var auth = GoogleJsonWebToken.GetAccessToken("billingvail@testgotyou-95045285.iam.gserviceaccount.com", MapPath, GoogleJsonWebToken.SCOPE_AUTH_ANDROIDPUBLISHER);

                    String URL = "https://www.googleapis.com/androidpublisher/v1.1/applications/" + packageName + "/inapp/" + product_id + "/purchases/" + token + "?access_token=" + auth["access_token"];

                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(URL);
                    req.Method = "GET";
                    req.Accept = "application/json";
                    WebResponse res = req.GetResponse();
                    StreamReader reader = new StreamReader(res.GetResponseStream(), Encoding.UTF8);
                    string result = reader.ReadToEnd();

                    var Vailed_Data = JObject.Parse(result);

                    int Purchase_State = System.Convert.ToInt32(Vailed_Data["purchaseState"]);

                    if (Purchase_State != 0) // 구매실패,환불등
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-37";
                        AsyncManager.OutstandingOperations.Decrement();
                        return;
                    }

                    MySqlCommand Package_Buy_Cmd = new MySqlCommand();
                    Package_Buy_Cmd.Connection = scon;
                    Package_Buy_Cmd.CommandText = "Pack_Buy";
                    Package_Buy_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Package_Buy_Cmd.Parameters.AddWithValue("?User_Code", User_Code);

                    if (product_id.Equals("package_1"))
                    {
                        Package_Buy_Cmd.Parameters.AddWithValue("?Pack_Code", 1);
                    }

                    if (product_id.Equals("package_2"))
                    {
                        Package_Buy_Cmd.Parameters.AddWithValue("?Pack_Code", 2);
                    }

                    if (product_id.Equals("package_3"))
                    {
                        Package_Buy_Cmd.Parameters.AddWithValue("?Pack_Code", 3);
                    }

                    if (product_id.Equals("package_4"))
                    {
                        Package_Buy_Cmd.Parameters.AddWithValue("?Pack_Code", 4);
                    }

                    MySqlDataReader Pbc = Package_Buy_Cmd.ExecuteReader();

                    if (Pbc.Read())
                    {
                        Results.Add("Pack_1", Pbc["pack_1"].ToString());
                        Results.Add("Pack_2", Pbc["pack_2"].ToString());
                        Results.Add("Pack_3", Pbc["pack_3"].ToString());
                        Results.Add("No_Ads", Pbc["No_Ads"].ToString());
                    }

                    scon.Close();
                    AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Package_BuyCompleted(string Result)
        {
            return Result;
        }

        public void Char_Buy_IOSAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            int Request_Char_Id = System.Convert.ToInt32(Request.Form["R_CharCode"]);
            string receipt = Request.Form["Receipt"];
            var receipt_data = JObject.Parse(receipt.ToString());

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    var Apple_Result = JObject.Parse(receipt);
                    string payload = Apple_Result["Payload"].ToString();

                    JObject Vailresult = IOS_Vaildate(payload, false);
                    int Vail_status = System.Convert.ToInt32(Vailresult["status"]);

                    if (Vail_status == 21007)
                    {
                        Vailresult = IOS_Vaildate(payload, true);
                    }

                    Vail_status = System.Convert.ToInt32(Vailresult["status"]);

                    if (Vail_status != 0)
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-37";
                        AsyncManager.OutstandingOperations.Decrement();
                        return;
                    }

                    //string itemID = Vailresult["product_id"].ToString().Replace("\"", "").Trim();

                    MySqlCommand Get_User_Status = new MySqlCommand();
                    Get_User_Status.Connection = scon;
                    Get_User_Status.CommandText = "Get_Char_Status";
                    Get_User_Status.CommandType = System.Data.CommandType.StoredProcedure;
                    Get_User_Status.Parameters.AddWithValue("?User_Code", User_Code);
                    MySqlDataReader usr = Get_User_Status.ExecuteReader();

                    JObject Results = new JObject();
                    JArray Ja = null;
                    JObject Characters = null;

                    if (usr.Read())
                    {
                        Characters = JObject.Parse(usr["info"].ToString());
                    }

                    Ja = JArray.Parse(Characters["Characters"].ToString());

                    JObject char_info = new JObject();
                    char_info.Add("char_id", Request_Char_Id);
                    Ja.Add(char_info);

                    Results.Add("Characters", Ja);

                    usr.Close();
                    MySqlCommand Set_User_Status = new MySqlCommand();
                    Set_User_Status.Connection = scon;
                    Set_User_Status.CommandText = "Buy_Char";
                    Set_User_Status.CommandType = System.Data.CommandType.StoredProcedure;
                    Set_User_Status.Parameters.AddWithValue("?User_Code", User_Code);
                    Set_User_Status.Parameters.AddWithValue("?Char_Info", JsonConvert.SerializeObject(Results));
                    Set_User_Status.Parameters.AddWithValue("?Char_id", Request_Char_Id);
                    MySqlDataReader sus = Set_User_Status.ExecuteReader();

                    if (sus.Read())
                    {
                        Results.Add("Gold", sus["Gold"].ToString());
                        Results.Add("New_Char", char_info["char_id"]);
                    }

                    sus.Close();

                    scon.Close();
                    AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message + "////" + User_Code + " : " + Request_Char_Id + " :" + receipt;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Char_Buy_IOSCompleted(string Result)
        {
            return Result;
        }

        public void Char_Buy_IOS2Async(string m_User_Code, string R_CharCode, string receipt_datas)
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = m_User_Code;
            int Request_Char_Id = System.Convert.ToInt32(R_CharCode);
            string receipt = receipt_datas;
            var receipt_data = JObject.Parse(receipt.ToString());

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    var Apple_Result = JObject.Parse(receipt);
                    string payload = Apple_Result["Payload"].ToString();

                    JObject Vailresult = IOS_Vaildate(payload, false);
                    int Vail_status = System.Convert.ToInt32(Vailresult["status"]);

                    if (Vail_status == 21007)
                    {
                        Vailresult = IOS_Vaildate(payload, true);
                    }

                    Vail_status = System.Convert.ToInt32(Vailresult["status"]);

                    if (Vail_status != 0)
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-37";
                        AsyncManager.OutstandingOperations.Decrement();
                        return;
                    }

                    MySqlCommand Get_User_Status = new MySqlCommand();
                    Get_User_Status.Connection = scon;
                    Get_User_Status.CommandText = "Get_Char_Status";
                    Get_User_Status.CommandType = System.Data.CommandType.StoredProcedure;
                    Get_User_Status.Parameters.AddWithValue("?User_Code", User_Code);
                    MySqlDataReader usr = Get_User_Status.ExecuteReader();

                    JObject Results = new JObject();
                    JArray Ja = null;
                    JObject Characters = null;

                    if (usr.Read())
                    {
                        Characters = JObject.Parse(usr["info"].ToString());
                    }

                    Ja = JArray.Parse(Characters["Characters"].ToString());

                    JObject char_info = new JObject();
                    char_info.Add("char_id", Request_Char_Id);
                    Ja.Add(char_info);

                    Results.Add("Characters", Ja);

                    usr.Close();
                    MySqlCommand Set_User_Status = new MySqlCommand();
                    Set_User_Status.Connection = scon;
                    Set_User_Status.CommandText = "Buy_Char";
                    Set_User_Status.CommandType = System.Data.CommandType.StoredProcedure;
                    Set_User_Status.Parameters.AddWithValue("?User_Code", User_Code);
                    Set_User_Status.Parameters.AddWithValue("?Char_Info", JsonConvert.SerializeObject(Results));
                    Set_User_Status.Parameters.AddWithValue("?Char_id", Request_Char_Id);
                    MySqlDataReader sus = Set_User_Status.ExecuteReader();

                    if (sus.Read())
                    {
                        Results.Add("Gold", sus["Gold"].ToString());
                        Results.Add("New_Char", char_info["char_id"]);
                    }

                    sus.Close();

                    scon.Close();
                    AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message + "////" + User_Code + " : " + Request_Char_Id + " :" + receipt;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Char_Buy_IOS2Completed(string Result)
        {
            return Result;
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public JObject IOS_Vaildate(string receipt, bool isSand)
        {
            //var receipt_data = Base64FormattingOptions(receipt);
            String URL = "https://buy.itunes.apple.com/verifyReceipt";
            string URL_SandBox = "https://sandbox.itunes.apple.com/verifyReceipt";
            if (isSand)
            {
                URL = URL_SandBox;
            }
            var json = new JObject(new JProperty("receipt-data", receipt)).ToString();
            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] postBytes = Encoding.UTF8.GetBytes(json);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(URL_SandBox);


            req.Method = "POST";
            req.Accept = "application/json";
            req.ContentLength = postBytes.Length;

            using (var stream = req.GetRequestStream())
            {
                stream.Write(postBytes, 0, postBytes.Length);
                stream.Flush();
            }

            var sendresponse = req.GetResponse();

            string sendresponsetext = "";
            using (var streamReader = new StreamReader(sendresponse.GetResponseStream()))
            {
                sendresponsetext = streamReader.ReadToEnd().Trim();
            }
            var vailresult = JObject.Parse(sendresponsetext);
            return vailresult;
        }

        public void Package_Buy_IOSAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            string receipt = Request.Form["Receipt"];
            string Request_Package_Code = Request.Form["R_PackageCode"];
            //Response.Write(receipt);
            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    JObject Results = new JObject();

                    var Apple_Result = JObject.Parse(receipt);
                    string payload = Apple_Result["Payload"].ToString();

                    JObject Vailresult = IOS_Vaildate(payload, false);
                    int Vail_status = System.Convert.ToInt32(Vailresult["status"]);

                    if (Vail_status == 21007)
                    {
                        Vailresult = IOS_Vaildate(payload, true);
                    }

                    Vail_status = System.Convert.ToInt32(Vailresult["status"]);

                    if (Vail_status != 0)
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-37";
                        AsyncManager.OutstandingOperations.Decrement();
                        return;
                    }

                    //string itemID = Vailresult["product_id"].ToString().Replace("\"", "").Trim();

                    MySqlCommand Package_Buy_Cmd = new MySqlCommand();
                    Package_Buy_Cmd.Connection = scon;
                    Package_Buy_Cmd.CommandText = "Pack_Buy";
                    Package_Buy_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Package_Buy_Cmd.Parameters.AddWithValue("?User_Code", User_Code);

                    if (Request_Package_Code.Equals("package_1"))
                    {
                        Package_Buy_Cmd.Parameters.AddWithValue("?Pack_Code", 1);
                    }

                    if (Request_Package_Code.Equals("package_2"))
                    {
                        Package_Buy_Cmd.Parameters.AddWithValue("?Pack_Code", 2);
                    }

                    if (Request_Package_Code.Equals("package_3"))
                    {
                        Package_Buy_Cmd.Parameters.AddWithValue("?Pack_Code", 3);
                    }

                    if (Request_Package_Code.Equals("package_4"))
                    {
                        Package_Buy_Cmd.Parameters.AddWithValue("?Pack_Code", 4);
                    }

                    MySqlDataReader Pbc = Package_Buy_Cmd.ExecuteReader();

                    if (Pbc.Read())
                    {
                        Results.Add("Pack_1", Pbc["pack_1"].ToString());
                        Results.Add("Pack_2", Pbc["pack_2"].ToString());
                        Results.Add("Pack_3", Pbc["pack_3"].ToString());
                        Results.Add("No_Ads", Pbc["No_Ads"].ToString());
                    }

                    scon.Close();
                    AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Package_Buy_IOSCompleted(string Result)
        {
            return Result;
        }
        #endregion
        /*
        public string MyRankingSet() // 랭킹데이터베이스 테스트용 더미 유저 리스트
        {
            try
            {

                for (int i = 0; i < 100; i++)
                {
                    caches = Connections.GetDatabase();

                    int rs = Rd.Next(0, 50);
                    string userid = "g" + ":test" + i;
                    int score = rs;
                    caches.SortedSetAdd("MaxFloor", userid, score);
                }

                Connections.Close();
                return "Setting Complete 100 Users";
            }
            catch (Exception e)
            {
                Connections.Close();
                return e.Message;
            }
        }
        */
        public void GetRankingAsync()// 10명단위로 끊어서 보여주기위한 메소드!
        {
            AsyncManager.OutstandingOperations.Increment();
            JObject Results = new JObject();
            int Auth_Type = System.Convert.ToInt32(Request.Form["Auth_Type"]);
            string Theme_Id = Request.Form["Theme"];
            string Id = Request.Form["User_Code"];
            string nickname = Request.Form["nickname"];
            int firstperson = 0; // 0 = 0 , 1 = 10, 2 = 20 ...
            int lastperson = firstperson + 9; // 0 = 9 , 1 = 19 , 2 = 29 ...
            string userid = Auth_Type + ":" + Id + "*";

            try
            {
                cache = connection.GetDatabase();
                JArray Ja = new JArray();

                SortedSetEntry[] ranks2 = cache.SortedSetRangeByRankWithScores(Theme_Id + "Theme", 0, 30, Order.Descending);
                for (int i = 0; i < ranks2.Length; i++)
                {
                    JObject Rank_Data = new JObject();
                    string[] Ranking = ranks2[i].Element.ToString().Split(':');

                    if(Ranking[0].Equals("3"))
                    {
                        Rank_Data.Add("Auth_Type", Ranking[0]);
                        //Rank_Data.Add("User_Nick", Ranking[1]);
                        Rank_Data.Add("Id", Ranking[1] + ":"+ Ranking[2]);
                        Rank_Data.Add("Char_Id", Ranking[3]);
                        Rank_Data.Add("Score", ranks2[i].Score.ToString());
                        Rank_Data.Add("Rank", (firstperson + i + 1).ToString());
                        Rank_Data.Add("User_Nick", cache.HashGet("Nickname", Ranking[1] + ":"+ Ranking[2]).ToString());
                    } else
                    {
                        Rank_Data.Add("Auth_Type", Ranking[0]);
                        //Rank_Data.Add("User_Nick", Ranking[1]);
                        Rank_Data.Add("Id", Ranking[1]);
                        Rank_Data.Add("Char_Id", Ranking[2]);
                        Rank_Data.Add("Score", ranks2[i].Score.ToString());
                        Rank_Data.Add("Rank", (firstperson + i + 1).ToString());
                        Rank_Data.Add("User_Nick", cache.HashGet("Nickname", Ranking[1]).ToString());
                    }

                    Ja.Add(Rank_Data);
                }

                Results.Add("Ranking", Ja);

                IEnumerable<SortedSetEntry> My_List = cache.SortedSetScan(Theme_Id + "Theme", userid);

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

                    long? s = cache.SortedSetRank(Theme_Id + "Theme", user_key, Order.Descending) + 1;

                    string[] user_data = user_key.Split(':');

                    MyRanking.Add("Auth_Type", user_data[0]);

                    if(user_data[0].Equals("3"))
                    {
                        MyRanking.Add("User_Nick", cache.HashGet("Nickname", user_data[1] + ":" + user_data[2]).ToString());
                        MyRanking.Add("Id", user_data[1] + ":" + user_data[2]);
                        MyRanking.Add("Char_Id", user_data[3]);
                        MyRanking.Add("Score", top_data[1]);
                        MyRanking.Add("Rank", s);
                    } else
                    {
                        MyRanking.Add("User_Nick", cache.HashGet("Nickname", user_data[1]).ToString());
                        MyRanking.Add("Id", user_data[1]);
                        MyRanking.Add("Char_Id", user_data[2]);
                        MyRanking.Add("Score", top_data[1]);
                        MyRanking.Add("Rank", s);
                    }
                    //MyRanking.Add("User_Nick", user_data[1]);


                    Results.Add("MyRanking", MyRanking);
                }

                connection.Close();
                AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                AsyncManager.OutstandingOperations.Decrement();
            }
            catch (Exception e)
            {
                connection.Close();
                AsyncManager.Parameters["Result"] = e.Message;
                AsyncManager.OutstandingOperations.Decrement();
            }
        }

        public string GetRankingCompleted(string Result)
        {
            return Result;
        }

        public void GetRankingListAsync()// 10명단위로 끊어서 보여주기위한 메소드!
        {
            AsyncManager.OutstandingOperations.Increment();
            JObject Results = new JObject();
            int Auth_Type = System.Convert.ToInt32(Request.Form["Auth_Type"]);
            string Theme_Id = Request.Form["Theme"];
            string Id = Request.Form["User_Code"];
            string nickname = Request.Form["nickname"];
            int firstperson = 0; // 0 = 0 , 1 = 10, 2 = 20 ...
            int lastperson = firstperson + 9; // 0 = 9 , 1 = 19 , 2 = 29 ...
            string userid = Auth_Type + ":" + Id + "*";

            try
            {
                cache = connection.GetDatabase();
                JArray Ja = new JArray();

                SortedSetEntry[] ranks2 = cache.SortedSetRangeByRankWithScores(Theme_Id + "Theme", 0, 30, Order.Descending);
                for (int i = 0; i < ranks2.Length; i++)
                {
                    JObject Rank_Data = new JObject();
                    string[] Ranking = ranks2[i].Element.ToString().Split(':');
                    Rank_Data.Add("Auth_Type", Ranking[0]);

                    if(Ranking[0].Equals("3"))
                    {
                        Rank_Data.Add("Id", Ranking[1] + ":" + Ranking[2]);
                        Rank_Data.Add("Char_Id", Ranking[3]);
                        Rank_Data.Add("Score", ranks2[i].Score.ToString());
                        Rank_Data.Add("Rank", (firstperson + i + 1).ToString());
                        Rank_Data.Add("User_Nick", cache.HashGet("Nickname", Ranking[1] + ":" + Ranking[2]).ToString());
                    } else
                    {
                        Rank_Data.Add("Id", Ranking[1]);
                        Rank_Data.Add("Char_Id", Ranking[2]);
                        Rank_Data.Add("Score", ranks2[i].Score.ToString());
                        Rank_Data.Add("Rank", (firstperson + i + 1).ToString());
                        Rank_Data.Add("User_Nick", cache.HashGet("Nickname", Ranking[1]).ToString());
                    }
                    //Rank_Data.Add("User_Nick", Ranking[1]);

                    // Auth_Type:User_Nick:Id:Char_Id;
                    int UserCount = (int)cache.SortedSetLength(Theme_Id + "Theme");

                    Rank_Data.Add("Percent", UserCount);
                    Ja.Add(Rank_Data);
                }

                Results.Add("Ranking", Ja);

                IEnumerable<SortedSetEntry> My_List = cache.SortedSetScan(Theme_Id + "Theme", userid);

                List<string> Rank_Datas = new List<string>();

                foreach (SortedSetEntry d in My_List)
                {
                    Rank_Datas.Add(d.Element.ToString() + "/" + d.Score);
                }

                if (Rank_Datas.Count >= 1)
                {
                    JObject MyRanking = new JObject();
                    string[] top_data = Rank_Datas[0].Split('/');

                    string user_key = top_data[0];

                    long? s = cache.SortedSetRank(Theme_Id + "Theme", user_key, Order.Descending) + 1;

                    string[] user_data = user_key.Split(':');

                    int UserCount = (int) cache.SortedSetLength(Theme_Id + "Theme");

                    MyRanking.Add("Percent", UserCount);
                    MyRanking.Add("Auth_Type", user_data[0]);
                    if (user_data[0].Equals("3"))
                    {
                        MyRanking.Add("User_Nick", cache.HashGet("Nickname", user_data[1] + ":" + user_data[2]).ToString());
                        MyRanking.Add("Id", user_data[1] + ":" + user_data[2]);
                        MyRanking.Add("Char_Id", user_data[3]);
                        MyRanking.Add("Score", top_data[1]);
                        MyRanking.Add("Rank", s);
                    }
                    else
                    {
                        //MyRanking.Add("User_Nick", user_data[1]);
                        MyRanking.Add("User_Nick", cache.HashGet("Nickname", user_data[1]).ToString());
                        MyRanking.Add("Id", user_data[1]);
                        MyRanking.Add("Char_Id", user_data[2]);
                        MyRanking.Add("Score", top_data[1]);
                        MyRanking.Add("Rank", s);
                    }
                    Results.Add("MyRanking", MyRanking);
                }

                connection.Close();
                AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                AsyncManager.OutstandingOperations.Decrement();
            }
            catch (Exception e)
            {
                connection.Close();
                AsyncManager.Parameters["Result"] = e.Message;
                AsyncManager.OutstandingOperations.Decrement();
            }
        }

        public string GetRankingListCompleted(string Result)
        {
            return Result;
        }

        public void Set_NickAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            string Nickname = Request.Form["Request_Nick"];

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    JObject Results = new JObject();

                    MySqlCommand Get_User_Status = new MySqlCommand();
                    Get_User_Status.Connection = scon;
                    Get_User_Status.CommandText = "Set_Nick";
                    Get_User_Status.CommandType = System.Data.CommandType.StoredProcedure;
                    Get_User_Status.Parameters.AddWithValue("?User_Code", User_Code);
                    Get_User_Status.Parameters.AddWithValue("?Nick_Name", Nickname);
                    MySqlDataReader usr = Get_User_Status.ExecuteReader();

                    if (usr.Read())
                    {
                        Results.Add("Nick", usr["Nick"].ToString());
                    }

                    usr.Close();

                    scon.Close();
                    AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Set_NickCompleted(string Result)
        {
            return Result;
        }

        public void No_AdsAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    JObject Results = new JObject();

                    MySqlCommand Get_User_Status = new MySqlCommand();
                    Get_User_Status.Connection = scon;
                    Get_User_Status.CommandText = "No_Ads";
                    Get_User_Status.CommandType = System.Data.CommandType.StoredProcedure;
                    Get_User_Status.Parameters.AddWithValue("?User_Code", User_Code);
                    MySqlDataReader usr = Get_User_Status.ExecuteReader();

                    if (usr.Read())
                    {
                        Results.Add("No_Ads", usr["No_Ads"].ToString());
                    }

                    usr.Close();

                    scon.Close();
                    AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string No_AdsCompleted(string Result)
        {
            return Result;
        }

        public void Game_EndAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            string Gold = Request.Form["Gold"];
            string Quest_Info = Request.Form["Quest_Info"];
            int Auth_Type = System.Convert.ToInt32(Request.Form["Auth_Type"]);
            string User_Nick = Request.Form["Nick"];
            int Score = System.Convert.ToInt32(Request.Form["Score"]);
            string Theme_Id = Request.Form["Theme"];
            string Id = Request.Form["Id"];
            string Char_Id = Request.Form["Char_Id"];

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    JObject Results = new JObject();

                    cache = connection.GetDatabase();

                    string userid = Auth_Type + ":" + User_Code + ":" + Char_Id;
                    string search_user_id = Auth_Type + ":" + User_Code + ":*";

                    IEnumerable<SortedSetEntry> HighScores = cache.SortedSetScan(Theme_Id + "Theme", search_user_id);

                    List<string> My_HighScores = new List<string>();

                    foreach (SortedSetEntry d in HighScores)
                    {
                        My_HighScores.Add(d.Element.ToString() + "/" + d.Score);
                    }

                    int Top_Data = 0;

                    string[] exist_user_data;

                    string exist_user_id = "";

                    if (My_HighScores.Count >= 1)
                    {
                        string[] top_data = My_HighScores[My_HighScores.Count - 1].Split('/');

                        string user_key = top_data[0];

                        exist_user_data = user_key.Split(':');

                        exist_user_id = exist_user_data[0] + ":" + exist_user_data[1] + ":" + exist_user_data[2];

                        Top_Data = System.Convert.ToInt32(top_data[1]);
                    }

                    if (Score > Top_Data)
                    {
                        cache.SortedSetRemove(Theme_Id + "Theme", exist_user_id);
                        cache.SortedSetAdd(Theme_Id + "Theme", userid, Score);
                    }
                    cache.HashSet("Nickname", new HashEntry[] { new HashEntry(User_Code, User_Nick) });


                    MySqlCommand User_Info_Cmd = new MySqlCommand();
                    User_Info_Cmd.Connection = scon;
                    User_Info_Cmd.CommandText = "Game_Clear";
                    User_Info_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    User_Info_Cmd.Parameters.AddWithValue("?User_Code", User_Code);
                    User_Info_Cmd.Parameters.AddWithValue("?Gain_Gold", Gold);

                    MySqlDataReader uic = User_Info_Cmd.ExecuteReader();

                    if (uic.Read())
                    {
                        Results.Add("UserGold", uic["Gold"].ToString());
                    }

                    uic.Close();

                    IEnumerable<SortedSetEntry> My_List = cache.SortedSetScan(Theme_Id + "Theme", userid);

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

                        long? s = cache.SortedSetRank(Theme_Id + "Theme", user_key, Order.Descending) + 1;

                        string[] user_data = user_key.Split(':');

                        Results.Add(Theme_Id + "TScore", top_data[1]);
                    }

                    connection.Close();
                    MySqlCommand Quest_Set_Cmd = new MySqlCommand();
                    Quest_Set_Cmd.Connection = scon;
                    Quest_Set_Cmd.CommandText = "Refresh_Quest";
                    Quest_Set_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Quest_Set_Cmd.Parameters.AddWithValue("?User_Code", User_Code);

                    JObject Receive_Data = JObject.Parse(Quest_Info);

                    JArray Receive_Quest = JArray.Parse(Receive_Data["Quests"].ToString()) as JArray;


                    for (int i = 0; i < Receive_Quest.Count; i++)
                    {
                        Quest_Set_Cmd.Parameters.AddWithValue("?v_Quest" + i, JsonConvert.SerializeObject(Receive_Quest[i]));
                    }

                    for (int i = 0; i < Quest_Set_Cmd.Parameters.Count; i++)
                    {
                        Quest_Set_Cmd.Parameters[i].Direction = ParameterDirection.Input;
                    }

                    Results.Add("Quests", Receive_Quest);
                    int Query_Result = Quest_Set_Cmd.ExecuteNonQuery();

                    if (Query_Result >= 1)
                    {
                        AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    }
                    else
                    {
                        AsyncManager.Parameters["Result"] = "-1";
                    }

                    scon.Close();
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Game_EndCompleted(string Result)
        {
            return Result;
        }

        public void Game_End_TestAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            string Gold = Request.Form["Gold"];
            string Quest_Info = Request.Form["Quest_Info"];
            int Auth_Type = System.Convert.ToInt32(Request.Form["Auth_Type"]);
            string User_Nick = Request.Form["Nick"];
            int Score = System.Convert.ToInt32(Request.Form["Score"]);
            string Theme_Id = Request.Form["Theme"];
            string Id = Request.Form["Id"];
            string Char_Id = Request.Form["Char_Id"];

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    JObject Results = new JObject();

                    cache = connection.GetDatabase();

                    string userid = Auth_Type + ":" + User_Code + ":" + Char_Id;
                    string search_user_id = Auth_Type + ":" + User_Code + ":*";

                    IEnumerable<SortedSetEntry> HighScores = cache.SortedSetScan(Theme_Id + "Theme", search_user_id);

                    List<string> My_HighScores = new List<string>();

                    foreach (SortedSetEntry d in HighScores)
                    {
                        My_HighScores.Add(d.Element.ToString() + "/" + d.Score);
                    }

                    int Top_Data = 0;

                    string[] exist_user_data;

                    string exist_user_id = "";

                    if (My_HighScores.Count >= 1)
                    {
                        string[] top_data = My_HighScores[My_HighScores.Count - 1].Split('/');

                        string user_key = top_data[0];

                        exist_user_data = user_key.Split(':');

                        exist_user_id = exist_user_data[0] + ":" + exist_user_data[1] + ":" + exist_user_data[2];

                        Top_Data = System.Convert.ToInt32(top_data[1]);
                    }

                    if (Score > Top_Data)
                    {
                        cache.SortedSetRemove(Theme_Id + "Theme", exist_user_id);
                        cache.SortedSetAdd(Theme_Id + "Theme", userid, Score);
                    }
                    cache.HashSet("Nickname", new HashEntry[] { new HashEntry(User_Code, User_Nick) });


                    MySqlCommand User_Info_Cmd = new MySqlCommand();
                    User_Info_Cmd.Connection = scon;
                    User_Info_Cmd.CommandText = "Game_Clear";
                    User_Info_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    User_Info_Cmd.Parameters.AddWithValue("?User_Code", User_Code);
                    User_Info_Cmd.Parameters.AddWithValue("?Gain_Gold", Gold);

                    MySqlDataReader uic = User_Info_Cmd.ExecuteReader();

                    if (uic.Read())
                    {
                        Results.Add("UserGold", uic["Gold"].ToString());
                    }

                    uic.Close();

                    IEnumerable<SortedSetEntry> My_List = cache.SortedSetScan(Theme_Id + "Theme", userid);

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

                        long? s = cache.SortedSetRank(Theme_Id + "Theme", user_key, Order.Descending) + 1;

                        string[] user_data = user_key.Split(':');

                        Results.Add(Theme_Id + "TScore", top_data[1]);
                    }

                    connection.Close();
                    MySqlCommand Quest_Set_Cmd = new MySqlCommand();
                    Quest_Set_Cmd.Connection = scon;
                    Quest_Set_Cmd.CommandText = "Refresh_Quest";
                    Quest_Set_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Quest_Set_Cmd.Parameters.AddWithValue("?User_Code", User_Code);

                    JObject Receive_Data = JObject.Parse(Quest_Info);

                    JArray Receive_Quest = JArray.Parse(Receive_Data["Quests"].ToString()) as JArray;


                    for (int i = 0; i < Receive_Quest.Count; i++)
                    {
                        Quest_Set_Cmd.Parameters.AddWithValue("?v_Quest" + i, JsonConvert.SerializeObject(Receive_Quest[i]));
                    }

                    for (int i = 0; i < Quest_Set_Cmd.Parameters.Count; i++)
                    {
                        Quest_Set_Cmd.Parameters[i].Direction = ParameterDirection.Input;
                    }

                    Results.Add("Quests", Receive_Quest);
                    int Query_Result = Quest_Set_Cmd.ExecuteNonQuery();

                    if (Query_Result >= 1)
                    {
                        AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    }
                    else
                    {
                        AsyncManager.Parameters["Result"] = "-1";
                    }

                    scon.Close();
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Game_End_TestCompleted(string Result)
        {
            return Result;
        }

        public void Game_ResultAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            string Gold = Request.Form["Gold"];
            string Quest_Info = Request.Form["Quest_Info"];
            int Auth_Type = System.Convert.ToInt32(Request.Form["Auth_Type"]);
            string User_Nick = Request.Form["Nick"];
            int Score = System.Convert.ToInt32(Request.Form["Score"]);
            string Theme_Id = Request.Form["Theme"];
            string Id = Request.Form["Id"];
            string Char_Id = Request.Form["Char_Id"];

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    JObject Results = new JObject();

                    cache = connection.GetDatabase();

                    string userid = Auth_Type + ":" + User_Code + ":" + Char_Id;
                    string search_user_id = Auth_Type + ":" + User_Code + ":*";

                    IEnumerable<SortedSetEntry> HighScores = cache.SortedSetScan(Theme_Id + "Theme", search_user_id);

                    List<string> My_HighScores = new List<string>();

                    foreach (SortedSetEntry d in HighScores)
                    {
                        My_HighScores.Add(d.Element.ToString() + "/" + d.Score);
                    }

                    int Top_Data = 0;

                    string[] exist_user_data;

                    string exist_user_id = "";

                    if (My_HighScores.Count >= 1)
                    {
                        string[] top_data = My_HighScores[0].Split('/');

                        string user_key = top_data[0];

                        exist_user_data = user_key.Split(':');

                        exist_user_id = exist_user_data[0] + ":" + exist_user_data[1] + ":" + exist_user_data[2];

                        if(exist_user_data[0].Equals("3"))
                        {
                            exist_user_id = exist_user_data[0] + ":" + exist_user_data[1] + ":" + exist_user_data[2] + ":" + exist_user_data[3];
                        }

                        Top_Data = System.Convert.ToInt32(top_data[1]);
                    }

                    if (Score > Top_Data)
                    {
                        cache.SortedSetRemove(Theme_Id + "Theme", exist_user_id);
                        cache.SortedSetAdd(Theme_Id + "Theme", userid, Score);
                    }

                    cache.HashSet("Nickname", new HashEntry[] { new HashEntry(User_Code, User_Nick) });


                    MySqlCommand User_Info_Cmd = new MySqlCommand();
                    User_Info_Cmd.Connection = scon;
                    User_Info_Cmd.CommandText = "Game_Clear";
                    User_Info_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    User_Info_Cmd.Parameters.AddWithValue("?User_Code", User_Code);
                    User_Info_Cmd.Parameters.AddWithValue("?Gain_Gold", Gold);

                    MySqlDataReader uic = User_Info_Cmd.ExecuteReader();

                    if (uic.Read())
                    {
                        Results.Add("UserGold", uic["Gold"].ToString());
                    }

                    uic.Close();

                    IEnumerable<SortedSetEntry> My_List = cache.SortedSetScan(Theme_Id + "Theme", userid);

                    List<string> Rank_Datas = new List<string>();

                    foreach (SortedSetEntry d in My_List)
                    {
                        Rank_Datas.Add(d.Element.ToString() + "/" + d.Score);
                    }

                    if (Rank_Datas.Count >= 1)
                    {
                        JObject MyRanking = new JObject();
                        string[] top_data = Rank_Datas[0].Split('/');

                        string user_key = top_data[0];

                        long? s = cache.SortedSetRank(Theme_Id + "Theme", user_key, Order.Descending) + 1;

                        string[] user_data = user_key.Split(':');

                        Results.Add(Theme_Id + "TScore", top_data[1]);
                    }

                    connection.Close();
                    MySqlCommand Quest_Set_Cmd = new MySqlCommand();
                    Quest_Set_Cmd.Connection = scon;
                    Quest_Set_Cmd.CommandText = "Refresh_Quest";
                    Quest_Set_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Quest_Set_Cmd.Parameters.AddWithValue("?User_Code", User_Code);

                    JObject Receive_Data = JObject.Parse(Quest_Info);

                    JArray Receive_Quest = JArray.Parse(Receive_Data["Quests"].ToString()) as JArray;


                    for (int i = 0; i < Receive_Quest.Count; i++)
                    {
                        Quest_Set_Cmd.Parameters.AddWithValue("?v_Quest" + i, JsonConvert.SerializeObject(Receive_Quest[i]));
                    }

                    for (int i = 0; i < Quest_Set_Cmd.Parameters.Count; i++)
                    {
                        Quest_Set_Cmd.Parameters[i].Direction = ParameterDirection.Input;
                    }

                    Results.Add("Quests", Receive_Quest);
                    int Query_Result = Quest_Set_Cmd.ExecuteNonQuery();

                    if (Query_Result >= 1)
                    {
                        AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    }
                    else
                    {
                        AsyncManager.Parameters["Result"] = "-1";
                    }

                    scon.Close();
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Game_ResultCompleted(string Result)
        {
            return Result;
        }

        public void Game_Result2Async()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            string Gold = Request.Form["Gold"];
            string Quest_Info = Request.Form["Quest_Info"];
            int Auth_Type = System.Convert.ToInt32(Request.Form["Auth_Type"]);
            string User_Nick = Request.Form["Nick"];
            int Score = System.Convert.ToInt32(Request.Form["Score"]);
            string Theme_Id = Request.Form["Theme"];
            string Id = Request.Form["Id"];
            string Char_Id = Request.Form["Char_Id"];

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    JObject Results = new JObject();

                    cache = connection.GetDatabase();

                    string userid = Auth_Type + ":" + User_Code + ":" + Char_Id;
                    string search_user_id = Auth_Type + ":" + User_Code + ":*";

                    IEnumerable<SortedSetEntry> HighScores = cache.SortedSetScan(Theme_Id + "Theme", search_user_id);

                    List<string> My_HighScores = new List<string>();

                    foreach (SortedSetEntry d in HighScores)
                    {
                        My_HighScores.Add(d.Element.ToString() + "/" + d.Score);
                    }

                    int Top_Data = 0;

                    string[] exist_user_data;

                    string exist_user_id = "";

                    if (My_HighScores.Count >= 1)
                    {
                        string[] top_data = My_HighScores[0].Split('/');

                        string user_key = top_data[0];

                        exist_user_data = user_key.Split(':');

                        exist_user_id = exist_user_data[0] + ":" + exist_user_data[1] + ":" + exist_user_data[2];

                        if (exist_user_data[0].Equals("3"))
                        {
                            exist_user_id = exist_user_data[0] + ":" + exist_user_data[1] + ":" + exist_user_data[2] + ":" + exist_user_data[3];
                        }

                        Top_Data = System.Convert.ToInt32(top_data[1]);
                    }

                    if (Score > Top_Data)
                    {
                        cache.SortedSetRemove(Theme_Id + "Theme", exist_user_id);
                        cache.SortedSetAdd(Theme_Id + "Theme", userid, Score);
                    }

                    cache.HashSet("Nickname", new HashEntry[] { new HashEntry(User_Code, User_Nick) });


                    MySqlCommand User_Info_Cmd = new MySqlCommand();
                    User_Info_Cmd.Connection = scon;
                    User_Info_Cmd.CommandText = "Game_Clear";
                    User_Info_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    User_Info_Cmd.Parameters.AddWithValue("?User_Code", User_Code);
                    User_Info_Cmd.Parameters.AddWithValue("?Gain_Gold", Gold);

                    MySqlDataReader uic = User_Info_Cmd.ExecuteReader();

                    if (uic.Read())
                    {
                        Results.Add("UserGold", uic["Gold"].ToString());
                    }

                    uic.Close();

                    IEnumerable<SortedSetEntry> My_List = cache.SortedSetScan(Theme_Id + "Theme", userid);

                    List<string> Rank_Datas = new List<string>();

                    foreach (SortedSetEntry d in My_List)
                    {
                        Rank_Datas.Add(d.Element.ToString() + "/" + d.Score);
                    }

                    if (Rank_Datas.Count >= 1)
                    {
                        JObject MyRanking = new JObject();
                        string[] top_data = Rank_Datas[0].Split('/');

                        string user_key = top_data[0];

                        long? s = cache.SortedSetRank(Theme_Id + "Theme", user_key, Order.Descending) + 1;

                        string[] user_data = user_key.Split(':');

                        Results.Add(Theme_Id + "TScore", top_data[1]);
                    }

                    connection.Close();
                    MySqlCommand Quest_Set_Cmd = new MySqlCommand();
                    Quest_Set_Cmd.Connection = scon;
                    Quest_Set_Cmd.CommandText = "Refresh_Quest";
                    Quest_Set_Cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    Quest_Set_Cmd.Parameters.AddWithValue("?User_Code", User_Code);

                    JObject Receive_Data = JObject.Parse(Quest_Info);

                    JArray Receive_Quest = JArray.Parse(Receive_Data["Quests"].ToString()) as JArray;


                    for (int i = 0; i < Receive_Quest.Count; i++)
                    {
                        Quest_Set_Cmd.Parameters.AddWithValue("?v_Quest" + i, JsonConvert.SerializeObject(Receive_Quest[i]));
                    }

                    for (int i = 0; i < Quest_Set_Cmd.Parameters.Count; i++)
                    {
                        Quest_Set_Cmd.Parameters[i].Direction = ParameterDirection.Input;
                    }

                    Results.Add("Quests", Receive_Quest);
                    int Query_Result = Quest_Set_Cmd.ExecuteNonQuery();

                    if (Query_Result >= 1)
                    {
                        AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    }
                    else
                    {
                        AsyncManager.Parameters["Result"] = "-1";
                    }

                    scon.Close();
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = User_Code + "/" + Gold + "/" + Quest_Info + "/" + Auth_Type + "/" + User_Nick + "/" + Score + "/" + Theme_Id + "/" + Id + "/" + Char_Id ;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Game_Result2Completed(string Result)
        {
            return Result;
        }


        public void Friends_RankingAsync()// 10명단위로 끊어서 보여주기위한 메소드!
        {
            AsyncManager.OutstandingOperations.Increment();
            JObject Results = new JObject();
            int Auth_Type = System.Convert.ToInt32(Request.Form["Auth_Type"]);
            string Theme_Id = Request.Form["Theme"];
            string Id = Request.Form["Id"];
            string nickname = Request.Form["nickname"];
            string User_Code = Request.Form["User_Code"];
            string friends_data = Request.Form["data"];

            string userid = Auth_Type + ":" + User_Code + "*";

            JObject datas = JObject.Parse(friends_data);

            try
            {
                cache = connection.GetDatabase();
                JArray Ja = JArray.Parse(datas["data"].ToString());

                JArray Friends = new JArray();

                for (int i = 0; i < Ja.Count; i++)
                {
                    JObject Friend = new JObject();
                    string friends_id = Auth_Type + ":" + Ja[i]["id"] + "*";
                    IEnumerable<SortedSetEntry> Friends_Rankings = cache.SortedSetScan(Theme_Id + "Theme", friends_id);
                    List<string> Rank_Datas = new List<string>();

                    foreach (SortedSetEntry d in Friends_Rankings)
                    {
                        Rank_Datas.Add(d.Element.ToString() + "/" + d.Score);
                    }

                    if (Rank_Datas.Count == 0)
                    {
                        continue;
                    }

                    string[] top_data = Rank_Datas[Rank_Datas.Count - 1].Split('/');

                    string user_key = top_data[0];

                    long? s = cache.SortedSetRank(Theme_Id + "Theme", user_key, Order.Descending) + 1;

                    string[] user_data = user_key.Split(':');

                    if(user_data[0].Equals("3"))
                    {
                        Friend.Add("Auth_Type", user_data[0]);
                        Friend.Add("Id", user_data[1] + ":" + user_data[2]);
                        Friend.Add("Char_Id", user_data[3]);
                        Friend.Add("Score", top_data[1]);
                        Friend.Add("Rank", s);
                    } else
                    {
                        Friend.Add("Auth_Type", user_data[0]);
                        Friend.Add("Id", user_data[1]);
                        Friend.Add("Char_Id", user_data[2]);
                        Friend.Add("Score", top_data[1]);
                        Friend.Add("Rank", s);
                    }

                    int UserCount = (int)cache.SortedSetLength(Theme_Id + "Theme");

                    Friend.Add("Percent", UserCount);
                    Friend.Add("User_Nick", cache.HashGet("Nickname", user_data[1]).ToString());

                    Friends.Add(Friend);
                }

                Results.Add("Friends", Friends);

                List<string> MyRanking_Datas = new List<string>();
                IEnumerable<SortedSetEntry> My_Rankings = cache.SortedSetScan(Theme_Id + "Theme", userid);

                foreach (SortedSetEntry d in My_Rankings)
                {
                    MyRanking_Datas.Add(d.Element.ToString() + "/" + d.Score);
                }


                if (MyRanking_Datas.Count >= 1)
                {
                    JObject MyRanking = new JObject();
                    string[] top_data = MyRanking_Datas[MyRanking_Datas.Count - 1].Split('/');
                    string user_key = top_data[0];

                    long? s = cache.SortedSetRank(Theme_Id + "Theme", user_key, Order.Descending) + 1;

                    string[] user_data = user_key.Split(':');

                    if (user_data[0].Equals("3"))
                    {
                        MyRanking.Add("Auth_Type", user_data[0]);
                        MyRanking.Add("Id", user_data[1] + ":"+  user_data[2]);
                        MyRanking.Add("Char_Id", user_data[3]);
                        MyRanking.Add("Score", top_data[1]);
                        MyRanking.Add("Rank", s);
                        MyRanking.Add("User_Nick", cache.HashGet("Nickname", user_data[1] + ":" + user_data[2]).ToString());
                    }
                    else
                    {
                        MyRanking.Add("Auth_Type", user_data[0]);
                        MyRanking.Add("Id", user_data[1]);
                        MyRanking.Add("Char_Id", user_data[2]);
                        MyRanking.Add("Score", top_data[1]);
                        MyRanking.Add("Rank", s);
                        MyRanking.Add("User_Nick", cache.HashGet("Nickname", user_data[1]).ToString());
                    }


                    Results.Add("MyRanking", MyRanking);
                }
                connection.Close();

                AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                AsyncManager.OutstandingOperations.Decrement();
            }
            catch (Exception e)
            {
                connection.Close();
                AsyncManager.Parameters["Result"] = e.Message;
                AsyncManager.OutstandingOperations.Decrement();
            }
        }

        public string Friends_RankingCompleted(string Result)
        {
            return Result;
        }

        public void Gold_BuffAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {
                    JObject Results = new JObject();
                    DateTime Buff_Time = DateTime.Now.AddMinutes(8);

                    MySqlCommand Get_User_Status = new MySqlCommand();
                    Get_User_Status.Connection = scon;
                    Get_User_Status.CommandText = "Gold_Buff_On";
                    Get_User_Status.CommandType = System.Data.CommandType.StoredProcedure;
                    Get_User_Status.Parameters.AddWithValue("?User_Code", User_Code);
                    Get_User_Status.Parameters.AddWithValue("?Buff_Time", Buff_Time);
                    MySqlDataReader usr = Get_User_Status.ExecuteReader();

                    if (usr.Read())
                    {
                        Results.Add("Gold_Time", usr["Gold_buff"].ToString());
                    }

                    usr.Close();

                    scon.Close();
                    AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Gold_BuffCompleted(string Result)
        {
            return Result;
        }
    }
}

public class GoogleJsonWebToken
{
    //private static ILogger Log = LogManager.GetCurrentClassLogger();
    public const string SCOPE_AUTH_ANDROIDPUBLISHER = "https://www.googleapis.com/auth/androidpublisher";

    public static dynamic GetAccessToken(string clientIdEMail, string keyFilePath, string szScope)
    {
        // certificate
        var certificate = new X509Certificate2(keyFilePath, "notasecret");



        // header
        var header = new { alg = "RS256", typ = "JWT" };



        // claimset
        var times = GetExpiryAndIssueDate();
        var claimset = new
        {
            iss = clientIdEMail,
            scope = szScope,
            aud = "https://accounts.google.com/o/oauth2/token",
            iat = times[0],
            exp = times[1],
        };

        JavaScriptSerializer ser = new JavaScriptSerializer();



        // encoded header
        var headerSerialized = ser.Serialize(header);
        var headerBytes = Encoding.UTF8.GetBytes(headerSerialized);
        var headerEncoded = Base64UrlEncode(headerBytes);



        // encoded claimset
        var claimsetSerialized = ser.Serialize(claimset);
        //Log.DebugFormat("claimset[{0}]", claimsetSerialized);
        var claimsetBytes = Encoding.UTF8.GetBytes(claimsetSerialized);
        var claimsetEncoded = Base64UrlEncode(claimsetBytes);

        // input
        var input = headerEncoded + "." + claimsetEncoded;
        var inputBytes = Encoding.UTF8.GetBytes(input);

        // signiture
        var rsa = certificate.PrivateKey as RSACryptoServiceProvider;
        var cspParam = new CspParameters
        {
            KeyContainerName = rsa.CspKeyContainerInfo.KeyContainerName,
            KeyNumber = rsa.CspKeyContainerInfo.KeyNumber == KeyNumber.Exchange ? 1 : 2
        };
        var aescsp = new RSACryptoServiceProvider(cspParam) { PersistKeyInCsp = false };
        var signatureBytes = aescsp.SignData(inputBytes, "SHA256");
        var signatureEncoded = Base64UrlEncode(signatureBytes);



        // jwt
        var jwt = headerEncoded + "." + claimsetEncoded + "." + signatureEncoded;
        //Log.DebugFormat("jwt[{0}]", jwt);

        var client = new WebClient();
        client.Encoding = Encoding.UTF8;
        var uri = "https://accounts.google.com/o/oauth2/token";
        var content = new NameValueCollection();

        content["assertion"] = jwt;
        content["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer";

        string response = Encoding.UTF8.GetString(client.UploadValues(uri, "POST", content));

        //Log.DebugFormat("response[{0}]", response);
        var result = ser.Deserialize<dynamic>(response);

        return result;
    }



    private static string Base64UrlEncode(byte[] input)
    {
        var output = Convert.ToBase64String(input);
        output = output.Split('=')[0]; // Remove any trailing '='s
        output = output.Replace('+', '-'); // 62nd char of encoding
        output = output.Replace('/', '_'); // 63rd char of encoding
        return output;
    }



    private static int[] GetExpiryAndIssueDate()
    {
        var utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        var issueTime = DateTime.UtcNow;

        var iat = (int)issueTime.Subtract(utc0).TotalSeconds;
        var exp = (int)issueTime.AddMinutes(55).Subtract(utc0).TotalSeconds;

        return new[] { iat, exp };
    }

}