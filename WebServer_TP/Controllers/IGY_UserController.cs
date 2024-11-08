using System;
using System.Data;
using System.Web.Mvc;
using JsWebServer_CP.Models;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsWebServer_CP.Controllers
{
    public class IGY_UserController : AsyncController
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlCommand scom = new SqlCommand();
        static Random Ran = new Random();

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

        #region Quest
        public void Quest_ClearAsync(/*string User_Code, string In_id*/) // 퀘스트 클리어
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            //int Target_Id = System.Convert.ToInt32(Request.Form["Target_Ids"]);

            int In_id = System.Convert.ToInt32(Request.Form["In_ids"]);
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Igy_db_conn"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string Check_String = "Get_Quest_Check @UserCode,@I_id";
                    SqlCommand Check_String_Cmd = new SqlCommand(Check_String, scon);

                    Check_String_Cmd.Parameters.AddWithValue("@UserCode", User_Code);
                    Check_String_Cmd.Parameters.AddWithValue("@I_id", In_id);

                    SqlDataReader Status_Reader = Check_String_Cmd.ExecuteReader();

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
                    string Clear_String = "[Clear_Quest] @UserCode,@Quest_Status,@I_id,@Id,@Value";

                    SqlCommand Clear_String_Cmd = new SqlCommand(Clear_String, scon);
                    Clear_String_Cmd.Parameters.AddWithValue("@UserCode", User_Code);
                    Clear_String_Cmd.Parameters.AddWithValue("@Quest_Status", Cleared);
                    Clear_String_Cmd.Parameters.AddWithValue("@I_id", In_id);
                    Clear_String_Cmd.Parameters.AddWithValue("@Id", Id);
                    Clear_String_Cmd.Parameters.AddWithValue("@Value", Current_Value);


                    SqlDataReader Clear_Reader = Clear_String_Cmd.ExecuteReader();
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
                                Quests.Add("I_id", i);
                                Quests.Add("Id", 0);
                                Quests.Add("Date", "0");
                                Quests.Add("Cleared_Date", "0");
                                Quests.Add("Value", 0);
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

        public void Quest_SetAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            string Q_Status = Request.Form["Q_Status"];

            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Igy_db_conn"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string Quest_Set = "Get_Quest_Status @UserCode";
                    SqlCommand Quest_Set_Cmd = new SqlCommand(Quest_Set, scon);

                    Quest_Set_Cmd.Parameters.AddWithValue("@UserCode", User_Code);

                    SqlDataReader Quest_Set_Reader = Quest_Set_Cmd.ExecuteReader();

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
                                Quest_Info.Add("I_id", i);
                                Quest_Info.Add("Id", 0);
                                Quest_Info.Add("Date", "0");
                                Quest_Info.Add("Cleared_Date", "0");
                                Quest_Info.Add("Value", 0);
                            }
                            else
                            {
                                Quest_Info = JObject.Parse(Quest_Infos[i]);
                            }

                            Quest_List.Add(Quest_Info);
                        }
                    }

                    Results.Add("Quests", Quest_List);

                    Quest_Set_Reader.Close();

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
        } // 퀘스트 정보 세팅(스테이지 클리어시)

        public string Quest_SetCompleted(string Result)
        {
            return Result;
        }

        public void Quest_GetAsync(/*string User_Code, int isAd, int In_id*/) // 퀘스트 받기
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            int isAd = System.Convert.ToInt32(Request.Form["isAd"]);
            int In_id = System.Convert.ToInt32(Request.Form["In_id"]);

            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Igy_db_conn"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string Quest_Set = "Get_Quest_Status @UserCode";
                    SqlCommand Quest_Set_Cmd = new SqlCommand(Quest_Set, scon);

                    Quest_Set_Cmd.Parameters.AddWithValue("@UserCode", User_Code);

                    SqlDataReader Quest_Set_Reader = Quest_Set_Cmd.ExecuteReader();

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
                                Quest_Info.Add("I_id", i);
                                Quest_Info.Add("Id", 0);
                                Quest_Info.Add("Date", "0");
                                Quest_Info.Add("Cleared_Date", "0");
                                Quest_Info.Add("Value", 0);
                            }
                            else
                            {
                                Quest_Info = JObject.Parse(Quest_Infos[i]);
                            }

                            Quest_List.Add(Quest_Info);
                        }
                    }

                    Quest_Set_Reader.Close();

                    string Refresh_Quest = "Refresh_Quest @UserCode, @Quest_Status0, @Quest_Status1, @Quest_Status2";
                    SqlCommand Refresh_Quest_Cmd = new SqlCommand(Refresh_Quest, scon);

                    Refresh_Quest_Cmd.Parameters.AddWithValue("@UserCode", User_Code);

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
                                        Quest_List[i] = Quest_Generate(i);
                                    }
                                }
                            }
                        }

                        Refresh_Quest_Cmd.Parameters.AddWithValue("@Quest_Status" + i, JsonConvert.SerializeObject(Quest_List[i]));
                    }

                    int Refreshed = Refresh_Quest_Cmd.ExecuteNonQuery();
                    if (Refreshed >= 1)
                    {
                        Results.Add("Quests", Quest_List);
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

        public string Quest_GetCompleted(string Result)
        {
            return Result;
        }

        public JObject Quest_Generate(int I_id)
        {
            JObject Quest = new JObject();

            int Quest_Id = Ran.Next(1, 4);

            Quest.Add("I_id", I_id);
            Quest.Add("Id", Quest_Id);
            Quest.Add("Date", System.DateTime.Now);
            Quest.Add("Cleared_Date", System.DateTime.Now);
            Quest.Add("Value", "0");

            return Quest;
        }

        public void Refresh_QuestAsync(/*string User_Code, string Quest_Info*/)
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            string Quest_Info = Request.Form["Quest_Info"];
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Igy_db_conn"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string Quest_Set = "Refresh_Quest @UserCode, @Quest_Status0, @Quest_Status1, @Quest_Status2";
                    SqlCommand Quest_Set_Cmd = new SqlCommand(Quest_Set, scon);

                    Quest_Set_Cmd.Parameters.AddWithValue("@UserCode", User_Code);

                    JObject Results = new JObject();
                    JObject Receive_Data = JObject.Parse(Quest_Info);

                    JArray Receive_Quest = JArray.Parse(Receive_Data["Quests"].ToString()) as JArray;


                    for (int i = 0; i < Receive_Quest.Count; i++)
                    {
                        Quest_Set_Cmd.Parameters.AddWithValue("@Quest_Status" + i, JsonConvert.SerializeObject(Receive_Quest[i]));
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

            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Igy_db_conn"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string Quest_Set = "Get_Quest_Status @UserCode";
                    SqlCommand Quest_Set_Cmd = new SqlCommand(Quest_Set, scon);

                    Quest_Set_Cmd.Parameters.AddWithValue("@UserCode", User_Code);

                    SqlDataReader Quest_Set_Reader = Quest_Set_Cmd.ExecuteReader();

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
                                Quest_Info.Add("I_id", i);
                                Quest_Info.Add("Id", 0);
                                Quest_Info.Add("Date", "0");
                                Quest_Info.Add("Cleared_Date", "0");
                                Quest_Info.Add("Value", 0);
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

                    string Refresh_Quest = "Refresh_Quest @UserCode, @Quest_Status0, @Quest_Status1, @Quest_Status2";
                    SqlCommand Refresh_Quest_Cmd = new SqlCommand(Refresh_Quest, scon);

                    Refresh_Quest_Cmd.Parameters.AddWithValue("@UserCode", User_Code);

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
                                    Quest_List[i] = Quest_Generate(i);
                                }
                            }
                            else
                            {
                                Quest_List[i] = Quest_Generate(i);
                            }

                            Refresh_Quest_Cmd.Parameters.AddWithValue("@Quest_Status" + i, JsonConvert.SerializeObject(Quest_List[i]));
                        }
                        else
                        {
                            Refresh_Quest_Cmd.Parameters.AddWithValue("@Quest_Status" + i, JsonConvert.SerializeObject(Quest_List[i]));
                        }
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
        #endregion

        #region Shop

        public void Get_ItemAsync()
        {

        }

        public string Get_ItemCompleted(string Result)
        {
            return Result;
        }

        public void Change_AuthAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            string Auth_Type = Request.Form["Auth_Type"];
            string New_User_Code = Request.Form["NUser_Code"];
            //int Target_Id = System.Convert.ToInt32(Request.Form["Target_Ids"]);

            int In_id = System.Convert.ToInt32(Request.Form["In_ids"]);
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Igy_db_conn"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {/*
                    string Auth_String = "Auth_Change @UserCode,@Auth_Type,@Auth_Code";
                    SqlCommand Auth_String_Cmd = new SqlCommand(Auth_String, scon);

                    Auth_String_Cmd.Parameters.AddWithValue("@UserCode", User_Code);
                    Auth_String_Cmd.Parameters.AddWithValue("@I_id", In_id);s

                    SqlDataReader Status_Reader = Auth_String_Cmd.ExecuteReader();

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
                    }*/
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

        public void Buy_ChaAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string User_Code = Request.Form["User_Code"];
            string Quest_Info = Request.Form["Quest_Info"];
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Igy_db_conn"];
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string Quest_Set = "Refresh_Quest @UserCode, @Quest_Status0, @Quest_Status1, @Quest_Status2";
                    SqlCommand Quest_Set_Cmd = new SqlCommand(Quest_Set, scon);

                    Quest_Set_Cmd.Parameters.AddWithValue("@UserCode", User_Code);
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Buy_ChaCompleted(string Result)
        {
            return Result;
        }
        #endregion

        
    }
}