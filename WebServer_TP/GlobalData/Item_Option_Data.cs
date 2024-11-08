using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;

namespace JsWebServer_CP.GlobalData
{
    public enum ItemType
    {
        Weapon = 1,
        Armor = 2,
        Other = 3
    }


    public class Item_Option_Data
    {
        static Random Ra = new Random();
        //int[] Option_Types = { 14, 16, 17, 18, 21, 23, 32, 34, 36 };

        #region Option_Status
        static int[,] Options_value = {
        { 10, 15, 18, 18 }, // AP = 14          0
        { 5, 8, 12, 12 }, // DEF = 16           1
        { 5, 8, 15, 15 }, // HP = 17            2
        { 3, 5, 8, 8 }, // AS = 18              3
        { 0, 5, 8, 8 }, // MS = 21              4
        { 3, 5, 8, 8 }, // CRI = 23             5
        { 10, 15, 20, 20 }, // RES = 29         6
        { 1, 2, 3, 4 }, // RES_ELE = 28         7
        { 0, 0, 20, 25 } // RES_AP = 32,34,36   8
        };

        static int[] Weapon_Option_Limit = { 0, 2, 3, 6, 6 };
        static int[] Weapon_Option_List = { 14, 18, 23, 32, 34, 36 };
        static int[] Weapon_Option_Stat = { 0, 3, 5, 8, 8, 8 };
        static int[] Weapon_Elemental = { 32, 34, 36 };

        static int[] Armor_Option_Limit = { 2, 3, 4, 4, 4 };
        static int[] Armor_Option_List = { 16, 17, 29, 28, 18 };
        static int[] Armor_Option_Stat = { 1, 2, 2, 1, 3 };

        static int[] Other_Option_Limit = { 2, 3, 5, 5, 5};
        static int[] Other_Option_List = { 16, 17, 23, 29, 21, 18 };
        static int[] Other_Option_Stat = { 1, 2, 5, 1, 4, 3 };
        #endregion

        public string Get_Option_Value(int Option_Type, int Item_Grade)
        {
            try
            {
                if (Item_Grade < 0)
                {
                    Item_Grade = 0;
                }
                bool Confirmed = false;
                int Option = 0;
                while (!Confirmed)
                {
                    Option = Ra.Next(1, Options_value[Option_Type, Item_Grade]);
                    Confirmed = true;
                }

                return Option.ToString();
            } catch (Exception e)
            {
                return "Error:" + Option_Type.ToString();
            }
        }

        public int Get_Grade(int Min_Grade)
        {
            int RareTem = Ra.Next(100);
            int Returned_Grade = 0;

            if(RareTem < 60)
            {
                Returned_Grade= 0;
            }
            if(RareTem >= 60 && RareTem < 79)
            {
                Returned_Grade = 1;
            }
            else if (RareTem >= 80 && RareTem < 95)
            {
                Returned_Grade = 2;
            }
            else if (RareTem >= 95)
            {
                Returned_Grade = 3;
            }
            else
            {
                Returned_Grade = 0;
            }

            if(Returned_Grade < Min_Grade)
            {
                Returned_Grade = Min_Grade;
            }

            return Returned_Grade;
        }

        private ItemType Get_Item_Type(int itemType)
        {
            switch (itemType)
            {
                case 1:
                    {
                        return ItemType.Armor;
                    }
                case 6:
                    {
                        return ItemType.Weapon;
                    }
                default:
                    {
                        return ItemType.Other;
                    }
            }
        }

        public Dictionary<string, string> Option_Generate(int item_type, int Item_Grade)
        {
            Dictionary<string, string> Option_info = new Dictionary<string, string>();
            ItemType Type = Get_Item_Type(item_type);
            int[] Option_Limits = null;
            int[] Option_List = null;
            int[] Option_Stat = null;
            switch (Type)
            {
                case ItemType.Armor:
                    {
                        Option_Limits = Armor_Option_Limit;
                        Option_List = Armor_Option_List;
                        Option_Stat = Armor_Option_Stat;
                        break;
                    }
                case ItemType.Weapon:
                    {
                        Option_Limits = Weapon_Option_Limit;
                        Option_List = Weapon_Option_List;
                        Option_Stat = Weapon_Option_Stat;
                        break;
                    }
                case ItemType.Other:
                    {
                        Option_Limits = Other_Option_Limit;
                        Option_List = Other_Option_List;
                        Option_Stat = Other_Option_Stat;
                        break;
                    }
            }

            int Grade = Item_Grade;

            //Grade = Get_Grade(Min_Grade);

            if (Grade >= 1)
            {
                bool Completed = false;

                while (!Completed)
                {
                    int Randoms = Ra.Next(0, (Option_Limits[Grade]));
                    bool isElementaled = false;

                    if (!Option_info.ContainsKey(Option_List[Randoms].ToString()))
                    {
                        for (int i = 0; i < Weapon_Elemental.Length; i++)
                            if (Option_info.ContainsKey(Weapon_Elemental[i].ToString()))
                                isElementaled = true;

                        if(isElementaled )
                            if(Weapon_Elemental.Contains(Option_List[Randoms]))
                                continue;

                        Option_info.Add(Option_List[Randoms].ToString(), Get_Option_Value(Option_Stat[Randoms], Grade - 1));
                    }

                    if (Option_info.Count == Grade)
                    {
                        Completed = true;
                        continue;
                    }

                }
            }
            return Option_info;
        }

        public Dictionary<string, string> String_To_Option(string Options, string Optionv)
        {
            Dictionary<string, string> Option_info = new Dictionary<string, string>();
            if(Options.Equals("") || Options == null)
            {
                return Option_info;
            }

            string[] Option_List = Options.Split(',');
            string[] Option_Value = Optionv.Split(',');

            for(int i = 0; i< Option_List.Length; i++)
            {
                Option_info.Add(Option_List[i],Option_Value[i]); // 공격력
            }
            return Option_info;
        }

        public Dictionary<string, string> Add_Option(Dictionary<string, string> Option, int item_type, int Item_Grade)
        {
            Dictionary<string, string> Option_info = Option;
            ItemType Type = Get_Item_Type(item_type);
            int[] Option_Limits = null;
            int[] Option_List = null;
            int[] Option_Stat = null;
            switch (Type)
            {
                case ItemType.Armor:
                    {
                        Option_Limits = Armor_Option_Limit;
                        Option_List = Armor_Option_List;
                        Option_Stat = Armor_Option_Stat;
                        break;
                    }
                case ItemType.Weapon:
                    {
                        Option_Limits = Weapon_Option_Limit;
                        Option_List = Weapon_Option_List;
                        Option_Stat = Weapon_Option_Stat;
                        break;
                    }
                case ItemType.Other:
                    {
                        Option_Limits = Other_Option_Limit;
                        Option_List = Other_Option_List;
                        Option_Stat = Other_Option_Stat;
                        break;
                    }
            }

            int Grade = Item_Grade;

            bool Completed = false;

            while (!Completed)
            {
                int Randoms = Ra.Next(0, (Option_Limits[Grade]));
                bool isElementaled = false;

                if (!Option_info.ContainsKey(Option_List[Randoms].ToString()))
                {
                    if (Type == ItemType.Weapon)
                    {
                        for (int i = 0; i < Weapon_Elemental.Length; i++)
                            if (Option_info.ContainsKey(Weapon_Elemental[i].ToString()))
                                isElementaled = true;

                        if (isElementaled)
                            if (Weapon_Elemental.Contains(Option_List[Randoms]))
                                continue;
                    }

                    Option_info.Add(Option_List[Randoms].ToString(), Get_Option_Value(Option_Stat[Randoms], Grade - 1));
                }

                if (Option_info.Count >= Grade+1)
                {
                    Completed = true;
                    continue;
                }
            }

            return Option_info;
        }

        public Dictionary<string, string> Convert_Option(Dictionary<string, string> Option, int item_type, int Item_Grade, int Target_Slot, bool Option_Added)
        {
            Dictionary<string, string> Option_info = Option;
            string Target_Option_Key = Option.Keys.ElementAt(System.Convert.ToInt32(Target_Slot));
            ItemType Type = Get_Item_Type(item_type);
            int[] Option_Limits = null;
            int[] Option_List = null;
            int[] Option_Stat = null;
            switch (Type)
            {
                case ItemType.Armor:
                    {
                        Option_Limits = Armor_Option_Limit;
                        Option_List = Armor_Option_List;
                        Option_Stat = Armor_Option_Stat;
                        break;
                    }
                case ItemType.Weapon:
                    {
                        Option_Limits = Weapon_Option_Limit;
                        Option_List = Weapon_Option_List;
                        Option_Stat = Weapon_Option_Stat;
                        break;
                    }
                case ItemType.Other:
                    {
                        Option_Limits = Other_Option_Limit;
                        Option_List = Other_Option_List;
                        Option_Stat = Other_Option_Stat;
                        break;
                    }
            }

            int Grade = Item_Grade;
            int Slot_Limit = Grade;
            if(Option_Added)
            {
                Slot_Limit += 1;
            }

            bool Completed = false;

            Option_info.Remove(Target_Option_Key);

            while (!Completed)
            {
                int Randoms = Ra.Next(0, (Option_Limits[Grade]));
                bool isElementaled = false;
                if (!Option_info.ContainsKey(Option_List[Randoms].ToString()))
                {
                    for (int i = 0; i < Weapon_Elemental.Length; i++)
                    {
                        if (Option_info.ContainsKey(Weapon_Elemental[i].ToString()))
                            isElementaled = true;
                    }

                    if (isElementaled)
                        if (Weapon_Elemental.Contains(Option_List[Randoms]))
                            continue;

                    Option_info.Add(Option_List[Randoms].ToString(), Get_Option_Value(Option_Stat[Randoms], Grade - 1));
                }

                    if (Option_info.Count == Slot_Limit)
                    {
                        Completed = true;
                        continue;
                    }
            }

            return Option_info;
        }


        public int Upgrade_Percent(int Item_Enc_Grade)
        {
            int[] Percents = { 100, 100, 100, 100, 100, 90, 85, 80, 75, 50, 30, 25, 20, 10, 5 };
            int isSuccess = 0;
            int dummy = Ra.Next(100);

            if (dummy <= Percents[Item_Enc_Grade])
            {
                isSuccess = 1;
            }

            return isSuccess;

        }
    }
}