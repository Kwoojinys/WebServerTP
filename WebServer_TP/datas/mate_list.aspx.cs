using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;


namespace JsWebServer.datas
{
    public partial class mate_list : System.Web.UI.Page
    {
        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlConnection scon = new SqlConnection();
        static SqlCommand scom = new SqlCommand();
        protected void Page_Load(object sender, EventArgs e)
        {
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            try
            {
                using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
                {
                    scon.Open();
                    XmlTextWriter writer =
                    new XmlTextWriter(this.Response.OutputStream, System.Text.Encoding.UTF8);
                    SqlCommand cmd = new SqlCommand("select * from matelist()", scon);

                    SqlDataReader rd = cmd.ExecuteReader();

                    writer.Formatting = System.Xml.Formatting.Indented;
                    writer.WriteStartDocument();
                    //루트 설정
                    writer.WriteStartElement("mate_list");
                    int i = 0;

                    while (rd.Read())
                    {
                        writer.WriteStartElement("mate_info");
                        for (int j = 0; j < rd.FieldCount; j++)
                        {
                            //노드와 값 설정
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