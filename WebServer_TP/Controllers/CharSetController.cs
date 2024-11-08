﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsWebServer_CP.Controllers
{
    public class CharSetController : AsyncController
    {
        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlCommand scom = new SqlCommand();

        public void Level_upAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            string char_id = Request.Form["char_id"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    SqlCommand Exp_Check = new SqlCommand("exec Char_Level_Up " + char_id, scon);
                    SqlDataReader Check_Result = Exp_Check.ExecuteReader();
                    string[] Return_Data = new string[3];
                    System.Text.StringBuilder Sb = new System.Text.StringBuilder();

                    if (Check_Result.Read())
                    {
                        Return_Data[0] = Check_Result["Lev"].ToString();
                        Return_Data[1] = Check_Result["CurExp"].ToString();
                        Return_Data[2] = Check_Result["TotalExp"].ToString();
                        for (int i = 0; i < Return_Data.Length; i++)
                        {
                            Sb.Append(Return_Data[i]);
                            if (i != (Return_Data.Length - 1))
                            {
                                Sb.Append(":");
                            }
                        }
                    }
                    else
                    {
                        Sb.Append("0");
                    }

                    Check_Result.Close();
                    scon.Close();
                    AsyncManager.Parameters["Result"] = Sb.ToString();
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

        public string Level_upCompleted(string Result)
        {
            return Result;
        }

        public void Equip_SaveAsync(string user_id, string char_id, string Equiplist)
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            /*string user_id = Request.Form["user_id"];
            string char_id = Request.Form["char_id"];
            string Equiplist = Request.Form["Equiplist"];*/

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    SqlCommand Equip_Saving = new SqlCommand("update Char_info set Item_Equiped = '" + Equiplist + "' where User_Code = N'" + user_id + "' and Char_id = '" + char_id + "'", scon);
                    int Saving_Result = Equip_Saving.ExecuteNonQuery();

                    if (Saving_Result == 1)
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "1";
                    }
                    else
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-1";
                    }
                    AsyncManager.OutstandingOperations.Decrement();

                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = "-1";
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Equip_SaveCompleted(string Result)
        {
            return Result;
        }

        public void Quest_ClearAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            string user_id = Request.Form["User_code"];
            string char_id = Request.Form["char_id"];
            int Request_Quest_ID = System.Convert.ToInt32(Request.Form["Quest_ID"]);
            int slot = 0;
            string User_Status = "";
            string Quest_Condition = "";
            string[] Quest_info = new string[2];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    SqlCommand Quest_Data = new SqlCommand("exec Check_Char_Quest " + char_id + "," + Request_Quest_ID, scon);
                    SqlDataReader Check_Result = Quest_Data.ExecuteReader();
                    System.Text.StringBuilder Sb = new System.Text.StringBuilder();
                    if (Check_Result.Read())
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            User_Status = Check_Result["Cur_Quest" + i].ToString();
                            Quest_info = User_Status.Split(':');
                            int Quest_id = System.Convert.ToInt32(Quest_info[0]);

                            if (Quest_id == Request_Quest_ID)
                            {
                                slot = i;
                                break;
                            }
                            else
                            {
                                if (i == 1)
                                {
                                    scon.Close();
                                    AsyncManager.Parameters["Result"] = "-2";
                                    AsyncManager.OutstandingOperations.Decrement();
                                    return;
                                } else
                                {
                                    continue;
                                }
                            }
                        }

                        string[] User_counter = Quest_info[1].Split(',');

                        Quest_Condition = Check_Result["Counter"].ToString();
                        Check_Result.Close();

                        string[] Quest_counter = Quest_Condition.Split(';');
                        int[] Condition = new int[Quest_counter.Length];

                        int[] counter = new int[Quest_counter.Length];

                        for (int j = 0; j < User_counter.Length; j++)
                        {
                            counter[j] = System.Convert.ToInt32(User_counter[j]);
                        }

                        for (int j = 0; j < Condition.Length; j++)
                        {
                            Condition[j] = System.Convert.ToInt32(Quest_counter[j]);
                        }

                        bool IsCleared = true;
                        for (int j = 0; j < Condition.Length; j++)
                        {
                            if (counter[j] < Condition[j]) IsCleared = false;
                        }

                        if (!IsCleared)
                        {
                            scon.Close();
                            AsyncManager.Parameters["Result"] = "-3";
                            AsyncManager.OutstandingOperations.Decrement();
                            return;
                        }

                        JObject json = new JObject();
                        SqlCommand Quest_Clear = new SqlCommand("exec Quest_Clear '" + user_id + "'," + char_id + "," + Request_Quest_ID + "," + slot, scon);
                        SqlDataReader Clear_Result = Quest_Clear.ExecuteReader();
                        if (Clear_Result.Read())
                        {
                            Sb.Append(Request_Quest_ID + "|");
                            Sb.Append(Clear_Result[0].ToString() + "|");
                            Sb.Append(Clear_Result[1].ToString() + "|");
                            Sb.Append(Clear_Result[2].ToString() + "|");
                            Sb.Append(Clear_Result[3].ToString() + "|");
                            Sb.Append(Clear_Result[4].ToString());
                            AsyncManager.Parameters["Result"] = Sb.ToString();
                        }


                        Clear_Result.Close();
                        scon.Close();
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                    else
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-4";
                        AsyncManager.OutstandingOperations.Decrement();
                    }
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

        public void Quest_Clear2Async()
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            string user_id = Request.Form["User_code"];
            string char_id = Request.Form["char_id"];
            int Request_Quest_ID = System.Convert.ToInt32(Request.Form["Quest_ID"]);
            int slot = 0;
            string User_Status = "";
            string Quest_Condition = "";
            string[] Quest_info = new string[2];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    SqlCommand Quest_Data = new SqlCommand("exec Check_Char_Quest " + char_id + "," + Request_Quest_ID, scon);
                    SqlDataReader Check_Result = Quest_Data.ExecuteReader();
                    System.Text.StringBuilder Sb = new System.Text.StringBuilder();
                    if (Check_Result.Read())
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            User_Status = Check_Result["Cur_Quest" + i].ToString();
                            Quest_info = User_Status.Split(':');
                            int Quest_id = System.Convert.ToInt32(Quest_info[0]);

                            if (Quest_id == Request_Quest_ID)
                            {
                                slot = i;
                                break;
                            }
                            else
                            {
                                if (i == 1)
                                {
                                    scon.Close();
                                    AsyncManager.Parameters["Result"] = "-2";
                                    AsyncManager.OutstandingOperations.Decrement();
                                    return;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }

                        string[] User_counter = Quest_info[1].Split(',');

                        Quest_Condition = Check_Result["Counter"].ToString();
                        Check_Result.Close();

                        string[] Quest_counter = Quest_Condition.Split(';');
                        int[] Condition = new int[Quest_counter.Length];

                        int[] counter = new int[Quest_counter.Length];

                        for (int j = 0; j < User_counter.Length; j++)
                        {
                            counter[j] = System.Convert.ToInt32(User_counter[j]);
                        }

                        for (int j = 0; j < Condition.Length; j++)
                        {
                            Condition[j] = System.Convert.ToInt32(Quest_counter[j]);
                        }

                        bool IsCleared = true;
                        for (int j = 0; j < Condition.Length; j++)
                        {
                            if (counter[j] < Condition[j]) IsCleared = false;
                        }

                        if (!IsCleared)
                        {
                            scon.Close();
                            AsyncManager.Parameters["Result"] = "-3";
                            AsyncManager.OutstandingOperations.Decrement();
                            return;
                        }

                        JObject json = new JObject();
                        SqlCommand Quest_Clear = new SqlCommand("exec Quest_Clear '" + user_id + "'," + char_id + "," + Request_Quest_ID + "," + slot, scon);
                        SqlDataReader Clear_Result = Quest_Clear.ExecuteReader();
                        if (Clear_Result.Read())
                        {

                            json.Add("ID", Request_Quest_ID);
                            json.Add("info_0", Clear_Result[0].ToString());
                            json.Add("info_1", Clear_Result[1].ToString());
                            json.Add("CurExp", Clear_Result[2].ToString());
                            json.Add("TotalExp", Clear_Result[3].ToString());
                            json.Add("Cur_Gold", Clear_Result[4].ToString());
                            /*
                            Sb.Append(Request_Quest_ID + "|");
                            Sb.Append(Clear_Result[0].ToString() + "|");
                            Sb.Append(Clear_Result[1].ToString() + "|");
                            Sb.Append(Clear_Result[2].ToString() + "|");
                            Sb.Append(Clear_Result[3].ToString() + "|");
                            Sb.Append(Clear_Result[4].ToString());*/
                            AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(json);
                        }


                        Clear_Result.Close();
                        scon.Close();
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                    else
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-4";
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Quest_Clear2Completed(string Result)
        {
            return Result;
        }

        public void Quest_ReceiveAsync(/* char_id, string Quest_ID, int slot*/)
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            string char_id = Request.Form["char_id"];
            string Quest_ID = Request.Form["Quest_ID"];
            int slot = System.Convert.ToInt32(Request.Form["slot"]);

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    SqlCommand Quest_Data = new SqlCommand("exec Check_Char_Quest " + char_id + "," + Quest_ID, scon);
                    SqlDataReader Check_Result = Quest_Data.ExecuteReader();

                    if (Check_Result.Read())
                    {
                        string Cur_Quest0 = Check_Result[0].ToString().Split(':')[0];
                        string Cur_Quest1 = Check_Result[1].ToString().Split(':')[0];
                        Check_Result.Close();

                        if (Cur_Quest0.Equals(Quest_ID) || Cur_Quest1.Equals(Quest_ID))
                        {
                            scon.Close();
                            AsyncManager.Parameters["Result"] = "-1";
                            AsyncManager.OutstandingOperations.Decrement();
                            return;
                        }

                        if (Cur_Quest0.Equals("0"))
                        {
                            slot = 0;
                        }
                        else if (Cur_Quest1.Equals("0"))
                        {
                            slot = 1;
                        }
                        else
                        {
                            scon.Close();
                            AsyncManager.Parameters["Result"] = "-1";
                            AsyncManager.OutstandingOperations.Decrement();
                            return;
                        }

                        SqlCommand Quest_Receive = new SqlCommand("exec [Quest_Receive] " + char_id + "," + Quest_ID + "," + slot.ToString(), scon);
                        SqlDataReader Check_Quest = Quest_Receive.ExecuteReader();
                        System.Text.StringBuilder Sb = new System.Text.StringBuilder();

                        if (Check_Quest.Read())
                        {
                            Sb.Append(Check_Quest[0] + "|");
                            Sb.Append(Check_Quest[1]);
                            AsyncManager.Parameters["Result"] = Sb.ToString();
                        }
                        else
                        {
                            AsyncManager.Parameters["Result"] = "-1";
                        }

                        Check_Result.Close();
                        scon.Close();
                        AsyncManager.OutstandingOperations.Decrement();
                        return;
                    }
                    else
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-1";
                        AsyncManager.OutstandingOperations.Decrement();
                        return;
                    }
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Quest_ReceiveCompleted(string Result)
        {
            return Result;
        }

        public void L_BonusAsync()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];

            string user_id = Request.Form["User_code"];
            string char_id = Request.Form["char_id"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    SqlCommand Request_Bonus = new SqlCommand("exec L_Bonus '" + user_id + "'," + char_id, scon);
                    SqlDataReader Check_Result = Request_Bonus.ExecuteReader();

                    if (Check_Result.Read())
                    {
                        for (int i = 0; i < Check_Result.VisibleFieldCount; i++)
                        {
                            sb.Append(Check_Result[i]);
                            if (i != (Check_Result.VisibleFieldCount - 1))
                            {
                                sb.Append("|");
                            }
                        }

                        scon.Close();
                        AsyncManager.Parameters["Result"] = sb.ToString();
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                    else
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-1";
                        AsyncManager.OutstandingOperations.Decrement();
                    }
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

        public void Rank_listAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            int rank_page = System.Convert.ToInt32(Request.Form["page_number"]);
            int rank_kind = System.Convert.ToInt32(Request.Form["kind"]); //  3,4,5
            string cmd = "dbo.[Rank_list] " + (rank_page * 6 - 5) + "," + (rank_page * 6) + "," + (rank_kind+1).ToString();

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    SqlCommand Rank_Load = new SqlCommand(cmd, scon);
                    SqlDataReader Check_Result = Rank_Load.ExecuteReader();

                    var loop = true;
                    while (loop)
                    {
                        loop = Check_Result.Read();
                        if (!loop)
                        {
                            if (sb.Length >= 1)
                                sb.Remove((sb.Length - 1), 1);
                        }
                        else
                        {
                            sb.Append(Check_Result[0] + ":");
                            sb.Append(Check_Result[1] + ":");
                            sb.Append(Check_Result[rank_kind] + "|");
                        }
                    }
                    AsyncManager.Parameters["Result"] = sb.ToString();
                    Check_Result.Close();
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

        public string Rank_listCompleted(string Result)
        {
            return Result;
        }

        public void Check_NickAsync(/*string user_id, string char_id, string Nick*/)
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            
            string user_id = Request.Form["User_code"];
            string char_id = Request.Form["char_id"];
            string Nick = Request.Form["Nickname"];
            
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    SqlCommand Check_Nick = new SqlCommand("exec Check_Nick '" + user_id + "'," + char_id + ",'" + Nick + "'", scon);
                    SqlDataReader Check_Result = Check_Nick.ExecuteReader();

                    if (Check_Result.Read())
                    {
                        sb.Append(Check_Result["Nick"].ToString());
                        scon.Close();
                        AsyncManager.Parameters["Result"] = sb.ToString();
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                    else
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "Fail";
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Check_NickCompleted(string Result)
        {
            return Result;
        }

        public void Cancel_NickAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];

            string user_id = Request.Form["User_code"];
            string char_id = Request.Form["char_id"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    SqlCommand Cancel_Nick = new SqlCommand("exec Cancel_Nick '" + user_id + "'," + char_id, scon);
                    int Check_Result = Cancel_Nick.ExecuteNonQuery();
                    if(Check_Result >= 1)
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "1";
                        AsyncManager.OutstandingOperations.Decrement();
                    } else
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-1";
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Cancel_NickCompleted(string Result)
        {
            return Result;
        }

        public void Json_Test(string test)
        {
            try
            {
                var jsondata = new Person();
                jsondata.Name = test;

                JObject json = new JObject();
                json.Add("Name", jsondata.Name);
                json.Add("Name2", jsondata.Name);
                json.Add("Name3", jsondata.Name);

                JArray Ja = new JArray();
                Ja.Add(json);
                Ja.Add(Ja);

                JObject json2 = new JObject();
                json2.Add("Names", Ja);

                Ja.Add(json2);

                string output = Ja.ToString();

                Response.Write(JsonConvert.SerializeObject(Ja));
            } catch (Exception e)
            {
                Response.Write(e.Message);
            }
        }


    }

    public class Person
    {
        public string Name { get; set; }
        public string Defalut_Value { get; set; }
    }
}