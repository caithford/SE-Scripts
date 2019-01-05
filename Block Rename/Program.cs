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
    partial class Program : MyGridProgram
    {


        public void Main(string argument)
        {
            bool renumber = true; // renumber the blocks
            string searchname = ""; // name of blocks to find
            string replacename = ""; // rename blocks

            bool prepend = false; // prepend?
            string prependname = ""; // name to prepend

            bool append = false; // append?
            string appendname = ""; // name to append

            int i = 0; // counter
            int j = 0; // temp variable

            string temp = "";

            char[] digits_arr = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>(); // new blocklist

            if (append || prepend)
            {
                Echo("Append or Prepend selected. Search and replace disabled.");

                GridTerminalSystem.GetBlocks(blockList); // get the blocks

                for (i = 0; i < blockList.Count; i++)
                {
                    temp = ""; // clear the temp string
                    if (prepend)
                    {
                        temp += prependname; // add the prepend string
                    }
                    temp += blockList[i].CustomName; // add the current block name
                    if (append)
                    {
                        temp += appendname; // add the append name
                    }
                    blockList[i].CustomName = temp; // set the block
                }
            }
            else
            {
                Echo("Search and replace mode.");
                if (renumber)
                {
                    Echo("Renumbering enabled.");
                }

                GridTerminalSystem.SearchBlocksOfName(searchname, blockList); // get blocks with search name

                for (i = 0; i < blockList.Count; i++)
                {
                    temp = blockList[i].CustomName; // get a copy of the current custom name
                    temp = temp.Replace(searchname, replacename); // replace the contents of the string

                    if (renumber)
                    {
                        temp = temp.TrimEnd(digits_arr);
                        j = i + 1;
                        temp += j.ToString();
                    }
                    Echo(temp);
                    //blockList[i].CustomName = temp;
                }
            }
            Echo("Program complete.");
        }
    }
}