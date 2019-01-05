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
        public class DisplayControl
        {
            // "Constant" Members ----------------------------------------------
            // Members ---------------------------------------------------------
            protected List<IMyTextPanel> droneStatusList; // List of drone status panels
            protected List<IMyTextPanel> gridInfoList; // List of grid info panels
            protected List<IMyTextPanel> worldStatusList; // List of world status panels
            protected List<IMyTextPanel> gridStatusList; // List of grid status panels
            protected List<IMyTextPanel> cargoStatusList; // List of cargo status panels
            protected List<IMyTextPanel> cargoInventoryList; // List of cargo inventory panels
            protected List<IMyTextPanel> consoleList; // List of console panels

            // "Constant" Properties -------------------------------------------
            // Properties ------------------------------------------------------
            // Constructor -----------------------------------------------------
            public DisplayControl()
            {
                droneStatusList = new List<IMyTextPanel>();
                gridInfoList = new List<IMyTextPanel>();
                worldStatusList = new List<IMyTextPanel>();
                gridStatusList = new List<IMyTextPanel>();
                cargoStatusList = new List<IMyTextPanel>();
                cargoInventoryList = new List<IMyTextPanel>();
                consoleList = new List<IMyTextPanel>();

                droneStatusList.Clear(); // clear blocks list
                gridInfoList.Clear(); // clear blocks list
                worldStatusList.Clear(); // clear blocks list
                gridStatusList.Clear(); // clear blocks list
                cargoStatusList.Clear(); // clear blocks list
                cargoInventoryList.Clear(); // clear blocks list
                consoleList.Clear(); // clear blocks list
            } // DisplayControl constructor

            // Public Methods --------------------------------------------------

            public void Initialize (List<IMyTerminalBlock> lcdList)
            {
                int i = 0; // counter
                IMyTextPanel lcd; // lcd pointer

                droneStatusList.Clear(); // clear blocks list
                gridInfoList.Clear(); // clear blocks list
                worldStatusList.Clear(); // clear blocks list
                gridStatusList.Clear(); // clear blocks list
                cargoStatusList.Clear(); // clear blocks list
                cargoInventoryList.Clear(); // clear blocks list
                consoleList.Clear(); // clear blocks list

                for (i = 0; i < lcdList.Count; i++)
                {
                    Color amber = new Color(255, 151, 53, 255); // amber
                    Color invBlue = new Color(40, 40, 255, 255); // blue, just a shade lighter

                    lcd = lcdList[i] as IMyTextPanel;
                    if (lcd.CustomData.Contains("MAPdronestatus"))
                    {
                        lcd.SetValue("FontSize", 2.0f); // set the font size
                        lcd.SetValue("FontColor", Color.Green); // set the font color
                        lcd.SetValue("BackgroundColor", Color.Black); // set the background color
                        lcd.ShowPublicTextOnScreen();
                        droneStatusList.Add(lcd); // add the lcd to the Drone Status List
                    }
                    else if (lcd.CustomData.Contains("MAPgridinfo"))
                    {
                        lcd.SetValue("FontSize", 2.0f); // set the font size
                        lcd.SetValue("FontColor", amber); // set the font color
                        lcd.SetValue("BackgroundColor", Color.Black); // set the background color
                        lcd.ShowPublicTextOnScreen();
                        gridInfoList.Add(lcd); // add the lcd to the Grid Info List
                    }
                    else if (lcd.CustomData.Contains("MAPworldstatus"))
                    {
                        lcd.SetValue("FontSize", 2.0f); // set the font size
                        lcd.SetValue("FontColor", amber); // set the font color
                        lcd.SetValue("BackgroundColor", Color.Black); // set the background color
                        lcd.ShowPublicTextOnScreen();
                        worldStatusList.Add(lcd); // add the lcd to the World Status list
                    }
                    else if (lcd.CustomData.Contains("MAPgridstatus"))
                    {
                        lcd.SetValue("FontSize", 2.0f); // set the font size
                        lcd.SetValue("FontColor", amber); // set the font color
                        lcd.SetValue("BackgroundColor", Color.Black); // set the background color
                        lcd.ShowPublicTextOnScreen();
                        gridStatusList.Add(lcd); // add the lcd to the Grid Status list
                    }
                    else if (lcd.CustomData.Contains("MAPcargostatus"))
                    {
                        lcd.SetValue("FontSize", 2.0f); // set the font size
                        lcd.SetValue("FontColor", invBlue); // set the font color
                        lcd.SetValue("BackgroundColor", Color.Black); // set the background color
                        lcd.ShowPublicTextOnScreen();
                        cargoStatusList.Add(lcd); // add the lcd to the Cargo Status list
                    }
                    else if (lcd.CustomData.Contains("MAPcargoinventory"))
                    {
                        lcd.SetValue("FontSize", 2.0f); // set the font size
                        lcd.SetValue("FontColor", invBlue); // set the font color
                        lcd.SetValue("BackgroundColor", Color.Black); // set the background color
                        lcd.ShowPublicTextOnScreen();
                        cargoInventoryList.Add(lcd); // add the lcd to the Cargo Inventory list
                    }
                    else if (lcd.CustomData.Contains("MAPconsole"))
                    {
                        lcd.SetValue("FontSize", 1.0f); // set the font size
                        lcd.SetValue("FontColor", Color.White); // set the font color
                        lcd.SetValue("BackgroundColor", Color.Black); // set the background color
                        lcd.ShowPublicTextOnScreen();
                        consoleList.Add(lcd); // add the lcd to the Console list
                    }
                } // for i
            } // Initialize method

            public void WriteDroneStatus (string s)
            {
                int i = 0; // counter

                for (i = 0; i < droneStatusList.Count; i++)
                {
                    droneStatusList[i].WritePublicText(s); // write the status
                }
            } // WriteDroneStatus method

            public void WriteGridInfo (string s)
            {
                int i = 0; // counter

                for (i = 0; i < gridStatusList.Count; i++)
                {
                    gridStatusList[i].WritePublicText(s); // write the status
                }
            } // WriteGridInfo method

            public void WriteWorldStatus (string s)
            {
                int i = 0; // counter

                for (i = 0; i < worldStatusList.Count; i++)
                {
                    worldStatusList[i].WritePublicText(s); // write the status
                }
            } // WriteWorldStatus method

            public void WriteGridStatus (string s)
            {
                int i = 0; // counter

                for (i = 0; i < gridStatusList.Count; i++)
                {
                    gridStatusList[i].WritePublicText(s); // write the status
                }
            } // WriteGridStatus method

            public void WriteCargoStatus (string s)
            {
                int i = 0; // counter

                for (i = 0; i < cargoStatusList.Count; i++)
                {
                    cargoStatusList[i].WritePublicText(s); // write the status
                }
            } // WriteCargoStatus method

            public void WriteCargoInventory (string s)
            {
                int i = 0; // counter

                for (i = 0; i < cargoInventoryList.Count; i++)
                {
                    cargoInventoryList[i].WritePublicText(s); // write the status
                }
            } // WriteCargoInventory method

            public void WriteConsole (string s)
            {
                int i = 0; // counter

                for (i = 0; i < consoleList.Count; i++)
                {
                    consoleList[i].WritePublicText(s); // write the status
                }
            } // WriteConsole method
            // Supporting Methods ----------------------------------------------


        } // DisplayControl class
    } // Program partial class
} // IngameScript namespace
