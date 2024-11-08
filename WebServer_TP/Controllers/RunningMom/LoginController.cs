using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using Google.Apis.Auth.OAuth2;


namespace JsWebServer.Controllers
{
    public class LoginController : AsyncController
    {
        public void Login(string token, string id)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes("code=" + token + "&client_id=11878788501-t9qak31ghmv69jam84d6e2tc8h0ec7nl.apps.googleusercontent.com&client_secret=EUVp859Gw2-Ia9-oU3ezIPrC&redirect_uri=http://jsdoms.japanwest.cloudapp.azure.com/return.php&grant_type=authorization_code");
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://accounts.google.com/o/oauth2/token");
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                req.ContentLength = buffer.Length;
                Stream strm = req.GetRequestStream();
                strm.Write(buffer, 0, buffer.Length);
                strm.Close();

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                Stream stReadData = resp.GetResponseStream();
                StreamReader srReadData = new StreamReader(stReadData, Encoding.Default);
                string accesstoken = srReadData.ReadToEnd();

                //Response.Write(accesstoken);

                var jsonobject = JObject.Parse(accesstoken);
                string tokens = jsonobject.GetValue("access_token").ToString();
                byte[] buffer2 = Encoding.UTF8.GetBytes("access_token=" + tokens);

                //Response.Write(tokens);

                HttpWebRequest req2 = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/oauth2/v1/tokeninfo");
                req2.Method = "POST";
                req2.ContentType = "application/x-www-form-urlencoded";
                req2.ContentLength = buffer2.Length;

                Stream strm2 = req2.GetRequestStream();
                strm2.Write(buffer2, 0, buffer2.Length);
                strm2.Close();

                HttpWebResponse resp2 = (HttpWebResponse)req2.GetResponse();


                Stream stReadData2 = resp2.GetResponseStream();
                StreamReader srReadData2 = new StreamReader(stReadData2, Encoding.Default);
                string tokeninfo = srReadData2.ReadToEnd();

                var Res_Client = JObject.Parse(tokeninfo);

                string Res_Client_id = Res_Client.GetValue("issued_to").ToString();

                // 클라이언트 정보 비교 단계 -
                string path = Server.MapPath("~/App_Data/Client_Info.json");
                string json = System.IO.File.ReadAllText(path);


                var Client_Info = JObject.Parse(json).Property("web").Value;

                string Client_id = Client_Info.Value<string>("client_id");

                if (Client_id.Equals(Res_Client_id))
                    Response.Write("true");

            }
            catch (Exception e)
            {
                Response.Write(e.Message + e.ToString());
            }
        }
    }
}