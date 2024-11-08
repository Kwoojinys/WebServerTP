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
    public class Shop_SetController : AsyncController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/root");
        static SqlCommand scom = new SqlCommand();

        public string Mat_Buy()
        {
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            string userid = Request.Form["userid"];
            string mat_id = Request.Form["mat_id"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {

            }

            return null;
        }

        public void Gold_BuyAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            string userid = Request.Form["userid"];
            string Gold_Amount = Request.Form["Gold_Amount"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                SqlCommand Buy_Gold = new SqlCommand("update User_list set User_gold += " + Gold_Amount + " where User_code = '" + userid + "';", scon);
                int Buy_GoldResult = Buy_Gold.ExecuteNonQuery();
                if (Buy_GoldResult == 1)
                {
                    AsyncManager.Parameters["Result"] = Gold_Amount;
                }
                else
                {
                    AsyncManager.Parameters["Result"] = "-1";
                }
                AsyncManager.OutstandingOperations.Decrement();
            }
        }

        public string Gold_BuyCompleted(string Result)
        {
            return Result;
        }

        public void Cash_BuyAsync()
        {
            AsyncManager.OutstandingOperations.Increment();
            System.Configuration.ConnectionStringSettings connString = rootWebConfig.ConnectionStrings.ConnectionStrings["Rm_db_conn"];

            string userid = Request.Form["userid"];
            string Cash_Amount = Request.Form["Cash_Amount"];

            using (SqlConnection scon = new SqlConnection(connString.ConnectionString))
            {
                scon.Open();
                SqlCommand Buy_Gold = new SqlCommand("update User_list set User_cash += " + Cash_Amount + " where User_code = '" + userid + "';", scon);
                int Buy_GoldResult = Buy_Gold.ExecuteNonQuery();
                if (Buy_GoldResult == 1)
                {
                    AsyncManager.Parameters["Result"] = Cash_Amount;
                }
                else
                {
                    AsyncManager.Parameters["Result"] = "-1";
                }
                AsyncManager.OutstandingOperations.Decrement();
            }
        }

        public string Cash_BuyCompleted(string Result)
        {
            return Result;
        }
    }
}