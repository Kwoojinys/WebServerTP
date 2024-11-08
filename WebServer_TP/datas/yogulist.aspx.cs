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
namespace JsWebServer
{
    public partial class yogulist : System.Web.UI.Page
    {
        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlConnection scon = new SqlConnection();
        static SqlCommand scom = new SqlCommand();
        protected void Page_Load(object sender, EventArgs e)
        {
            string User_id = Request["User_id"]; 
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];
            try
            {
                using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
                {
                    scon.Open();
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                    XmlTextWriter writer =
                        new XmlTextWriter(this.Response.OutputStream, System.Text.Encoding.UTF8);
                    //SqlCommand cmd = new SqlCommand("select * from dbo.recipe_list item_info FOR XML AUTO, ROOT('recipes')", scon);
                    SqlCommand cmd = new SqlCommand("select * from selectyogu('" + User_id + "') ", scon);

                    SqlDataReader rd = cmd.ExecuteReader();

                    writer.Formatting = System.Xml.Formatting.Indented;
                    writer.WriteStartDocument();
                    //루트 설정
                    writer.WriteStartElement("yogu_list");
                    int i = 0;

                    while (rd.Read())
                    {
                        writer.WriteStartElement("yogu_info");
                        for (int j = 0; j < rd.FieldCount; j++)
                        {
                            writer.WriteAttributeString(rd.GetName(j), rd[j].ToString());
                        }
                        i++;
                        writer.WriteEndElement();

                    }

                    writer.WriteEndDocument();
                    Response.ContentEncoding = System.Text.Encoding.UTF8;
                    Response.ContentType = "text/xml";
                    writer.Flush();
                    writer.Close();

                    rd.Close();
                    scon.Close();
                }
            }
            catch (Exception ex)
            {
                scon.Close();
                Response.Output.WriteLine("Exception : {0}", ex.Message);
            }


        }
    }
}