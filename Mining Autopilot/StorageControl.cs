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
        public class StorageControl
        {
            // Variables --------------------------------------------------------------------------------
            protected string mode; // Mode string value
            protected int submode; // Submode Int Value
            protected Vector3D curPos; // current position
            protected QuaternionD curRot; // current rotation
            protected IMyRemoteControl rc; // Remote Control block
            protected IMyTimerBlock timer; // Timer block
            protected IMySensorBlock sensor; // Sensor block
            protected bool cont; // Continue runs value
            protected bool pause; // Pause value
            protected List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>(); // Block list for grabbing blocks
            protected List<IMyTerminalBlock> connectorList = new List<IMyTerminalBlock>(); // Block list for connectors
            protected List<IMyTerminalBlock> LCDList = new List<IMyTerminalBlock>(); // Block list for LCD's
            protected List<IMyTerminalBlock> drillList = new List<IMyTerminalBlock>(); // Block list for drills
            protected List<IMyTerminalBlock> ejectorList = new List<IMyTerminalBlock>(); // Block list for ejectors
            protected List<IMyTerminalBlock> cargoList = new List<IMyTerminalBlock>(); // Block list for cargo containers
            protected List<IMyTerminalBlock> reactorList = new List<IMyTerminalBlock>(); // Block list for reactors

            // Properties -------------------------------------------------------------------------------
            public string Mode // Mode String Value
            {
                get { return mode; }
                set { mode = value; }
            } // Mode property

            public int SubMode // SubMode Int Value
            {
                get { return submode; }
                set { submode = value; }
            } // Submode property

            public bool Cont
            {
                get { return cont; }
                set { cont = value; }
            } // Cont property

            public bool Pause
            {
                get { return pause; }
                set { pause = value; }
            } // Pause value

            // Methods ----------------------------------------------------------------------------------
            public StorageControl ()
            {
                mode = "Idle"; // set mode to "Idle"
                submode = 0; // set submode to 0
            } // StorageControl constructor method

            public void Load ()
            {

            } // Load method

            public void Save ()
            {

            } // Save method

            public void Update()
            {
                curPos = rc.GetPosition(); // get current position vector
                curRot = QuaternionD.CreateFromRotationMatrix(rc.WorldMatrix.GetOrientation()); // get current rotation quaternion
            } // Update method

            public void Delay(int sec = 0)
            {
                if (sec == 0) // Check if there is a defined time
                {
                    timer.ApplyAction("TriggerNow"); // Run "Trigger Now" on timer
                } // if sec 0
                else
                {
                    timer.SetValue("TriggerDelay", Convert.ToSingle(sec)); // Set the timer delay
                    timer.ApplyAction("Start"); // Start the timer
                    //timer.ApplyAction("Stop");
                } // else sec 0
            } // Delay method

            public void UnloadCargo (IMyGridTerminalSystem gts)
            {
                //List<IMyTerminalBlock> drillList = new List<IMyTerminalBlock>();
                //List<IMyTerminalBlock> ejectorList = new List<IMyTerminalBlock>();
                //List<IMyTerminalBlock> cargoList = new List<IMyTerminalBlock>();
                List<IMyTerminalBlock> destcargoList = new List<IMyTerminalBlock>();
                int i = 0;
                int j = 0;
                int k = 0;

                //GridTerminalSystem.SearchBlocksOfName(shipName + drillName, drillList, g => { return (g.CubeGrid == Me.CubeGrid && g.GetInventory(0).CurrentVolume > 0); }); // get drill block list
                //GridTerminalSystem.SearchBlocksOfName(shipName + ejectorName, ejectorList, g => { return (g.CubeGrid == Me.CubeGrid && g.GetInventory(0).CurrentVolume > 0); }); // get ejector block list
                //GridTerminalSystem.SearchBlocksOfName(shipName + cargoName, cargoList, g => { return (g.CubeGrid == Me.CubeGrid && g.GetInventory(0).CurrentVolume > 0); }); // get cargo block list
                blockList.Clear(); // clear all blocks from our block list
                blockList.AddRange(cargoList); // add local cargo blocks
                blockList.AddRange(drillList); // add local drills
                blockList.AddRange(ejectorList); // add local ejectors
                //gts.SearchBlocksOfName(baseCargoName, destcargoList, g => !g.GetInventory(0).IsFull);
                gts.GetBlocksOfType<IMyCargoContainer>(destcargoList, g => { return (g.CubeGrid != rc.CubeGrid && !g.GetInventory(0).IsFull); });

                for (i = 0; i < cargoList.Count; i++)
                {
                    for (j = 0; j < cargoList[i].GetInventory(0).GetItems().Count; j++)
                    {
                        while (k < destcargoList.Count && destcargoList[k].GetInventory(0).IsFull)
                        { k++; }
                        if (k == destcargoList.Count && destcargoList[k].GetInventory(0).IsFull)
                        { return; }

                        cargoList[i].GetInventory(0).TransferItemTo(destcargoList[k].GetInventory(0), 0, null, true, null);
                    } // for j
                } // for i
                blockList.Clear(); // clear the block list again
            } // UnloadCargo method

            /// <summary>
            /// Not completed
            /// </summary>
            /// <param name="gts"></param>
            /// <param name="minUR"></param>
            public void ReactorRefuel(IMyGridTerminalSystem gts, VRage.MyFixedPoint minUR)
            {
                int i = 0; // counter
                int j = 0; // counter
                //IMyTerminalBlock reactor = null;
                //IMyTerminalBlock sourceReactor = null;

                blockList.Clear();
                gts.GetBlocksOfType<IMyReactor>(blockList, g => g.CubeGrid != rc.CubeGrid);

                for (i = 0; i < blockList.Count; i++)
                {
                    if (blockList[i].GetInventory(0).GetItems()[0].Amount < 2 * minUR)
                    {
                        blockList.Remove(blockList[i]); // remove item from list
                        i = 0; // reset index to 0
                    } // if amount < 2 * minUR
                } // for i (block traverse)

                if (blockList.Count == 0)
                { return; } // no source reactors

                //reactor = GridTerminalSystem.GetBlockWithName(shipName + reactorName); // get ship reactor

                //if (reactor == null) // kill the program so it can be fixed
                    //throw new Exception("ReactorRefuel: Cannot find reactor named " + shipName + reactorName);

                //sourceReactor = GridTerminalSystem.GetBlockWithName(baseReactorFuelName); // get source reactor

                //if (sourceReactor == null)
                //{ return; } // do not crash the program, return without refueling

                //if (reactor.GetInventory(0).GetItems()[0].Amount >= minUR)
                //{ return; } // no need to refuel

                //if (sourceReactor.GetInventory(0).GetItems()[0].Amount >= (2 * minUR))
                //{
               //     sourceReactor.GetInventory(0).TransferItemTo(reactor.GetInventory(0), 0, null, true,
                //        (minUR - reactor.GetInventory(0).GetItems()[0].Amount));
                //}

                for (i = 0; i < reactorList.Count; i++)
                {
                    if (reactorList[i].GetInventory(0).GetItems()[0].Amount < minUR)
                    {

                    }
                }

            } // ReactorRefuel method
        } // StorageControl Class
    } // Program partial class
} // IngameScript namespace
