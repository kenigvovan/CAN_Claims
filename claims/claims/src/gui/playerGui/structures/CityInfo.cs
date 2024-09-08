using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace claims.src.gui.playerGui.structures
{
    public class CityInfo
    {
        public string Name;
        public string MayorName;
        public long TimeStampCreated;
        public HashSet<string> PlayersNames;
        public int MaxCountPlots;
        public int CountPlots;
        public string Prefix { get; set; }
        public string AfterName { get; set; }
        public HashSet<string> CityTitles { get; set; } = new HashSet<string>();
        public List<RankCellElement> CitizensRanks { get; set; } = new List<RankCellElement>();
        public HashSet<string> PossibleCityRanks { get; set; }
        public int PlotsColor;
        public double cityBalance;

        public CityInfo()
        {
            Name = "";
            PossibleCityRanks = new HashSet<string>();
        }
        public CityInfo(string cityName, string mayorName, long timeStampCreated, HashSet<string> citizens, int maxCountPlots, int countPlots,
            string prefix, string afterName, HashSet<string> cityTitles, int plotsColor, double cityBalance)
        {
            Name = cityName;
            MayorName = mayorName;
            TimeStampCreated = timeStampCreated;
            PlayersNames = citizens;
            MaxCountPlots = maxCountPlots;
            CountPlots = countPlots;
            Prefix = prefix;
            AfterName = afterName;
            CityTitles = cityTitles;
            PlotsColor = plotsColor;
            this.cityBalance = cityBalance;
        }
    }
}
