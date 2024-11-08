using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;

namespace JsWebServer_CP.Controllers
{
    public class SkillSetController : AsyncController
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

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    SqlCommand Exp_Check = new SqlCommand("exec Skill_Level_Up " + skill_id + "," + char_id, scon);
                    SqlDataReader Exp_Check_Result = Exp_Check.ExecuteReader();
                    string[] Return_Data = new string[4];
                    System.Text.StringBuilder Sb = new System.Text.StringBuilder();

                    if (Exp_Check_Result.Read())
                    {
                        Return_Data[0] = Exp_Check_Result["CurExp"].ToString();
                        Return_Data[1] = Exp_Check_Result["S_lev"].ToString();
                        Return_Data[2] = Exp_Check_Result["S_id"].ToString();
                        Return_Data[3] = Exp_Check_Result["gold"].ToString();
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

                    Exp_Check_Result.Close();
                    scon.Close();
                    AsyncManager.Parameters["Result"] = Sb.ToString();
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

                    if (Exp_Check_Result.Read())
                    {
                        Sb.Append(Exp_Check_Result["CurExp"].ToString() + ":");
                        Sb.Append(Exp_Check_Result["S_lev"].ToString() + ":");
                        Sb.Append(Exp_Check_Result["S_id"].ToString() + ":");
                        Sb.Append(Exp_Check_Result["R0"].ToString() + ":");
                        Sb.Append(Exp_Check_Result["R1"].ToString() + ":");
                        Sb.Append(Exp_Check_Result["R2"].ToString() + ":");
                        Sb.Append(Exp_Check_Result["R3"].ToString() + ":");
                        Sb.Append(Exp_Check_Result["gold"].ToString());
                    }
                    else
                    {
                        Sb.Append("0");
                    }

                    Exp_Check_Result.Close();
                    scon.Close();
                    AsyncManager.Parameters["Result"] = Sb.ToString();
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

        public string Rune_UpCompleted(string Result)
        {
            return Result;
        }
    }
}