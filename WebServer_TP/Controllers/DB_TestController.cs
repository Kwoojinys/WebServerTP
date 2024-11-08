using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using System.Web.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsWebServer_CP.Controllers
{
    public class DB_TestController : Controller
    {
        Random Ra_id = new Random();

        public void Test()
        {
            try
            {
                using (MySqlConnection scon = new MySqlConnection(WebConfigurationManager.ConnectionStrings["little_jumper3"].ConnectionString))
                {
                    scon.Open();

                    Response.Write("성공");

                    scon.Close();
                }
            } catch (Exception e)
            {
                Response.Write(e.Message + ":" + e.StackTrace + ":" + e.ToString());
            }
        }

        public void JSON_TEST()
        {
            JObject Results = new JObject();
            JArray Ja = new JArray();

            for(int i = 0; i < 1; i++)
            {
                int char_id = 1;
                var cha_info = new JObject();
                cha_info.Add("char_id", char_id);

                Ja.Add(cha_info);
            }

            Results.Add("Characters", Ja);

            Response.Write(JsonConvert.SerializeObject(Results));
        }

    }
}