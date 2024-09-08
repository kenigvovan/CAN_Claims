﻿using caneconomy.src.accounts;
using claims.src.auxialiry;
using claims.src.cityplotsgroups;
using claims.src.delayed.invitations;
using claims.src.gui.playerGui.structures;
using claims.src.part.interfaces;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using claims.src.perms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;

namespace claims.src.part
{
    public class City : Part, ISender, IReceiver, IGetStatus, ICooldown
    {
        HashSet<PlayerInfo> cityCitizens = new HashSet<PlayerInfo>();
        PermsHandler permsHandler = new PermsHandler();
        PlayerInfo mayor;
        List<Plot> cityPlots = new List<Plot>();
        public double DebtBalance { get; set; }
        public long TimeStampCreated {  get; set; }
        List<Invitation> listSentInvitations = new List<Invitation>();
        List<Invitation> listReceivedInvitations = new List<Invitation>();      
        List<CityPlotsGroup> cityPlotsGroups = new List<CityPlotsGroup>();
        List<Prison> prisons = new List<Prison>();
        int defaultPlotCost = 0;
        public List<PlayerInfo> criminals = new List<PlayerInfo>();
        public string invMsg { get; set; } = "";
        public bool openCity { get; set; } = false;
        public List<CityPlotsGroupInvitation> groupInvitations = new List<CityPlotsGroupInvitation>();
        public int fee { get; set; } = 0;
        bool isTechnical { get; set; } = false;
        int bonusPlots { get; set; } = 0;
        public HashSet<Plot> summonPlots = new HashSet<Plot>();
        public int Extrachunksbought { get; set; } = 0;
        public int cityColor { get; set; } = -992222222;
        public string MoneyAccountName => claims.config.CITY_ACCOUNT_STRING_PREFIX + Guid;
        public Dictionary<Vec2i, Vec3i> TempleRespawnPoints { get; } = new Dictionary<Vec2i, Vec3i>();
        public City(string valName, string guid, bool isTechnical = false) : base(valName, guid)
        {
            this.isTechnical = isTechnical;
        }


        /*****************************************************************/
        public void updateMarkedPVP()
        {
            foreach (var it in cityPlots)
            {
                if (it.getPermsHandler().pvpFlag)
                    it.setMarkedNoPVP(false);
            }
        }
        public double getNoPVPCost()
        {
            double tmp = 0;
            foreach (Plot it in cityPlots)
            {
                if (it.isMarkedNoPVP())
                {
                    ++tmp;
                }
            }
            return tmp * claims.config.PLOT_NO_PVP_FLAG_COST;
        }
        public List<PlayerInfo> getCriminals()
        {
            return criminals;
        }
        public int getBonusPlots()
        {
            return bonusPlots;
        }
        public void setBonusPlots(int val)
        {
            this.bonusPlots = val;
        }
        public bool isTechnicalCity()
        {
            return isTechnical;
        }
        public void setIsTechnicalCity(bool val)
        {
            this.isTechnical = val;
        }
        public void setIsTechnicalCity(string val, TextCommandResult tcr)
        {
            if (val.Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                isTechnical = false;
                tcr.StatusMessage = "claims:city_is_not_technical";
            }
            else if (val.Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                isTechnical = true;
                tcr.StatusMessage = "claims:city_is_now_technical";
            }
            else
            {
                tcr.Status = EnumCommandStatus.Error;
            }
        }
        public int getDefaultPlotCost()
        {
            return defaultPlotCost;
        }
        public void setDefaultPlotCost(int cost)
        {
            this.defaultPlotCost = cost;
        }
        public List<CityPlotsGroup> getCityPlotsGroups()
        {
            return cityPlotsGroups;
        }
        public List<Prison> getPrisons()
        {
            return prisons;
        }

        public Prison getRandomPrison()
        {
            return prisons[claims.dataStorage.r.Next() % prisons.Count];
        }

        public bool hasPrison()
        {
            return prisons.Count > 0;
        }

        public bool isCitizen(PlayerInfo playerInfo)
        {
            return cityCitizens.Contains(playerInfo);
        }  

        public HashSet<PlayerInfo> getPlayerInfos()
        {
            return cityCitizens;
        }

        public bool isMayor(PlayerInfo playerInfo)
        {
            if (mayor == null)
            {
                return false;
            }
            if(mayor.Equals(playerInfo))
            {
                return true;
            }
            return false;
        }

        public List<IServerPlayer> getOnlineCitizens()
        {
            List<IServerPlayer> outList = new List<IServerPlayer>();
            IServerPlayer tmp;
            foreach (var it in getCityCitizens())
            {
                tmp = (IServerPlayer)claims.sapi.World.PlayerByUid(it.Guid);
                if (tmp != null && claims.sapi.World.AllOnlinePlayers.Contains(tmp))
                {
                    outList.Add(tmp);
                }
            }
            return outList;
        }
        public List<Plot> getCityPlots()
        {
            return cityPlots;
        }

        public HashSet<PlayerInfo> getCityCitizens()
        {
            return cityCitizens;
        }

        public PlayerInfo getMayor()
        {
            return mayor;
        }

        public bool setMayor(PlayerInfo player)
        {
            this.mayor = player;
            return true;
        }

        public override bool saveToDatabase(bool update = true)
        {
            return claims.getModInstance().getDatabaseHandler().saveCity(this, update);
        }

        public List<Invitation> getSentInvitations()
        {
            return listSentInvitations;
        }

        public List<Invitation> getReceivedInvitations()
        {
            return listReceivedInvitations;
        }

        public void deleteSentInvitation(Invitation invitation)
        {
            this.listSentInvitations.Remove(invitation);
        }

        public void addSentInvitation(Invitation invitation)
        {
            this.listSentInvitations.Add(invitation);
        }

        public int getMaxSentInvitations()
        {
            return claims.config.MAX_SENT_INVITATIONS_CITY;
        }

        public void deleteReceivedInvitation(Invitation invitation)
        {
            this.listReceivedInvitations.Remove(invitation);
        }

        public void addReceivedInvitation(Invitation invitation)
        {
            this.listReceivedInvitations.Add(invitation);
        }

        public int getMaxReceivedInvitations()
        {
            return claims.config.MAX_RECEIVED_INVITATIONS_CITY;
        }

        public double getExpense()
        {
            double outSum = 0;
            foreach (Plot plot in getCityPlots())
            {
                PlotInfo.dictPlotTypes.TryGetValue(plot.getType(), out PlotInfo plotInfo);
                outSum += plotInfo.getCost();
            }
            return outSum;
        }
        public void AddTempleRespawnPoint(Plot plot, BlockPos blockPos)
        {
            if(TempleRespawnPoints.ContainsKey(plot.getPos()))
            {
                TempleRespawnPoints[plot.getPos()] = blockPos.ToVec3i();
            }
            else
            {
                TempleRespawnPoints[plot.getPos()] = blockPos.ToVec3i();
            }
        }

        public void AddTempleRespawnPoint(Vec2i rPlotPos, Vec3i blockPos)
        {
            if (TempleRespawnPoints.ContainsKey(rPlotPos))
            {
                TempleRespawnPoints[rPlotPos] = blockPos;
            }
            else
            {
                TempleRespawnPoints[rPlotPos] = blockPos;
            }
        }

        public bool RemoveTempleRespawnPoint(Plot plot)
        {
            return TempleRespawnPoints.Remove(plot.getPos());
        }

        public bool HasTempleRespawnPoints()
        {
            return TempleRespawnPoints.Count > 0;
        }

        public List<string> getStatus(PlayerInfo forPlayer = null)
        {
            List<string> outStrings = new List<string>
            {
                "[" + getPartNameReplaceUnder() + "]" + (this.openCity
                                                                        ? Lang.Get("claims:open_city")
                                                                        : "") + "\n"
            };

            if(isMayor(forPlayer))
            {
                Plot plot = getCityPlots()[0];
                outStrings.Add(plot.getPos().ToString() + "\n");
            }

            if (invMsg.Length != 0)
            {
                outStrings.Add($"{Lang.Get("claims:invite_msg_city", invMsg)}\n");
            }
            if(this.mayor != null)
                outStrings.Add($"{Lang.Get("claims:mayor")}  {getMayor().getPartNameReplaceUnder()}\n");

            outStrings.Add(Lang.Get("claims:bank_status", claims.economyHandler.getBalance(this.MoneyAccountName)));
            outStrings.Add(DebtBalance > 0 ? Lang.Get("claims:city_debt_status") + DebtBalance + "\n" : "\n");
            CityLevelInfo cityLevelInfo = Settings.getCityLevelInfo(getCityCitizens().Count);
            outStrings.Add($"{Lang.Get("claims:city_claimed_amount_status", this.getCityPlots().Count, cityLevelInfo.AmountOfPlots) + (cityLevelInfo.Maxextrachunksbought > 0 ? " " + Lang.Get("claims:city_claimed_extra_amount_status", this.Extrachunksbought, cityLevelInfo.Maxextrachunksbought) + "\n" : "\n")}");
            outStrings.Add($"{Lang.Get("claims:created")} {TimeFunctions.getDateFromEpochSeconds(TimeStampCreated)}\n");
            outStrings.Add(Lang.Get("claims:city_outgo") + (getExpense() + cityLevelInfo.UnconditionalPayment).ToString() + (getNoPVPCost() > 0 ? "(+" + getNoPVPCost().ToString() + ")" : "") + "\n");
            outStrings.Add($"{Lang.Get("claims:citizens")} {StringFunctions.makeStringPlayersName(this.getCityCitizens(), ", ")}");

            outStrings.Add("\n");
            outStrings.Add(permsHandler.getStringForChat() + "\n");
            
            return outStrings;
        }

        public PermsHandler getPermsHandler()
        {
            return permsHandler;
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + Guid.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj)
        {
            if(obj is City)
            {
                return this.Guid.Equals(((City)obj).Guid);
            }
            return false;
        }

        public string getNameSender()
        {
            return GetPartName();
        }

        public string getNameReceiver()
        {
            return GetPartName();
        }

        public bool setCityOpenCloseState(string newVal)
        {
            if (newVal.Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                openCity = false;
                return true;
            }
            else if (newVal.Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                openCity = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if name of the city can be changed to provided.
        /// Change it if can be, change also account name, save to db, set tcr value.
        /// Do NOT check for permissions or money.
        /// </summary>
        /// <param name="tcr"></param>
        /// <param name="newCityName"></param>
        public bool rename(string newCityName)
        {
            string filteredName = Filter.filterName(newCityName);
            if (filteredName.Length == 0 || !Filter.checkForBlockedNames(filteredName))
            {
                return false;
            }
            if (filteredName.Length > claims.config.MAX_LENGTH_CITY_NAME)
            {
                return false;
            }
            if (claims.dataStorage.nameForCityOrVillageIsTaken(filteredName))
            {
                return false;
            }

            if(claims.economyHandler.updateAccount(this.MoneyAccountName, new Dictionary<string, object> { { "lastknownname", filteredName } }))
            {
                claims.dataStorage.changeCityName(this, filteredName);
                SetPartName(filteredName);
                saveToDatabase();
                UsefullPacketsSend.AddToQueueCityInfoUpdate(Guid, EnumPlayerRelatedInfo.CITY_NAME);
                return true;
            }
            return false;                   
        }
        public void trySetPlotColor(int color)
        {
            cityColor = color;
            this.saveToDatabase();
        }
        public void setAllCityPlotsMarkedAsUpdated()
        {
            //for all plots set new mark and add to queue for send
            foreach(var plot in getCityPlots())
            {
                claims.dataStorage.setNowEpochZoneTimestampFromPlotPosition(plot.getPos());
                claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            }            
        }
    }
}
