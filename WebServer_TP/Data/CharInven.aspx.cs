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
    public partial class Charinven : System.Web.UI.Page
    {
        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlCommand scom = new SqlCommand();

        protected void Page_Load(object sender, EventArgs e)
        {
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Cp_db_conn"];
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                string Char_id = Request["Char_id"];

                //SqlCommand cmd = new SqlCommand("select * from dbo.recipe_list item_info FOR XML AUTO, ROOT('recipes')", scon);

                XmlTextWriter writer =
                new XmlTextWriter(this.Response.OutputStream, System.Text.Encoding.UTF8);
                SqlCommand cmd = new SqlCommand("SELECT Uin.Char_id, Item_id, Uin.Item_info, Item_Count, Enc_grade, identifyed, Rarity, OL.option_id,OL.options, OL.options_value from Option_List OL , User_inven Uin where OL.Option_id = Uin.Option_id and Uin.Char_id = '" + Char_id + "'", scon);

                //Response.Output.WriteLine(Nick_name + "," + User_id);
                writer.Formatting = System.Xml.Formatting.Indented;
                writer.WriteStartDocument();
                //루트 설정
                writer.WriteStartElement("User_Inven");

                SqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    writer.WriteStartElement("User_Inven");
                    for (int j = 0; j < rd.FieldCount; j++)
                    {
                        if (!rd[j].ToString().Equals(""))
                        {
                            writer.WriteAttributeString(rd.GetName(j), rd[j].ToString());
                        }
                    }
                    writer.WriteEndElement();
                }
                rd.Close();
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.ContentType = "text/xml";
                writer.Flush();
                writer.Close();
            }
        }
    }
}
