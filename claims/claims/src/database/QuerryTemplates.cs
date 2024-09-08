﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace claims.src.database
{
    public static class QuerryTemplates
    {

        //CITY
        public static readonly string DELETE_CITY = "DELETE FROM CITIES WHERE guid = @guid";
        public static readonly string INSERT_CITY = "INSERT INTO CITIES (NAME, MAYOR, GUID, TIMESTAMPCREATED, debtbalance, perm, plotgroups, prisons, defaultplotcost, invmsg, opencity, fee, criminals, istechnical, bonusplots, extrachunksbought, citycolor, templerespawnpoints) VALUES (@name, @mayor, @guid, @timestampcreated, @debtbalance,@perm,@plotgroups,@prisons,@defaultplotcost,@invmsg, @opencity, @fee, @criminals, @istechnical, @bonusplots, @extrachunksbought, @citycolor, @templerespawnpoints)";
        public static readonly string UPDATE_CITY = "UPDATE CITIES SET NAME=@name, MAYOR=@mayor, GUID=@guid, timestampcreated=@timestampcreated, debtbalance=@debtbalance, perm=@perm, plotgroups=@plotgroups, prisons=@prisons,defaultplotcost=@defaultplotcost, invmsg=@invmsg, opencity=@opencity, fee=@fee, criminals=@criminals, istechnical=@istechnical, bonusplots=@bonusplots, extrachunksbought=@extrachunksbought, citycolor=@citycolor, templerespawnpoints=@templerespawnpoints WHERE guid=@guid";
        
        //PLAYER
        public static readonly string DELETE_PLAYER = "DELETE FROM PLAYERS WHERE UID=@uid";
        public static readonly string INSERT_PLAYER = "INSERT INTO PLAYERS (NAME, UID,timestampfirstjoined, timestamplastonline, comrades, city, citytitles, title, aftername, perms, prisonguid, prisonhoursleft) VALUES (@name, @uid, @timestampfirstjoined, @timestamplastonline, @comrades,@city,@citytitles,@title,@aftername,@perms,@prisonguid,@prisonhoursleft)";
        public static readonly string UPDATE_PLAYER = "UPDATE PLAYERS SET NAME=@name, UID=@uid, timestampfirstjoined=@timestampfirstjoined, timestamplastonline=@timestamplastonline, comrades=@comrades, city=@city, citytitles=@citytitles, title=@title, aftername=@aftername, perms=@perms,prisonguid=@prisonguid, prisonhoursleft=@prisonhoursleft  WHERE UID=@uid";

        //PLOTGROUP
        public static readonly string DELETE_CITYPLOTGROUP = "DELETE FROM CITYPLOTSGROUP WHERE guid=@guid";
        public static readonly string INSERT_CITYPLOTGROUP = "INSERT INTO CITYPLOTSGROUP (name, guid, perms, players, plotsgroupfee, city) VALUES (@name, @guid, @perms,@players, @plotsgroupfee, @city)";
        public static readonly string UPDATE_CITYPLOTGROUP = "UPDATE CITYPLOTSGROUP SET name=@name, guid=@guid, perms=@perms, players=@players, plotsgroupfee=@plotsgroupfee, city=@city where guid=@guid";

        //PRISON
        public static readonly string DELETE_PRISON = "DELETE FROM PRISONS WHERE guid=@guid";
        public static readonly string INSERT_PRISON = "INSERT INTO PRISONS (name, guid, prisonCells, city, x,z) VALUES (@name,@guid,@prisonCells, @city,@x,@z)";
        public static readonly string UPDATE_PRISON = "UPDATE PRISONS SET name=@name, guid=@guid, prisonCells=@prisonCells, city=@city, x=@x,z=@z where guid=@guid";

        //WORLD
        public static readonly string DELETE_WORLD = "DELETE FROM WORLDS WHERE guid=@guid";
        public static readonly string INSERT_WORLD = "INSERT INTO WORLDS (name, guid,pvpeverywhere,fireeverywhere,blasteverywhere,fireforbidden,pvpforbidden,blastforbidden) VALUES (@name,@guid,@pvpeverywhere,@fireeverywhere,@blasteverywhere,@fireforbidden,@pvpforbidden,@blastforbidden)";
        public static readonly string UPDATE_WORLD = "UPDATE WORLDS SET name=@name, guid=@guid, pvpeverywhere=@pvpeverywhere, fireeverywhere=@fireeverywhere,blasteverywhere=@blasteverywhere,fireforbidden=@fireforbidden,pvpforbidden=@pvpforbidden,blastforbidden=@blastforbidden where guid=@guid";

        //PLOT
        public static readonly string DELETE_PLOT = "DELETE FROM PLOTS WHERE x=@x AND z=@z";
        public static readonly string INSERT_PLOT = "INSERT INTO PLOTS (name, x,z,city,ownerofplot,type,price,customtax,perms,plotgroupguid, markednopvp, plotdesc, extraBought)" +
                                                    " VALUES (@name,@x,@z,@city,@ownerofplot,@type,@price,@customtax,@perms,@plotgroupguid, @markednopvp, @plotdesc, @extraBought)";
        public static readonly string UPDATE_PLOT = "UPDATE PLOTS SET name=@name, x=@x,z=@z,city=@city, ownerofplot=@ownerofplot, type=@type,price=@price,customtax=@customtax,perms=@perms, plotgroupguid=@plotgroupguid, markednopvp=@markednopvp, plotdesc=@plotdesc, extraBought=@extraBought" +
                                                    " where x=@x and z=@z";
    }
}
