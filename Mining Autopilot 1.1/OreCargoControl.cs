using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class OreCargoControl
        {
            // "Constant" Members ----------------------------------------------
            // Members ---------------------------------------------------------
            private double currentVolume; // current used volume
            private double maxVolume; // max volume of all containers
            private double stone; // volume of stone
            private double fe; // volume of iron
            private double ni; // volume of nickle
            private double co; // volume of cobalt
            private double mg; // volume of magnesium
            private double si; // volume of silicon
            private double ag; // volume of silver
            private double au; // volume of gold
            private double pt; // volume of platinum
            private double u; // volume of uranium
            private double ice; // volume of ice

            // "Constant" Properties -------------------------------------------

            // Properties ------------------------------------------------------
            public double CurVol
            {
                get { return currentVolume * 1000; }
            } // CurVol property

            public double MaxVol
            {
                get { return maxVolume * 1000; }
            } // MaxVol property

            public float VolPct
            {
                get { return ((float)Math.Round(100 * currentVolume / maxVolume, 1)); }
            } // VolPct property

            // Constructor -----------------------------------------------------
            public OreCargoControl ()
            {
                currentVolume = 0;
                maxVolume = 0;
                ice = 0;
                stone = 0;
                fe = 0;
                pt = 0;
                u = 0;
                si = 0;
                ni = 0;
                mg = 0;
                au = 0;
                ag = 0;
                co = 0;
            } // OreCargoControl constructor

            // Public Methods --------------------------------------------------
            public void Update (List<IMyTerminalBlock> blockList)
            {
                CalcVolume(blockList); // calculate the volume of the list of blocks
                CalcInventory(blockList); // update the inventory
            } // Update method

            public string GetInvStr ()
            {
                string temp = ""; // string to build output

                temp += "Cargo Inventory\n";
                if (stone > 0)
                { temp += "Stone: " + Math.Round(stone, 2).ToString("N") + "\n"; }
                if (ice > 0)
                { temp += "Ice: " + Math.Round(ice, 2).ToString("N") + "\n"; }
                if (fe > 0)
                { temp += "Iron: " + Math.Round(fe, 2).ToString("N") + "\n"; }
                if (ni > 0)
                { temp += "Nickel: " + Math.Round(ni, 2).ToString("N") + "\n"; }
                if (co > 0)
                { temp += "Cobalt: " + Math.Round(co, 2).ToString("N") + "\n"; }
                if (mg > 0)
                { temp += "Magnesium: " + Math.Round(mg, 2).ToString("N") + "\n"; }
                if (si > 0)
                { temp += "Silicon: " + Math.Round(si, 2).ToString("N") + "\n"; }
                if (ag > 0)
                { temp += "Silver: " + Math.Round(ag, 2).ToString("N") + "\n"; }
                if (au > 0)
                { temp += "Gold: " + Math.Round(au, 2).ToString("N") + "\n"; }
                if (pt > 0)
                { temp += "Platinum: " + Math.Round(pt, 2).ToString("N") + "\n"; }
                if (u > 0)
                { temp += "Uranium: " + Math.Round(u, 2).ToString("N") + "\n"; }

                return temp; // return the value
            } // getInvStr method

            // Supporting Methods ----------------------------------------------
            protected void CalcVolume (List<IMyTerminalBlock> blockList)
            {
                int i = 0;

                maxVolume = 0; // initialize to be safe
                currentVolume = 0; // initialize to be safe

                for (i = 0; i < blockList.Count; i++)
                {
                    if (blockList[i].HasInventory)
                    {
                        maxVolume = maxVolume + (double)blockList[i].GetInventory(0).MaxVolume;
                        currentVolume = currentVolume + (double)blockList[i].GetInventory(0).CurrentVolume;
                    } // if hasinventory
                } // for blocklist
            } // calcVolume method

            protected void CalcInventory (List<IMyTerminalBlock> blockList)
            {
                int i = 0;
                int j = 0;
                List<IMyInventoryItem> invList = new List<IMyInventoryItem>();
                for (i = 0; i < blockList.Count; i++)
                {
                    if (blockList[i].HasInventory)
                    {
                        invList = blockList[i].GetInventory(0).GetItems();
                        for (j = 0; j < invList.Count; j++)
                        {
                            switch (invList[j].Content.SubtypeName)
                            {
                                case "Iron":
                                    fe += (double)invList[j].Amount;
                                    break;
                                case "Cobalt":
                                    co += (double)invList[j].Amount;
                                    break;
                                case "Nickel":
                                    ni += (double)invList[j].Amount;
                                    break;
                                case "Magnesium":
                                    mg += (double)invList[j].Amount;
                                    break;
                                case "Gold":
                                    au += (double)invList[j].Amount;
                                    break;
                                case "Silver":
                                    ag += (double)invList[j].Amount;
                                    break;
                                case "Platinum":
                                    pt += (double)invList[j].Amount;
                                    break;
                                case "Silicon":
                                    si += (double)invList[j].Amount;
                                    break;
                                case "Uranium":
                                    u += (double)invList[j].Amount;
                                    break;
                                case "Stone":
                                    stone += (double)invList[j].Amount;
                                    break;
                                case "Ice":
                                    ice += (double)invList[j].Amount;
                                    break;
                            } // switch
                        } // for invList
                    } // if HasInventory
                } // for BlockList

            } // calcInventory method
        }
    }
}
