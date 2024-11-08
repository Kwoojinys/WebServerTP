using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;
using JsWebServer_CP.GlobalData;
using System.Globalization;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsWebServer_CP.Controllers
{
    public class ItemSetController : AsyncController
    {
        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlCommand scom = new SqlCommand();
        Item_Option_Data iod = new Item_Option_Data();
        Shop_Data Sd = new Shop_Data();

        public void BuyAsync() // 아이템 구매
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            string char_id = Request.Form["char_id"];
            string user_id = Request.Form["user_id"];
            int number = System.Convert.ToInt32(Request.Form["number"]);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            System.Text.StringBuilder Result = new System.Text.StringBuilder();
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string cmds = "";
                    cmds = "exec [Check_User_Shop] " + char_id;
                    SqlCommand Shop_Status = new SqlCommand(cmds, scon);
                    SqlDataReader Shop_Status_Rd = Shop_Status.ExecuteReader();
                    string[] Shop_Data = new string[6];
                    string[] Shop_Boughts = new string[6];

                    if(Shop_Status_Rd.Read())
                    {
                        Shop_Data = Shop_Status_Rd["Shop_Goods"].ToString().Split(',');
                        Shop_Boughts = Shop_Status_Rd["S_Bought"].ToString().Split(',');
                    }

                    if (Shop_Boughts[number].Equals("1"))
                    {
                        Shop_Boughts[number] = "0";
                    } else
                    {
                        Shop_Status_Rd.Close();
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "asdasdas";
                        AsyncManager.OutstandingOperations.Decrement();
                        return;
                    }

                    Shop_Status_Rd.Close();
                    sb.Clear();

                    cmds = "exec [Base_Item_Info] " + char_id;
                    SqlCommand Base_Item_Info = new SqlCommand(cmds, scon);
                    SqlDataReader Base_Item_Info_Rd = Base_Item_Info.ExecuteReader();

                    int Item_Type = 0;
                    int Base_Grade = 0;
                    if (Base_Item_Info_Rd.Read())
                    {
                        Item_Type = System.Convert.ToInt32(Base_Item_Info_Rd["Item_Type"].ToString());
                        Base_Grade = System.Convert.ToInt32(Base_Item_Info_Rd["Base_Grade"].ToString());
                    }

                    Base_Item_Info_Rd.Close();

                    string[] Options_Data = { "0", "0" };

                    if (Base_Grade >= 1)
                    {
                        Dictionary<string, string> Option_info = iod.Option_Generate(Item_Type, Base_Grade);
                        Options_Data = Get_Option_String(Option_info);
                    }

                    string Update_Status = "";
                    for(int i = 0; i < Shop_Boughts.Length;i++)
                    {
                        Update_Status += Shop_Boughts[i];
                        if (i != (Shop_Boughts.Length - 1))
                            Update_Status += ",";
                    }

                    cmds = "exec [Check_Buy] '" + user_id + "'," + char_id + "," + Shop_Data[number] + ",1,'" + Update_Status +"','" + Options_Data[0] + "','" + Options_Data[1] + "'";

                    SqlCommand Buy_Item = new SqlCommand(cmds, scon);
                    SqlDataReader Buy_Item_Rd = Buy_Item.ExecuteReader();
                    sb.Clear();

                    if (Buy_Item_Rd.Read())
                    {
                        Result.Append(Buy_Item_Rd["Item_Id"].ToString());
                        Result.Append("|");
                        Result.Append(Buy_Item_Rd["Item_info"].ToString());
                        Result.Append("|");
                        Result.Append(Buy_Item_Rd["Item_Count"].ToString());
                        Result.Append("|");
                        Result.Append(Buy_Item_Rd["Rarity"].ToString());
                        Result.Append("|");
                        Result.Append(Buy_Item_Rd["Option_id"].ToString());
                        Result.Append("|");
                        Result.Append(Buy_Item_Rd["Enc_Grade"].ToString());
                        Result.Append("|");
                        Result.Append(Buy_Item_Rd["identifyed"].ToString());
                        Result.Append("|");
                        Result.Append(Buy_Item_Rd["Options"].ToString());
                        Result.Append("|");
                        Result.Append(Buy_Item_Rd["Option_Value"].ToString());
                        Result.Append("|");
                    } else
                    {
                        AsyncManager.Parameters["Result"] = "-4";
                        Buy_Item_Rd.Close();
                        scon.Close();
                        AsyncManager.OutstandingOperations.Decrement();
                        return;
                    }

                    Buy_Item_Rd.Close();
                    cmds = "exec [Get_Char_Resource] '" + user_id + "'," + char_id;

                    SqlCommand User_Info = new SqlCommand(cmds, scon);
                    SqlDataReader User_Info_Rd = User_Info.ExecuteReader();
                    sb.Clear();

                    if(User_Info_Rd.Read())
                    {
                        Result.Append(User_Info_Rd["gold"].ToString());
                        Result.Append("|");
                        Result.Append(User_Info_Rd["cash"].ToString());
                        Result.Append("|");
                        Result.Append(User_Info_Rd["Shop_Goods"].ToString());
                        Result.Append("|");
                        Result.Append(User_Info_Rd["S_Bought"].ToString());
                    }

                    AsyncManager.Parameters["Result"] = Result.ToString();
                    User_Info_Rd.Close();
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

        public void Item_Option_Test()
        {

        }

        public string BuyCompleted(string Result)
        {
            return Result;
        }

        public void DeleteAsync() // 아이템 삭제
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            string char_id = Request.Form["char_id"];
            string item_id = Request.Form["item_id"];
            //string item_count = Request.Form["item_count"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    int Server_item_id = Convert.ToInt32(item_id);
                    if (Server_item_id < 0)
                    {
                        AsyncManager.Parameters["Result"] = ErrorCode.Error_On_Connect;
                        AsyncManager.OutstandingOperations.Decrement();
                    }

                    SqlCommand Item_Delete;
                    Item_Delete = new SqlCommand("delete from User_Inven where Item_Id = " + item_id + " and Char_id = " + char_id, scon);

                    int Delete_Result = Item_Delete.ExecuteNonQuery();

                    if (Delete_Result == 1)
                    {
                        AsyncManager.Parameters["Result"] = item_id;
                    }
                    else
                    {
                        AsyncManager.Parameters["Result"] = ErrorCode.Error_On_Connect;
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

        public string DeleteCompleted(string Result) // 아이템 삭제
        {
            return Result;
        }

        public void Re_Identify()
        {
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];

            string User_code = Request.Form["User_code"];
            string char_id = Request.Form["char_id"];
            string Server_item_id = Request.Form["Server_item_id"];
            string Target_Slot_Number = Request.Form["Target_Slot_Number"];
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    int Targeted_Slot = System.Convert.ToInt32(Target_Slot_Number);
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    string cmd = "";
                    cmd = "exec Get_Item " + Server_item_id;

                    string Option_Value = "";
                    string Option_List = "";
                    string Option_id = "";
                    int Re_Identifyed = 0;
                    string Slot_Number = "0";
                    string Item_info = "";
                    int Item_Type = 0;
                    int Rarity = 0;
                    bool Option_Added = false;
                    SqlCommand Get_Item_info = new SqlCommand(cmd, scon);
                    SqlDataReader rd = Get_Item_info.ExecuteReader();
                    Dictionary<string, string> Converted_Option = null;

                    if (rd.Read())
                    {
                        Option_id = rd["Option_id"].ToString();
                        Item_info = rd["Item_info"].ToString();
                        Option_List = rd["Options"].ToString();
                        Option_Value = rd["Options_value"].ToString();
                        Re_Identifyed = System.Convert.ToInt32(rd["Re_Identifyed"]);
                        Option_Added = System.Convert.ToBoolean(rd["Option_Added"]);
                        Slot_Number = rd["Slot_Number"].ToString();
                        Item_Type = System.Convert.ToInt32(rd["Item_Type"].ToString());
                        Rarity = System.Convert.ToInt32(rd["Rarity"].ToString());
                    }

                    rd.Close();
                    double dia = Math.Pow(2, Re_Identifyed) * 10;

                    if (Re_Identifyed >= 7)
                    {
                        dia = 640;
                    }

                    if (Re_Identifyed >= 1)
                    {
                        if (Slot_Number.Equals(Target_Slot_Number))
                            Converted_Option = iod.Convert_Option(iod.String_To_Option(Option_List, Option_Value), Item_Type, Rarity, Targeted_Slot, Option_Added);
                        else
                        {
                            Response.Write(ErrorCode.Error_On_Connect);
                            return;
                        }
                    }
                    else
                        Converted_Option = iod.Convert_Option(iod.String_To_Option(Option_List, Option_Value), Item_Type, Rarity, Targeted_Slot, Option_Added);

                    string[] Options_Data = Get_Option_String(Converted_Option);

                    sb.Append("exec Item_Re_Identify '");
                    sb.Append(User_code + "',");
                    sb.Append(Option_id + ",'");
                    sb.Append(Options_Data[0] + "','");
                    sb.Append(Options_Data[1] + "',");
                    sb.Append(Target_Slot_Number + ",");
                    sb.Append(dia.ToString());

                    SqlCommand Re_IdentifyCmd = new SqlCommand(sb.ToString(), scon);
                    SqlDataReader Re_IdentifyRd = Re_IdentifyCmd.ExecuteReader();
                    if (Re_IdentifyRd.Read())
                    {
                        Response.Write(Server_item_id + "|" + Re_IdentifyRd["Cash"] + "|" + Re_IdentifyRd["Options"] + "|" + Re_IdentifyRd["Options_value"] + "|" + Re_IdentifyRd["Re_Identifyed"] + "|" + Re_IdentifyRd["Slot_Number"]);
                    }
                    else
                    {
                        Response.Write(ErrorCode.Error_On_Connect);
                    }

                    Re_IdentifyRd.Close();
                    scon.Close();
                }
                catch (Exception e)
                {
                    scon.Close();
                    Response.Write(e.Message);
                }
            }
        }

        public void Identify()
        {
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];

            string char_id = Request.Form["char_id"];
            string Server_item_id = Request.Form["Server_item_id"];
            JObject Results = new JObject();
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string cmd = "exec Get_Item " + Server_item_id;
                    int Item_Grade = 0;
                    int Item_info = 0;
                    bool Item_Identified = false;
                    int type = 0;

                    SqlCommand Get_Item_info = new SqlCommand(cmd, scon);
                    SqlDataReader rd = Get_Item_info.ExecuteReader();
                    if (rd.Read())
                    {
                        Item_info = System.Convert.ToInt32(rd["Item_info"].ToString());
                        Item_Grade = System.Convert.ToInt32(rd["Rarity"].ToString());
                        Item_Identified = Convert.ToBoolean(rd["identifyed"]);
                        type = Convert.ToInt32(rd["Item_Type"]);
                    }
                    rd.Close();

                    if (Item_Identified)
                    {
                        Response.Write(ErrorCode.Error_On_Connect);
                        return;
                    }

                    Dictionary<string, string> Option_info = iod.Option_Generate(type, Item_Grade);
                    string[] Options_Data = Get_Option_String(Option_info);
                    cmd = "exec Item_Identify " + char_id + "," + Server_item_id + "," + Item_Grade + ",'" + Options_Data[0] + "','" + Options_Data[1] + "'";

                    SqlCommand Option_Create = new SqlCommand(cmd, scon);
                    SqlDataReader Option_Create_Rd = Option_Create.ExecuteReader();
                    if (Option_Create_Rd.Read())
                    {
                        Response.Write(Server_item_id + "|" + Option_Create_Rd["Gold"] + "|" + Option_Create_Rd["Options"] + "|" + Option_Create_Rd["Options_value"] + "|" + Option_Create_Rd["Re_Identifyed"] + "|" + Option_Create_Rd["Slot_Number"]);
                    }
                    else
                    {
                        Response.Write(ErrorCode.Error_On_Connect);
                    }

                    Option_Create_Rd.Close();
                    scon.Close();
                }
                catch (Exception e)
                {
                    scon.Close();
                    Response.Write(e.Message + e.ToString());
                    return;
                }
            }
        }

        public void Option_Test(int Grade)
        {
            Dictionary<string, string> Option_info = iod.Option_Generate(6, Grade);
            string[] Options_Data = Get_Option_String(Option_info);

            Response.Write(Options_Data[0] + ":" + Options_Data[1]);

        }

        public void Add_Option()
        {
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];

            string User_code = Request.Form["User_code"];
            string Server_item_id = Request.Form["Server_item_id"];
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string cmd = "exec Get_Item " + Server_item_id;
                    int Item_Grade = 0;
                    string Item_info = "";
                    bool Item_Identified = false;
                    int type = 0;
                    string Option_id = "";
                    string Options = "";
                    string Optionv = "";
                    int dia = 500;
                    bool Option_Added = false;

                    SqlCommand Get_Item_info = new SqlCommand(cmd, scon);
                    SqlDataReader rd = Get_Item_info.ExecuteReader();
                    if (rd.Read())
                    {
                        Item_info = rd["Item_info"].ToString();
                        Item_Grade = System.Convert.ToInt32(rd["Rarity"].ToString());
                        Item_Identified = Convert.ToBoolean(rd["identifyed"]);
                        type = Convert.ToInt32(rd["Item_Type"]);
                        Option_id = rd["Option_id"].ToString();
                        Options = rd["Options"].ToString();
                        Optionv = rd["Options_value"].ToString();
                        Option_Added = Convert.ToBoolean(rd["Option_Added"].ToString());
                    }
                    rd.Close();

                    if (Item_Grade != 0 && !Item_Identified || Option_Added)
                    {
                        scon.Close();
                        Response.Write("ITEM_IDENTIFIED : " + Item_Identified + " | OPTION_ADDED : " + Option_Added);
                        return;
                    }

                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    Dictionary<string, string> Option_info = iod.Add_Option(iod.String_To_Option(Options, Optionv), type, Item_Grade);

                    string[] Options_Data = Get_Option_String(Option_info);

                    if (Option_id.Equals("0"))
                    {
                        sb.Append("exec Item_Add_Options ");
                        sb.Append(User_code + ",");
                        sb.Append(Server_item_id + ",");
                    }
                    else
                    {
                        sb.Append("exec Item_Add_Option ");
                        sb.Append(User_code + ",");
                    }
                    sb.Append(Option_id + ",'");
                    sb.Append(Options_Data[0] + "','");
                    sb.Append(Options_Data[1] + "',");
                    sb.Append(dia.ToString());

                    SqlCommand Re_IdentifyCmd = new SqlCommand(sb.ToString(), scon);
                    SqlDataReader Re_IdentifyRd = Re_IdentifyCmd.ExecuteReader();
                    if (Re_IdentifyRd.Read())
                    {
                        Response.Write(Server_item_id + "|" + Re_IdentifyRd["Cash"] + "|" + Re_IdentifyRd["Options"] + "|" + Re_IdentifyRd["Options_value"] + "|" + Re_IdentifyRd["Option_Added"]);
                    }
                    else
                    {
                        Response.Write("-2");
                    }

                    Re_IdentifyRd.Close();
                    scon.Close();
                }
                catch (Exception e)
                {
                    scon.Close();
                    Response.Write(e.Message);
                }
            }
        }

        public string Upgrade()
        {
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];

            string char_id = Request.Form["char_id"];
            string user_id = Request.Form["user_id"];
            string Server_item_id = Request.Form["Server_item_id"];
            System.Text.StringBuilder Result = new System.Text.StringBuilder();
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string cmd = "exec [Get_Item_info] " + Server_item_id;
                    int Item_Enc_Grade = 0;
                    int Item_info = 0;
                    int Level_Limit = 0;
                    int Rarity = 0;
                    bool Item_Identified = false;

                    SqlCommand Get_Item_info = new SqlCommand(cmd, scon);
                    SqlDataReader rd = Get_Item_info.ExecuteReader();
                    if (rd.Read())
                    {
                        Item_info = Convert.ToInt32(rd["Item_info"].ToString());
                        Item_Enc_Grade = Convert.ToInt32(rd["Enc_grade"].ToString());
                        Level_Limit = Convert.ToInt32(rd["LevLimit"].ToString());
                        Rarity = Convert.ToInt32(rd["Rarity"].ToString());
                        Item_Identified = Convert.ToBoolean(rd["identifyed"]);
                    }
                    else
                    {
                        scon.Close();
                        return "-2";
                    }

                    rd.Close();

                    if (Rarity != 0)
                    {
                        if (!Item_Identified && Item_Enc_Grade >= 15)
                        {
                            scon.Close();
                            return "-3";
                        }
                    }
                    else
                    {
                        if (Item_Enc_Grade >= 15)
                        {
                            scon.Close();
                            return "-3";
                        }
                    }

                    double Require = (300) * Math.Pow((1 + (Item_Enc_Grade * 0.3)), 2);
                    if (Item_Enc_Grade > 4) Require = 0;
                    int Upgrade_Percent = iod.Upgrade_Percent(Item_Enc_Grade);
                    int Req_Gold = (int)Require;

                    cmd = "exec Item_Upgrade '" + user_id + "'," + char_id + "," + Server_item_id + "," + Require + "," + Item_Enc_Grade + "," + Upgrade_Percent;
                    SqlCommand Execute_Item_Upgrade = new SqlCommand(cmd, scon);
                    SqlDataReader Eiu_Rd = Execute_Item_Upgrade.ExecuteReader();
                    if (Eiu_Rd.Read())
                    {
                        Result.Append(Convert.ToInt32(Eiu_Rd["item_id"].ToString()) + ":");
                        Result.Append(Convert.ToInt32(Eiu_Rd["Enc_grade"].ToString()) + ":");
                        Result.Append(Convert.ToInt32(Eiu_Rd["gold"].ToString()) + ":");
                        Result.Append(Convert.ToInt32(Eiu_Rd["cash"].ToString()) + ":");
                        Result.Append(Convert.ToInt32(Eiu_Rd["stone_id"].ToString()) + ":");
                        Result.Append(Convert.ToInt32(Eiu_Rd["stone_count"].ToString()) + ":");
                        Result.Append(Upgrade_Percent);
                    }
                    else
                    {
                        Eiu_Rd.Close();
                        scon.Close();
                        return "-4";
                    }
                    Eiu_Rd.Close();
                    scon.Close();
                    return Result.ToString();
                }
                catch (Exception e)
                {
                    scon.Close();
                    return e.Message;
                }
            }
        }

        public void Create()
        {
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];

            string char_id = Request.Form["char_id"];
            string Trid = Request.Form["Recipe_id"];
            string Trin = Request.Form["Recipe_index"];
            string Target_Item_id = Request.Form["Server_item_id"];
            string count = Request.Form["Count"];

            System.Text.StringBuilder Result = new System.Text.StringBuilder();
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    int Req_gold = 0;
                    int Char_gold = 0;
                    int m1_id = 0;
                    int m2_id = 0;
                    int m3_id = 0;
                    int m4_id = 0;
                    int m1_count = 0;
                    int m2_count = 0;
                    int m3_count = 0;
                    int m4_count = 0;
                    int char_m1 = 0;
                    int char_m2 = 0;
                    int char_m3 = 0;
                    int char_m4 = 0;
                    int ItemType = 0;

                    string cmd = "exec [Check_Create] " + char_id + "," + Trid + "," + Trin + "," + count;
                    SqlCommand Check_Create = new SqlCommand(cmd, scon);
                    SqlDataReader rd = Check_Create.ExecuteReader();

                    if (rd.Read())
                    {
                        ItemType = Convert.ToInt32(rd["ItemType"]);
                        m1_id = Convert.ToInt32(rd["req_m1_id"]);
                        m2_id = Convert.ToInt32(rd["req_m2_id"]);
                        m3_id = Convert.ToInt32(rd["req_m3_id"]);
                        m4_id = Convert.ToInt32(rd["req_m4_id"]);
                        m1_count = Convert.ToInt32(rd["req_m1_count"]);
                        m2_count = Convert.ToInt32(rd["req_m2_count"]);
                        m3_count = Convert.ToInt32(rd["req_m3_count"]);
                        m4_count = Convert.ToInt32(rd["req_m4_count"]);
                        char_m1 = Convert.ToInt32(rd["char_m1_count"]);
                        char_m2 = Convert.ToInt32(rd["char_m2_count"]);
                        char_m3 = Convert.ToInt32(rd["char_m3_count"]);
                        char_m4 = Convert.ToInt32(rd["char_m4_count"]);
                        Req_gold = Convert.ToInt32(rd["gold"]);
                        Char_gold = Convert.ToInt32(rd["usergold"]);
                    }
                    rd.Close();

                    if (m1_count <= char_m1 && m2_count <= char_m2 && m3_count <= char_m3 && m4_count <= char_m4 && Req_gold <= Char_gold)
                    {
                        char_m1 -= m1_count;
                        char_m2 -= m2_count;
                        char_m3 -= m3_count;
                        char_m4 -= m4_count;

                        Dictionary<string, string> Option_info = iod.Option_Generate(ItemType, 3);
                        string[] Options_Data = Get_Option_String(Option_info);
                        string cmd2 = "exec [Execute_Create] " + char_id + "," + Trid + "," + Trin + ",'" + Options_Data[0] + "','" + Options_Data[1] + "'," + Target_Item_id + "," + count;
                        SqlCommand Create_Item = new SqlCommand(cmd2, scon);
                        SqlDataReader rd2 = Create_Item.ExecuteReader();
                        if (rd2.Read())
                        {
                            Result.Append(rd2["Item_id"] + "|");
                            Result.Append(rd2["Item_info"] + "|");
                            Result.Append(rd2["identifyed"] + "|");
                            Result.Append(rd2["Rarity"] + "|");
                            Result.Append(rd2["Options"] + "|");
                            Result.Append(rd2["Options_value"] + "|");
                            Result.Append(rd2["gold"] + "|");
                            Result.Append(m1_id + "|");
                            Result.Append(m2_id + "|");
                            Result.Append(m3_id + "|");
                            Result.Append(m4_id + "|");
                            Result.Append(char_m1 + "|");
                            Result.Append(char_m2 + "|");
                            Result.Append(char_m3 + "|");
                            Result.Append(char_m4 + "|");
                            Result.Append(rd2["Option_id"] + "|");
                            Result.Append(Target_Item_id + "|");
                            Result.Append(rd2["Item_Count"]);
                        }
                        else
                        {
                            rd2.Close();
                            scon.Close();
                            Response.Write("-3");
                        }
                        rd2.Close();
                        scon.Close();
                        Response.Write(Result.ToString());
                    }
                    else
                    {
                        scon.Close();
                        Response.Write("-2");
                    }
                }
                catch (Exception e)
                {
                    Response.Write(e.Message);
                    scon.Close();
                }
            }
        }

        public void Save()
        {
            string char_id = Request.Form["char_id"];
            string[] items = Request.Form["items"].Split(',');
            string exp = Request.Form["exp"];

            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("update Char_info set CurExp += " + exp + " , TotalExp += " + exp + " where Char_id = " + char_id);
                scon.Open();
                try
                {
                    for (int i = 0; i < items.Length; i++)
                    {
                        string[] item_info = items[i].Split(':');
                        if (item_info.Length > 1)
                        {
                            //sb.Append(" exec dbo.Add_Item @char_id='" + char_id + "', @item_info=" + item_info[0] + ", @count=" + item_info[1]);
                        }
                    }

                    SqlCommand Add_Item = new SqlCommand(sb.ToString(), scon);
                    int Add_Result = Add_Item.ExecuteNonQuery();

                    if (Add_Result >= 1)
                    {
                        Response.Write(Add_Result);
                    }
                    scon.Close();
                }
                catch (Exception e)
                {
                    Response.Write(e.Message);
                    scon.Close();
                    return;
                }
            }
        }

        public void Save_Char()
        {
            string char_id = Request.Form["char_id"];
            string exp = Request.Form["exp"];
            string gold = Request.Form["gold"];
            string itemgrades = Request.Form["itemgrades"];
            string itemids = Request.Form["itemids"];
            string iteminternalids = Request.Form["iteminternalids"];
            string questinfo1 = Request.Form["questinfo1"];
            string questinfo2 = Request.Form["questinfo2"];

            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                System.Text.StringBuilder Result = new System.Text.StringBuilder();
                scon.Open();
                try
                {
                    if (exp.Equals("") || exp == null) exp = "0";

                    if (gold.Equals("") || gold == null) gold = "0";

                    string Increase_string = "exec Get_Exp " + char_id + "," + exp + "," + gold + ",'" + questinfo1 + "','" + questinfo2 + "'";
                    SqlCommand Increase_cmd = new SqlCommand(Increase_string, scon);
                    SqlDataReader Add_Exp_Reader = Increase_cmd.ExecuteReader();
                    if (Add_Exp_Reader.Read())
                    {
                        Result.Append(Add_Exp_Reader["UserExp"] + ";" + Add_Exp_Reader["UserGold"] + ";" + Add_Exp_Reader["TotalExp"] + ";" + Add_Exp_Reader["Cur_Quest1"] + ";" + Add_Exp_Reader["Cur_Quest2"] + "|");
                    }
                    Add_Exp_Reader.Close();

                    if (itemids != null && !itemids.Equals(""))
                    {
                        sb.Append("exec dbo.Save_Item " + char_id + ",'" + itemids + "','" + itemgrades + "','" + iteminternalids + "'");
                        SqlCommand Add_Item = new SqlCommand(sb.ToString(), scon);
                        SqlDataReader Add_Item_Reader = Add_Item.ExecuteReader();
                        sb.Clear();
                        var loop = true;
                        while (loop)
                        {
                            loop = Add_Item_Reader.Read();
                            if (!loop)
                            {
                                if (Result.Length >= 1)
                                    Result.Remove((Result.Length - 1), 1);
                            }
                            else
                            {
                                Result.Append(Add_Item_Reader["Item"] + ":" + Add_Item_Reader["Grade"] + ":" + Add_Item_Reader["internal_id"] + ",");
                            }
                        }
                    }

                    Response.Write(Result);
                    scon.Close();
                }
                catch (Exception e)
                {
                    Response.Write(e.Message);
                    scon.Close();
                    return;
                }
            }
        }

        public void Item_Sell()
        {
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            string char_id = Request.Form["char_id"];
            string Server_item_id = Request.Form["Server_item_id"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                System.Text.StringBuilder Result = new System.Text.StringBuilder();
                scon.Open();
                try
                {
                    string cmd = "exec [Sell_Item] " + Server_item_id + "," + char_id;
                    SqlCommand Sell_Cmd = new SqlCommand(cmd, scon);
                    SqlDataReader Sell_Cmd_Reader = Sell_Cmd.ExecuteReader();

                    if (Sell_Cmd_Reader.Read())
                    {
                        Result.Append(Sell_Cmd_Reader["itemprice"]);
                        Result.Append(":");
                        Result.Append(Sell_Cmd_Reader["chargold"]);
                        Result.Append(":");
                        Result.Append(Server_item_id);
                    }
                    else
                    {
                        Result.Append("-1");
                    }

                    Response.Write(Result);

                    Sell_Cmd_Reader.Close();
                    scon.Close();
                }
                catch (Exception e)
                {
                    Response.Write(e.Message);
                    scon.Close();
                }
            }
        }

        public Dictionary<string, string> Option_Generate(string item_id, bool Grade_Fix, int Grade, int itemtype)
        {
            Dictionary<string, string> Option_info = new Dictionary<string, string>();
            if (itemtype == 8)
            {
                return Option_info;
            }
            else
            {
                return Option_info;
            }
        }

        public string[] Get_Option_String(Dictionary<string, string> Option_info)
        {
            System.Text.StringBuilder Option_s = new System.Text.StringBuilder();
            System.Text.StringBuilder Option_v = new System.Text.StringBuilder();
            if (Option_info.Count >= 1)
            {
                for (int i = 0; i < Option_info.Count; i++)
                {
                    Option_s.Append(Option_info.Keys.ElementAt(i));
                    Option_v.Append(Option_info.Values.ElementAt(i));
                    if (i != (Option_info.Count - 1))
                    {
                        Option_s.Append(",");
                        Option_v.Append(",");
                    }
                }
                return new string[] { Option_s.ToString(), Option_v.ToString() };

            }
            else
            {
                return new string[] { "0", "0" };
            }

        }

        public void Get_MailAsync(/*string user_id, string char_id, string mail_id*/)
        {
            string user_id = Request.Form["User_code"];
            string char_id = Request.Form["char_id"];
            string mail_id = Request.Form["mail_id"];
            string cmd = "exec [Get_Message] '" + user_id + "'," + char_id + "," + mail_id;

            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                scon.Open();
                try
                {
                    SqlCommand Mail_Cmd = new SqlCommand(cmd, scon);
                    SqlDataReader Mail_Reader = Mail_Cmd.ExecuteReader();

                    if (Mail_Reader.Read())
                    {
                        sb.Append(mail_id);
                        sb.Append(":");
                        sb.Append(Mail_Reader["CurExp"]);
                        sb.Append(":");
                        sb.Append(Mail_Reader["TotalExp"]);
                        sb.Append(":");
                        sb.Append(Mail_Reader["gold"]);
                        sb.Append(":");
                        sb.Append(Mail_Reader["cash"]);
                        sb.Append(":");
                        sb.Append(Mail_Reader["i_id"]);
                        sb.Append(":");
                        sb.Append(Mail_Reader["i_count"]);
                        sb.Append(":");
                        sb.Append(Mail_Reader["i_s_id"]);
                    }
                    else
                    {
                        sb.Append("-1");
                    }

                    AsyncManager.Parameters["Result"] = sb.ToString();
                    Mail_Reader.Close();
                    scon.Close();
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    AsyncManager.Parameters["Result"] = cmd;
                    scon.Close();
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Get_MailCompleted(string Result)
        {
            return Result;
        }

        public void DisassembleAsync(/*string char_info, string Server_item_ids*/)
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            string char_info = Request.Form["char_id"];
            string Server_item_ids = Request.Form["Server_item_ids"];

            string cmd = "Get_Items_info '" + Server_item_ids + "'";
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                System.Text.StringBuilder cmds = new System.Text.StringBuilder();
                scon.Open();
                try
                {
                    SqlCommand Disass_Cmd = new SqlCommand(cmd, scon);
                    SqlDataReader Disass_Reader = Disass_Cmd.ExecuteReader();

                    List<int> Item_Raritys = new List<int>();
                    List<int> Item_Prices = new List<int>();
                    List<int> Item_Enc_Grades = new List<int>();

                    while (Disass_Reader.Read())
                    {
                        int Rarity = System.Convert.ToInt32(Disass_Reader["Rarity"]);
                        int Price = System.Convert.ToInt32(Disass_Reader["Price"]);
                        int Enc_Grade = System.Convert.ToInt32(Disass_Reader["Enc_Grade"]);
                        Item_Prices.Add(Price);
                        Item_Raritys.Add(Rarity);
                        Item_Enc_Grades.Add(Enc_Grade);
                    }

                    Disass_Reader.Close();

                    Dictionary<int, int> Material_Count = Convert_To_Material(Item_Prices, Item_Raritys, Item_Enc_Grades);

                    cmds.Append("[dbo].[Add_Materials] " + char_info + "," + Material_Count[90018] + "," + Material_Count[90019] + "," + Material_Count[90020]);

                    SqlCommand Add_Material = new SqlCommand(cmds.ToString(), scon);
                    SqlDataReader Materials = Add_Material.ExecuteReader();
                    var loop = true;
                    while (loop)
                    {
                        loop = Materials.Read();
                        if (!loop)
                        {
                            if (sb.Length >= 1)
                                sb.Remove((sb.Length - 1), 1);
                        }
                        else
                        {
                            sb.Append(Materials[0] + ";" + Materials[1] + ";" + Materials[2] + ":");
                        }
                    }

                    sb.Append("|" + Server_item_ids);
                    scon.Close();
                    AsyncManager.Parameters["Result"] = sb.ToString();
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

        public void Disassemble2Async(string char_info, string Server_item_ids)
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            //string char_info = Request.Form["char_id"];
            //string Server_item_ids = Request.Form["Server_item_ids"];

            string cmd = "Get_Items_info '" + Server_item_ids + "'";
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                System.Text.StringBuilder cmds = new System.Text.StringBuilder();
                scon.Open();
                try
                {
                    SqlCommand Disass_Cmd = new SqlCommand(cmd, scon);
                    SqlDataReader Disass_Reader = Disass_Cmd.ExecuteReader();

                    List<int> Item_Raritys = new List<int>();
                    List<int> Item_Prices = new List<int>();
                    List<int> Item_Enc_Grades = new List<int>();

                    while (Disass_Reader.Read())
                    {
                        int Rarity = System.Convert.ToInt32(Disass_Reader["Rarity"]);
                        int Price = System.Convert.ToInt32(Disass_Reader["Price"]);
                        int Enc_Grade = System.Convert.ToInt32(Disass_Reader["Enc_Grade"]);
                        Item_Prices.Add(Price);
                        Item_Raritys.Add(Rarity);
                        Item_Enc_Grades.Add(Enc_Grade);
                    }

                    Disass_Reader.Close();

                    Dictionary<int, int> Material_Count = Convert_To_Material(Item_Prices, Item_Raritys, Item_Enc_Grades);

                    cmds.Append("[dbo].[Add_Materials] " + char_info + "," + Material_Count[90018] + "," + Material_Count[90019] + "," + Material_Count[90020]);
                    scon.Close();
                    AsyncManager.Parameters["Result"] = cmds.ToString();
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

        public string Disassemble2Completed(string Result)
        {
            return Result;
        }

        public string DisassembleCompleted(string Result)
        {
            return Result;
        }

        Dictionary<int, int> Convert_To_Material(List<int> Price, List<int> Rarity, List<int> Enc_Grade)
        {
            Dictionary<int, int> Materials = new Dictionary<int, int>();
            Materials.Add(90018, 0);
            Materials.Add(90019, 0);
            Materials.Add(90020, 0);

            for (int i = 0; i < Price.Count; i++)
            {
                int Item_Rarity = Rarity[i];
                int Item_Grade = Enc_Grade[i];
                float Item_Price = (int)(Price[i] * (1 + Item_Grade * 0.1f));
                int Material_Count = (int)(Item_Price * 0.025f);
                if (Item_Rarity < 2)
                {
                    Materials[90018] += Material_Count;
                }
                else if (Item_Rarity == 2)
                {
                    //Material_Count /= 10;
                    Materials[90019] += Material_Count;
                }
                else if (Item_Rarity == 3)
                {
                    //Material_Count /= 100;
                    Materials[90020] += Material_Count;
                }
            }

            return Materials;
        }

        public void Get_All_MailAsync(/*string user_id, string char_id, string mail_ids*/)
        {
            string user_id = Request.Form["User_code"];
            string char_id = Request.Form["char_id"];
            string mail_ids = Request.Form["mail_ids"];
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            string cmd = "";

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                scon.Open();
                try
                {
                    cmd += " exec [Get_All_Message] '" + user_id + "'," + char_id + ",'" + mail_ids + "'";
                    SqlCommand Mail_Cmd = new SqlCommand(cmd, scon);
                    SqlDataReader Mail_Reader = Mail_Cmd.ExecuteReader();
                    var loop = true;
                    while (loop)
                    {
                        loop = Mail_Reader.Read();
                        if (!loop)
                        {
                            if (sb.Length >= 1)
                            {
                                sb.Remove((sb.Length - 1), 1);
                                sb.Append(";");
                            }
                        }
                        else
                        {
                            sb.Append(Mail_Reader["Msg_id"]);
                            sb.Append(":");
                            sb.Append(Mail_Reader["m_gold"]);
                            sb.Append(":");
                            sb.Append(Mail_Reader["m_cash"]);
                            sb.Append(":");
                            sb.Append(Mail_Reader["m_exp"]);
                            sb.Append(":");
                            sb.Append(Mail_Reader["m_item_id"]);
                            sb.Append(":");
                            sb.Append(Mail_Reader["m_item_count"]);
                            sb.Append(":");
                            sb.Append(Mail_Reader["m_item_s_id"] + "|");
                        }
                    }

                    Mail_Reader.Close();

                    cmd = "exec [dbo].[Get_User_info] '" + user_id + "'," + char_id ;
                    SqlCommand User_Info = new SqlCommand(cmd, scon);
                    SqlDataReader User_Info_Reader = User_Info.ExecuteReader();
                    if(User_Info_Reader.Read())
                    {
                        sb.Append(User_Info_Reader["CurExp"] + ":");
                        sb.Append(User_Info_Reader["TotalExp"] + ":");
                        sb.Append(User_Info_Reader["Gold"] + ":");
                        sb.Append(User_Info_Reader["Cash"]);
                    }
                    User_Info_Reader.Close();
                    AsyncManager.Parameters["Result"] = sb.ToString();
                    scon.Close();
                    AsyncManager.OutstandingOperations.Decrement();
                }
                catch (Exception e)
                {
                    AsyncManager.Parameters["Result"] = e.Message;
                    scon.Close();
                    AsyncManager.OutstandingOperations.Decrement();
                }
            }
        }

        public string Get_All_MailCompleted(string Result)
        {
            return Result;
        }

        public void Refresh_ShopAsync(/*string char_id*/)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            string user_id = Request.Form["User_code"];
            string char_id = Request.Form["char_id"];
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            string cmd = "exec Get_Shop_Status " + char_id;

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                scon.Open();
                try
                {
                    SqlCommand Shop_Cmd = new SqlCommand(cmd, scon);
                    SqlDataReader Shop_Reader = Shop_Cmd.ExecuteReader();
                    int isFree = 0;
                    int WayPoint = 0;
                    if (Shop_Reader.Read())
                    {
                        WayPoint = System.Convert.ToInt32(Shop_Reader["WayPoint"]);
                        isFree = System.Convert.ToInt32(Shop_Reader["Free"]);
                    }

                    Shop_Reader.Close();
                    int Shop_Tier = (WayPoint / 10) + 1;
                    cmd = "exec Get_Shop_Items " + Shop_Tier;
                    SqlCommand Shop_Items = new SqlCommand(cmd, scon);
                    SqlDataReader Shop_Items_Reader = Shop_Items.ExecuteReader();
                    List<string> Common_Items = new List<string>();
                    List<string> Rare_Items = new List<string>();

                    while (Shop_Items_Reader.Read())
                    {
                        string Item_Info = Shop_Items_Reader["Item_id"].ToString();
                        string Cash = Shop_Items_Reader["Cash"].ToString();
                        if(Cash.Equals("0"))
                        {
                            Common_Items.Add(Item_Info);
                        } else
                        {
                            Rare_Items.Add(Item_Info);
                        }
                    }

                    Shop_Items_Reader.Close();
                    string Shop_Data = Sd.Select_Shop_Items(Common_Items, Rare_Items);
                    cmd = "Refresh_Shop_Data '" + user_id + "'," + char_id + ",'" + Shop_Data + "'," + isFree ;
                    SqlCommand Result = new SqlCommand(cmd, scon);
                    SqlDataReader Result_Reader = Result.ExecuteReader();
                    if(Result_Reader.Read())
                    {
                        sb.Append(Result_Reader["sg"] + ":");
                        sb.Append(Result_Reader["sb"] + ":");
                        sb.Append(Result_Reader["ca"] + "|");
                        sb.Append(Result_Reader["gd"]);
                    } else
                    {
                        Result_Reader.Close();
                        scon.Close();
                        AsyncManager.Parameters["Result"] = "-1";
                        AsyncManager.OutstandingOperations.Decrement();
                    }
                    Result_Reader.Close();
                    scon.Close();
                    AsyncManager.Parameters["Result"] = sb.ToString();
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

        public string Refresh_ShopCompleted(string Result)
        {
            return Result;
        }
        public void Check_Connecting()
        {
            Response.Write("ㅇㅁㄴㅇㅁㄴ");
        }
    }
}