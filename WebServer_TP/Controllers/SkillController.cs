using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace JsWebServer_CP.Controllers
{
    public class SkillController : AsyncController
    {
        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlCommand scom = new SqlCommand();

        // GET: SkillSet
        public void Skill_UpAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            string char_id = Request.Form["char_id"];
            string skill_id = Request.Form["skill_id"];
            JObject Results = new JObject();
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    SqlCommand Exp_Check = new SqlCommand("exec Skill_Level_Up " + skill_id + "," + char_id, scon);
                    SqlDataReader Exp_Check_Result = Exp_Check.ExecuteReader();

                    if (Exp_Check_Result.Read())
                    {
                        Results.Add("CurExp", Exp_Check_Result["CurExp"].ToString());
                        Results.Add("S_Lev", Exp_Check_Result["S_lev"].ToString());
                        Results.Add("S_Id", Exp_Check_Result["S_id"].ToString());
                        Results.Add("Gold", Exp_Check_Result["gold"].ToString());
                        Exp_Check_Result.Close();
                        scon.Close();
                        AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                    else
                    {
                        Exp_Check_Result.Close();
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-1";
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = "-1";
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Skill_UpCompleted(string Result)
        {
            return Result;
        }

        public void Rune_UpAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            string char_id = Request.Form["char_id"];
            string skill_id = Request.Form["skill_id"];
            string Rune_id = Request.Form["Rune_id"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    SqlCommand Exp_Check = new SqlCommand("exec Rune_Level_Up " + skill_id + "," + char_id + "," + Rune_id, scon);
                    SqlDataReader Exp_Check_Result = Exp_Check.ExecuteReader();
                    string[] Return_Data = new string[3];
                    System.Text.StringBuilder Sb = new System.Text.StringBuilder();
                    JObject Results = new JObject();

                    if (Exp_Check_Result.Read())
                    {
                       Results.Add("CurExp", Exp_Check_Result["CurExp"].ToString());
                        Results.Add("S_Lev", Exp_Check_Result["S_lev"].ToString());
                        Results.Add("S_Id", Exp_Check_Result["S_id"].ToString());
                        Results.Add("Rune_0", Exp_Check_Result["R0"].ToString());
                        Results.Add("Rune_1", Exp_Check_Result["R1"].ToString());
                        Results.Add("Rune_2", Exp_Check_Result["R2"].ToString());
                        Results.Add("Rune_3", Exp_Check_Result["R3"].ToString());
                        Results.Add("Gold", Exp_Check_Result["gold"].ToString());
                        Exp_Check_Result.Close();
                        scon.Close();
                        AsyncManager.Parameters["Result"] = JsonConvert.SerializeObject(Results);
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                    else
                    {
                        Exp_Check_Result.Close();
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-1";
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = "-1";
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Rune_UpCompleted(string Result)
        {
            return Result;
        }
    }
}