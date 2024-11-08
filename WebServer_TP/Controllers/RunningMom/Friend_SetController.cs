using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using JsWebServer_CP.Models;
using System.Data.SqlClient;

namespace JsWebServer.Controllers
{
    public class Friend_SetController : AsyncController
    { // 0 : 보내는 요청 1 : 받은 요청 2 : 쌍방친구추가
        private ApplicationDbContext db = new ApplicationDbContext();
        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlCommand scom = new SqlCommand();
        public string Add_Friend(string user_id, string target_id)
        {
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string cmds = "insert into User_Friend values('" + user_id + "','" + target_id + "',1,0); insert into User_Friend values('" + target_id + "','" + user_id + "',0,0)";
                    SqlCommand cmd = new SqlCommand(cmds, scon);
                    int value = cmd.ExecuteNonQuery();
                    scon.Close();
                    return value.ToString(); // 2==성공
                }
                catch (Exception e)
                {
                    scon.Close();
                    return e.Message + e.ToString();
                }
            }
        }

        public string Delete_Friend(string user_id, string target_id)
        {
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];
            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string cmds = "delete from User_Friend where user_code = '" + user_id + "' and Friend_Code = '" + target_id + "';delete from User_Friend where user_code = '" + target_id + "' and Friend_Code = '" + user_id + "'";
                    SqlCommand cmd = new SqlCommand(cmds, scon);
                    int value = cmd.ExecuteNonQuery();
                    scon.Close();
                    return value.ToString(); // 2==성공
                }
                catch (Exception e)
                {
                    scon.Close();
                    return e.Message + e.ToString();
                }
            }
        }
    }
}