﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace claims.src.rights
{
    public enum EnumPlayerPermissions
    {
        PLOT_SET_FS, PLOT_SET_NFS,

        PLOT_CLAIM, PLOT_UNCLAIM,

        PLOT_SET_ALL_CITY_PLOTS,

        PLOT_SET_ALL_OWN_PLOT,

        PLOT_SET_NAME, PLOT_SET_FEE, PLOT_SET_TYPE, PLOT_SET_PVP, PLOT_SET_FIRE, PLOT_SET_BLAST, PLOT_SET_PLOT_ACCESS_PERMISSIONS,

        PLOT_INNER_PLOT,


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        PLAYER_INFO_OTHER = 512,

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        CITY_CLAIM_PLOT = 1024, CITY_UNCLAIM_PLOT, CITY_BUY_EXTRA_PLOT, CITY_BUY_OUTPOST,

        CITY_INVITE, CITY_KICK, CITY_UNINVITE, SHOW_INVITES_SENT,

        CITY_SET_ALL, CITY_SET_NAME, CITY_SET_OPEN_STATE, CITY_SET_PVP, CITY_SET_FIRE, CITY_SET_BLAST, CITY_SET_GLOBAL_FEE, CITY_SET_DAILY_MSG, CITY_SET_PLOT_ACCESS_PERMISSIONS,
        CITY_SET_INV_MSG,

        CITY_INFO, CITY_HERE, 

        CITY_SHOW_RANK_OTHERS, CITY_SET_RANK, CITY_REMOVE_RANK,

        CITY_PRISON_ALL, CITY_CRIMINAL_ALL, CITY_ADD_CRIMINAL, CITY_REMOVE_CRIMINAL, CITY_PRISON_ADD_CELL, CITY_PRISON_REMOVE_CELL, CITY_PRISON_LIST,

        CITY_SET_SUMMON,

        CITY_SET_OTHERS_PREFIX,

        CITY_PLOTSGROUP_CREATE, CITY_PLOTSGROUP_REMOVE, CITY_PLOTSGROUP_ADD, CITY_PLOTSGROUP_KICK,
        CITY_PLOTSGROUP_ADD_PLOT, CITY_PLOTSGROUP_REMOVE_PLOT, CITY_PLOTSGROUP_LIST, CITY_PLOTSGROUP_SET,

        CITY_SET_PLOTS_COLOR,

        CITY_SEE_BALANCE

    }

    public class PlayerPermissions
    {
        HashSet<EnumPlayerPermissions> permissions = new HashSet<EnumPlayerPermissions>();
        public bool HasPermission(EnumPlayerPermissions permission)
        {
            return permissions.Contains(permission);
        }
        public bool AddPermission(EnumPlayerPermissions permission)
        {
            return permissions.Add(permission);
        }
        public bool RemovePermission(EnumPlayerPermissions permission)
        {
            return permissions.Remove(permission);
        }
        public void AddPermissions(HashSet<EnumPlayerPermissions> newPermissions)
        {
            permissions.UnionWith(newPermissions);
        }
        public void ClearPermissions() 
        {
            permissions.Clear();
        }
        public IReadOnlyCollection<EnumPlayerPermissions> GetPermissions()
        {
            return permissions;
        }
    }
}
