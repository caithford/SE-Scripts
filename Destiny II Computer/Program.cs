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
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.

        ReactorControl reactors; // Reactor Object
        BatteryControl batteries; // Battery Object

        List<IMyTerminalBlock> blockList; // block list

        IMyTextPanel cockpitLCD1; // LCD 1
        IMyTextPanel cockpitLCD2; // LCD 2
        const string cockpitLCD1ID = "DestinyComputerLCD1"; // LCD1 CustomData
        const string cockpitLCD2ID = "DestinyComputerLCD2"; // LCD2 CustomData
        const string cockpitLCD1Title = "Destiny II Computer"; // LCD1 Title
        const string cockpitLCD2Title = "Destiny II Computer"; // LCD2 Title
        const string cockpitLCD1Header = "           Destiny II\n           Computer\n"; // LCD1 Header
        const string cockpitLCD2Header = "           Destiny II\n           Computer\n"; // LCD2 Header

        int initialized;
        double initTimer;

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set RuntimeInfo.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            initialized = 0;
            initTimer = 0;

            blockList = new List<IMyTerminalBlock>(); // allocate memory for blockList
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from. Be aware that the
            // updateSource is a  bitfield  and might contain more than 
            // one update type.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.

            if (initialized > -1)
            {
                initTimer += Runtime.TimeSinceLastRun.TotalMilliseconds;

                if (initTimer > 1500)
                {
                    Initialize();
                } // if initTimer > 1500
            } // if initialized > -1
            else
            {
                if (((updateSource & UpdateType.Terminal) != 0) || ((updateSource & UpdateType.Trigger) != 0))
                {
                    // parse argument
                    ParseTerminalArgument(argument);
                } // if updateSource is Terminal or Trigger
                //if ((updateSource & UpdateType.) != 0) // !trigger, !terminal, !script, !antenna, !once

            } // else initialized > 1
        }

        #region Init

        public void Initialize ()
        {
            bool b = false;

            switch (initialized)
            {
                case 0:
                    Echo("Initializing Destiny II Computer.\n");
                    Echo("Initialize - Phase I Begin\n");
                    b = Init0(); // execute phase 0
                    if (b)
                    {
                        initialized += 2; // skip the next step
                        Echo("Initialize - Phase I Complete\n");
                        WriteLCD("Init - Phase I Complete\n", 3, true);
                    }
                    else
                    {
                        initialized++; // go to next step
                    }
                    break;
                case 1:
                    b = Init0(); // execute phase 0
                    if (b)
                    {
                        initialized++; // go to next step
                        Echo("Initialize - Phase I Complete\n");
                        WriteLCD("Init - Phase I Complete\n", 3, true);
                        Echo("Initialize - Phase II Begin\n");
                        WriteLCD("Init - Phase II Begin\n", 3, true);
                    }
                    break;
                case 2:
                    b = Init1(); // execute phase 1
                    if (b)
                    {
                        initialized++; // go to next step
                        Echo("Initialize - Phase II Complete\n");
                        WriteLCD("Init - Phase II Complete.\n", 3, true);
                        Echo("Initialize - Phase III Begin\n");
                        WriteLCD("Init - Phase III Begin.\n", 3, true);
                    }
                    break;
                case 3:
                    b = Init2(); // execute phase 2
                    if (b)
                    {
                        initialized++;
                        Echo("Initialize - Phase III Complete\n");
                        WriteLCD("Init - Phase III Complete.\n", 3, true);
                    }
                    break;
                case 4:
                    Echo("Destiny II Computer Initialization Complete.\n");
                    WriteLCD("Initialization Complete.\n", 3);
                    initialized = -1; // complete initialization process
                    break;
            } // switch initialized

            // mode 0 - LCD's
            // mode 1 - assign memory
            // mode 2 - gyro & thruster
            // mode 3 - battery & reactor
        } // Initialize()

        public bool Init0 ()
        {
            int i; // counter
            Color computerColor = new Color(255, 69, 0);

            blockList.Clear(); // clear the block list
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(blockList, b => b.IsSameConstructAs(Me)); // get text panels

            cockpitLCD1 = null;
            cockpitLCD2 = null;

            for (i = 0; i < blockList.Count; i++)
            {
                if (blockList[i].CustomData.Contains(cockpitLCD1ID))
                {
                    cockpitLCD1 = blockList[i] as IMyTextPanel;
                } // if contains cockpitLCD1ID
                else if (blockList[i].CustomData.Contains(cockpitLCD2ID))
                {
                    cockpitLCD2 = blockList[i] as IMyTextPanel;
                } // else if contains cockpitLCD2ID
            } // for i

            if (cockpitLCD1 == null)
            {
                Echo("System: Unable to find LCD1\n");
            } // if cockpitLCD1 == null
            else
            {
                cockpitLCD1.Enabled = true;
                //cockpitLCD1.Font = "";
                cockpitLCD1.BackgroundColor = Color.Black;
                cockpitLCD1.FontColor = computerColor;
                cockpitLCD1.FontSize = 2.0f;
                cockpitLCD1.ShowPublicTextOnScreen();
                cockpitLCD1.WritePublicTitle(cockpitLCD1Title);
                WriteLCD("LCD1 found.\n", 1);
            } // else cockpitLCD1 == null

            if (cockpitLCD2 == null)
            {
                Echo("System: Unable to find LCD2\n");
            } // if cockpitLCD2 == null
            else
            {
                cockpitLCD2.Enabled = true;
                //cockpitLCD2.Font = "";
                cockpitLCD2.BackgroundColor = Color.Black;
                cockpitLCD2.FontColor = computerColor;
                cockpitLCD2.FontSize = 2.0f;
                cockpitLCD2.ShowPublicTextOnScreen();
                cockpitLCD2.WritePublicTitle(cockpitLCD2Title);
                WriteLCD("LCD2 found.\n", 2);
            } // else cockpitLCD2 == null

            return true;
        } // Init0()

        public bool Init1 ()
        {
            batteries = new BatteryControl(this);
            reactors = new ReactorControl(this);
            return true;
        } // Init1()

        public bool Init2 ()
        {
            bool a = false, b = false;

            a = batteries.Initialize(); // run init routine for batteries
            b = reactors.Initialize(); // run init routine for reactors

            return (a && b); // return the value
        } // Init2()

        public bool Init3 ()
        {
            return true;
        } // Init3()

        #endregion Init

        #region Control

        public void ParseTerminalArgument (string argument)
        {
            string[] commands;
            int i;

            commands = argument.Split(';'); // split the argument on ';'
            
            for (i = 0; i < commands.Length; i++)
            {
                if (commands[i].Contains("batterytoggle"))
                {
                    batteries.ToggleRD(); // toggle the battery
                } // if ... "batterytoggle"
                if (commands[i].Contains("reactortoggle"))
                {
                    reactors.Toggle(); // toggle the reactors
                } // if ... "reactortoggle"
            }
        } // ParseTerminalArgument

        #endregion Control

        #region LCD

        public void WriteLCD (string text, int selection = 3, bool append = false)
        {
            if ((selection == 1 || selection == 3) && (cockpitLCD1 != null))
            {
                if (!append)
                {
                    text = cockpitLCD1Header + text;
                } // if !append
                cockpitLCD1.WritePublicText(text, append); // Write to LCD 1
            } // if selection 1,3 and lcd exists

            if ((selection == 2 || selection == 3) && (cockpitLCD2 != null))
            {
                if (!append)
                {
                    text = cockpitLCD2Header + text;
                } // if !append
                cockpitLCD2.WritePublicText(text, append); // Write to LCD 2
            } // if selection 2,3 and lcd exists
        } // WriteLCD

        #endregion LCD
    } // partial class program
} // namespace