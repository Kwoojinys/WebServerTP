using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using JsWebServer_CP.Models;
using System.Data.SqlClient;
using System.Globalization;
using System.Xml;
using System.Threading;
using System.Threading.Tasks;

namespace JsWebServer.Controllers
{
    public class Employee_SetController : AsyncController
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlCommand scom = new SqlCommand();
        public void GoAsync()
        {
            AsyncManager.OutstandingOperations.Increment();

            string user_id = Request.Form["userid"];
            string Em_id = Request.Form["Em_id"];
            string stage_id = Request.Form["stage_id"];

            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string path = Server.MapPath("~/DataXml/stage_list.xml");
                    XmlDocument ItemData = new XmlDocument();
                    ItemData.Load(path);
                    XmlNodeList StageNodes = ItemData.DocumentElement.SelectNodes("stage_list");
                    int stage_code = System.Convert.ToInt32(stage_id) - 1;
                    string Job_time = StageNodes[stage_code].Attributes["Req_time"].Value;
                    string finish_check = "select * from User_Employee where User_code = N'" + user_id + "' and Dateadd(minute,Em_Jobtime,Em_starttime) < getdate() and Em_id = " + Em_id;

                    SqlCommand Job_Check = new SqlCommand(finish_check, scon);
                    SqlDataReader Job_Check_rd = Job_Check.ExecuteReader();
                    if (Job_Check_rd.Read())
                    {
                        Job_Check_rd.Close();
                        string Jobgo = "update User_Employee set Em_stageid = " + stage_id + ", Em_starttime = getdate(), Em_Jobtime = " + Job_time + " where User_code = N'" + user_id + "' and Em_id = " + Em_id;
                        SqlCommand Jobgo_rd = new SqlCommand(Jobgo, scon);
                        int value = Jobgo_rd.ExecuteNonQuery();
                        if (value >= 1)
                        {
                            Jobgo_rd.Clone();
                            scon.Close();
                            AsyncManager.Parameters["Result"] = Em_id + ":" + Job_time;
                            AsyncManager.OutstandingOperations.Decrement();
                        }
                        else
                        {
                            Jobgo_rd.Clone();
                            scon.Close();
                            AsyncManager.Parameters["Result"] = "-21";
                            AsyncManager.OutstandingOperations.Decrement();
                        }

                    }
                    else
                    {
                        Job_Check_rd.Close();
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-21";
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

        public string GoCompleted(string Result)
        {
            return Result;
        }

        public void ReturnAsync()
        {
            AsyncManager.OutstandingOperations.Increment();

            string user_id = Request.Form["userid"];
            string Em_id = Request.Form["Em_id"];
            string stage_id = Request.Form["stage_id"];

            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    float dummy = System.Convert.ToInt32(stage_id);
                    int Theme_id = 0; // 테마 번호

                    if (dummy / 3 > (int)dummy / 3)
                        Theme_id = (int)dummy / 3 + 1;
                    else
                        Theme_id = (int)dummy / 3;

                    string finish_check = "select * from User_Employee where User_code = N'" + user_id + "' and Em_Jobtime != 0 and Dateadd(minute,Em_Jobtime,Em_starttime) <= Dateadd(minute,1,getdate()) and Em_id = " + Em_id;

                    SqlCommand Job_Check = new SqlCommand(finish_check, scon);
                    SqlDataReader Job_Check_rd = Job_Check.ExecuteReader();
                    if (Job_Check_rd.Read())
                    {
                        Job_Check_rd.Close();
                        string path = Server.MapPath("~/DataXml/Material_list_Theme" + Theme_id + ".xml");
                        XmlDocument ItemData = new XmlDocument();
                        ItemData.Load(path);
                        XmlNodeList Materials = ItemData.DocumentElement.SelectNodes("material_list");
                        Dictionary<string, string> Material_Gets = new Dictionary<string, string>();
                        Random rnd = new Random();
                        foreach (XmlElement mate_id in Materials)
                        {
                            Material_Gets.Add(mate_id.GetAttribute("material_id"), rnd.Next(1, 4).ToString());
                        }
                        List<int> Got_Materials = new List<int>();
                        string Material_Add_Cmd = "";
                        string resultstring = Em_id + "~" + stage_id + "=";
                        int i = 0;
                        while (Got_Materials.Count < 2)
                        {
                            int id = rnd.Next(0, (Material_Gets.Count));
                            if (Got_Materials.Contains(id))
                            {
                                continue;
                            }
                            else
                            {
                                Material_Add_Cmd += "update User_Employee set Em_Jobtime = 0, Em_starttime = getdate(), Em_stageid = 0 where User_code = N'" + user_id + "' and Em_id = " + Em_id + " exec dbo.Modify_Mat @user_id = N'" + user_id + "',@mat_id = " + Material_Gets.Keys.ElementAt(id) + ",@amount = " + Material_Gets.Values.ElementAt(id) + " ";
                                resultstring += Material_Gets.Keys.ElementAt(id) + ":" + Material_Gets.Values.ElementAt(id);
                                Got_Materials.Add(id);
                                i++;
                                if (i != 2)
                                {
                                    resultstring += ",";
                                }
                            }
                        }
                        SqlCommand Material_Add = new SqlCommand(Material_Add_Cmd, scon);
                        int result = Material_Add.ExecuteNonQuery();
                        if (result >= 1)
                        {
                            scon.Close();
                            AsyncManager.Parameters["Result"] = resultstring;
                            AsyncManager.OutstandingOperations.Decrement();
                            return;
                        }
                    }
                    else
                    {
                        Job_Check_rd.Close();
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-23";
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

        public string ReturnCompleted(string Result)
        {
            return Result;
        }


        public void HireAsync()
        {
            AsyncManager.OutstandingOperations.Increment();

            string user_id = Request.Form["userid"];
            string Em_id = Request.Form["Em_id"];
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string path = Server.MapPath("~/DataXml/Employee_list.xml");
                    XmlDocument ItemData = new XmlDocument();
                    ItemData.Load(path);
                    XmlNodeList Employee_lists = ItemData.DocumentElement.SelectNodes("Employee_list");
                    int Hire_Target = System.Convert.ToInt32(Em_id);
                    string Req_gold = Employee_lists[Hire_Target].Attributes["req_gold"].Value;

                    string Check_Gold = "select User_gold from User_list where User_gold >= " + Req_gold + " and User_code = N'" + user_id + "'";
                    SqlCommand Check_Gold_Cmd = new SqlCommand(Check_Gold, scon);
                    SqlDataReader Check_Gold_Rd = Check_Gold_Cmd.ExecuteReader();
                    if (Check_Gold_Rd.Read())
                    {
                        Check_Gold_Rd.Close();
                        string cmds = "insert into User_Employee values(N'" + user_id + "'," + Em_id + ",0,getdate(),1,0) update User_list set User_gold -= " + Req_gold + " where User_code = N'" + user_id + "'";
                        SqlCommand cmd = new SqlCommand(cmds, scon);
                        int value = cmd.ExecuteNonQuery();
                        if (value == 2)
                        {
                            scon.Close();
                            AsyncManager.Parameters["Result"] = Hire_Target.ToString();
                            AsyncManager.OutstandingOperations.Decrement();
                        }
                        else
                        {
                            scon.Close();
                            AsyncManager.Parameters["Result"] = "-22";
                            AsyncManager.OutstandingOperations.Decrement();
                        }
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

        public string HireCompleted(string Result)
        {
            return Result;
        }


    }
}