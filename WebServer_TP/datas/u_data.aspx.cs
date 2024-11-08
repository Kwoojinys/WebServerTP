using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsWebServer.datas
{
    public partial class u_data : System.Web.UI.Page
    {
        static Random Ran = new Random();
        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlCommand scom = new SqlCommand();

        protected virtual void DoTask()
        {
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Igy_db_conn"];
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
               // try
                //{
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                    string User_Code = Request["User_Code"];
                    string Auth_Type = Request["Auth_Type"];
                    XmlTextWriter writer =
                    new XmlTextWriter(this.Response.OutputStream, System.Text.Encoding.UTF8);
                    SqlCommand cmd = new SqlCommand("Get_User_Info @UserCode", scon);
                    cmd.Parameters.AddWithValue("@UserCode", User_Code);

                    writer.Formatting = System.Xml.Formatting.Indented;
                    writer.WriteStartDocument();
                    writer.WriteStartElement("user_data");

                    SqlDataReader rd = cmd.ExecuteReader();

                    if (!rd.HasRows)
                    {
                        if (rd != null)
                            rd.Close();

                        string SignString = "[Sign_User] @User_Code, @Quest0, @Quest1, @Quest2";
                        SqlCommand Sign_User = new SqlCommand(SignString, scon);

                        Sign_User.Parameters.AddWithValue("@User_Code", User_Code);
                        //Sign_User.Parameters.AddWithValue("@Auth_Type", Auth_Type);

                        for (int i = 0; i < 3; i++)
                        {
                            JObject B_Quest = Base_Quest(i);

                            Sign_User.Parameters.AddWithValue("@Quest" + i, JsonConvert.SerializeObject(B_Quest));
                        }

                        int Sign_Usered = Sign_User.ExecuteNonQuery();

                        if (Sign_Usered == 0)
                        {
                            return;
                        }
                    }

                    if (!rd.IsClosed)
                        rd.Close();

                    bool Today_Refresh = false;

                    string UiString = "Get_User_Info @UserCode";
                    SqlCommand User_Info = new SqlCommand(UiString, scon);

                    User_Info.Parameters.AddWithValue("@UserCode", User_Code);

                    SqlDataReader Info_Rd = User_Info.ExecuteReader();
                    while (Info_Rd.Read())
                    {
                        writer.WriteStartElement("User_Info");
                        for (int j = 0; j < Info_Rd.FieldCount; j++)
                        {
                            writer.WriteAttributeString(Info_Rd.GetName(j), Info_Rd[j].ToString());
                            if (j == Info_Rd.FieldCount - 2)
                            {
                                Today_Refresh = System.Convert.ToBoolean(Info_Rd[j].ToString());
                            }
                        }
                        writer.WriteEndElement();
                    }
                    Info_Rd.Close();

                    UiString = "Get_Quest_Info @UserCode";
                    SqlCommand Quest_Ask = new SqlCommand(UiString, scon);
                    Quest_Ask.Parameters.AddWithValue("@UserCode", User_Code);
                    SqlDataReader Quest_rd = Quest_Ask.ExecuteReader();

                    JObject Results = new JObject();

                    JArray Quests = new JArray();

                    while (Quest_rd.Read())
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            string info = Quest_rd["Quest" + j].ToString();
                            JObject Quest = JObject.Parse(info);
                            Quests.Add(Quest);
                        }
                    }

                    Quest_rd.Close();

                    DateTime Today = DateTime.Now;

                    string Quest_Set = "Today_Refresh_Quest @UserCode, @Quest_Status0, @Quest_Status1, @Quest_Status2";
                    SqlCommand Quest_Set_Cmd = new SqlCommand(Quest_Set, scon);

                    Quest_Set_Cmd.Parameters.AddWithValue("@UserCode", User_Code);

                    for (int i = 0; i < Quests.Count; i++)
                    {
                        DateTime Cleared_Time;
                        string CDate = Quests[i]["Cleared_Date"].ToString();

                        if (CDate.Equals("0"))
                        {
                            Cleared_Time = DateTime.MinValue;
                        }
                        else
                        {
                            Cleared_Time = System.Convert.ToDateTime(CDate);
                        }

                        string RDate = Quests[i]["Date"].ToString();
                        DateTime Received_Time;
                        if (RDate.Equals("0"))
                        {
                            Received_Time = DateTime.MinValue;
                        }
                        else
                        {
                            Received_Time = System.Convert.ToDateTime(RDate);
                        }
                        if (Today_Refresh && Today.Day != Received_Time.Day)
                        {
                            Quests[i] = Quest_Generate(i);
                        }

                        Quest_Set_Cmd.Parameters.AddWithValue("@Quest_Status" + i, JsonConvert.SerializeObject(Quests[i]));
                    }
                    SqlDataReader Quest_Infos = Quest_Set_Cmd.ExecuteReader();

                    while (Quest_Infos.Read())
                    {
                        writer.WriteStartElement("Quest");
                        for (int j = 0; j < Quest_Infos.FieldCount; j++)
                        {
                            //노드와 값 설정
                            writer.WriteAttributeString(Quest_Infos.GetName(j), Quest_Infos[j].ToString());
                        }
                    }

                    Quest_Infos.Close();
                    writer.WriteEndDocument();
                    Response.ContentEncoding = System.Text.Encoding.UTF8;
                    Response.ContentType = "text/xml";
                    writer.Flush();
                    writer.Close();
                    if (rd != null)
                        rd.Close();
                }
                /*catch (Exception ex)
                {
                    scon.Close();
                    Response.Output.WriteLine("Exception : {0}", ex.Message + " : " + ex.Data);
                }
                finally
                {
                    scon.Close();
                }*/
            //}
        }


        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Clear();
            Action action = new Action(DoTask);
            var task = Task.Factory.FromAsync(action.BeginInvoke, action.EndInvoke, null);
            Thread.Sleep(10);
            task.Wait();
        }

        public JObject Quest_Generate(int I_id)
        {
            JObject Quest = new JObject();

            int Quest_Id = Ran.Next(1, 4);

            Quest.Add("I_id", I_id);
            Quest.Add("Id", Quest_Id);
            Quest.Add("Date", System.DateTime.Now);
            Quest.Add("Cleared_Date", System.DateTime.MinValue);
            Quest.Add("Value", "9999");

            return Quest;
        }

        public JObject Base_Quest(int I_id)
        {
            JObject Quest = new JObject();

            Quest.Add("I_id", I_id);
            Quest.Add("Id", 0);
            Quest.Add("Date", System.DateTime.MinValue);
            Quest.Add("Cleared_Date", System.DateTime.MinValue);
            Quest.Add("Value", "9999");

            return Quest;
        }

    }
}