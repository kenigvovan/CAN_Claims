﻿using claims.src.auxialiry;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.database
{

    public class SQLiteDatabaseHanlder : DatabaseHandler
    {
        ConcurrentQueue<QuerryInfo> queryQueue = new ConcurrentQueue<QuerryInfo>();
        private SqliteConnection SqliteConnection = null;

        public SQLiteDatabaseHanlder() : base()
        {
            string folderPath;
            if (claims.config.PATH_TO_DB_AND_JSON_FILES.Length == 0)
            {
                folderPath = @"" + Path.Combine(GamePaths.ModConfig, claims.config.DB_NAME);
            }
            else
            {
                folderPath = Path.Combine(claims.config.PATH_TO_DB_AND_JSON_FILES, claims.config.DB_NAME);
            }

            folderPath.Replace(@"\\", @"\");

            claims.sapi.Logger.Debug("[claims] db path is " + folderPath);

            SqliteConnection = new SqliteConnection(@"Data Source=" + folderPath);
            if (SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }

            if (!initializeTables())
            {
                claims.sapi.Logger.Error("[claims] SQLiteDatabaseHanlder::initializeTables error.");
            }

            claims.sapi.Event.Timer((() =>
            {
                while (!this.queryQueue.IsEmpty)
                {
                    QuerryInfo query;
                    this.queryQueue.TryDequeue(out query);

                    if (query.action == QuerryType.UPDATE)
                    {
                        updateDatabase(query);
                    }
                    else if (query.action == QuerryType.INSERT)
                    {
                        insertToDatabase(query);
                    }
                    else
                    {
                        deleteFromDatabase(query);
                    }
                }
            }
            ), 0.5);
        }
        public SqliteConnection getConnection()
        {
            return SqliteConnection;
        }

        public bool initializeTables()
        {
            try
            {
                //CITY
                SqliteCommand command = new SqliteCommand(SQLiteTables.cityTable, SqliteConnection);
                command.ExecuteNonQuery();
                //PLAYER
                command = new SqliteCommand(SQLiteTables.playerTable, SqliteConnection);
                command.ExecuteNonQuery();

                //PLOT
                command = new SqliteCommand(SQLiteTables.plotTable, SqliteConnection);
                command.ExecuteNonQuery();

                //CITYPLOTGROUP
                command = new SqliteCommand(SQLiteTables.plotGroupTable, SqliteConnection);
                command.ExecuteNonQuery();

                //WORLD
                command = new SqliteCommand(SQLiteTables.worldTable, SqliteConnection);
                command.ExecuteNonQuery();

                //PRISON
                command = new SqliteCommand(SQLiteTables.prisonsTable, SqliteConnection);
                command.ExecuteNonQuery();

                try
                {
                    command.CommandText = "SELECT templerespawnpoints FROM CITIES LIMIT 1";
                    command.ExecuteNonQuery();

                }
                catch (Exception e)
                {
                    command.CommandText = "ALTER TABLE CITIES ADD COLUMN templerespawnpoints TEXT DEFAULT \"\"";
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                claims.sapi.Logger.Error("initializeTables error." + ex.Message);
                return false;
            }

            return true;
        }

        public bool updateDatabase(QuerryInfo querry)
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            string querryString = "";
            switch (querry.targetTable)
            {
                case "CITIES":
                    querryString = QuerryTemplates.UPDATE_CITY;
                    break;
                case "PLAYERS":
                    querryString = QuerryTemplates.UPDATE_PLAYER;
                    break;
                case "CITYPLOTSGROUP":
                    querryString = QuerryTemplates.UPDATE_CITYPLOTGROUP;
                    break;
                case "PRISONS":
                    querryString = QuerryTemplates.UPDATE_PRISON;
                    break;
                case "WORLDS":
                    querryString = QuerryTemplates.UPDATE_WORLD;
                    break;
                case "PLOTS":
                    querryString = QuerryTemplates.UPDATE_PLOT;
                    break;
            }


            int rowsChanged;
            using (var cmd = new SqliteCommand(querryString, SqliteConnection))
            {
                foreach (var pair in querry.parameters)
                {
                    cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                }
                rowsChanged = cmd.ExecuteNonQuery();
            }
            if (rowsChanged == 0)
            {
                querry.action = QuerryType.INSERT;
                queryQueue.Enqueue(querry);
            }
            return true;
        }

        public bool deleteFromDatabase(QuerryInfo querry)
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            string querryString = "";
            switch (querry.targetTable)
            {
                case "CITIES":
                    querryString = QuerryTemplates.DELETE_CITY;
                    break;
                case "PLAYERS":
                    querryString = QuerryTemplates.DELETE_PLAYER;
                    break;
                case "CITYPLOTSGROUP":
                    querryString = QuerryTemplates.DELETE_CITYPLOTGROUP;
                    break;
                case "PRISONS":
                    querryString = QuerryTemplates.DELETE_PRISON;
                    break;
                case "WORLDS":
                    querryString = QuerryTemplates.DELETE_WORLD;
                    break;
                case "PLOTS":
                    querryString = QuerryTemplates.DELETE_PLOT;
                    break;
            }


            int rowsChanged;
            using (var cmd = new SqliteCommand(querryString, SqliteConnection))
            {
                foreach (var pair in querry.parameters)
                {
                    cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                }
                rowsChanged = cmd.ExecuteNonQuery();
            }

            return rowsChanged > 0;

        }

        public bool insertToDatabase(QuerryInfo querry)
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            string querryString = "";
            switch (querry.targetTable)
            {
                case "CITIES":
                    querryString = QuerryTemplates.INSERT_CITY;
                    break;
                case "PLAYERS":
                    querryString = QuerryTemplates.INSERT_PLAYER;
                    break;
                case "CITYPLOTSGROUP":
                    querryString = QuerryTemplates.INSERT_CITYPLOTGROUP;
                    break;
                case "PRISONS":
                    querryString = QuerryTemplates.INSERT_PRISON;
                    break;
                case "WORLDS":
                    querryString = QuerryTemplates.INSERT_WORLD;
                    break;
                case "PLOTS":
                    querryString = QuerryTemplates.INSERT_PLOT;
                    break;
            }


            int rowsChanged;
            using (var cmd = new SqliteCommand(querryString, SqliteConnection))
            {
                foreach (var pair in querry.parameters)
                {
                    cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                }
                rowsChanged = cmd.ExecuteNonQuery();
            }

            return rowsChanged > 0;
        }

        public DataTable readFromDatabase(string querry, Dictionary<string, object> dict)
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            using (var cmd = new SqliteCommand(querry, SqliteConnection))
            {
                foreach (KeyValuePair<string, object> entry in dict)
                {
                    cmd.Parameters.AddWithValue(entry.Key, entry.Value);
                }
                var dt = new DataTable();
                using (var reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                    return dt;
                }
            }
        }

        public DataTable readFromDatabaseWithoutID(string querry)
        {
            using (var cmd = new SqliteCommand(querry, SqliteConnection))
            {
                var dt = new DataTable();
                using (var reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                    return dt;
                }
            }
        }


        //////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////
        //PLAYER
        public override bool savePlayerInfo(PlayerInfo player, bool update = true)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@name", player.GetPartName() },
                { "@uid", player.Guid},
                { "@timestampfirstjoined", player.TimeStampFirstJoined},
                { "@timestamplastonline", player.TimeStampLasOnline},
                { "@comrades", string.Join(";", StringFunctions.concatStringsWithDelim(player.Friends, ';'))},
                { "@city", player.hasCity() ? player.City.Guid : ""},
                { "@citytitles", string.Join(";", player.getCityTitles().ToArray()) },
                { "@title", player.Prefix},
                { "@aftername", player.AfterName },
                { "@perms", player.PermsHandler.ToString() },
                { "@prisonguid", player.isPrisoned() ? player.PrisonedIn.Guid : "" },
                { "@prisonhoursleft", player.PrisonHoursLeft }

            };

            queryQueue.Enqueue(new QuerryInfo("PLAYERS", update ? QuerryType.UPDATE : QuerryType.INSERT, tmpDict));
            return true;
        }

        public override bool deleteFromDatabasePlayerInfo(PlayerInfo player)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object>
            {
                {"@uid",player.Guid }
            };
            queryQueue.Enqueue(new QuerryInfo("PLAYERS", QuerryType.DELETE, tmpDict));
            return true;
        }

        public override bool loadPlayerInfo(DataRow it)
        {
            claims.dataStorage.getPlayerByUid(it["uid"].ToString(), out PlayerInfo tmp);
            tmp.TimeStampFirstJoined = long.Parse(it["timestampfirstjoined"].ToString());
            tmp.TimeStampLasOnline = long.Parse(it["timestamplastonline"].ToString());
            foreach (string str in it["comrades"].ToString().Split(';'))
            {
                if (str.Length == 0)
                    continue;

                claims.dataStorage.getPlayerByUid(str, out PlayerInfo plTmp);
                tmp.addComrade(plTmp);
            }
            if (it["city"].ToString().Length != 0)
            {
                claims.dataStorage.getCityByGUID(it["city"].ToString(), out City city);
                tmp.setCity(city);
                city.getCityCitizens().Add(tmp);
            }
            foreach (string str in it["citytitles"].ToString().Split(';'))
            {
                if (str.Length == 0)
                    continue;
                tmp.addCityTitle(str);
            }
            tmp.Prefix = it["title"].ToString();
            tmp.AfterName = it["aftername"].ToString();
            tmp.setPerms(it["perms"].ToString());
            if (claims.dataStorage.getPrison(it["prisonguid"].ToString(), out Prison prison))
            {
                tmp.PrisonedIn = prison;
                tmp.PrisonHoursLeft = int.Parse(it["prisonhoursleft"].ToString());
            }
            return true;
        }

        public override bool loadAllPlayersInfo()
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            try
            {
                DataTable dt = readFromDatabase("SELECT * FROM PLAYERS", new Dictionary<string, object> { });
                foreach (DataRow it in dt.Rows)
                {
                    loadPlayerInfo(it);
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("loadAllPlayersInfo::error" + e.Message);
                return false;
            }
            return true;
        }

        public override bool loadDummyPlayers()
        {
            MessageHandler.sendDebugMsg("Load dummy players.");
            DataTable dt = readFromDatabase("SELECT name, uid FROM PLAYERS", new Dictionary<string, object> { });
            foreach (DataRow it in dt.Rows)
            {
                PlayerInfo tmp = new PlayerInfo(it["name"].ToString(), it["uid"].ToString());
                claims.dataStorage.addPlayer(tmp);
            }
            return true;
        }
        //CITY
        public override bool loadDummyCitis()
        {
            MessageHandler.sendDebugMsg("Load dummy cities.");
            DataTable dt = readFromDatabase("SELECT name, guid FROM CITIES", new Dictionary<string, object> { });
            foreach (DataRow it in dt.Rows)
            {
                City tmp = new City(it["name"].ToString(), it["guid"].ToString());
                claims.dataStorage.addCity(tmp);
                MessageHandler.sendDebugMsg("Load dummy city: " + tmp.GetPartName());
            }
            return true;
        }

        public override bool saveCity(City city, bool update = true)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@name", city.GetPartName() },
                { "@guid", city.Guid},
                { "@mayor", city.getMayor() == null ? "" : city.getMayor().Guid},
                { "@timestampcreated", city.TimeStampCreated},
                { "@debtbalance", city.DebtBalance},
                { "@perm", city.getPermsHandler().ToString() },
                { "@plotgroups", StringFunctions.concatStringsWithDelim(city.getCityPlotsGroups(), ';') },
                { "@prisons",  StringFunctions.concatStringsWithDelim(city.getPrisons(), ';')},
                { "@defaultplotcost", city.getDefaultPlotCost() },
                { "@invmsg", city.invMsg },
                { "@opencity", city.openCity },
                { "@fee", city.fee },
                { "@criminals", StringFunctions.concatStringsWithDelim(city.getCriminals(), ';') },
                { "@istechnical", city.isTechnicalCity() ? 1 : 0 },
                { "@bonusplots", city.getBonusPlots() },
                { "@extrachunksbought", city.Extrachunksbought },
                { "@citycolor", city.cityColor },
                { "@templerespawnpoints", JsonConvert.SerializeObject(city.TempleRespawnPoints) }
            };

            queryQueue.Enqueue(new QuerryInfo("CITIES", update ? QuerryType.UPDATE : QuerryType.INSERT, tmpDict));
            return true;
        }

        public override bool deleteFromDatabaseCity(City city)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@guid", city.Guid }
            };

            queryQueue.Enqueue(new QuerryInfo("CITIES", QuerryType.DELETE, tmpDict));
            return true;
        }

        public override bool loadCity(DataRow it)
        {
            claims.dataStorage.getCityByGUID(it["guid"].ToString(), out City city);
            city.setIsTechnicalCity(it["istechnical"].ToString().Equals("0") ? false : true);
            if (!city.isTechnicalCity())
            {
                claims.dataStorage.getPlayerByUid(it["mayor"].ToString(), out PlayerInfo playerInfo);
                city.setMayor(playerInfo);
            }
            else
            {
                city.setMayor(null);
            }
            city.TimeStampCreated = long.Parse(it["timestampcreated"].ToString());
            city.DebtBalance = int.Parse(it["debtbalance"].ToString());
            city.getPermsHandler().setPerms(it["perm"].ToString());
            foreach (string str in it["plotgroups"].ToString().Split(';'))
            {
                if (str.Length == 0)
                    continue;
                claims.dataStorage.getPlotsGroup(str, out CityPlotsGroup cityPlotsGroup);
                city.getCityPlotsGroups().Add(cityPlotsGroup);
            }
            foreach (string str in it["prisons"].ToString().Split(';'))
            {
                if (str.Length == 0)
                    continue;
                claims.dataStorage.getPrison(str, out Prison prison);
                if (prison != null)
                    city.getPrisons().Add(prison);
            }
            city.setDefaultPlotCost(int.Parse(it["defaultplotcost"].ToString()));          
            foreach (string str in it["criminals"].ToString().Split(';'))
            {
                if (str.Length == 0)
                    continue;

                claims.dataStorage.getPlayerByUid(str, out PlayerInfo criminalPlayer);
                if (criminalPlayer == null)
                {
                    continue;
                }
                city.getCriminals().Add(criminalPlayer);
            }
            city.fee = int.Parse(it["fee"].ToString());
            city.openCity = it["opencity"].ToString().Equals("0") ? false : true;
            city.setBonusPlots(int.Parse(it["bonusplots"].ToString()));
            city.Extrachunksbought = int.Parse(it["extrachunksbought"].ToString());
            try
            {
                city.cityColor = int.Parse(it["citycolor"].ToString());
            }
            catch (Exception ex)
            {
                MessageHandler.sendDebugMsg("loadCity::exc no color" + city.GetPartName());
            }
            string rPoints = it["templerespawnpoints"].ToString();
            if (rPoints.Length != 0)
            {
                foreach (var rPoint in JsonConvert.DeserializeObject<Dictionary<Vec2i, Vec3i>>(rPoints))
                {
                    city.AddTempleRespawnPoint(rPoint.Key, rPoint.Value);
                }
            }
            MessageHandler.sendDebugMsg("loadCity::load city" + city.GetPartName());
            return true;

        }

        public override bool loadAllCitis()
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            try
            {
                DataTable dt = readFromDatabase("SELECT * FROM CITIES", new Dictionary<string, object> { });
                foreach (DataRow it in dt.Rows)
                {
                    loadCity(it);
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("loadAllCitis::error" + e.Message);
                return false;
            }
            return true;
        }
        //PLOT
        public override bool loadDummyPlots()
        {
            MessageHandler.sendDebugMsg("Load dummy plots.");
            try
            {
                DataTable dt = readFromDatabase("SELECT x, z, city FROM PLOTS", new Dictionary<string, object> { });
                foreach (DataRow it in dt.Rows)
                {
                    if (it["city"].ToString() != "")
                    {
                        claims.dataStorage.getCityByGUID(it["city"].ToString(), out City city);
                        if (city != null)
                        {
                            Plot tmp = new Plot(new Vec2i(int.Parse(it["x"].ToString()), int.Parse(it["z"].ToString())));
                            tmp.setCity(city);
                            claims.dataStorage.addClaimedPlot(new PlotPosition(int.Parse(it["x"].ToString()), int.Parse(it["z"].ToString())), tmp);
                            city.getCityPlots().Add(tmp);
                        }
                        continue;
                    }
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("loadDummyPlots::error" + e.Message);
                return false;
            }
            return true;
        }

        public override bool savePlot(Plot plot, bool update = true)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@name", plot.GetPartName() },
                { "@x", plot.getPos().X },
                { "@z", plot.getPos().Y},
                { "@city", plot.hasCity() ? plot.getCity().Guid :"" },
                { "@ownerofplot", plot.hasPlotOwner() ? plot.getPlotOwner().Guid : ""},
                { "@type", (int)plot.getType() },
                { "@price", plot.getPrice() },
                { "@customtax", plot.getCustomTax() },
                { "@perms", plot.getPermsHandler().ToString() },
                { "@plotgroupguid", plot.hasPlotGroup() ? plot.getPlotGroup().Guid : "" },
                { "@markednopvp", plot.isMarkedNoPVP() },
                { "@plotdesc",  PlotInfo.getPlotDescByType(plot) },
                { "@extraBought", plot.extraBought }
            };

            queryQueue.Enqueue(new QuerryInfo("PLOTS", update ? QuerryType.UPDATE : QuerryType.INSERT, tmpDict));
            return true;
        }

        public override bool deleteFromDatabasePlot(Plot plot)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@x", plot.getPos().X},
                { "@z", plot.getPos().Y }
            };

            queryQueue.Enqueue(new QuerryInfo("PLOTS", QuerryType.DELETE, tmpDict));
            return true;
        }

        public override bool loadPlot(DataRow it)
        {
            claims.dataStorage.getPlot(new PlotPosition(int.Parse(it["x"].ToString()), int.Parse(it["z"].ToString())), out Plot plot);
            if (plot == null)
            {
                MessageHandler.sendErrorMsg("loadPlot at " + it["x"].ToString() + " " + it["z"].ToString() + " failed");
                return false;
            }
            plot.SetPartName(it["name"].ToString());
            if (it["ownerofplot"].ToString().Length != 0)
            {
                claims.dataStorage.getPlayerByUid(it["ownerofplot"].ToString(), out PlayerInfo playerInfo);
                playerInfo.PlayerPlots.Add(plot);
                plot.setPlotOwner(playerInfo);
            }

            plot.setType((PlotType)(int.Parse(it["type"].ToString())));
            plot.setPrice(int.Parse(it["price"].ToString()));
            plot.setCustomTax(int.Parse(it["customtax"].ToString(), CultureInfo.InvariantCulture));
            plot.getPermsHandler().setPerms(it["perms"].ToString());
            if (claims.dataStorage.getCityPlotsGroupsDict().TryGetValue(it["plotgroupguid"].ToString(), out CityPlotsGroup cityPlotsGroup))
                plot.setPlotGroup(cityPlotsGroup);
            plot.setMarkedNoPVP(it["markednopvp"].ToString().Equals("0") ? false : true);
            switch (plot.getType())
            {
                case PlotType.SUMMON:
                    PlotDescSummon tmp = new PlotDescSummon();
                    tmp.fromStringPoint(it["plotdesc"].ToString());
                    plot.setPlotDesc(tmp);
                    plot.getCity().summonPlots.Add(plot);
                    break;
                case PlotType.PRISON:
                    PlotDescPrison tmpPri = new PlotDescPrison(it["plotdesc"].ToString());
                    plot.setPlotDesc(tmpPri);
                    claims.dataStorage.getPrison(tmpPri.getPrisonGuid(), out Prison prison);
                    plot.setPrison(prison);
                    break;
                case PlotType.TAVERN:
                    PlotDescTavern tmpTav = new PlotDescTavern();
                    plot.setPlotDesc(tmpTav);
                    tmpTav.fromLoadStringInnerClaims(it["plotdesc"].ToString());
                    break;
            }
            plot.extraBought = it["extraBought"].ToString().Equals("0") ? false : true;
            return true;
        }

        public override bool loadAllPlots()
        {
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            try
            {
                DataTable dt = readFromDatabase("SELECT * FROM PLOTS", new Dictionary<string, object> { });
                foreach (DataRow it in dt.Rows)
                {
                    loadPlot(it);
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("loadAllPlots::error" + e.Message);
                return false;
            }
            return true;
        }
        //WORLD
        public override bool loadDummyWolrdInfo()
        {
            MessageHandler.sendDebugMsg("Load dummy world.");
            string a = claims.sapi.World.Seed.ToString();
            DataTable dt = readFromDatabase("SELECT * FROM WORLDS", new Dictionary<string, object> { { "@name", claims.sapi.World.Seed.ToString() } });
            foreach (DataRow dr in dt.Rows)
            {
                claims.dataStorage.setWorldInfo(new WorldInfo(dt.Rows[0]["name"].ToString(), dt.Rows[0]["guid"].ToString()));
                var world = claims.dataStorage.getWorldInfo();
                world.SetPartName(dr[0].ToString());
                world.Guid = dt.Rows[0]["guid"].ToString();
                world.fireEverywhere = dr["fireeverywhere"].ToString().Equals("0") ? false : true;
                world.pvpEverywhere = dr["pvpeverywhere"].ToString().Equals("0") ? false : true;
                world.blastEverywhere = dr["blasteverywhere"].ToString().Equals("0") ? false : true;
                world.fireForbidden = dr["fireforbidden"].ToString().Equals("0") ? false : true;
                world.pvpForbidden = dr["pvpforbidden"].ToString().Equals("0") ? false : true;
                world.blastForbidden = dr["blastforbidden"].ToString().Equals("0") ? false : true;
            }
            return true;
        }

        public override bool saveWorldInfo(WorldInfo worldInfo, bool update = true)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@name", worldInfo.GetPartName() },
                { "@guid", worldInfo.Guid },
                { "@pvpeverywhere", worldInfo.pvpEverywhere},
                { "@fireeverywhere", worldInfo.fireEverywhere },
                { "@blasteverywhere", worldInfo.blastEverywhere },
                { "@fireforbidden", worldInfo.fireForbidden },
                { "@pvpforbidden", worldInfo.pvpForbidden },
                { "@blastforbidden", worldInfo.blastForbidden }
            };

            queryQueue.Enqueue(new QuerryInfo("WORLDS", update ? QuerryType.UPDATE : QuerryType.INSERT, tmpDict));
            return true;
        }

        public override bool deleteFromDatabaseWorldInfo(WorldInfo worldInfo)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@guid", worldInfo.Guid}
            };

            queryQueue.Enqueue(new QuerryInfo("WORLDS", QuerryType.DELETE, tmpDict));
            return true;
        }

        public override bool loadWorldInfo(DataRow it)
        {
            throw new NotImplementedException();
        }
        //OTHER
        public override bool saveEveryThing()
        {
            throw new NotImplementedException();
        }

        public ConcurrentQueue<QuerryInfo> getQueue()
        {
            return queryQueue;
        }

        public override bool makeBackup(string fileName)
        {
            string cs;
            if (claims.config.PATH_TO_DB_AND_JSON_FILES.Length == 0)
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    cs = @"" + Path.Combine(GamePaths.ModConfig, fileName);
                }
                else
                {
                    cs = @"" + Path.Combine(GamePaths.ModConfig, fileName);
                }

            }
            else
            {
                cs = claims.config.PATH_TO_DB_AND_JSON_FILES + "/" + fileName;
            }
            try
            {
                using (SqliteConnection dest = new SqliteConnection(@"Data Source=" + cs))
                {
                    dest.Open();
                    this.getConnection().BackupDatabase(dest, "main", "main");
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("[claims] makeBackup::" + fileName + " - " + e.Message);
                return false;
            }
            return true;
        }
        //PRISON
        public override bool savePrison(Prison prison, bool update = true)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@name", prison.GetPartName() },
                { "@guid", prison.Guid },
                { "@x", prison.getPlot().getPos().X },
                { "@z", prison.getPlot().getPos().Y },
                { "@prisonCells", prison.ToString() },
                { "@city", prison.getCity().Guid },
            };

            queryQueue.Enqueue(new QuerryInfo("PRISONS", update ? QuerryType.UPDATE : QuerryType.INSERT, tmpDict));
            return true;
        }

        public override bool deleteFromDatabasePrison(Prison prison)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@guid", prison.Guid}
            };

            queryQueue.Enqueue(new QuerryInfo("PRISONS", QuerryType.DELETE, tmpDict));
            return true;
        }

        public override bool loadPrison(DataRow it)
        {
            claims.dataStorage.getPrison(it["guid"].ToString(), out Prison prison);
            prison.fromString(it["prisonCells"].ToString());
            claims.dataStorage.getCityByGUID(it["city"].ToString(), out City city);
            prison.setCity(city);
            claims.dataStorage.getPlot(new PlotPosition(int.Parse(it["x"].ToString()), int.Parse(it["z"].ToString())), out Plot plot);
            prison.setPlot(plot);
            return true;
        }

        public override bool loadAllPrisons()
        {
            MessageHandler.sendDebugMsg("Load all prisons.");
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            try
            {
                DataTable dt = readFromDatabase("SELECT * FROM PRISONS", new Dictionary<string, object> { });
                foreach (DataRow it in dt.Rows)
                {
                    loadPrison(it);
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("loadAllPrisons::error" + e.Message);
                return false;
            }
            return true;
        }

        public override bool loadDummyPrisons()
        {
            MessageHandler.sendDebugMsg("Load dummy prisons.");
            DataTable dt = readFromDatabase("SELECT guid, city, name, x, z FROM PRISONS", new Dictionary<string, object> { });
            foreach (DataRow it in dt.Rows)
            {
                claims.dataStorage.getCityByGUID(it["city"].ToString(), out City city);
                Prison tmp = new Prison(it["name"].ToString(), it["guid"].ToString());
                claims.dataStorage.addPrison(tmp);
                city.getPrisons().Add(tmp);
            }
            return true;
        }

        //CITYPLOTSGROUP
        public override bool saveCityPlotGroup(CityPlotsGroup plotgroup, bool update = true)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@name", plotgroup.GetPartName() },
                { "@guid", plotgroup.Guid },
                { "@perms", plotgroup.PermsHandler.ToString() },
                { "@players", StringFunctions.concatStringsWithDelim(plotgroup.PlayersList, ';') },
                { "@plotsgroupfee", plotgroup.PlotsGroupFee },
                { "@city", plotgroup.City.Guid}
            };

            queryQueue.Enqueue(new QuerryInfo("CITYPLOTSGROUP", update ? QuerryType.UPDATE : QuerryType.INSERT, tmpDict));
            return true;
        }

        public override bool deleteFromDatabaseCityPlotGroup(CityPlotsGroup plotgroup)
        {
            Dictionary<string, object> tmpDict = new Dictionary<string, object> {
                { "@guid", plotgroup.Guid}
            };

            queryQueue.Enqueue(new QuerryInfo("CITYPLOTSGROUP", QuerryType.DELETE, tmpDict));
            return true;
        }

        public override bool loadCityPlotGroup(DataRow it)
        {
            claims.dataStorage.getCityByGUID(it["city"].ToString(), out City city);
            if (city == null)
            {
                return false;
            }

            //claims.guidToCityPlotsGroupDict.TryGetValue(it["guid"].ToString(), out CityPlotsGroup cityPlotsGroup);
            claims.dataStorage.getCityPlotsGroupsDict().TryGetValue(it["guid"].ToString(), out CityPlotsGroup cityPlotsGroup);
            cityPlotsGroup.City = city;
            claims.dataStorage.addPlotsGroup(cityPlotsGroup);
            cityPlotsGroup.PermsHandler.setPerms(it["perms"].ToString());
            foreach (string str in it["players"].ToString().Split(';'))
            {
                if (str.Length == 0)
                    continue;

                claims.dataStorage.getPlayerByUid(str, out PlayerInfo plTmp);
                cityPlotsGroup.PlayersList.Add(plTmp);
            }

            cityPlotsGroup.PlotsGroupFee = int.Parse(it["plotsgroupfee"].ToString(), CultureInfo.InvariantCulture);
            return true;
        }

        public override bool loadAllCityPlotGroups()
        {
            MessageHandler.sendDebugMsg("Load all cityplotgroups.");
            if (this.SqliteConnection.State != System.Data.ConnectionState.Open)
            {
                SqliteConnection.Open();
            }
            try
            {
                DataTable dt = readFromDatabase("SELECT * FROM CITYPLOTSGROUP", new Dictionary<string, object> { });
                foreach (DataRow it in dt.Rows)
                {
                    loadCityPlotGroup(it);
                }
            }
            catch (SqliteException e)
            {
                MessageHandler.sendErrorMsg("loadAllCityPlotGroups::error" + e.Message);
                return false;
            }
            return true;
        }
        public override bool loadDummyCityPlotGroups()
        {
            DataTable dt = readFromDatabase("SELECT guid, name FROM CITYPLOTSGROUP", new Dictionary<string, object> { });
            foreach (DataRow it in dt.Rows)
            {
                CityPlotsGroup cpg = new CityPlotsGroup(it["name"].ToString(), it["guid"].ToString());
                claims.dataStorage.addPlotsGroup(cpg);
            }
            return true;
        }
    }
}
