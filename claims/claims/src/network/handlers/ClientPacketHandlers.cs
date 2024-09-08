﻿using claims.src.auxialiry;
using claims.src.clientMapHandling;
using claims.src.gui.playerGui.structures;
using claims.src.network.packets;
using claims.src.playerMovements;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.MathTools;

namespace claims.src.network.handlers
{
    public static class ClientPacketHandlers
    {
        public static void RegisterHandlers()
        {
            claims.clientChannel.SetMessageHandler<SavedPlotsPacket>((packet) =>
            {
                //We expect list of saved plots if player entered new zone or just joined
                switch (packet.type)
                {
                    case PacketsContentEnum.ON_JOIN:
                    case PacketsContentEnum.ENTER_ZONE:
                        JsonSerializerSettings settings = new JsonSerializerSettings();
                        settings.Converters.Add(new VecJsonConverter());

                        List<KeyValuePair<Vec2i, List<KeyValuePair<Vec2i, SavedPlotInfo>>>> savedZones = JsonConvert.DeserializeObject<List<KeyValuePair<Vec2i, List<KeyValuePair<Vec2i, SavedPlotInfo>>>>>(packet.data);

                        foreach (var zone in savedZones)
                        {
                            foreach (var plot in zone.Value)
                            {
                                claims.clientDataStorage.addClientSavedPlots(plot.Key, plot.Value);
                                claims.getModInstance().plotsMapLayer.OnResChunkPixels(plot.Key, claims.clientDataStorage.ClientGetCityColor(plot.Value.cityName), plot.Value.cityName);
                            }
                        }
                        break;
                    case PacketsContentEnum.ADD_SINGLE_PLOT:
                        Tuple<Vec2i, SavedPlotInfo> savedPlotTuple = JsonConvert.DeserializeObject<Tuple<Vec2i, SavedPlotInfo>>(packet.data);
                        claims.clientDataStorage.addClientSavedPlots(savedPlotTuple.Item1, savedPlotTuple.Item2);
                        claims.getModInstance().plotsMapLayer.OnResChunkPixels(savedPlotTuple.Item1, claims.clientDataStorage.ClientGetCityColor(savedPlotTuple.Item2.cityName), savedPlotTuple.Item2.cityName);
                        break;
                    case PacketsContentEnum.REMOVE_SINGLE_PLOT:
                        //try to send saved plot as null without creating object
                        Tuple<Vec2i, SavedPlotInfo> savedPlotTupleRemove = JsonConvert.DeserializeObject<Tuple<Vec2i, SavedPlotInfo>>(packet.data);
                        claims.clientDataStorage.removeClientSavedPlots(savedPlotTupleRemove.Item1);
                        claims.getModInstance().plotsMapLayer.OnResChunkPixels(savedPlotTupleRemove.Item1, 0, "");
                        break;
                    case PacketsContentEnum.ALL_CITY_COLORS:
                        Dictionary<string, int> colors = JsonConvert.DeserializeObject<Dictionary<string, int>>(packet.data);
                        claims.clientDataStorage.ClientSetCityNameToColorDict(colors);
                        //claims.getModInstance().plotsMapLayer.RedrawPlots();
                        break;
                    case PacketsContentEnum.SERVER_UPDATED_ZONES_ANSWER:
                        HashSet<Tuple<Vec2i, long, List<KeyValuePair<Vec2i, SavedPlotInfo>>>> updatedZones = JsonConvert.DeserializeObject<HashSet<Tuple<Vec2i, long, List<KeyValuePair<Vec2i, SavedPlotInfo>>>>>(packet.data);
                        //we got new zones info from server
                        foreach (var tup in updatedZones)
                        {
                            //if zone was already known, but updated info from server arrived
                            if (claims.clientDataStorage.getClientSavedZone(tup.Item1, out var savedZone))
                            {
                                claims.getModInstance().plotsMapLayer.clearZoneSavedPlotsFromMap(tup.Item1);
                                savedZone.savedPlots = tup.Item3.ToDictionary(x => x.Key, x => x.Value);
                                savedZone.timestamp = tup.Item2;
                                claims.getModInstance().plotsMapLayer.generateFromZoneSavedPlotsOnMap(tup.Item1);
                            }
                            //no such zone, reset it
                            else
                            {
                                ClientSavedZone newZone = new ClientSavedZone(tup.Item3.ToDictionary(x => x.Key, x => x.Value));
                                newZone.timestamp = tup.Item2;
                                claims.clientDataStorage.addClientSavedZone(tup.Item1, newZone);
                                claims.getModInstance().plotsMapLayer.generateFromZoneSavedPlotsOnMap(tup.Item1);
                            }
                        }
                        break;
                    case PacketsContentEnum.SERVER_REMOVE_COLLECTED_PLOTS:
                        HashSet<Vec2i> plotsToRemove = JsonConvert.DeserializeObject<HashSet<Vec2i>>(packet.data);
                        foreach (var savedPlot in plotsToRemove)
                        {
                            claims.clientDataStorage.removeClientSavedPlots(savedPlot);
                            claims.getModInstance().plotsMapLayer.OnResChunkPixels(savedPlot, 0, "");
                        }
                        break;
                    case PacketsContentEnum.SERVER_UPDATE_COLLECTED_PLOTS:
                        List<Tuple<Vec2i, SavedPlotInfo>> plotsToUpdate = JsonConvert.DeserializeObject<List<Tuple<Vec2i, SavedPlotInfo>>>(packet.data);
                        foreach (var savedPlot in plotsToUpdate)
                        {
                            claims.clientDataStorage.addClientSavedPlots(savedPlot.Item1, savedPlot.Item2);
                        }
                        break;
                    case PacketsContentEnum.OWN_CITY_DELETED:
                        claims.clientDataStorage.clientPlayerInfo.CityInfo = null;
                        if(claims.CANCityGui?.IsOpened() ?? false)
                        {
                            claims.CANCityGui.BuildMainWindow();
                        }
                        break;
                    case PacketsContentEnum.AGREE_NEEDED_ON_NEW_CITY_CREATION:
                        claims.CANCityGui.CreateNewCityState = gui.playerGui.CANClaimsGui.EnumUpperWindowSelectedState.NEED_AGREE;
                        claims.CANCityGui.collectedNewCityName = packet.data;
                        if (claims.CANCityGui?.IsOpened() ?? false)
                        {
                            claims.CANCityGui.BuildMainWindow();
                        }
                        break;
                    case PacketsContentEnum.OWN_CITY_INFO_ON_JOIN:
                    case PacketsContentEnum.OWN_NEW_CITY_CREATED:
                        var cityValuesDict = JsonConvert.DeserializeObject<Dictionary<EnumPlayerRelatedInfo, string>>(packet.data);
                        claims.clientDataStorage.clientPlayerInfo = new ClientPlayerInfo();

                        //if player has city there will be name of it and we need to create instance of lists for cityinfo
                        if(cityValuesDict.ContainsKey(EnumPlayerRelatedInfo.CITY_NAME) && claims.clientDataStorage.clientPlayerInfo.CityInfo == null)
                        {
                            claims.clientDataStorage.clientPlayerInfo.CityInfo = new CityInfo();
                        }
                        claims.clientDataStorage.clientPlayerInfo.AcceptChangedValues(cityValuesDict);
                        if (claims.CANCityGui?.IsOpened() ?? false)
                        {
                            claims.CANCityGui.BuildMainWindow();
                        }
                        break;
                    case PacketsContentEnum.ON_CITY_JOINED:
                        var joinedValues = JsonConvert.DeserializeObject<Dictionary<EnumPlayerRelatedInfo, string>>(packet.data);
                        if(claims.clientDataStorage.clientPlayerInfo.CityInfo == null)
                        {
                            claims.clientDataStorage.clientPlayerInfo.CityInfo = new CityInfo();
                        }
                        claims.clientDataStorage.clientPlayerInfo.AcceptChangedValues(joinedValues);
                        claims.clientDataStorage.clientPlayerInfo.ReceivedInvitations.Clear();
                        if (claims.CANCityGui?.IsOpened() ?? false)
                        {
                            claims.CANCityGui.BuildMainWindow();
                        }
                        break;
                    case PacketsContentEnum.ON_KICKED_FROM_CITY:
                        claims.clientDataStorage.clientPlayerInfo.CityInfo = null;
                        if (claims.CANCityGui?.IsOpened() ?? false)
                        {
                            claims.CANCityGui.BuildMainWindow();
                        }
                        break;
                    case PacketsContentEnum.ON_SOME_CITY_PARAMS_UPDATED:
                        var someUpdateDict = JsonConvert.DeserializeObject<Dictionary<EnumPlayerRelatedInfo, string>>(packet.data);
                        if (claims.clientDataStorage.clientPlayerInfo.CityInfo == null)
                        {
                            claims.clientDataStorage.clientPlayerInfo.CityInfo = new CityInfo();
                        }
                        claims.clientDataStorage.clientPlayerInfo.AcceptChangedValues(someUpdateDict);
                        claims.clientDataStorage.clientPlayerInfo.ReceivedInvitations.Clear();
                        if (claims.CANCityGui?.IsOpened() ?? false)
                        {
                            claims.CANCityGui.BuildMainWindow();
                        }
                        break;
                    case PacketsContentEnum.CURRENT_PLOT_INFO:
                        claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo = JsonConvert.DeserializeObject<CurrentPlotInfo>(packet.data);
                        if (claims.CANCityGui?.IsOpened() ?? false)
                        {
                            claims.CANCityGui.BuildMainWindow();
                        }
                        break;
                }
            });
            claims.clientChannel.SetMessageHandler<PlayerGuiRelatedInfoPacket>((packet) =>
            {
                claims.clientDataStorage.clientPlayerInfo.AcceptChangedValues(packet.playerGuiRelatedInfoDictionary);
                if (claims.CANCityGui?.IsOpened() ?? false)
                {
                    claims.CANCityGui.BuildMainWindow();
                }
            });
            claims.clientChannel.SetMessageHandler<ConfigUpdateValuesPacket>((packet) =>
            {
                claims.config.NEW_CITY_COST = packet.NewCityCost;
                claims.config.PLOT_CLAIM_PRICE = packet.NewPlotClaimCost;
                claims.config.COINS_VALUES_TO_CODE = packet.COINS_VALUES_TO_CODE;
                claims.config.ID_TO_COINS_VALUES = packet.ID_TO_COINS_VALUES;
                claims.config.CITY_NAME_CHANGE_COST = packet.CITY_NAME_CHANGE_COST;
                claims.config.CITY_BASE_CARE = packet.CITY_BASE_CARE;
                claims.config.PLOT_COLORS = packet.PLOTS_COLORS;
            });
        }
    }
}
