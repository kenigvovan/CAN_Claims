﻿using claims.src.auxialiry;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Config;

namespace claims.src.timers
{
    public class DayTimer
    {
        public static Dictionary<City, List<Plot>> citiesPlots = new Dictionary<City, List<Plot>>();
        public static Dictionary<PlayerInfo, decimal> playerSumFee = new Dictionary<PlayerInfo, decimal>();
        public static List<City> toDeleteCities = new List<City>();

        public void Run(bool scheduleNewDayAfter)
        {
            MessageHandler.sendDebugMsg("[claims] DayTimer::start collecting");
            //EMPTY LIST FOR EVERY CITY
            foreach (City city in claims.dataStorage.getCitiesList())
            {
                citiesPlots.Add(city, new List<Plot>());
            }

            //FILL LISTS OF CITIES' PLOTS
            foreach(Plot plot in claims.dataStorage.getClaimedPlots().Values)
            {
                if(plot.hasCity() && citiesPlots.TryGetValue(plot.getCity(), out List<Plot> plots))
                {
                    plots.Add(plot);
                }
            }
            MessageHandler.sendDebugMsg("[claims] DayTimer::from cities" + StringFunctions.concatStringsWithDelim(citiesPlots.Keys.ToArray(), ','));
            //All players processed
            processCitiesFee();
            //All cities
            processCitiesCare();

            citiesPlots.Clear();

            MessageHandler.sendGlobalMsg("New day here.");
            MessageHandler.sendDebugMsg("[claims] DayTimer::new day here");
            claims.getModInstance().getDatabaseHandler().makeBackup(claims.config.DAILY_BACKUP_FILE_NAME);
            if (scheduleNewDayAfter)
            {
                claims.sapi.Event.RegisterCallback((dt =>
                {
                    new Thread(new ThreadStart(() =>
                    {
                        new DayTimer().Run(true);
                    })).Start();
                }), (int)TimeFunctions.getSecondsBeforeNextDayStart() * 1000);
            }
        }

        public static void processCitiesCare()
        {
            foreach (City city in claims.dataStorage.getCitiesList())
            {
                if(city.isTechnicalCity())
                {
                    MessageHandler.sendErrorMsg(Lang.Get("claims:city_istechnical_no_care_processing", city.GetPartName()));
                    continue;
                }
                processCityCare(city);
            }
            //DELETE CITIES WHICH WERE MARKED
            foreach (City city in toDeleteCities)
                PartDemolition.demolishCity(city);

            toDeleteCities.Clear();
        }
        public static void processCityCare(City city)
        {
            decimal sumToPay = (decimal)claims.config.CITY_BASE_CARE;
            // Add additional cost for plot with plot with pvp on
            if (claims.config.ADDITIONAL_COST_OF_NO_PVP_PLOT)
            {
                sumToPay += (decimal)city.getNoPVPCost();
                //It's a new day, if plot has not pvp turned on we unmark it
                city.updateMarkedPVP();
            }

            foreach (Plot plot in city.getCityPlots())
            {
                PlotInfo.dictPlotTypes.TryGetValue(plot.getType(), out PlotInfo plotInfo);
                sumToPay += (decimal)plotInfo.getCost();
            }
            CityLevelInfo cityLevelInfo = Settings.getCityLevelInfo(city.getCityCitizens().Count);
            int cityOutGo = cityLevelInfo.UnconditionalPayment;

            sumToPay += cityOutGo;
            claims.sapi.Logger.Debug(string.Format("[claims] processCityCare, withdraw {0} from city {1} account. Balance before is {2}, debt is {3}.",
                sumToPay, city.GetPartName(), claims.economyHandler.getBalance(city.MoneyAccountName), city.DebtBalance));
            
            if (claims.economyHandler.getBalance(city.MoneyAccountName) < sumToPay + (decimal)city.DebtBalance) 
            {
                city.DebtBalance = city.DebtBalance + (double)sumToPay;

                //0=> WE DON'T DELETE CITIES, JUST SINK THEM DEEPER IN DEBT
                if (claims.config.CITY_MAX_DEBT != 0)
                {
                    if (city.DebtBalance > claims.config.CITY_MAX_DEBT)
                    {
                        toDeleteCities.Add(city);
                    }
                }
            }
            else
            {
                if (sumToPay < 1)
                {
                    return;
                }
                if(claims.economyHandler.withdraw(city.MoneyAccountName, Math.Floor(sumToPay) + (decimal)city.DebtBalance).ResultState == caneconomy.src.implementations.OperationResult.EnumOperationResultState.SUCCCESS )
                {
                    if (city.DebtBalance > 0)
                    {
                        city.DebtBalance = 0;
                    }
                }
            }
            claims.sapi.Logger.Debug(string.Format("[claims] processCityCare, withdrew {0} from city {1} account. Balance after is {2}, debt is {3}.",
                sumToPay, city.GetPartName(), claims.economyHandler.getBalance(city.MoneyAccountName), city.DebtBalance));
            city.saveToDatabase();
        }
        
       
        public static void processCitiesFee()
        {
            foreach(City city in citiesPlots.Keys)
            {
                processCityFee(city);
            }           
        }
        public static void processCityFee(City city)
        {
            citiesPlots.TryGetValue(city, out List<Plot> cityPlotsList);
            foreach(Plot plot in cityPlotsList)
            {          
                if(plot.hasPlotOwner())
                {
                    if(plot.getPlotOwner().hasCity() && plot.getPlotOwner().City.isMayor(plot.getPlotOwner()))
                    {
                        continue;
                    }
                    if(plot.hasCutomTax())
                    {
                        if (playerSumFee.TryGetValue(plot.getPlotOwner(), out decimal val))
                        {
                            playerSumFee[plot.getPlotOwner()] += (decimal)plot.getCustomTax();
                        }
                        else
                        {
                            playerSumFee[plot.getPlotOwner()] = (decimal)plot.getCustomTax();
                        }
                    }
                    /*else
                    {
                        PlotInfo.dictPlotTypes.TryGetValue(plot.getType(), out PlotInfo plotInfo);
                        if(plotInfo != null)
                        {
                            if (playerSumFee.TryGetValue(plot.getPlotOwner(), out double val))
                            {
                                playerSumFee[plot.getPlotOwner()] += plotInfo.getCost();
                            }
                            else
                            {
                                playerSumFee[plot.getPlotOwner()] = plotInfo.getCost();
                            }
                        }
                    }*/
                }
                else if (plot.hasCityPlotsGroup())
                {
                    if (plot.getPlotGroup().HasFee())
                    {
                        foreach(PlayerInfo player in plot.getPlotGroup().PlayersList)
                        {
                            if (plot.getCity().isMayor(player))
                            {
                                continue;
                            }
                            if (playerSumFee.TryGetValue(player, out decimal val))
                            {
                                playerSumFee[player] += (decimal)plot.getPlotGroup().PlotsGroupFee;
                            }
                            else
                            {
                                playerSumFee[player] = (decimal)plot.getPlotGroup().PlotsGroupFee;
                            }
                        }                       
                    }
                }
            }
            //So all citizen will pay city's fee
            foreach(PlayerInfo citizen in city.getCityCitizens())
            {
                //mayor shouldn't pay city's fee
                if (citizen.hasCity() && citizen.City.isMayor(citizen))
                {
                    continue;
                }
                if (!playerSumFee.Keys.Contains(citizen))
                {
                    playerSumFee[citizen] = 0;
                }
            }
            foreach (PlayerInfo it in playerSumFee.Keys)
            {
                if (playerSumFee.TryGetValue(it, out decimal toPay))
                {
                    if(it.hasCity() && it.City.Equals(city))
                    {
                        toPay += city.fee;
                    }
                    if (toPay < 1)
                    {
                        continue;
                    }
                    if (claims.economyHandler.getBalance(it.MoneyAccountName) < toPay)
                    {
                        //WE DELETE PLAYER FROM EVERY PLOTGROUP IN THIS CITY
                        foreach(CityPlotsGroup cpg in city.getCityPlotsGroups())
                        {
                            foreach(PlayerInfo playerInfoHere in cpg.PlayersList.ToArray())
                            {
                                if (playerInfoHere.Equals(it))
                                {
                                    cpg.PlayersList.Remove(playerInfoHere);
                                }
                            }
                        }
                        if (claims.config.DELETE_CITIZEN_FROM_CITY_IF_DOESN_PAY_FEE)
                        {
                            //IF HE HAS EMBASSY WE DON'T WANT TO KICK HIM FROM HIS CITY OR IF HE DOESN'T HAVE ONE
                            if (it.hasCity() && it.City.Equals(city))
                            {
                                MessageHandler.sendDebugMsg("processCityFee:kicking player " + it.GetPartName());
                                MessageHandler.sendMsgInCity(city, Lang.Get("claims:citizen_didnt_pay_kicked", city.getPartNameReplaceUnder()));
                                it.clearCity(true);
                            }
                        }
                        else
                        {
                            //HE DIDN'T PAY BUT WE ARE NOT SAVAGIES AFTER ALL, LET HIM BE IN THE CITY
                            foreach (Plot plot in it.PlayerPlots.ToArray())
                            {
                                //BUT NOT AT OTHER CITIES, THAT WILL BE TAKEN CARE IN DIFFERENT ITERATION FOR ANOTHER CITY
                                if (!plot.getCity().Equals(city))
                                {
                                    continue;
                                }
                                plot.resetOwner();
                                plot.setPrice(-1);
                                plot.setType(PlotType.DEFAULT);
                                plot.saveToDatabase();
                            }
                        }
                    }
                    else
                    {
                        if (claims.economyHandler.depositFromAToB(it.MoneyAccountName, city.MoneyAccountName, toPay).ResultState == caneconomy.src.implementations.OperationResult.EnumOperationResultState.SUCCCESS)
                        {
                            MessageHandler.sendMsgToPlayerInfo(it, Lang.Get("claims:you_paid_fee_to_city", toPay.ToString(), city.getPartNameReplaceUnder()));
                        }
                        else
                        {
                            //todo workaround for failed state
                            MessageHandler.sendMsgToPlayerInfo(it, Lang.Get("claims:economy_money_transaction_error"));
                        }
                    }
                }
            }

            playerSumFee.Clear();
        }

    }
}
