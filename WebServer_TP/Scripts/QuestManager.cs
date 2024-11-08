using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace JsWebServer_CP.Scripts
{
    public class QuestManager
    {
        static Random Ran = new Random();

        public JObject Base_Quest(int I_id)
        {
            JObject Quest = new JObject();

            Quest.Add("I_id", I_id);
            Quest.Add("Id", 0);
            Quest.Add("Date", System.DateTime.Now);
            Quest.Add("Cleared_Date", System.DateTime.Now);
            Quest.Add("Refreshed_Date", System.DateTime.Now.AddMinutes(10));
            Quest.Add("Value", "0");

            return Quest;
        }

        public JObject Quest_Base(int I_id)
        {
            JObject Quest = new JObject();

            Quest.Add("I_id", I_id);
            Quest.Add("Id", 0);
            Quest.Add("Date", System.DateTime.Now);
            Quest.Add("Cleared_Date", System.DateTime.Now);
            Quest.Add("Refreshed_Date", System.DateTime.Now.AddMinutes(10));
            Quest.Add("Value", "0");

            return Quest;
        }

        public JObject Quest_Generate(int I_id, string User_Code)
        {
            JObject Quest = new JObject();
            int Quest_Id = 0;

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {

                    MySqlCommand Available_Quest_List = new MySqlCommand();
                    Available_Quest_List.Connection = scon;
                    Available_Quest_List.CommandText = "Get_Available_Quest";
                    Available_Quest_List.CommandType = System.Data.CommandType.StoredProcedure;
                    Available_Quest_List.Parameters.AddWithValue("?User_Code", User_Code);
                    MySqlDataReader Aql = Available_Quest_List.ExecuteReader();
                    if (Aql.Read())
                    {
                        DataTable Table = new DataTable();
                        Table.Load(Aql);

                        int Table_Count = Table.Rows.Count;
                        int Random_Target = Ran.Next(0, Table_Count);
                        DataRow Target_Quest = Table.Select()[Random_Target];
                        Quest_Id = System.Convert.ToInt32(Target_Quest["Id"]);
                    }
                    Aql.Close();
                    scon.Close();
                }
                catch (Exception e)
                {
                     Quest_Id = 1;
                    scon.Close();
                }
            }

            Quest.Add("I_id", I_id);
            Quest.Add("Id", Quest_Id);
            Quest.Add("Date", System.DateTime.Now);
            Quest.Add("Cleared_Date", System.DateTime.Now);
            Quest.Add("Refreshed_Date", System.DateTime.Now.AddMinutes(10));
            Quest.Add("Value", "0");

            return Quest;
        }

        public JObject Quest_Generate2(int I_id, string User_Code, int[] Received_QuestIds)
        {
            JObject Quest = new JObject();
            int Quest_Id = 0;

            using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper"].ConnectionString))
            {
                scon.Open();
                try
                {

                    MySqlCommand Available_Quest_List = new MySqlCommand();
                    Available_Quest_List.Connection = scon;
                    Available_Quest_List.CommandText = "Get_Available_Quest";
                    Available_Quest_List.CommandType = System.Data.CommandType.StoredProcedure;
                    Available_Quest_List.Parameters.AddWithValue("?User_Code", User_Code);
                    MySqlDataReader Aql = Available_Quest_List.ExecuteReader();
                    DataTable Table = new DataTable();

                    if (Aql.Read())
                    {
                        Table.Load(Aql);
                    }

                    while (true)
                    {
                        int Table_Count = Table.Rows.Count;
                        int Random_Target = Ran.Next(0, Table_Count);

                        DataRow Target_Quest = Table.Select()[Random_Target];
                        Quest_Id = System.Convert.ToInt32(Target_Quest["Id"]);

                        if(Received_QuestIds.Contains<int>(Quest_Id))
                        {
                            continue;
                        } else
                        {
                            break;
                        }
                    }

                    Aql.Close();
                    scon.Close();
                }
                catch (Exception e)
                {
                    Quest_Id = 1;
                    scon.Close();
                }
            }

            Quest.Add("I_id", I_id);
            Quest.Add("Id", Quest_Id);
            Quest.Add("Date", System.DateTime.Now);
            Quest.Add("Cleared_Date", System.DateTime.Now);
            Quest.Add("Refreshed_Date", System.DateTime.Now.AddMinutes(10));
            Quest.Add("Value", "0");

            return Quest;
        }

        public JObject WQuest_Base(int I_id)
        {
            JObject Quest = new JObject();


            int Quest_Id = 0;

            Quest.Add("I_id", I_id);
            Quest.Add("Id", Quest_Id);
            Quest.Add("Date", System.DateTime.Now);
            Quest.Add("Cleared_Date", System.DateTime.Now);
            Quest.Add("Value", "0");

            return Quest;
        }

        public JObject WQuest_Generate(int I_id, string User_Code, DataTable Available_Quests)
        {
            JObject Quest = new JObject();
            int Quest_Id = 0;
            int Table_Count = Available_Quests.Rows.Count;
            int Random_Target = Ran.Next(0, Table_Count);
            DataRow Target_Quest = Available_Quests.Select()[Random_Target];
            Quest_Id = System.Convert.ToInt32(Target_Quest["Id"]);
            Quest.Add("I_id", I_id);
            Quest.Add("Id", Quest_Id);
            Quest.Add("Date", System.DateTime.Now);
            Quest.Add("Cleared_Date", System.DateTime.Now);
            Quest.Add("Refreshed_Date", System.DateTime.Now.AddMinutes(10));
            Quest.Add("Value", "0");

            return Quest;
        }
    }
}