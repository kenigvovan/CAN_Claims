﻿using claims.src.part;
using claims.src.rights;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Datastructures;

namespace claims.src.gui.playerGui.structures
{
    public class ClientPlayerInfo 
    {
        public CityInfo CityInfo { get; set; }
        public HashSet<string> Friends { get; set; } = new HashSet<string>();
        public List<ClientToCityInvitation> ReceivedInvitations { get; set; } = new List<ClientToCityInvitation>();
        public EnumShowPlotMovement ShowPlotMovement { get; set; } = EnumShowPlotMovement.SHOW_NONE;
        public PlayerPermissions PlayerPermissions { get; set; }
        public CurrentPlotInfo CurrentPlotInfo { get; set; }
        public ClientPlayerInfo()
        {
            CityInfo = null;
            ShowPlotMovement = EnumShowPlotMovement.SHOW_HUD;
            PlayerPermissions = new PlayerPermissions();
            CurrentPlotInfo = new CurrentPlotInfo();
        }
        public ClientPlayerInfo(string cityName, string mayorName, long timeStampCreated, HashSet<string> citizens, int maxCountPlots, int countPlots, string prefix,
            string afterName, HashSet<string> cityTitles, EnumShowPlotMovement showPlotMovement, int PlotColor, double cityBalance)
        {
            CityInfo = new CityInfo(cityName, mayorName, timeStampCreated, citizens, maxCountPlots, countPlots, prefix, afterName, cityTitles, PlotColor, cityBalance);
            ShowPlotMovement = showPlotMovement;
        }

        public ClientPlayerInfo(CityInfo cityInfo, HashSet<string> friends, List<ClientToCityInvitation> receivedInvitations, EnumShowPlotMovement showPlotMovement)
        {
            CityInfo = cityInfo;
            Friends = friends;
            ReceivedInvitations = receivedInvitations;
            ShowPlotMovement = showPlotMovement;
        }
        public ClientPlayerInfo(string cityName, string mayorName, string timeStampCreated, string citizens, string maxCountPlots, string countPlots, string prefix,
           string afterName, string cityTitles, string showPlotMovement, int PlotColor, double cityBalance)
        {
            long.TryParse(timeStampCreated, out long longStamp);

            HashSet<string> citizenList = JsonConvert.DeserializeObject<HashSet<string>>(citizens);

            int.TryParse(maxCountPlots, out int maxCountInt);
            int.TryParse(countPlots, out int curCountPlotInt);

            HashSet<string> titles = JsonConvert.DeserializeObject<HashSet<string>>(cityTitles);

            int.TryParse(showPlotMovement, out int showInt);

            CityInfo = new CityInfo(cityName, mayorName, longStamp, citizenList, maxCountInt, curCountPlotInt, prefix, afterName, titles, PlotColor, cityBalance);
            ShowPlotMovement = (EnumShowPlotMovement)showInt;
        }

        public static ClientPlayerInfo OnJoinAllInfo(string cityName, string mayorName, string timeStampCreated, string citizens, string maxCountPlots, string countPlots, string prefix,
                                                     string afterName, string cityTitles, string showPlotMovement, string friendsList, int PlotColor, double cityBalance)
        {
            long.TryParse(timeStampCreated, out long longStamp);

            HashSet<string> citizenList = JsonConvert.DeserializeObject<HashSet<string>>(citizens);

            int.TryParse(maxCountPlots, out int maxCountInt);
            int.TryParse(countPlots, out int curCountPlotInt);

            HashSet<string> titles = JsonConvert.DeserializeObject<HashSet<string>>(cityTitles);
            int.TryParse(showPlotMovement, out int showInt);

            HashSet<string> friends = JsonConvert.DeserializeObject<HashSet<string>>(friendsList);

            List<ClientToCityInvitation> receivedInvitations = new List<ClientToCityInvitation>(); //todo

            CityInfo cityInfo = new CityInfo(cityName, mayorName, longStamp, citizenList, maxCountInt, curCountPlotInt, prefix, afterName, titles, PlotColor, cityBalance);

            return new ClientPlayerInfo(cityInfo, friends, receivedInvitations, (EnumShowPlotMovement)showInt);
        }

        public void AcceptChangedValues(Dictionary<EnumPlayerRelatedInfo, string> valueDict)
        {
            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.MAYOR_NAME, out string mayor))
            {
                this.CityInfo.MayorName = mayor;
            }

            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.CITY_NAME, out string cityName))
            {
                this.CityInfo.Name = cityName;
            }

            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.CITY_CREATED_TIMESTAMP, out string created))
            {
                long.TryParse(created, out long longStamp);
                CityInfo.TimeStampCreated = longStamp;
            }

            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.CITY_MEMBERS, out string cityMembers))
            {
                CityInfo.PlayersNames = JsonConvert.DeserializeObject<HashSet<string>>(cityMembers);
            }

            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, out string maxPlotCount))
            {
                int.TryParse(maxPlotCount, out int maxPlot);
                CityInfo.MaxCountPlots = maxPlot;
            }

            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.CLAIMED_PLOTS, out string claimedPlots))
            {
                int.TryParse(claimedPlots, out int claimed);
                CityInfo.CountPlots = claimed;
            }

            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.PLAYER_PREFIX, out string prefix))
            {
                CityInfo.Prefix = prefix;
            }

            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.PLAYER_AFTER_NAME, out string afterName))
            {
                CityInfo.AfterName = afterName;
            }
            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.PLAYER_CITY_TITLES, out string titles))
            {
                CityInfo.CityTitles = JsonConvert.DeserializeObject<HashSet<string>>(titles);
            }
            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.SHOW_PLOT_MOVEMENT, out string showMovement))
            {
                int.TryParse(showMovement, out int showInt);
                ShowPlotMovement = (EnumShowPlotMovement)showInt;
            }

            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.CITY_INVITE_ADD, out string inviteAddNew))
            {
                this.ReceivedInvitations.Add(JsonConvert.DeserializeObject<ClientToCityInvitation>(inviteAddNew));
            }

            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.CITY_INVITE_REMOVE, out string inviteRemove))
            {
                var invitationToReemove = this.ReceivedInvitations.Where(invitation => invitation.CityName == inviteRemove).FirstOrDefault();
                if (invitationToReemove != null)
                {
                    this.ReceivedInvitations.Remove(invitationToReemove);
                }
            }

            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.PLAYER_PERMISSIONS, out string permissionsSet))
            {
                PlayerPermissions.ClearPermissions();
                PlayerPermissions.AddPermissions(JsonConvert.DeserializeObject<HashSet<EnumPlayerPermissions>>(permissionsSet));
            }
            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.FRIENDS, out string friends))
            {
                Friends = JsonConvert.DeserializeObject<List<string>>(friends).ToHashSet();
            }

            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.CITY_POSSIBLE_RANKS, out string cityPossibleRanks))
            {
                CityInfo.PossibleCityRanks = JsonConvert.DeserializeObject<List<string>>(cityPossibleRanks).ToHashSet();
            }

            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.CITY_CITIZENS_RANKS, out string citizenRaks))
            {
                Dictionary<string, List<string>> di = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(citizenRaks);
                foreach(var it in di)
                {
                    var foundCell = CityInfo.CitizensRanks.FirstOrDefault(cell => cell.RankName == it.Key);
                    if(foundCell != null) 
                    {
                        foundCell.CitizensRanks = it.Value;
                    }
                    else
                    {
                        CityInfo.CitizensRanks.Add(new RankCellElement(it.Key, it.Value));
                    }
                }
            }

            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.CITY_CITIZEN_RANK_ADDED, out string citizenRaksAdded))
            {
                Dictionary<string, List<string>> di = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(citizenRaksAdded);
                foreach (var it in di)
                {
                    var foundCell = CityInfo.CitizensRanks.FirstOrDefault(cell => cell.RankName == it.Key);
                    if (foundCell != null)
                    {
                        foundCell.CitizensRanks = it.Value;
                    }
                    else
                    {
                        CityInfo.CitizensRanks.Add(new RankCellElement(it.Key, it.Value));
                    }
                }
            }

            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.CITY_CITIZEN_RANK_REMOVED, out string citizenRaksRemoved))
            {
                Dictionary<string, List<string>> di = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(citizenRaksRemoved);
                foreach (var it in di)
                {
                    var foundCell = CityInfo.CitizensRanks.FirstOrDefault(cell => cell.RankName == it.Key);
                    if (foundCell != null)
                    {
                        foundCell.CitizensRanks = it.Value;
                    }
                    else
                    {
                        CityInfo.CitizensRanks.Add(new RankCellElement(it.Key, it.Value));
                    }
                }
            }
            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.CITY_PLOTS_COLOR, out string cityPlotsColor))
            {
                CityInfo.PlotsColor = int.Parse(cityPlotsColor);
            }

            if (valueDict.TryGetValue(EnumPlayerRelatedInfo.CITY_BALANCE, out string cityBalance))
            {
                CityInfo.cityBalance = (double)decimal.Parse(cityBalance);
            }

            

        }
    }
}
