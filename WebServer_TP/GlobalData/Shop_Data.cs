using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JsWebServer_CP.GlobalData
{
    public class Shop_Data
    {
        static Random Ra = new Random();

        public string Select_Shop_Items(List<string> Common_Items, List<string> Rare_Items)
        {
            System.Text.StringBuilder Sb = new System.Text.StringBuilder();
            List<string> Selected_Items = new List<string>();
            while(Selected_Items.Count < 4) { 
                int Index = Ra.Next(0, (Common_Items.Count));
                    Selected_Items.Add(Common_Items[Index]);
                Common_Items.Remove(Common_Items[Index]);
            }

            while (Selected_Items.Count < 6)
            {
                int Index = Ra.Next(0, (Rare_Items.Count));
                Selected_Items.Add(Rare_Items[Index]);
                Rare_Items.Remove(Rare_Items[Index]);
            }

            for(int i = 0;  i < Selected_Items.Count;i++)
            {
                Sb.Append(Selected_Items[i]);
                if(i != (Selected_Items.Count-1)) {
                    Sb.Append(",");
                }
            }

            return Sb.ToString();
        }
    }
}