﻿using claims.src.auxialiry.innerclaims;
using claims.src.auxialiry;
using claims.src.messages;
using claims.src.part.structure;
using claims.src.part;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using System.Security.Cryptography;
using Vintagestory.API.Util;
using claims.src.clientMapHandling;
using Vintagestory.API.Common;
using System.Security.Policy;
using Vintagestory.API.Config;
using System.Drawing;
using Newtonsoft.Json;
using Cairo;
using claims.src.part.structure.plots;
using claims.src.playerMovements;
using claims.src.perms.type;
using claims.src.gui.playerGui.structures;
using claims.src.auxialiry.claimAreas;

namespace claims.src
{
    public class DataStorage
    {
        protected Dictionary<string, int> partToColor = new Dictionary<string, int>();
        protected Dictionary<string, int> tmpColors = new Dictionary<string, int> ();
        protected ConcurrentDictionary<string, Prison> guidToPrisonDict = new ConcurrentDictionary<string, Prison>();

        protected ConcurrentDictionary<string, CityPlotsGroup> guidToCityPlotsGroupDict = new ConcurrentDictionary<string, CityPlotsGroup>();

        protected ConcurrentDictionary<string, City> guidToCityDict = new ConcurrentDictionary<string, City>();
        protected ConcurrentDictionary<string, City> nameToCityDict = new ConcurrentDictionary<string, City>();

        protected ConcurrentDictionary<string, PlayerInfo> nameToPlayerDict = new ConcurrentDictionary<string, PlayerInfo>();
        protected ConcurrentDictionary<string, PlayerInfo> uidToPlayerDict = new ConcurrentDictionary<string, PlayerInfo>();

        protected ConcurrentDictionary<PlotPosition, Plot> claimedPlots = new ConcurrentDictionary<PlotPosition, Plot>();
        protected Dictionary<Vec2i, HashSet<Plot>> plotsZones = new Dictionary<Vec2i, HashSet<Plot>>();
        protected Dictionary<string, ClaimsChatType> mapPlayerChat = new Dictionary<string, ClaimsChatType>();

        protected ConcurrentDictionary<string, Vec3i> playersPositions = new ConcurrentDictionary<string, Vec3i>();
        public Random r = new Random();

        protected Dictionary<string, InnerClaimRecord> innerClaimRecords = new Dictionary<string, InnerClaimRecord>();
        protected WorldInfo world = null;
        protected Dictionary<Vec2i, ServerZoneInfo> PlotZones = new Dictionary<Vec2i, ServerZoneInfo>();
        public Dictionary<Vec2i, long> serverZonesTimestamps = new Dictionary<Vec2i, long>();
        public Dictionary<long, ClaimArea> claimAreas = new Dictionary<long, ClaimArea>();
        //CLIENT SIDE
        //############################################
        //############################################

        //just zone and all plots in it
        //and timestamp for it
        protected Dictionary<Vec2i, ClientSavedZone> ClientSavedPlotsInZones;
        public ClientPlayerInfo clientPlayerInfo { get; set; }

        //zone pos and timestamp when we get info about it last time
        //will be used by client when it enters new zone and send to server this timestamps
        //where server will decide if it needs to resend more relevant info about zone or client is ok
        //protected Dictionary<Vec2i, long> zoneReceivedTimestamp;
        public DataStorage(bool serverSide = true)
        {
            if (serverSide)
            {
                claimsPlayerGroup = claims.sapi.Groups.GetPlayerGroupByName(claims.config.CHAT_WINDOW_NAME);
                if (claimsPlayerGroup == null)
                {
                    claimsPlayerGroup = new PlayerGroup()
                    {
                        Name = claims.config.CHAT_WINDOW_NAME,
                        OwnerUID = null
                    };
                    claims.sapi.Groups.AddPlayerGroup(claimsPlayerGroup);
                    claimsPlayerGroup.Md5Identifier = GameMath.Md5Hash(claimsPlayerGroup.Uid.ToString() + "null");
                }
            }
            else
            {              
                ClientSavedPlotsInZones = new Dictionary<Vec2i, ClientSavedZone>();
                clientPlayerInfo = new ClientPlayerInfo();
            }
        }
        /*==============================================================================================*/
        /*=====================================PLOTS====================================================*/
        /*==============================================================================================*/
        public bool getPlot(PlotPosition plotPosition, out Plot plot)
        {
            if (claimedPlots.TryGetValue(plotPosition, out plot))
            {
                return true;
            }
            return false;
        }
        public bool addClaimedPlot(PlotPosition location, Plot plot)
        {          
            return claimedPlots.TryAdd(location, plot) && addPlotToZoneSet(plot);
        }
        public bool removeClaimedPlot(PlotPosition location)
        {          
            return claimedPlots.TryRemove(location, out Plot removedPlot) && removePlotFromZoneSet(removedPlot);
        }
        public ConcurrentDictionary<PlotPosition, Plot> getClaimedPlots()
        {
            return claimedPlots;
        }
        private static PlayerGroup claimsPlayerGroup = null;
        public PlayerGroup getModChatGroup()
        {
            return claimsPlayerGroup;
        }

        /*==============================================================================================*/
        /*=====================================PLAYERS==================================================*/
        /*==============================================================================================*/
        public bool getPlayerByUid(string uid, out PlayerInfo playerInfo)
        {
            if (uidToPlayerDict.TryGetValue(uid, out playerInfo))
            {
                return true;
            }
            return false;
        }
        public bool addPlayer(PlayerInfo player)
        {
            if (uidToPlayerDict.TryAdd(player.Guid, player))
            {
                return nameToPlayerDict.TryAdd(player.GetPartName(), player);
            }
            return false;
        }
        public bool getPlayerByName(string name, out PlayerInfo playerInfo)
        {
            if (nameToPlayerDict.TryGetValue(name, out playerInfo))
            {
                return true;
            }
            return false;
        }
        public ConcurrentDictionary<string, PlayerInfo> getPlayersDict()
        {
            return uidToPlayerDict;
        }


        /*==============================================================================================*/
        /*=====================================CITY=====================================================*/
        /*==============================================================================================*/
        public bool getCityByName(string name, out City city)
        {
            if (nameToCityDict.TryGetValue(name, out city))
            {
                return true;
            }
            return false;
        }
        public bool getCityByGUID(string cityGUID, out City city)
        {
            if (guidToCityDict.TryGetValue(cityGUID, out  city))
            {
                return true;
            }
            return false;
        }
        public bool removeCityByGUID(string cityGUID)
        {
            if(guidToCityDict.TryRemove(cityGUID, out City city))
            {
                return nameToCityDict.TryRemove(city.GetPartName(), out _);
            }
            return false;
        }
        public bool addCity(City city)
        {
            if (guidToCityDict.TryAdd(city.Guid, city))
            {
                return nameToCityDict.TryAdd(city.GetPartName(), city);
            }
            return false;
        }
        public bool cityExistsByGUID(string guid)
        {
            return guidToCityDict.ContainsKey(guid);
        }
        public bool cityExistsByName(string name)
        {
            return nameToCityDict.ContainsKey(name);
        }
        public bool hasCityByPlayerGUID(string guid)
        {
            if(uidToPlayerDict.TryGetValue(guid, out PlayerInfo player))
            {
                return player.hasCity();
            }
            return false;
        }
        public City getCityByPlayerGUID(string guid)
        {
            if (uidToPlayerDict.TryGetValue(guid, out PlayerInfo player))
            {
                return player.City;
            }
            return null;
        }
        public List<City> getCitiesList()
        {
            return guidToCityDict.Values.ToList();
        }
        public HashSet<string> getAllCitiesGUIDS()
        {
            HashSet<string> listGUIDS = new HashSet<string>();
            foreach (City city in guidToCityDict.Values)
            {
                listGUIDS.Add(city.Guid);
            }
            return listGUIDS;
        }
        public bool changeCityName(City city, string newName)
        {
            nameToCityDict.TryRemove(city.GetPartName(), out city);
            return nameToCityDict.TryAdd(newName, city);
        }

        /*==============================================================================================*/
        /*=====================================PRISON===================================================*/
        /*==============================================================================================*/
        public bool removePrison(string guid)
        {
            return guidToPrisonDict.TryRemove(guid, out _);
        }
        public bool addPrison(Prison prison)
        {
            return guidToPrisonDict.TryAdd(prison.Guid, prison);
        }
        public bool getPrison(string guid, out Prison prison)
        {
            return this.guidToPrisonDict.TryGetValue(guid, out prison);
        }
        public bool resetPlayerCacheByGUID(string playerGUID)
        {
            if (this.uidToPlayerDict.TryGetValue(playerGUID, out PlayerInfo playerInfo))
            {
                playerInfo.PlayerCache.Reset();
                return true;
            }
            return false;
        }
        public bool prisonExistsByGUID(string guid)
        {
            return guidToPrisonDict.ContainsKey(guid);
        }

        public void ClientSetCityPlotsColor(string cityName, int color)
        {
            partToColor[cityName] = color;           
        }
        public int ClientGetCityColor(string cityName)
        {
            if(partToColor.TryGetValue(cityName, out int color))
            {
                return color;
            }
            return -14425100;
        }
        public Dictionary<string, int> getTmpColorDict()
        {
            return tmpColors;
        }
        public bool ClientSetCityNameToColorDict(Dictionary<string, int> val)
        {
            partToColor = val;
            return true;
        }

        /*==============================================================================================*/
        /*=====================================WORLD====================================================*/
        /*==============================================================================================*/
        public WorldInfo getWorldInfo() 
        {
            return world;
        }
        public WorldInfo setWorldInfo(WorldInfo worldInfo)
        {
            return world = worldInfo;
        }
        //PLAYER POS
        public bool getLastPlayerPos(string uid, out Vec3i vec)
        {
            return playersPositions.TryGetValue(uid, out vec);  
        }
        public bool setLastPlayerPos(string uid, Vec3i vec)
        {
            return playersPositions.TryAdd(uid, vec);
        }
        public ConcurrentDictionary<string, Vec3i> getPositionsDict()
        {
            return playersPositions;
        }
        public Dictionary<string, ClaimsChatType> getPlayerChatDict()
        {
            return mapPlayerChat;
        }
        public bool addToPlayerChatDict(string uid, ClaimsChatType type)
        {
            if(mapPlayerChat.ContainsKey(uid))
            {
                return false;
            }
            else
            {
                mapPlayerChat.Add(uid, type);
                return true;
            }
        }

        /*==============================================================================================*/
        /*=====================================PLOTSGROUP================================================*/
        /*==============================================================================================*/
        public ConcurrentDictionary<string, CityPlotsGroup> getCityPlotsGroupsDict()
        {
            return guidToCityPlotsGroupDict;
        }
        public bool addPlotsGroup(CityPlotsGroup val)
        {
            return guidToCityPlotsGroupDict.TryAdd(val.Guid, val);
        }
        public bool getPlotsGroup(string guid, out CityPlotsGroup val)
        {
            return guidToCityPlotsGroupDict.TryGetValue(guid, out val);
        }
        public bool PlotsGroupExistsByGUID(string guid)
        {
            return guidToCityPlotsGroupDict.ContainsKey(guid);
        }
        public bool removePlotsGroup(string guid)
        {
            return guidToCityPlotsGroupDict.TryRemove(guid, out _);
        }
        /*==============================================================================================*/
        /*=====================================INNER CLAIMS=============================================*/
        /*==============================================================================================*/
        public Dictionary<string, InnerClaimRecord> getInnerClaimRecords()
        {
            return innerClaimRecords;
        }

        public bool getInnerClaimRecord(string guid, out InnerClaimRecord val)
        {
            return innerClaimRecords.TryGetValue(guid, out val);
        }
        public bool tryRemoveClaimRecord(string guid)
        {
            return innerClaimRecords.Remove(guid);
        }
        public bool addClaimRecord(string playerUID, InnerClaimRecord val)
        {
            if(innerClaimRecords.ContainsKey(playerUID)) 
            {
                return false;
            }
            innerClaimRecords.Add(playerUID, val);
            return true;
        }

        /*==============================================================================================*/
        /*=====================================SERVER MAP ZONES=========================================*/
        /*==============================================================================================*/
        public bool addPlotToZoneSet(Plot plot)
        {
            var tmpPos = plot.getPos();
            var tmpVec = new Vec2i(tmpPos.X / claims.config.ZONE_PLOTS_LENGTH, tmpPos.Y / claims.config.ZONE_PLOTS_LENGTH);
            if (this.PlotZones.TryGetValue(tmpVec, out ServerZoneInfo plotZone))
            {
                plotZone.timestamp = TimeFunctions.getEpochSeconds();
                return plotZone.zonePlots.Add(plot);
            }
            else
            {
                ServerZoneInfo newZone = new ServerZoneInfo();
                newZone.timestamp = TimeFunctions.getEpochSeconds();
                newZone.zonePlots.Add(plot);
                this.PlotZones.Add(tmpVec, newZone);
                return true;
            }
        }      
        public bool removePlotFromZoneSet(Plot plot)
        {
            if (this.PlotZones.TryGetValue(plot.plotPosition.getPos(), out ServerZoneInfo plotZone))
            {
                plotZone.timestamp = TimeFunctions.getEpochSeconds();
                return plotZone.zonePlots.Remove(plot);
            }
            return false;
        }
        public bool getZone(Vec3d pos, out ServerZoneInfo zone)
        {
            var pp = new Vec2i((int)pos.X / claims.config.MAP_ZONE_SIZE, (int)pos.Z / claims.config.MAP_ZONE_SIZE);
            return PlotZones.TryGetValue(pp, out zone);
        }
        public bool getZone(Vec2i zoneCoords, out ServerZoneInfo zone)
        {
            return PlotZones.TryGetValue(zoneCoords, out zone);
        }
        public void setNowEpochZoneTimestamp(Vec2i vec)
        {
            if (serverZonesTimestamps.ContainsKey(vec))
            {
                serverZonesTimestamps[vec] = TimeFunctions.getEpochSeconds();
            }
        }
        public void setNowEpochZoneTimestampFromPlotPosition(Vec2i plotPositionVec)
        {
            setNowEpochZoneTimestamp(new Vec2i(plotPositionVec.X / claims.config.ZONE_PLOTS_LENGTH, plotPositionVec.Y / claims.config.ZONE_PLOTS_LENGTH));
        }
        public void ResetAllZoneTimestamps()
        {
            foreach(var zoneCoords in PlotZones.Keys)
            {
                serverZonesTimestamps[zoneCoords] = 0;
            }
        }

        /*==============================================================================================*/
        /*=====================================CLIENT FUNCTIONS=========================================*/
        /*==============================================================================================*/
        public void addClientSavedPlots(Vec2i vec, SavedPlotInfo savedPlotInfo)
        {
            if (ClientSavedPlotsInZones.TryGetValue(new Vec2i(vec.X / claims.config.ZONE_PLOTS_LENGTH, vec.Y / claims.config.ZONE_PLOTS_LENGTH),
                out ClientSavedZone clientSavedZone))
            {
                clientSavedZone.addClientSavedPlots(vec, savedPlotInfo);
            }
            else
            {
                ClientSavedPlotsInZones.Add(new Vec2i(vec.X / claims.config.ZONE_PLOTS_LENGTH, vec.Y / claims.config.ZONE_PLOTS_LENGTH),
                    new ClientSavedZone(new Dictionary<Vec2i, SavedPlotInfo> { { vec.Copy(), savedPlotInfo } }));
            }
        }
        public bool removeClientSavedPlots(Vec2i vec)
        {
            if (ClientSavedPlotsInZones.TryGetValue(new Vec2i(vec.X / claims.config.ZONE_PLOTS_LENGTH, vec.Y / claims.config.ZONE_PLOTS_LENGTH),
                out ClientSavedZone clientSavedZone))
            {
                return clientSavedZone.removeClientSavedPlot(vec);
            }
            return false;
        }
        public bool getSavedPlot(Vec2i vec, out SavedPlotInfo savedPlotInfo)
        {
            if (this.ClientSavedPlotsInZones.TryGetValue(new Vec2i(vec.X / claims.config.ZONE_PLOTS_LENGTH, vec.Y / claims.config.ZONE_PLOTS_LENGTH),
                out ClientSavedZone clientSavedZone))
            {
                return clientSavedZone.savedPlots.TryGetValue(vec, out savedPlotInfo);
            }
            savedPlotInfo = null;
            return false;
        }
        public Dictionary<Vec2i, ClientSavedZone> getClientSavedPlots()
        {
            return ClientSavedPlotsInZones;
        }                
        public bool getFlagValue(BlockSelection blockSel, EnumBlockAccessFlags accessType, out string claimant)
        {
            claimant = "claims";
            //find zone with saved plots on client
            Vec2i tmpVec = new Vec2i(blockSel.Position.X / claims.config.ZONE_BLOCKS_LENGTH, blockSel.Position.Z / claims.config.ZONE_BLOCKS_LENGTH);
            if (ClientSavedPlotsInZones.TryGetValue(new Vec2i(blockSel.Position.X / claims.config.ZONE_BLOCKS_LENGTH, blockSel.Position.Z / claims.config.ZONE_BLOCKS_LENGTH),
                out ClientSavedZone clientSavedZone))
            {
                //if zone exists we check if plot on pos exists
                //reuse vec again
                tmpVec.X = blockSel.Position.X / 16;
                tmpVec.Y = blockSel.Position.Z / 16;
                if (clientSavedZone.savedPlots.TryGetValue(tmpVec, out SavedPlotInfo savedPlot))
                {
                    if(savedPlot.clientInnerClaims != null)
                    {
                        foreach (var it in savedPlot.clientInnerClaims)
                        {
                            if (it.Contains(blockSel.Position))
                            {
                                // use, build,  attackAnimals
                                if (accessType == EnumBlockAccessFlags.BuildOrBreak)
                                {
                                    return it.permissionsFlags[1];
                                }
                                else if (accessType == EnumBlockAccessFlags.Use)
                                {
                                    return it.permissionsFlags[0];
                                }
                                return false;
                            }
                        }
                    }
                    //return flag value
                    if (accessType == EnumBlockAccessFlags.BuildOrBreak)
                    {
                        return savedPlot.buildFlag;
                    }
                    else if (accessType == EnumBlockAccessFlags.Use)
                    {
                        return savedPlot.useFlag;
                    }
                }                   
            }
            claimant = "";
            //plot is free, do whatever you want
            return true;
        }
        public void addClientSavedZone(Vec2i vec, ClientSavedZone clientSavedZone)
        {
            ClientSavedPlotsInZones.Add(vec, clientSavedZone);
        }
        public bool getClientSavedZone(Vec2i vec, out ClientSavedZone clientSavedZone)
        {
            clientSavedZone = null;
            return ClientSavedPlotsInZones.TryGetValue(vec, out clientSavedZone);
        }
        
        
        public bool nameForCityOrVillageIsTaken(string name)
        {
            if (cityExistsByName(name))
            {
                return true;
            }
            return false;
        }
        public void resetCacheForAll()
        {
            foreach (var it in claims.sapi.World.AllOnlinePlayers)
            {
               resetPlayerCacheByGUID(it.PlayerUID);
            }
        }
        public void clearCacheForPlayersInPlot(Plot plot)
        {
            foreach (var player in claims.sapi.World.AllOnlinePlayers)
            {
                if (((((int)player.Entity.ServerPos.X / PlotPosition.plotSize)) == plot.getPos().X &&
                    (((int)player.Entity.ServerPos.Z / PlotPosition.plotSize)) == plot.getPos().Y))
                {
                    resetPlayerCacheByGUID(player.PlayerUID);
                }
            }
        }
        public bool checkGuidForCityVillage(string guid)
        {
            if (cityExistsByGUID(guid))
            {
                return false;
            }
            return true;
        }
        public bool plotHasDistantEnoughFromOtherForNewCity(Vec2i pos)
        {
            foreach (City city in getCitiesList())
            {
                foreach (Plot plot in city.getCityPlots())
                {
                    //var o = MathClaims.distanceBetween(plot.getPos(), pos);
                    if (MathClaims.distanceBetween(plot.getPos(), pos) < claims.config.MIN_DISTANCE_FROM_OTHER_CITY_NEW_CITY)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public bool plotHasDistantEnoughFromOtherCities(Plot plot)
        {
            foreach (City city in getCitiesList())
            {
                if (plot.hasCity() && city.Equals(plot.getCity()))
                {
                    continue;
                }

                foreach (Plot plotInner in city.getCityPlots())
                {
                    if (MathClaims.distanceBetween(plotInner.getPos(), plot.getPos()) < claims.config.MIN_DISTANCE_FROM_OTHER_CITY_NEW_CITY)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
