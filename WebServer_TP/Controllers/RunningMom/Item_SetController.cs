using System;
using System.Web.Mvc;
using JsWebServer_CP.Models;
using System.Data.SqlClient;
using System.Xml;


namespace JsWebServer.Controllers
{
    public class Item_SetController : AsyncController
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlCommand scom = new SqlCommand();

        public void ToolAsync()
        {
            AsyncManager.OutstandingOperations.Increment();

            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            string userid = Request.Form["userid"];
            int tool_id = System.Convert.ToInt32(Request.Form["tool_id"]);
            int gold_req = System.Convert.ToInt32(Request.Form["gold_req"]);

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string value = "0";

                    SqlCommand cmd = new SqlCommand("DECLARE @return_value int exec @return_value=dbo.buy_tool @user_id ='" + userid + "', @gold_req=" + gold_req + ", @id=" + tool_id + "select 'Return Value' = @return_value", scon);
                    SqlDataReader rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        value = rd["Return Value"].ToString();
                    }
                    rd.Close();
                    scon.Close();
                    AsyncManager.Parameters["Result"] = tool_id + "," + value;
                    AsyncManager.OutstandingOperations.Decrement();
                    // 1이나오면 성공 , 0이 나오면 재료나 골드가 부족
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string ToolCompleted(string Result)
        {
            return Result;
        }

        public void TankAsync()
        {
            AsyncManager.OutstandingOperations.Increment();

            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            string userid = Request.Form["userid"];
            int gold_req = System.Convert.ToInt32(Request.Form["gold_req"]);

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string value = "0";
                    SqlCommand cmd = new SqlCommand("DECLARE @return_value int exec @return_value=dbo.buy_tank @user_id ='" + userid + "', @gold_req=" + gold_req + "select 'Return Value' = @return_value", scon);
                    SqlDataReader rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        value = rd["Return Value"].ToString();
                    }
                    rd.Close();
                    scon.Close();
                    AsyncManager.Parameters["Result"] = value;
                    AsyncManager.OutstandingOperations.Decrement(); // 1이나오면 성공 , 0이 나오면 재료나 골드가 부족

                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string TankCompleted(string Result)
        {
            return Result;
        }

        public void RewardAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            string userid = Request.Form["userid"];
            string stage_id = Request.Form["stage_id"];
            string Theme_id = stage_id.Substring(0, 1);
            int Stage_number = System.Convert.ToInt32(stage_id.Substring(2, 1));

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string RewardState = "";
                    string SatisfyState = "";
                    SqlCommand Stage_State = new SqlCommand("select * from User_Stage_State where Theme_id = " + Theme_id + " and User_id = '" + userid + "';", scon);
                    SqlDataReader Stageinfo = Stage_State.ExecuteReader();
                    while (Stageinfo.Read())
                    {
                        RewardState = Stageinfo["Reward"].ToString();
                        SatisfyState = Stageinfo["Satisfy"].ToString();
                    }

                    string[] Rewards = RewardState.Split(','); // 현재 보상 상태
                    string[] Satisfys = SatisfyState.Split(',');

                    int Target_Satisfys = System.Convert.ToInt32(Satisfys[Stage_number - 1]);

                    int Target_Rewards = System.Convert.ToInt32(Rewards[Stage_number - 1]);

                    if (Target_Rewards == 3)
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-6";
                        AsyncManager.OutstandingOperations.Decrement();
                    }

                    switch (Target_Rewards)
                    {
                        case 0:
                            if (Target_Satisfys >= 50)
                            {
                                Rewards[Stage_number - 1] = (Target_Rewards + 1).ToString();
                            }
                            else
                            {
                                scon.Close();
                                AsyncManager.Parameters["Result"] = "-7";
                                AsyncManager.OutstandingOperations.Decrement();
                            }
                            break;
                        case 1:
                            if (Target_Satisfys >= 70)
                            {
                                Rewards[Stage_number - 1] = (Target_Rewards + 1).ToString();
                            }
                            else
                            {
                                scon.Close();
                                AsyncManager.Parameters["Result"] = "-7";
                                AsyncManager.OutstandingOperations.Decrement();
                            }
                            break;
                        case 2:
                            if (Target_Satisfys >= 100)
                            {
                                Rewards[Stage_number - 1] = (Target_Rewards + 1).ToString();
                            }
                            else
                            {
                                scon.Close();
                                AsyncManager.Parameters["Result"] = "-7";
                                AsyncManager.OutstandingOperations.Decrement();
                            }
                            break;
                    }

                    RewardState = Rewards[0] + "," + Rewards[1] + "," + Rewards[2];
                    Stageinfo.Close();

                    string path = Server.MapPath("~/DataXml/stage_list.xml");
                    XmlDocument ItemData = new XmlDocument();
                    ItemData.Load(path);

                    XmlElement ItemListElement = ItemData["stage_list"];

                    string Current_Stage_Reward_Gold = "0";

                    foreach (XmlElement ItemElement in ItemListElement.GetElementsByTagName("stage_list"))
                    {
                        if (ItemElement.GetAttribute("Stage_id").Equals(stage_id))
                        {
                            string[] Reward_Golds = ItemElement.GetAttribute("Reward_Gold").Split(',');
                            Current_Stage_Reward_Gold = Reward_Golds[Target_Rewards];
                        }
                    }

                    string cmd = "update User_Stage_State set Reward = '" + RewardState + "' where User_id = '" + userid + "' and Theme_id=" + Theme_id;

                    if (!Current_Stage_Reward_Gold.Equals("0"))
                    {
                        cmd += " update User_list set User_gold += " + Current_Stage_Reward_Gold + " where User_code = '" + userid + "'";
                    }

                    SqlCommand Reward_Update = new SqlCommand(cmd, scon);
                    int Update_Result = Reward_Update.ExecuteNonQuery();
                    if (Update_Result == 2)
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = Current_Stage_Reward_Gold;
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

        public string RewardCompleted(string Result)
        {
            return Result;
        }

        public void RecipeAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            string userid = Request.Form["userid"];
            string item_id = Request.Form["item_id"];
            string gold_req = Request.Form["gold_req"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string value = "0";
                    SqlCommand cmd2 = new SqlCommand("DECLARE	@return_value int exec @return_value = dbo.buy_recipe @user_id='" + userid + "',@gold_req=" + gold_req + ",@recipe_id=" + item_id + "  SELECT	'Return Value' = @return_value", scon);
                    SqlDataReader rd2 = cmd2.ExecuteReader();
                    while (rd2.Read())
                    {
                        value = rd2["Return Value"].ToString();
                    }
                    rd2.Close();
                    scon.Close();

                    if (value.Equals("0"))
                    {
                        AsyncManager.Parameters["Result"] = value;
                        AsyncManager.OutstandingOperations.Decrement();// 1이나오면 성공 , 0이 나오면 오류
                    }
                    else
                    {
                        AsyncManager.Parameters["Result"] = item_id;
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                }
                catch (Exception e)
                {
                    scon.Close();
                    AsyncManager.Parameters["Result"] = e.Message;
                    AsyncManager.OutstandingOperations.Decrement(); // 알수없는 이유로 인한 실패
                }
            }
        }

        public string RecipeCompleted(string Result)
        {
            return Result;
        }

        public void MaterialAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            string userid = Request.Form["userid"];
            string mat_id = Request.Form["mat_id"];
            string amount = Request.Form["amount"];
            string[] mats = mat_id.Split(',');
            string[] amounts = amount.Split(',');
            string cmd = "DECLARE @return_value int ";

            if (mats.Length > 0)
            {//"DECLARE	@return_value int exec @return_value = dbo.return_yogu @id=" + id + ",@User_id=N'" + userid + "',@item_id=" + item_id + " SELECT	'Return Value' = @return_value"
                for (int i = 0; i < mats.Length; i++)
                {
                    if (!mats[i].Equals(""))
                    {
                        cmd += "exec @return_value = dbo.Modify_Mat @user_id = '" + userid + "',@mat_id = " + mats[i] + ",@amount = " + amounts[i] + "  SELECT 'Return Value' = @return_value ";
                    }
                }
            }

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    SqlCommand Mate_Add = new SqlCommand(cmd, scon);
                    int Mate_rd = Mate_Add.ExecuteNonQuery();
                    if (Mate_rd == mats.Length)
                    {
                        scon.Close();
                        AsyncManager.Parameters["Result"] = mat_id + ":" + amount;
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                    else
                    {
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

        public string MaterialCompleted(string Result)
        {
            return Result;
        }

        public string cheating()
        {
            string userid = Request.Form["userid"];
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];
            string cmd = "update User_list set User_gold = 9999999 where User_code = N'" + userid + "' update User_list set User_cash = 9999999 where User_code = N'" + userid + "' update User_Stage_State set Satisfy = '100,100,100' where User_id = N'" + userid + "'";
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    SqlCommand Cheat = new SqlCommand(cmd, scon);
                    int Cheats = Cheat.ExecuteNonQuery();
                    if (Cheats == 6)
                    {
                        scon.Close();
                        return "1";
                    }
                    else
                    {
                        scon.Close();
                        return "-1";
                    }
                }
                catch (Exception e)
                {
                    scon.Close();
                    return e.ToString();
                }
            }
        }

        public string data_reset()
        {
            string userid = Request.Form["userid"];
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];
            string cmd = "DECLARE @return_value int exec @return_value = dbo.data_reset @user_id = N'" + userid + "'";
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    SqlCommand Cheat = new SqlCommand(cmd, scon);
                    int Cheats = Cheat.ExecuteNonQuery();
                    if (Cheats >= 1)
                    {
                        scon.Close();
                        return "1";
                    }
                    else
                    {
                        scon.Close();
                        return "-1";
                    }
                }
                catch (Exception e)
                {
                    scon.Close();
                    return e.ToString();
                }
            }
        }
    }
}
