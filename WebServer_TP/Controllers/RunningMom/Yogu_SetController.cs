using System;
using System.Web.Mvc;
using JsWebServer_CP.Models;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JsWebServer.Controllers
{
    public class Yogu_SetController : AsyncController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlCommand scom = new SqlCommand();

        public void create_yoguAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            string userid = Request.Form["userid"];
            string item_id = Request.Form["item_id"];
            string m1 = Request.Form["m1"];
            string m2 = Request.Form["m2"];
            string m3 = Request.Form["m3"];
            string m4 = Request.Form["m4"];
            int req_mat = System.Convert.ToInt32(Request.Form["req_mat"]);

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    SqlCommand cmd = new SqlCommand("select count(*) count from dbo.createyogu('" + userid + "'," + item_id + "," + m1 + "," + m2 + "," + m3 + "," + m4 + ");", scon);
                    int i = 0;
                    string id = "";
                    string value = "-1";

                    SqlDataReader rd = cmd.ExecuteReader();
                    if (rd.Read())
                    {
                        i = System.Convert.ToInt32(rd["count"]);
                    }
                    rd.Close();

                    if (req_mat == i) // 테이블의 행 수가 재료 수와 같으면 맞다는 뜻이므로 
                    {
                        SqlCommand cmd2 = new SqlCommand("DECLARE @return_value int exec @return_value = dbo.sub_gold @item_id=" + item_id + ",@User_id=N'" + userid + "',@m1 = " + m1 + ", @m2 = " + m2 + ",@m3 = " + m3 + ", @m4 = " + m4 + ", @id=-1" + " SELECT	'Return Value' = @return_value", scon);
                        SqlDataReader rd2 = cmd2.ExecuteReader();
                        while (rd2.Read())
                        {
                            id = rd2["Return Value"].ToString();
                        }
                        scon.Close();
                        AsyncManager.Parameters["Result"] = item_id + "," + id;
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                    else
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-2";
                        AsyncManager.OutstandingOperations.Decrement();// 재료부족
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

        public string create_yoguCompleted(string Result)
        {
            return Result;
        }


        public void reset_yoguAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            string userid = Request.Form["userid"];
            string item_id = Request.Form["item_id"];
            string id = Request.Form["id"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string value = "0";
                    string Query =
                        ("update user_yogus set create_date = getdate() where id = " + id + " and user_code = '" + userid + "' update user_list set User_cash -= (select Refill_Req from recipe_list where item_id = " + item_id + ") where user_code = '" + userid + "'");
                    SqlCommand cmd = new SqlCommand(Query, scon);
                    int result = cmd.ExecuteNonQuery();
                    scon.Close();
                    if (result == 2)
                    {
                        AsyncManager.Parameters["Result"] = item_id + "," + id;
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                    else
                    {
                        AsyncManager.Parameters["Result"] = "-1";
                        AsyncManager.OutstandingOperations.Decrement();
                    }// 1이나오면 성공 , 0이 나오면 재료나 골드가 부족
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }


        public string reset_yoguCompleted(string Result)
        {
            return Result;
        }

        public void return_yoguAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            string userid = Request.Form["userid"];
            string item_id = Request.Form["item_id"];
            string id = Request.Form["id"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string value = "0";
                    SqlCommand cmd2 = new SqlCommand("DECLARE	@return_value int exec @return_value = dbo.return_yogu @id=" + id + ",@User_id=N'" + userid + "',@item_id=" + item_id + " SELECT	'Return Value' = @return_value", scon);
                    SqlDataReader rd2 = cmd2.ExecuteReader();
                    while (rd2.Read())
                    {
                        value = rd2["Return Value"].ToString();
                    }
                    scon.Close();
                    if (value.Equals("1"))
                    {
                        AsyncManager.Parameters["Result"] = id;
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                    else
                    {
                        AsyncManager.Parameters["Result"] = "-2";
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

        public string return_yoguCompleted(string Result)
        {
            return Result;
        }


        public void ConntestAsync(string test)
        {
            AsyncManager.OutstandingOperations.Increment();
            AsyncManager.Parameters["Result"] = test += "ddd";
            AsyncManager.OutstandingOperations.Decrement();
        }

        public string ConntestCompleted(string Result)
        {
            return Result;
        }



        public string upgrade_yogu(string userid, string item_id, string id)
        {
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            /*string userid = Request.Form["userid"];
            string item_id = Request.Form["item_id"];
            string id = Request.Form["id"];*/

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    Random a = new Random();
                    int Success = a.Next(0, 2);
                    string cmd = "update User_yogus set enc_grade += 1 where id = " + id + " and User_code = N'" + userid + "' update User_list set User_gold -= 3000 where User_code = N'" + userid + "' select enc_grade from User_yogus where id=" + id;
                    string value = "0";

                    if (Success == 1)
                    {
                        SqlCommand Upgrade_Cmd = new SqlCommand(cmd, scon);
                        SqlDataReader Upgrade_Cmd_Result = Upgrade_Cmd.ExecuteReader();

                        if (Upgrade_Cmd_Result.Read())
                        {
                            int enc_grade = System.Convert.ToInt32(Upgrade_Cmd_Result["enc_grade"]);
                            Upgrade_Cmd_Result.Close();
                            scon.Close();
                            return id + ":" + enc_grade.ToString();
                        }
                        else
                        {
                            Upgrade_Cmd_Result.Close();
                            scon.Close();
                            return "-4"; // 
                        }
                    }
                    else // 강화실패
                    {
                        scon.Close();
                        return "0:0";
                    }
                }
                catch (Exception e)
                {
                    scon.Close();
                    return "-1000"; // 알수없는 이유로 인한 실패
                }
            }
        }
    }
}

