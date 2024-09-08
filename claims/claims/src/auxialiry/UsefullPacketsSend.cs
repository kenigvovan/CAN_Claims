﻿using claims.src.part;
using claims.src.gui.playerGui.structures;
using Newtonsoft.Json;
using System.Collections.Generic;
using Vintagestory.API.Server;
using System.Linq;
using claims.src.network.packets;
using claims.src.delayed.invitations;
using System.Collections.Concurrent;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using claims.src.part.structure;

namespace claims.src.auxialiry
{
    public static class UsefullPacketsSend
    {
        public static ConcurrentDictionary<string, HashSet<EnumPlayerRelatedInfo>> cityDelayedInfoCollector = new ConcurrentDictionary<string, HashSet<EnumPlayerRelatedInfo>>();
        public static ConcurrentDictionary<string, HashSet<EnumPlayerRelatedInfo>> playerDelayedInfoCollector = new ConcurrentDictionary<string, HashSet<EnumPlayerRelatedInfo>>();
        public static void sendAllCitiesColorsToPlayer(IServerPlayer player)
        {
            Dictionary<string, int> cityColors = new Dictionary<string, int>();
            foreach (City cityItem in claims.dataStorage.getCitiesList())
            {
                cityColors.Add(cityItem.GetPartName(), cityItem.cityColor);
            }
            string serializedPlots = JsonConvert.SerializeObject(cityColors);

            claims.serverChannel.SendPacket(new SavedPlotsPacket()
            {
                type = PacketsContentEnum.ALL_CITY_COLORS,
                data = serializedPlots
            }, player);
        }       
        public static void SendPlayerCityRelatedInfo(IServerPlayer player)
        {
            Dictionary<EnumPlayerRelatedInfo, string> collector = new Dictionary<EnumPlayerRelatedInfo, string>();

            if(!claims.dataStorage.getPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return;
            }
            if (playerInfo.hasCity())
            {
                City city = playerInfo.City;
                collector.Add(EnumPlayerRelatedInfo.CITY_NAME, city.GetPartName());
                if (city.getMayor() != null)
                {
                    collector.Add(EnumPlayerRelatedInfo.MAYOR_NAME, city.getMayor().GetPartName());
                }
                collector.Add(EnumPlayerRelatedInfo.CITY_CREATED_TIMESTAMP, city.TimeStampCreated.ToString());
                collector.Add(EnumPlayerRelatedInfo.CITY_MEMBERS, JsonConvert.SerializeObject(StringFunctions.getNamesOfCitizens(city)));
                collector.Add(EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, Settings.getMaxNumberOfPlotForCity(city).ToString());
                collector.Add(EnumPlayerRelatedInfo.CLAIMED_PLOTS, city.getCityPlots().Count().ToString());
                collector.Add(EnumPlayerRelatedInfo.PLAYER_PREFIX, playerInfo.Prefix);
                collector.Add(EnumPlayerRelatedInfo.PLAYER_AFTER_NAME, playerInfo.AfterName);
                collector.Add(EnumPlayerRelatedInfo.PLAYER_CITY_TITLES, JsonConvert.SerializeObject(playerInfo.getCityTitles()));
                collector.Add(EnumPlayerRelatedInfo.CITY_POSSIBLE_RANKS, JsonConvert.SerializeObject(RightsHandler.GetCityRanks()));
                collector.Add(EnumPlayerRelatedInfo.CITY_PLOTS_COLOR, city.cityColor.ToString());

                if (city.isMayor(playerInfo)) { 
                    Dictionary<string, List<string>> rankToCitizens = new Dictionary<string, List<string>>();
                    foreach(var citizen in city.getCityCitizens())
                    {
                        foreach(var title in citizen.getCityTitles())
                        {
                            if(rankToCitizens.TryGetValue(title, out var li))
                            {
                                li.Add(citizen.GetPartName());
                            }
                            else
                            {
                                rankToCitizens.Add(title, new List<string> { citizen.GetPartName() });
                            }
                        }
                    
                    }

                
                
                    collector.Add(EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS, JsonConvert.SerializeObject(rankToCitizens));
                }
                if(playerInfo.PlayerPermissionsHandler.HasPermission(rights.EnumPlayerPermissions.CITY_SEE_BALANCE))
                {
                    collector.Add(EnumPlayerRelatedInfo.CITY_BALANCE, claims.economyHandler.getBalance(city.MoneyAccountName).ToString());
                }
            }

            collector.Add(EnumPlayerRelatedInfo.SHOW_PLOT_MOVEMENT, ((int)playerInfo.showPlotMovement).ToString());
            collector.Add(EnumPlayerRelatedInfo.FRIENDS, JsonConvert.SerializeObject(StringFunctions.getNamesOfFriends(playerInfo)));
            collector.Add(EnumPlayerRelatedInfo.TO_CITY_INVITES, JsonConvert.SerializeObject(InvitationHandler.getInvitesForReceiver(playerInfo)));

            claims.serverChannel.SendPacket(
                new SavedPlotsPacket()
                {
                    data = JsonConvert.SerializeObject(collector),
                    type = PacketsContentEnum.OWN_CITY_INFO_ON_JOIN
                }
                , player);

        }
        public static void SendCityRelatedInfoToAllOnlineCitizensOnPlayerJoinCity(City city, List<string> exceptListPlayersUIDs)
        {
            foreach(var onlinePlayer in city.getOnlineCitizens())
            {
                if(exceptListPlayersUIDs.Contains(onlinePlayer.PlayerUID))
                {
                    continue;
                }
                Dictionary<EnumPlayerRelatedInfo, string> collector = new Dictionary<EnumPlayerRelatedInfo, string>
                {
                    { EnumPlayerRelatedInfo.CITY_MEMBERS, JsonConvert.SerializeObject(StringFunctions.getNamesOfCitizens(city)) },
                    { EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, Settings.getMaxNumberOfPlotForCity(city).ToString() }
                };
                claims.serverChannel.SendPacket(
                    new SavedPlotsPacket()
                    {
                        data = JsonConvert.SerializeObject(collector),
                        type = PacketsContentEnum.ON_SOME_CITY_PARAMS_UPDATED
                    }
                    , onlinePlayer as IServerPlayer);
            }
        }
        public static void SendPlayerRelatedInfoOnCityJoined(PlayerInfo playerInfo)
        {
            Dictionary<EnumPlayerRelatedInfo, string> collector = new Dictionary<EnumPlayerRelatedInfo, string>();
            var player = claims.sapi.World.PlayerByUid(playerInfo.Guid);
            if (player != null)
            {
                if (playerInfo.hasCity())
                {
                    City city = playerInfo.City;
                    collector.Add(EnumPlayerRelatedInfo.CITY_NAME, city.GetPartName());
                    if (city.getMayor() != null)
                    {
                        collector.Add(EnumPlayerRelatedInfo.MAYOR_NAME, city.getMayor().GetPartName());
                    }
                    collector.Add(EnumPlayerRelatedInfo.CITY_CREATED_TIMESTAMP, city.TimeStampCreated.ToString());
                    collector.Add(EnumPlayerRelatedInfo.CITY_MEMBERS, JsonConvert.SerializeObject(StringFunctions.getNamesOfCitizens(city)));
                    collector.Add(EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, Settings.getMaxNumberOfPlotForCity(city).ToString());
                    collector.Add(EnumPlayerRelatedInfo.CLAIMED_PLOTS, city.getCityPlots().Count().ToString());
                    collector.Add(EnumPlayerRelatedInfo.PLAYER_PREFIX, playerInfo.Prefix);
                    collector.Add(EnumPlayerRelatedInfo.PLAYER_AFTER_NAME, playerInfo.AfterName);
                    collector.Add(EnumPlayerRelatedInfo.PLAYER_CITY_TITLES, JsonConvert.SerializeObject(playerInfo.getCityTitles()));
                }
                claims.serverChannel.SendPacket(
                    new SavedPlotsPacket()
                    {
                        data = JsonConvert.SerializeObject(collector),
                        type = PacketsContentEnum.ON_CITY_JOINED
                    }
                    , player as IServerPlayer);
            }
        }
        public static void SendPlayerRelatedInfoOnKickFromCity(PlayerInfo playerInfo)
        {
            var player = claims.sapi.World.PlayerByUid(playerInfo.Guid);
            if (player != null)
            {
                claims.serverChannel.SendPacket(
                    new SavedPlotsPacket()
                    {
                        data = "",
                        type = PacketsContentEnum.ON_KICKED_FROM_CITY
                    }
                    , player as IServerPlayer);
            }
        }
        public static void SendUpdatedConfigValues(IServerPlayer player)
        {
            claims.serverChannel.SendPacket(
                   new ConfigUpdateValuesPacket()
                   {
                       NewCityCost = claims.config.NEW_CITY_COST,
                       NewPlotClaimCost = claims.config.PLOT_CLAIM_PRICE,
                       COINS_VALUES_TO_CODE = claims.config.COINS_VALUES_TO_CODE,
                       ID_TO_COINS_VALUES = claims.config.ID_TO_COINS_VALUES,
                       CITY_NAME_CHANGE_COST = claims.config.CITY_NAME_CHANGE_COST,
                       CITY_BASE_CARE = claims.config.CITY_BASE_CARE,
                       PLOTS_COLORS = Settings.colors
                   }
                   , player);
        }
        public static void AddToQueueCityInfoUpdate(string cityName, params EnumPlayerRelatedInfo[] toUpdate)
        {
            if (cityDelayedInfoCollector.TryGetValue(cityName, out HashSet<EnumPlayerRelatedInfo> cityHashSet))
            {
                foreach (var it in toUpdate)
                {
                    cityHashSet.Add(it);
                }
            }
            else
            {
                cityDelayedInfoCollector.TryAdd(cityName, toUpdate.ToHashSet());
            }
        }
        public static void AddToQueuePlayerInfoUpdate(string playerName, EnumPlayerRelatedInfo toUpdate)
        {
            if (playerDelayedInfoCollector.TryGetValue(playerName, out HashSet<EnumPlayerRelatedInfo> playerHashSet))
            {
                playerHashSet.Add(toUpdate);
            }
            else
            {
                playerDelayedInfoCollector.TryAdd(playerName, new HashSet<EnumPlayerRelatedInfo> { toUpdate });
            }
        }
        public static void SendAllCollectedCityUpdatesToCitizens()
        {
            while(cityDelayedInfoCollector.Count > 0)
            {
                string currentCity = cityDelayedInfoCollector.ElementAt(0).Key;
                if (cityDelayedInfoCollector.Remove<string, HashSet<EnumPlayerRelatedInfo>>(currentCity,
                                                                                                out HashSet<EnumPlayerRelatedInfo> listToUpdate))
                {
                    if(!claims.dataStorage.getCityByGUID(currentCity, out City city))
                    {
                        continue;
                    }
                    var onlinePlayersFromCity = city.getOnlineCitizens();
                    //nobody need this info since nobody from the city is online
                    if(onlinePlayersFromCity.Count == 0) 
                    {
                        continue;
                    }
                    Dictionary<EnumPlayerRelatedInfo, string> collector = new Dictionary<EnumPlayerRelatedInfo, string>();

                    foreach(var relatedInfo in listToUpdate)
                    {
                        switch(relatedInfo)
                        {
                            case EnumPlayerRelatedInfo.CITY_CREATED_TIMESTAMP:
                                collector.Add(EnumPlayerRelatedInfo.CITY_CREATED_TIMESTAMP, city.TimeStampCreated.ToString());
                                break;
                            case EnumPlayerRelatedInfo.CITY_MEMBERS:
                                collector.Add(EnumPlayerRelatedInfo.CITY_MEMBERS, JsonConvert.SerializeObject(StringFunctions.getNamesOfCitizens(city)));
                                break;
                            case EnumPlayerRelatedInfo.MAYOR_NAME:
                                collector.Add(EnumPlayerRelatedInfo.MAYOR_NAME, city.getMayor() != null ? city.getMayor().GetPartName() : "");
                                break;
                            case EnumPlayerRelatedInfo.CITY_NAME:
                                collector.Add(EnumPlayerRelatedInfo.CITY_NAME, city.GetPartName());
                                break;
                            case EnumPlayerRelatedInfo.MAX_COUNT_PLOTS:
                                collector.Add(EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, Settings.getMaxNumberOfPlotForCity(city).ToString());
                                break;
                            case EnumPlayerRelatedInfo.CLAIMED_PLOTS:
                                collector.Add(EnumPlayerRelatedInfo.CLAIMED_PLOTS, city.getCityPlots().Count().ToString());
                                break;
                            case EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS:
                                Dictionary<string, List<string>> rankToCitizens = new Dictionary<string, List<string>>();
                                foreach (var cit in city.getCityCitizens())
                                {
                                    foreach (var title in cit.getCityTitles())
                                    {
                                        if (rankToCitizens.TryGetValue(title, out var li))
                                        {
                                            li.Add(cit.GetPartName());
                                        }
                                        else
                                        {
                                            rankToCitizens.Add(title, new List<string> { cit.GetPartName() });
                                        }
                                    }

                                }
                                collector.Add(EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS, JsonConvert.SerializeObject(rankToCitizens));
                                break;
                            case EnumPlayerRelatedInfo.CITY_BALANCE:
                                collector.Add(EnumPlayerRelatedInfo.CITY_BALANCE, claims.economyHandler.getBalance(city.MoneyAccountName).ToString());
                                break;
                        }
                    }
                    //collector now contains only general info for all citizens

                    foreach (var citizen in onlinePlayersFromCity)
                    {
                        Dictionary<EnumPlayerRelatedInfo, string> playerCollector = new Dictionary<EnumPlayerRelatedInfo, string>();
                        if (playerDelayedInfoCollector.Remove(citizen.PlayerName, out HashSet<EnumPlayerRelatedInfo> hashSet))
                        {
                            if (claims.dataStorage.getPlayerByUid(citizen.PlayerUID, out PlayerInfo playerInfo))
                            {
                                foreach (var relatedInfo in hashSet)
                                {
                                    switch (relatedInfo)
                                    {
                                        case EnumPlayerRelatedInfo.PLAYER_PERMISSIONS:
                                            playerCollector.Add(EnumPlayerRelatedInfo.PLAYER_PERMISSIONS, JsonConvert.SerializeObject(playerInfo.PlayerPermissionsHandler.GetPermissions()));
                                            break;
                                        case EnumPlayerRelatedInfo.PLAYER_PREFIX:
                                            playerCollector.Add(EnumPlayerRelatedInfo.PLAYER_PREFIX, playerInfo.Prefix);
                                            break;
                                        case EnumPlayerRelatedInfo.PLAYER_AFTER_NAME:
                                            playerCollector.Add(EnumPlayerRelatedInfo.PLAYER_AFTER_NAME, playerInfo.AfterName);
                                            break;
                                        case EnumPlayerRelatedInfo.PLAYER_CITY_TITLES:
                                            playerCollector.Add(EnumPlayerRelatedInfo.PLAYER_CITY_TITLES, JsonConvert.SerializeObject(playerInfo.getCityTitles()));
                                            break;
                                        case EnumPlayerRelatedInfo.FRIENDS:
                                            playerCollector.Add(EnumPlayerRelatedInfo.FRIENDS, JsonConvert.SerializeObject(StringFunctions.getNamesOfFriends(playerInfo)));
                                            break;                                        
                                    }
                                }
                                //TODO Rewrite delayed packets
                                if(!playerInfo.PlayerPermissionsHandler.HasPermission(rights.EnumPlayerPermissions.CITY_SEE_BALANCE))
                                {
                                    collector.Remove(EnumPlayerRelatedInfo.CITY_BALANCE);
                                }
                            }
                        }

                        
                        claims.serverChannel.SendPacket(
                            new SavedPlotsPacket()
                            {
                                data = JsonConvert.SerializeObject(playerCollector.Count > 0 ? playerCollector.Union(collector) : collector),
                                type = PacketsContentEnum.ON_SOME_CITY_PARAMS_UPDATED
                            }
                            , citizen as IServerPlayer);                      
                    }
                }

            }

            while(playerDelayedInfoCollector.Count > 0)
            {
                string currentPlayerUid = playerDelayedInfoCollector.ElementAt(0).Key;
                if (playerDelayedInfoCollector.Remove<string, HashSet<EnumPlayerRelatedInfo>>(currentPlayerUid,
                                                                                                out HashSet<EnumPlayerRelatedInfo> listToUpdate))
                {
                    Dictionary<EnumPlayerRelatedInfo, string> collector = new Dictionary<EnumPlayerRelatedInfo, string>();

                    if (claims.dataStorage.getPlayerByUid(currentPlayerUid, out PlayerInfo playerInfo))
                    {
                        foreach (var relatedInfo in listToUpdate)
                        {
                            switch (relatedInfo)
                            {
                                case EnumPlayerRelatedInfo.PLAYER_PERMISSIONS:
                                    collector.Add(EnumPlayerRelatedInfo.PLAYER_PERMISSIONS, JsonConvert.SerializeObject(playerInfo.PlayerPermissionsHandler.GetPermissions()));
                                    break;
                                case EnumPlayerRelatedInfo.PLAYER_PREFIX:
                                    collector.Add(EnumPlayerRelatedInfo.PLAYER_PREFIX, playerInfo.Prefix);
                                    break;
                                case EnumPlayerRelatedInfo.PLAYER_AFTER_NAME:
                                    collector.Add(EnumPlayerRelatedInfo.PLAYER_AFTER_NAME, playerInfo.AfterName);
                                    break;
                                case EnumPlayerRelatedInfo.PLAYER_CITY_TITLES:
                                    collector.Add(EnumPlayerRelatedInfo.PLAYER_CITY_TITLES, JsonConvert.SerializeObject(playerInfo.getCityTitles()));
                                    break;
                                case EnumPlayerRelatedInfo.FRIENDS:
                                    collector.Add(EnumPlayerRelatedInfo.FRIENDS, JsonConvert.SerializeObject(StringFunctions.getNamesOfFriends(playerInfo)));
                                    break;
                                case EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS:
                                    if(!playerInfo.hasCity())
                                    {
                                        break;
                                    }
                                    Dictionary<string, List<string>> rankToCitizens = new Dictionary<string, List<string>>();
                                    foreach (var cit in playerInfo.City.getCityCitizens())
                                    {
                                        foreach (var title in cit.getCityTitles())
                                        {
                                            if (rankToCitizens.TryGetValue(title, out var li))
                                            {
                                                li.Add(cit.GetPartName());
                                            }
                                            else
                                            {
                                                rankToCitizens.Add(title, new List<string> { cit.GetPartName() });
                                            }
                                        }

                                    }
                                    collector.Add(EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS, JsonConvert.SerializeObject(rankToCitizens));

                                    break;
                            }
                        }
                        if (collector.Count > 0)
                        {
                            var player = claims.sapi.World.PlayerByUid(currentPlayerUid);
                            if (player != null)
                            {
                                claims.serverChannel.SendPacket(
                                   new SavedPlotsPacket()
                                   {
                                       data = JsonConvert.SerializeObject(collector),
                                       type = PacketsContentEnum.ON_SOME_CITY_PARAMS_UPDATED
                                   }
                                   , player as IServerPlayer);
                            }
                        }
                    }
                }
            }
        }
        public static void SendCurrentPlotUpdate(IServerPlayer player, Plot plot)
        {
            CurrentPlotInfo cpi = new CurrentPlotInfo(plot.GetPartName(), plot.getPlotOwner()?.GetPartName() ?? "",
                plot.getType(), plot.getCustomTax(), plot.getPrice(), plot.getPermsHandler(), plot.extraBought, plot.getPos());
            string serializedZones = JsonConvert.SerializeObject(cpi);

            claims.serverChannel.SendPacket(new SavedPlotsPacket()
            {
                type = PacketsContentEnum.CURRENT_PLOT_INFO,
                data = serializedZones

            }, player);            
        }

    }
}
