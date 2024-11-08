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
namespace JsWebServer.datas
{
    public partial class user_data : System.Web.UI.Page
    {
        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlCommand scom = new SqlCommand();

        protected virtual void DoTask()
        {
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
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
                        
                        
                        SqlCommand Mate_Ask = new SqlCommand("select * from selectmate('" + User_id + "')", scon);
                        SqlDataReader Mate_rd = Mate_Ask.ExecuteReader();
                        writer.WriteStartElement("mate_list");
                        while (Mate_rd.Read())
                        {
                            writer.WriteStartElement("mate_info");
                            for (int j = 0; j < Mate_rd.FieldCount; j++)
                            {
                                //노드와 값 설정
                                writer.WriteAttributeString(Mate_rd.GetName(j), Mate_rd[j].ToString());

                            }
                            writer.WriteEndElement();
                        }
                        Mate_rd.Close();

                        SqlCommand Reci_Ask = new SqlCommand("select * from selectreci('" + User_id + "') ", scon);
                        SqlDataReader Reci_rd = Reci_Ask.ExecuteReader();
                        writer.WriteStartElement("reci_list");
                        while (Reci_rd.Read())
                        {
                            writer.WriteStartElement("reci_info");
                            for (int j = 0; j < Reci_rd.FieldCount; j++)
                            {
                                writer.WriteAttributeString(Reci_rd.GetName(j), Reci_rd[j].ToString());
                            }
                            writer.WriteEndElement();
                        }
                        Reci_rd.Close();
                        
                        SqlCommand Tool_Ask = new SqlCommand("select * from get_tool_list('" + User_id + "') ", scon);
                        SqlDataReader Tool_rd = Tool_Ask.ExecuteReader();
                        writer.WriteStartElement("tool_list");
                        while (Tool_rd.Read())
                        {
                            writer.WriteStartElement("tool_info");
                            for (int j = 0; j < Tool_rd.FieldCount; j++)
                            {
                                writer.WriteAttributeString(Tool_rd.GetName(j), Tool_rd[j].ToString());
                            }
                            writer.WriteEndElement();
                        }
                        Tool_rd.Close();

                        SqlCommand Stage_Ask = new SqlCommand("select * from selectstage('" + User_id + "')", scon);
                        SqlDataReader Stage_rd = Stage_Ask.ExecuteReader();
                        writer.WriteStartElement("stage_list");
                        while (Stage_rd.Read())
                        {
                            writer.WriteStartElement("stage_info");
                            for (int j = 0; j < Stage_rd.FieldCount; j++)
                            {
                                //노드와 값 설정
                                writer.WriteAttributeString(Stage_rd.GetName(j), Stage_rd[j].ToString());
                            }
                            writer.WriteEndElement();
                        }
                        Stage_rd.Close();

                        
                        SqlCommand Yogu_Ask = new SqlCommand("select * from selectyogu('" + User_id + "') ", scon);
                        SqlDataReader Yogu_rd = Yogu_Ask.ExecuteReader();
                        writer.WriteStartElement("yogu_list");
                        while (Yogu_rd.Read())
                        {
                            writer.WriteStartElement("yogu_info");
                            for (int j = 0; j < Yogu_rd.FieldCount; j++)
                            {
                                writer.WriteAttributeString(Yogu_rd.GetName(j), Yogu_rd[j].ToString());
                            }
                            writer.WriteEndElement();

                        }
                        Yogu_rd.Close();
                        
                        
                        SqlCommand Employee_ask = new SqlCommand("select * from User_Employee where User_code = N'" + User_id + "'", scon);
                        SqlDataReader Employee_rd = Employee_ask.ExecuteReader();
                        writer.WriteStartElement("Employee_list");
                        while (Employee_rd.Read())
                        {
                            writer.WriteStartElement("Employee_info");
                            for (int j = 0; j < Employee_rd.FieldCount; j++)
                            {
                                writer.WriteAttributeString(Employee_rd.GetName(j), Employee_rd[j].ToString());
                            }
                            writer.WriteEndElement();

                        }
                        Employee_rd.Close();
                        
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
                        rd.Close();
                        
                        
                        SqlCommand Mate_Ask = new SqlCommand("select * from selectmate('" + User_id + "')", scon);
                        SqlDataReader Mate_rd = Mate_Ask.ExecuteReader();
                        writer.WriteStartElement("mate_list");
                        while (Mate_rd.Read())
                        {
                            writer.WriteStartElement("mate_info");
                            for (int j = 0; j < Mate_rd.FieldCount; j++)
                            {
                                //노드와 값 설정
                                writer.WriteAttributeString(Mate_rd.GetName(j), Mate_rd[j].ToString());

                            }
                            writer.WriteEndElement();
                        }
                        Mate_rd.Close();

                        SqlCommand Reci_Ask = new SqlCommand("select * from selectreci('" + User_id + "') ", scon);
                        SqlDataReader Reci_rd = Reci_Ask.ExecuteReader();
                        writer.WriteStartElement("reci_list");
                        while (Reci_rd.Read())
                        {
                            writer.WriteStartElement("reci_info");
                            for (int j = 0; j < Reci_rd.FieldCount; j++)
                            {
                                writer.WriteAttributeString(Reci_rd.GetName(j), Reci_rd[j].ToString());
                            }
                            writer.WriteEndElement();
                        }
                        Reci_rd.Close();
                        
                        
                        SqlCommand Tool_Ask = new SqlCommand("select * from get_tool_list('" + User_id + "') ", scon);
                        SqlDataReader Tool_rd = Tool_Ask.ExecuteReader();
                        writer.WriteStartElement("tool_list");
                        while (Tool_rd.Read())
                        {
                            writer.WriteStartElement("tool_info");
                            for (int j = 0; j < Tool_rd.FieldCount; j++)
                            {
                                writer.WriteAttributeString(Tool_rd.GetName(j), Tool_rd[j].ToString());
                            }
                            writer.WriteEndElement();
                        }
                        Tool_rd.Close();

                        SqlCommand Stage_Ask = new SqlCommand("select * from selectstage('" + User_id + "')", scon);
                        SqlDataReader Stage_rd = Stage_Ask.ExecuteReader();
                        writer.WriteStartElement("stage_list");
                        while (Stage_rd.Read())
                        {
                            writer.WriteStartElement("stage_info");
                            for (int j = 0; j < Stage_rd.FieldCount; j++)
                            {
                                //노드와 값 설정
                                writer.WriteAttributeString(Stage_rd.GetName(j), Stage_rd[j].ToString());
                            }
                            writer.WriteEndElement();
                        }
                        Stage_rd.Close();
                        
                        
                        SqlCommand Yogu_Ask = new SqlCommand("select * from selectyogu('" + User_id + "') ", scon);
                        SqlDataReader Yogu_rd = Yogu_Ask.ExecuteReader();
                        writer.WriteStartElement("yogu_list");
                        while (Yogu_rd.Read())
                        {
                            writer.WriteStartElement("yogu_info");
                            for (int j = 0; j < Yogu_rd.FieldCount; j++)
                            {
                                writer.WriteAttributeString(Yogu_rd.GetName(j), Yogu_rd[j].ToString());
                            }
                            writer.WriteEndElement();

                        }
                        Yogu_rd.Close();
                        

                        
                        SqlCommand Employee_ask = new SqlCommand("select * from User_Employee where User_code = N'" + User_id + "'", scon);
                        SqlDataReader Employee_rd = Employee_ask.ExecuteReader();
                        writer.WriteStartElement("Employee_list");
                        while (Employee_rd.Read())
                        {
                            writer.WriteStartElement("Employee_info");
                            for (int j = 0; j < Employee_rd.FieldCount; j++)
                            {
                                writer.WriteAttributeString(Employee_rd.GetName(j), Employee_rd[j].ToString());
                            }
                            writer.WriteEndElement();

                        }
                        Employee_rd.Close();
                        
                        writer.WriteEndDocument();

                        Response.ContentEncoding = System.Text.Encoding.UTF8;
                        Response.ContentType = "text/xml";
                        writer.Flush();
                        writer.Close();
                        if (rd != null)
                            rd.Close();
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

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Clear();
            Action action = new Action(DoTask);
            var task = Task.Factory.FromAsync(action.BeginInvoke, action.EndInvoke, null);
            Thread.Sleep(10);
            task.Wait();
        }
    }
}