using System;
using System.Data;
using System.Web.Mvc;
using JsWebServer_CP.Models;
using System.Data.SqlClient;
using StackExchange.Redis;

namespace JsWebServer.Controllers
{
    public class User_SetController : AsyncController

    {
        private ApplicationDbContext db = new ApplicationDbContext();
        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlCommand scom = new SqlCommand();
        static string conn = "localhost";

        ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(conn);

        IDatabase cache;

        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            return ConnectionMultiplexer.Connect(conn);
        });

        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }

        public void set_stageAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            string satisfygroup = Request.Form["satisfy"];
            string Userid = "@user_id='" + Request.Form["userid"] + "'";
            string theme_id = ",@theme_id=" + Request.Form["theme_id"];
            string Delc = ",@delc='" + Request.Form["delc"] + "'";
            string Score = ",@score='" + Request.Form["score"] + "'";
            string Nickname = Request.Form["nickname"];
            double MaxScore = System.Convert.ToDouble(Request.Form["maxscore"]);
            string stage_id = ",@stage_id='" + Request.Form["stage_id"] + "'";
            string result = Request.Form["result"];
            string[] Satisfys = satisfygroup.Split(',');

            Double[] Stage_Satisfys = new Double[3];

            for (int i = 0; i < Satisfys.Length; i++)
            {
                Stage_Satisfys[i] = System.Convert.ToDouble(Satisfys[i]);
            }

            string Satisfy = ",@satisfy='" + Stage_Satisfys[0] + "," + Stage_Satisfys[1] + "," + Stage_Satisfys[2] + "'";

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string CmdString = "exec dbo.set_stages " + Userid + theme_id + Satisfy + Delc + Score + stage_id;
                    SqlCommand cmd = new SqlCommand(CmdString, scon);
                    int Cmd_Result = cmd.ExecuteNonQuery();

                    if (Cmd_Result >= 3)
                    {
                        scon.Close();
                        connection.Close();
                        AsyncManager.Parameters["Result"] = "1";
                        AsyncManager.OutstandingOperations.Decrement();// 1이나오면 성공 , 0이 나오면 재료나 골드가 부족
                    }
                    else
                    {
                        scon.Close();
                        connection.Close();
                        AsyncManager.Parameters["Result"] = "-1";
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                    //cache = Connection.GetDatabase();
                    //connection.Close();
                }
                catch (Exception e)
                {
                    connection.Close();
                    scon.Close();
                    AsyncManager.Parameters["Result"] = "-4";
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string set_stageCompleted(string Result)
        {
            return Result;
        }


        public void stage_clearAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            int stage_id = System.Convert.ToInt32(Request.Form["stage_number"]);

            string Userid = "@user_id='" + Request.Form["userid"] + "'";
            string Stage_id = ",@stage_id=" + stage_id.ToString();

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string value = "0";
                    SqlCommand cmd = new SqlCommand("DECLARE @return_value int exec @return_value=dbo.stage_clear " + Userid + Stage_id + "select 'Return Value' = @return_value", scon);
                    SqlDataReader rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        value = rd["Return Value"].ToString();
                    }
                    rd.Close();
                    scon.Close(); connection.Close();
                    if (value.Equals("0"))
                    {
                        AsyncManager.Parameters["Result"] = value;
                        AsyncManager.OutstandingOperations.Decrement();// 1이나오면 성공 , 0이 나오면 재료나 골드가 부족
                    }
                    else
                    {
                        AsyncManager.Parameters["Result"] = stage_id.ToString();
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                }
                catch (Exception e)
                {
                    connection.Close();
                    scon.Close();
                    AsyncManager.Parameters["Result"] = "-1";
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string stage_clearCompleted(string Result)
        {
            return Result;
        }

        public void Skill_UpAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string userid = Request.Form["userid"];
            string Sk_id = Request.Form["Sk_id"];

            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    int Skill_id = System.Convert.ToInt32(Sk_id);
                    string Skill_name = "";
                    switch (Skill_id)
                    {
                        case 0:
                            {
                                Skill_name = "Hp_Up";
                                break;
                            }
                        case 1:
                            {
                                Skill_name = "Fever_Up";
                                break;
                            }
                    }
                    string Gold_Check_str = "select User_gold," + Skill_name + " from User_list where User_code = N'" + userid + "' and User_gold >= (" + Skill_name + "+1) * 5000";
                    SqlCommand Gold_Check_cmd = new SqlCommand(Gold_Check_str, scon);
                    SqlDataReader Gold_Cmd_Result = Gold_Check_cmd.ExecuteReader();
                    if (Gold_Cmd_Result.Read())
                    {
                        int Skill_level = System.Convert.ToInt32(Gold_Cmd_Result[Skill_name]);
                        Gold_Cmd_Result.Close();

                        string Skill_Up = "update User_list set " + Skill_name + " += 1, User_gold -= (" + Skill_name + "+1) * 5000 where User_code = N'" + userid + "'";
                        SqlCommand Skill_Up_cmd = new SqlCommand(Skill_Up, scon);
                        int Skill_Up_Result = Skill_Up_cmd.ExecuteNonQuery();
                        if (Skill_Up_Result == 1)
                        {
                            Skill_level++;
                            scon.Close();
                            connection.Close();
                            AsyncManager.Parameters["Result"] = Skill_name + ":" + Skill_level;
                            AsyncManager.OutstandingOperations.Decrement();
                        }
                        else
                        {
                            scon.Close();
                            connection.Close();
                            AsyncManager.Parameters["Result"] = "-24";
                            AsyncManager.OutstandingOperations.Decrement();
                        }
                    }
                    else
                    {
                        scon.Close();
                        connection.Close();
                        AsyncManager.Parameters["Result"] = "-24";
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                    //cache = Connection.GetDatabase();
                    //connection.Close();
                }
                catch (Exception e)
                {
                    connection.Close();
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }


        public string Skill_UpCompleted(string Result)
        {
            return Result;
        }

        public void Ad_RewardAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            string userid = Request.Form["userid"];

            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string cmd = "Ads_Reward '" + userid + "'";
                    string Result = "";
                    SqlCommand Ads_Reward_cmd = new SqlCommand(cmd, scon);
                    SqlDataReader Ads_Reward_Result = Ads_Reward_cmd.ExecuteReader();

                    if (Ads_Reward_Result.Read())
                    {
                        Result = Ads_Reward_Result["User_cash"].ToString();
                    }

                    Ads_Reward_Result.Close();

                    scon.Close();
                    connection.Close();
                    AsyncManager.Parameters["Result"] = Result;
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    connection.Close();
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Ad_RewardCompleted(string Result)
        {
            return Result;
        }


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


    }
}
