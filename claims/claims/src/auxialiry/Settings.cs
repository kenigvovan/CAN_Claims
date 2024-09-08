﻿using claims.src.messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using claims.src.part;
using Vintagestory.API.Config;

namespace claims.src.auxialiry
{
    public class Settings
    {
        static int? itemID = null;
        public static HashSet<string> blockedNames;
        public static HashSet<string> blockedCommandsForPrison;
        public static HashSet<string> protectedAnimals;
        public static SortedDictionary<int, CityLevelInfo> cityLevelsDict;
        public static int[] colors;
        public static void loadAll()
        {
            blockedNames = new HashSet<string>();
            blockedCommandsForPrison = new HashSet<string>();
            protectedAnimals = new HashSet<string>();
            cityLevelsDict = new SortedDictionary<int, CityLevelInfo>();
            loadBlockedCommandsForPrison();
            loadProtectedAnimals();
            loadCityLevelsInfo();
            InitColors();
        }
        public static void clearAll()
        {
            //blockedNames.Clear();
            blockedNames = null;
            //blockedCommandsForPrison.Clear();
            blockedCommandsForPrison = null;
            //protectedAnimals.Clear();
            protectedAnimals = null;
            //cityLevelsDict.Clear();
            cityLevelsDict = null;
            colors = null;

        }
        public static bool loadCityLevelsInfo()
        {
            string filePath;
            if (claims.config.PATH_TO_DB_AND_JSON_FILES.Length == 0)
                filePath = @"" + Path.Combine(GamePaths.ModConfig, "city_level_info.json");
            else
                filePath = Path.Combine(claims.config.PATH_TO_DB_AND_JSON_FILES, "city_level_info.json");

            string json = "";
            if (File.Exists(filePath))
            {
                using (StreamReader r = new StreamReader(filePath))
                {
                    json = r.ReadToEnd();
                }
                Dictionary<int, Dictionary<String, Object>> levelsDict = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<String, Object>>>(json);
                try
                {
                    foreach (var it in levelsDict)
                    {
                        cityLevelsDict.Add(it.Key, new CityLevelInfo(int.Parse(it.Value["AmountOfPlots"].ToString()),
                                int.Parse(it.Value["UnconditionalPayment"].ToString()),
                                int.Parse(it.Value["SummonPlots"].ToString()),
                                int.Parse(it.Value["Maxextrachunksbought"].ToString())
                                ));
                    }
                }catch(Exception ex)
                {
                    cityLevelsDict.Clear();
                    createDefaultCityLevels(filePath);
                }
            }
            if (json == "")
            {
                createDefaultCityLevels(filePath);
                using (StreamReader r = new StreamReader(filePath))
                {
                    json = r.ReadToEnd();
                }
            }       
            return true;
        }

        public static CityLevelInfo getCityLevelInfo(int count)
        {
            foreach (int level in cityLevelsDict.Keys.Reverse()) //CHECK
            {
                if(count >= level)
                {
                    return cityLevelsDict[level];
                }
            }
            return cityLevelsDict[1];
        }
        public static void createDefaultCityLevels(string path)
        {
            //plot amount, unconditionalPayment, summon plot
            cityLevelsDict.Add(1, new CityLevelInfo(2, 0, 0, 2));
            cityLevelsDict.Add(2, new CityLevelInfo(4, 0, 0, 4));
            cityLevelsDict.Add(3, new CityLevelInfo(8, 0, 0, 8));
            cityLevelsDict.Add(4, new CityLevelInfo(16, 0, 0, 16));
            cityLevelsDict.Add(8, new CityLevelInfo(24, 0, 1, 24));
            cityLevelsDict.Add(16, new CityLevelInfo(30, 0, 1, 30));
            cityLevelsDict.Add(20, new CityLevelInfo(38, 0, 1, 38));
            cityLevelsDict.Add(24, new CityLevelInfo(44, 0, 1, 44));
            cityLevelsDict.Add(30, new CityLevelInfo(50, 0, 2, 50));
            cityLevelsDict.Add(36, new CityLevelInfo(56, 0, 2, 56));
            using (StreamWriter r = new StreamWriter(path))
            {
                string b = JsonConvert.SerializeObject(cityLevelsDict, Formatting.Indented);
                r.WriteLine(b);
            }
        }     
        public static void loadBlockedNames()
        {
            foreach (var it in claims.config.BLOCKED_NAMES)
            {
                blockedNames.Add(it);
            }
        }
        public static void loadBlockedCommandsForPrison()
        {
            foreach(var it in claims.config.BLOCKED_COMMANDS_PRISON)
            {
                blockedCommandsForPrison.Add(it);
            }
        }
        public static void loadProtectedAnimals()
        {
            foreach(string it in claims.config.PROTECTED_MOB_TYPES)
            {
                if(it.Trim().Length == 0)
                {
                    continue;
                }
                protectedAnimals.Add(it.Trim());
            }
        }
        public static bool isPvpTime()
        {
            float hoursNow = claims.sapi.World.Calendar.HourOfDay;
            if(claims.config.PVP_TIME_START < hoursNow && hoursNow < claims.config.PVP_TIME_END)
            {
                return true;
            }
            return false;
        }
        public static int getMaxNumberOfPlotForCity(City city)
        {
            CityLevelInfo cityLevel = getCityLevelInfo(city.getCityCitizens().Count);
            return city.getBonusPlots() + cityLevel.AmountOfPlots;
        }
        public static int getMaxNumberOfExtraChunksBought(City city)
        {
            CityLevelInfo cityLevel = getCityLevelInfo(city.getCityCitizens().Count);
            return cityLevel.Maxextrachunksbought;
        }

        public static void InitColors()
        {
            HashSet<int> tmpSet = new HashSet<int>();
            foreach(var it in claims.config.CITY_PLOTS_COLOR_AVAILABLE_COLORS_GUI)
            {
                ColorHandling.tryFindColor(it, out var colorInt);
                if(colorInt != 0)
                {
                    tmpSet.Add(colorInt);
                }
            }
            colors = tmpSet.ToArray();
        }
    }
}
