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
        bool initialized = false;
        int initmode = 0;
        List<AirlockType> airlockList = new List<AirlockType>();
        Dictionary<int, string> airlockInventory = new Dictionary<int, string>();

        //AirlockClass airlock;

        IMyTextPanel debugLCD = null;

        public Program()
        {
            airlockList.Clear();

            Runtime.UpdateFrequency = UpdateFrequency.Update100; // set PB to run every 100 ticks

            debugLCD = GridTerminalSystem.GetBlockWithName("Airlock Debug LCD") as IMyTextPanel;

            if (debugLCD == null)
            {
                throw new Exception("debugLCD is not assigned, please name LCD block 'Airlock Debug LCD'.");
            }
        }

        public void Save()
        {
        }

        public void Main(string argument)
        {


            /* 
             * initialize 1: get all air vent blocks. condense to list of airlock master vents
             * initialize 2: get all LCD blocks. This is for debug output.
             * initialize 3: get all door blocks. put doors in appropriate airlocks
             * initialize 4: lcd's
             * initialize 5: lights
             * initialize 6: sounds
             * 
             */

            if (!initialized)
            { Initialize(); } // run init
        }

        private void Initialize ()
        {
            switch (initmode)
            {
                case 0: Init_Phase1(); break;
                case 1: break;
                case 2: break;
                case 3: break;
                case 4: break;
                case 5: break;
                case 6: break;
            }
            initmode++;
        }

        private void Init_Phase1 ()
        {
            List<IMyTerminalBlock> blocklist = new List<IMyTerminalBlock>();
            AirlockType airlock;

            int i = 0;
            int id = 0;
            string s = "";
            string[] commands;
            string[] values;

            Echo("Searching for Airlocks...");

            blocklist.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyAirVent>(blocklist);

            for (i = 0; i < blocklist.Count; i++)
            {
                commands = blocklist[i].CustomData.Split(';');

                foreach (string command in commands)
                {
                    if (command.StartsWith("Airlock:"))
                    {
                        s = command.Remove(0, "Airlock:".Length);
                        values = s.Split(',');
                    }
                }
                // split on ;

                // loop the strings

                // split on ,

                // get the id, the name, create an index, check for master, slave

                if (s.Contains("Airlock") && s.Contains("Master"))
                {
                    // new airlock code
                    airlock = new AirlockType();
                    airlock.id = id; // set the id
                    airlock.masterVent = blocklist[i] as IMyAirVent;
                    airlockList.Add(airlock); // add to the list
                    Echo("Found Airlock " + id.ToString() + ".");
                }
            }

            Echo("Completed searching for Airlocks.");
        }

        public struct DoorType
        {
            public int id;
            public List<IMyDoor> doorList;
            public List<IMyTextPanel> lcdList;
            public List<IMySoundBlock> soundList;
            public List<IMyAirVent> ventList;
            public List<IMyLightingBlock> lightList;
            public List<IMySensorBlock> sensorList;
        }

        public struct AirlockType
        {
            public int id;
            public IMyAirVent masterVent;
            public List<DoorType> doorList;
            public List<IMyTextPanel> lcdList;
            public List<IMySoundBlock> soundList;
            public List<IMyAirVent> ventList;
            public List<IMyLightingBlock> lightList;
            public List<IMySensorBlock> sensorList;
        }
    }
}