using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Configuration;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;


namespace JsWebServer_CP.Data
{
    public partial class UserData : System.Web.UI.Page
    {
        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlCommand scom = new SqlCommand();

        protected void Page_Load(object sender, EventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string Nick_name = Request.Form["Nick_name"];
                    string User_id = Request.Form["User_id"];

                    XmlTextWriter writer =
                    new XmlTextWriter(this.Response.OutputStream, System.Text.Encoding.UTF8);
                    SqlCommand cmd = new SqlCommand("select * from User_info where User_code = '" + User_id + "'", scon);

                    //Response.Output.WriteLine(Nick_name + "," + User_id);
                    writer.Formatting = System.Xml.Formatting.Indented;
                    writer.WriteStartDocument();
                    //루트 설정
                    writer.WriteStartElement("user_data");

                    SqlDataReader rd = cmd.ExecuteReader();

                    if (!rd.HasRows)
                    {
                        if (rd != null)
                            rd.Close();

                        SqlCommand Sign_User = new SqlCommand("exec dbo.[Create_Char] '" + User_id + "'", scon);
                        SqlDataReader SignUser_info = Sign_User.ExecuteReader();
                        while (SignUser_info.Read())
                        {
                            writer.WriteStartElement("user_info");
                            for (int j = 0; j < SignUser_info.FieldCount; j++)
                            {
                                writer.WriteAttributeString(SignUser_info.GetName(j), SignUser_info[j].ToString());
                            }
                            writer.WriteEndElement();
                        }
                        if (SignUser_info != null)
                            SignUser_info.Close();

                        writer.WriteStartElement("char_data");
                        string Char_id = "";
                        SqlCommand Ask_Char = new SqlCommand("select * from char_Info where User_Code = '" + User_id +"'", scon);
                        SqlDataReader User_Char = Ask_Char.ExecuteReader();
                        while (User_Char.Read())
                        {
                            Char_id = User_Char["Char_id"].ToString();
                            writer.WriteStartElement("Char_Info");
                            for (int j = 0; j < User_Char.FieldCount; j++)
                            {
                                writer.WriteAttributeString(User_Char.GetName(j), User_Char[j].ToString());
                            }
                            writer.WriteEndElement();
                        }
                        User_Char.Close();

                        SqlCommand Ask_Skill = new SqlCommand("select * from char_skill where Char_id = '" + Char_id + "'", scon);
                        SqlDataReader Skill_Read = Ask_Skill.ExecuteReader();
                        while (Skill_Read.Read())
                        {
                            writer.WriteStartElement("Char_Skill");
                            for (int j = 0; j < Skill_Read.FieldCount; j++)
                            {
                                writer.WriteAttributeString(Skill_Read.GetName(j), Skill_Read[j].ToString());
                            }
                            writer.WriteEndElement();
                        }
                        Skill_Read.Close();

                        SqlCommand Ask_Items = new SqlCommand("SELECT Uin.Char_id, Item_id, Uin.Item_info, Item_Count, Enc_grade, identifyed, Rarity, OL.* from Option_List OL , User_inven Uin where OL.Option_id = Uin.Option_id and Uin.Char_id = '" + Char_id + "'", scon);
                        SqlDataReader Items_Read = Ask_Items.ExecuteReader();
                        while (Items_Read.Read())
                        {
                            writer.WriteStartElement("Char_inven");
                            for (int j = 0; j < Items_Read.FieldCount; j++)
                            {
                                writer.WriteAttributeString(Items_Read.GetName(j), Items_Read[j].ToString());
                            }
                            writer.WriteEndElement();
                        }
                        Items_Read.Close();

                        if (User_Char != null)
                        {
                            User_Char.Close();
                        }

                        SignUser_info.Close();
                        Response.ContentEncoding = System.Text.Encoding.UTF8;
                        Response.ContentType = "text/xml";
                        writer.Flush();
                        writer.Close();
                    }
                    else // ---------------------------------------회원 가입 진행 단계------------------------------------------------------------------------------------------------------------ //
                    {
                        while (rd.Read())
                        {
                            writer.WriteStartElement("user_info");
                            for (int j = 0; j < rd.FieldCount; j++)
                            {
                                writer.WriteAttributeString(rd.GetName(j), rd[j].ToString());
                            }
                            writer.WriteEndElement();
                        }
                        if (rd != null)
                            rd.Close();
                        writer.WriteStartElement("char_data");
                        string Char_id = "";
                        SqlCommand Ask_Char = new SqlCommand("select * from char_Info where User_Code = '" + User_id + "'", scon);
                        SqlDataReader User_Char = Ask_Char.ExecuteReader();
                        while (User_Char.Read())
                        {
                            Char_id = User_Char["Char_id"].ToString();
                            writer.WriteStartElement("Char_Info");
                            for (int j = 0; j < User_Char.FieldCount; j++)
                            {
                                writer.WriteAttributeString(User_Char.GetName(j), User_Char[j].ToString());
                            }
                            writer.WriteEndElement();
                        }
                        User_Char.Close();

                        SqlCommand Ask_Skill = new SqlCommand("select * from char_skill where Char_id = '" + Char_id + "'", scon);
                        SqlDataReader Skill_Read = Ask_Skill.ExecuteReader();
                        while (Skill_Read.Read())
                        {
                            writer.WriteStartElement("Char_Skill");
                            for (int j = 0; j < Skill_Read.FieldCount; j++)
                            {
                                writer.WriteAttributeString(Skill_Read.GetName(j), Skill_Read[j].ToString());
                            }
                            writer.WriteEndElement();
                        }
                        Skill_Read.Close();

                        SqlCommand Ask_Items = new SqlCommand("[Get_Char_Inven] '" + Char_id + "'", scon);
                        SqlDataReader Items_Read = Ask_Items.ExecuteReader();
                        while (Items_Read.Read())
                        {
                            writer.WriteStartElement("Char_inven");
                            for (int j = 0; j < Items_Read.FieldCount; j++)
                            {
                                writer.WriteAttributeString(Items_Read.GetName(j), Items_Read[j].ToString());
                            }
                            writer.WriteEndElement();
                        }
                        Items_Read.Close();

                        SqlCommand Ask_Quests = new SqlCommand("[Get_Char_Quest] '" + Char_id + "'", scon);
                        SqlDataReader Quests_Read = Ask_Quests.ExecuteReader();
                        while (Quests_Read.Read())
                        {
                            writer.WriteStartElement("Char_Quest");
                            for (int j = 0; j < Quests_Read.FieldCount; j++)
                            {
                                writer.WriteAttributeString(Quests_Read.GetName(j), Quests_Read[j].ToString());
                            }
                            writer.WriteEndElement();
                        }
                        Quests_Read.Close();

                        SqlCommand Ask_Msgs = new SqlCommand("[Get_Char_Msg] '" + Char_id + "'", scon);
                        SqlDataReader Msgs_Read = Ask_Msgs.ExecuteReader();
                        while (Msgs_Read.Read())
                        {
                            writer.WriteStartElement("Char_Msg");
                            for (int j = 0; j < Msgs_Read.FieldCount; j++)
                            {
                                writer.WriteAttributeString(Msgs_Read.GetName(j), Msgs_Read[j].ToString());
                            }
                            writer.WriteEndElement();
                        }
                        Msgs_Read.Close();

                        Response.ContentEncoding = System.Text.Encoding.UTF8;
                        Response.ContentType = "text/xml";
                        writer.Flush();
                        writer.Close();
                        if (User_Char != null)
                        {
                            User_Char.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    scon.Close();
                    Response.Output.WriteLine("Exception : {0}", ex.Message + " : " + ex.Data);
                }
                finally
                {
                    scon.Close();
                }
            }
        }

        public void Read_Char_info()
        {

        }
    }
}