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
using System.Threading.Tasks;

namespace JsWebServer.Controllers
{
    public class Gold_SetController : AsyncController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlCommand scom = new SqlCommand();


        public async Task<string> Modify_Gold()
        {
            string userid = Request.Form["userid"];
            string amount = Request.Form["amount"];


            return await Modify_Golds(userid, amount);
        }



        public async Task<string> Modify_Golds(string userid, string amount)
        {
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            /*string userid = Request.Form["userid"];
            string amount = Request.Form["amount"];*/

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                try
                {
                    string value = "0";
                    SqlCommand cmd = new SqlCommand("DECLARE @return_value int exec @return_value=dbo.Modify_Gold @Amount ='" + amount + "', @user_id=" + userid + " select 'Return Value' = @return_value", scon);
                    SqlDataReader rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        value = rd["Return Value"].ToString();
                    }
                    rd.Close();
                    scon.Close();
                    return value; // 1이나오면 성공 , 0이 나오면 재료나 골드가 부족
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
