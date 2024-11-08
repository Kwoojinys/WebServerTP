using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using JsWebServer_CP.Models;
using StackExchange.Redis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Utilities;
using System.Xml;
namespace JsWebServer.Controllers
{
    public class Rank_SetController : AsyncController
    {
        static string cst = "jsrankdb.redis.cache.windows.net:6380,password=BgX4rfagZ4puiyFASRNU3cP/d080iatKEvh/3FPEu8A=,ssl=True,abortConnect=False";

        static string conn = "localhost";
        static Random Rd = new Random();

        ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(conn);

        IDatabase cache;

        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            return ConnectionMultiplexer.Connect(conn);
        });

        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }



        public string MyRankingSet() // 랭킹데이터베이스 테스트용 더미 유저 리스트
        {
            try
            {

                for (int i = 0; i < 100; i++)
                {
                    cache = connection.GetDatabase();

                    int rs = Rd.Next(0, 50);
                    string userid = "test" + i;
                    int score = rs;
                    cache.SortedSetAdd("MaxFloor", userid, score);
                }

                connection.Close();
                return "Setting Complete 100 Users";
            }
            catch (Exception e)
            {
                connection.Close();
                return e.Message;
            }
        }

        public string Connection_test()
        {
            try
            {
                cache = connection.GetDatabase();
                connection.Close();
                return "Connection Test Complete";
            }
            catch (Exception e)
            {
                connection.Close();
                return e.Message;
            }
        }

        private ApplicationDbContext db = new ApplicationDbContext();

        public string MyRankingView(string sname, string id, int ids)
        {
            try
            {
                IDatabase cache = Connection.GetDatabase();
                cache.SortedSetAdd(sname, id, ids);
                connection.Close();
                return "Setting Complete";
            }
            catch (Exception e)
            {
                connection.Close();
                return e.Message;
            }
        }

        public void GetUsersRanking()// 10명단위로 끊어서 보여주기위한 메소드!
        {
            try
            {
                string themeid = Request.Form["themeid"];
                string nickname = Request.Form["nickname"];
                int listnum = System.Convert.ToInt32(Request.Form["listnum"]);
                int firstperson = listnum * 10; // 0 = 0 , 1 = 10, 2 = 20 ...
                int lastperson = firstperson + 9; // 0 = 9 , 1 = 19 , 2 = 29 ...
                //JArray ja = new JArray();
                XmlTextWriter writer =
                    new XmlTextWriter(this.Response.OutputStream, System.Text.Encoding.UTF8);
                writer.Formatting = System.Xml.Formatting.Indented;
                writer.WriteStartDocument();
                IDatabase cache = Connection.GetDatabase();

                writer.WriteStartElement("Request_Rank");
                writer.WriteStartElement("my_rank");
                double? score = cache.SortedSetScore("Theme" + themeid, nickname);
                long? rank = cache.SortedSetRank("Theme" + themeid, nickname, Order.Descending) + 1;
                writer.WriteAttributeString("Nickname", nickname);
                writer.WriteAttributeString("Score", score.ToString());
                writer.WriteAttributeString("Rank", rank.ToString());

                writer.WriteStartElement("rank_list");
                SortedSetEntry[] ranks2 = cache.SortedSetRangeByRankWithScores("Theme" + themeid, firstperson, lastperson, Order.Descending);
                for (int i = 0; i < ranks2.Length; i++)
                {
                    writer.WriteStartElement("rank_info");
                    char sp = ':';
                    string Rankinginfo = ranks2.ElementAt(i).ToString().Trim();
                    string[] Ranking = Rankinginfo.Split(sp);
                    writer.WriteAttributeString("Nickname", Ranking[0]);
                    writer.WriteAttributeString("Score", Ranking[1].Trim());
                    writer.WriteAttributeString("Rank", (firstperson + i + 1).ToString());
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();

                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.ContentType = "text/xml";
                writer.Flush();
                writer.Close();
                connection.Close();
            }
            catch (Exception e)
            {
                connection.Close();
                Response.Write(e.ToString());
            }
        }

        public void GetMyRanking(string sname, string Usercode)
        {
            try
            {
                JArray ja = new JArray();
                IDatabase cache = Connection.GetDatabase();
                //SortedSetEntry[] ranks = cache.SortedSetRangeByScoreWithScores
                double? x = cache.SortedSetScore(sname, Usercode);
                Response.Write(x.ToString());
            }
            catch (Exception e)
            {
                connection.Close();
            }
        }


        public string UserRankSet(string id, string Themeid, int score)// 유저 랭킹 세팅
        {
            try
            {
                cache = Connection.GetDatabase();
                string userid = id;
                cache.SortedSetAdd(Themeid, userid, score);
                connection.Close();
                return "Setting Complete";
            }
            catch (Exception e)
            {
                connection.Close();
                return e.Message;
            }
        }
    }
}
