using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;


namespace JsWebServer_CP.Data
{
    public partial class CharData : System.Web.UI.Page
    {
        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlCommand scom = new SqlCommand();

        protected void Page_Load(object sender, EventArgs e)
        {
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                string Nick_name = Request["Nick_name"];
                string User_id = Request["User_id"];

                //SqlCommand cmd = new SqlCommand("select * from dbo.recipe_list item_info FOR XML AUTO, ROOT('recipes')", scon);

                XmlTextWriter writer =
                new XmlTextWriter(this.Response.OutputStream, System.Text.Encoding.UTF8);
                SqlCommand cmd = new SqlCommand("select * from User_list where User_code = '" + User_id + "'", scon);

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
                    if (!Nick_name.Equals(""))
                    {
                        SqlCommand Nick_Check = new SqlCommand("select User_nick from User_list where User_nick = N'" + Nick_name + "'", scon);
                        SqlDataReader Check_Result = Nick_Check.ExecuteReader();
                        while (Check_Result.HasRows)
                        {
                            Response.Output.WriteLine(2); // 닉네임 중복으로 인한 실패
                            Check_Result.Close();
                            scon.Close();
                            return;
                        }
                        Check_Result.Close();
                    }
                    SqlCommand Sign_User = new SqlCommand("DECLARE @return_value int exec @return_value=dbo.Sign_User @user_id ='" + User_id + "', @User_nick = N'" + Nick_name + "' select * from User_list where User_code = '" + User_id + "'", scon);
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
                    SignUser_info.Close();
                    Response.ContentEncoding = System.Text.Encoding.UTF8;
                    Response.ContentType = "text/xml";
                    writer.Flush();
                    writer.Close();
                }
            }
        }
    }
}