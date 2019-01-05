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
        public class LogControl
        {
            // "Constant" Members ----------------------------------------------
            // Members ---------------------------------------------------------
            List<IMyTerminalBlock> LCDList; // list of log LCD's
            IMyTextPanel lcd; // lcd variable
            IMyProgrammableBlock Me; // this programmable block
            string log; // log value

            // "Constant" Properties -------------------------------------------
            // Properties ------------------------------------------------------
            // Constructor -----------------------------------------------------

            public LogControl ()
            {
                LCDList = new List<IMyTerminalBlock>(); // create memory space
                LCDList.Clear(); // clear the list
                log = ""; // clear the log
            } // LogControl constructor

            // Public Methods --------------------------------------------------
            public void Initialize (List<IMyTerminalBlock> blockList, IMyProgrammableBlock pb)
            {
                int i = 0;

                Me = pb; // set the PB

                for (i = 0;i < blockList.Count; i++)
                {
                    if (blockList[i].CustomData.Contains("MAPLog"))
                    {
                        LCDList.Add(blockList[i]); // Add the block to the list
                        lcd = blockList[i] as IMyTextPanel; // grab the screen itself
                        ConfigureScreen(lcd); // Configure the screen
                        lcd.WritePublicText(""); // write the title to the screen
                    }
                } // for i

                log = ""; // clear any log that may have been generated

            } // Initialize method

            public void AddLog (string s)
            {
                log = "[" + System.DateTime.Now.ToString("HHmmss") + "] " + s + "\n" + log; // add the new string to the log
                
            } // AddLog method

            public void Update ()
            {
                const string title = "Log File\n"; // Screen Title

                int i = 0; // loop counter
                string temp = ""; // temp string

                if (log == "")
                {
                    return; // nothing to do
                }
                
                lcd = LCDList[0] as IMyTextPanel; // grab the first lcd

                temp = lcd.GetPublicText(); // grab the text
                if (temp.Contains(title))
                {
                    temp.Remove(temp.IndexOf(title,0), title.Length); // remove the title
                } // if title
                temp = title + log + temp; // update the log string

                lcd.WritePublicText(temp); // write to first panel

                for (i = 1; i < LCDList.Count; i++)
                {
                    lcd = LCDList[i] as IMyTextPanel; // grab the panel
                    lcd.WritePublicText(temp); // write subsequent panel(s)
                } // lcd for

                log = ""; // reset log buffer
            } // Update method

            // Supporting Methods ----------------------------------------------
            protected void ConfigureScreen (IMyTextPanel screen)
            {
                screen.SetValue("FontSize", 0.8f); // set the font size
                screen.SetValue("FontColor", Color.Red); // set the font color
                screen.SetValue("BackgroundColor", Color.Black); // set the background color
            } // ConfigureScreen

        } // LogControl class
    } // Program partial class
} // IngameScript namespace
