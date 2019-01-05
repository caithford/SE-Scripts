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
        public class MiningAutoPilotShip : StorageControl
        {
            // "Constant" Members ----------------------------------------------
            protected float dockDErr; // Docking RC Block Distance Error
            protected float dockStageDist; // Docking RC Block Staging Distance
            protected float gridStageDist; // Grid RC Block Staging Distance
            protected float apDErr; // Autopilot general waypoint distance error
            protected double rotAErr; // Rotation Alignment Error
            protected float rPct; // Percentage of dock distance to begin rotation
            protected float gOPwr; // Gyro Override Power
            protected double shipWidth; // Ship "radius" for grid
            protected int shaftPerRun; // # of shafts to drill per run
            protected int gridSide; // length of side of square of drilling grid (number of shafts)
            protected int clockTickCount; // Number of ticks between full program runs
            protected bool returnIfFull; // Returns drone to dock point before shaft complete if cargo full
            protected bool ejectStone; // Eject stone while drilling?
            protected VRage.MyFixedPoint minUr; // Amount of uranium to keep in the reactor
            protected bool initialized; // Is system initialized?

            // Members ---------------------------------------------------------
            protected Program pgm; // This program (that the programmable block is running)
            protected List<IMyTerminalBlock> blockList; // list of blocks
            protected List<IMyTerminalBlock> thrusterList; // list of thrusters
            protected List<IMyTerminalBlock> gyroList; // list of gyros
            protected List<IMyTerminalBlock> connectorList; // list of connectors
            protected List<IMyTerminalBlock> cargoList; // list of cargo containers
            protected List<IMyTerminalBlock> ejectorList; // list of ejectors
            protected List<IMyTerminalBlock> drillList; // list of drills
            protected List<IMyTerminalBlock> LCDList; // list of LCD's
            protected List<IMyTerminalBlock> reactorList; // list of reactors
            protected IMySensorBlock sensor; // Forward sensor
            protected IMyTimerBlock timer; // Timer
            protected IMyRemoteControl rc; // RC Block
            protected IMyProgrammableBlock Me; // (this) programmable block
            protected IMyShipConnector connector; // Connector block
            protected Vector3D curPos; // Current Pos
            protected QuaternionD curRot; // Current Rotation
            protected Vector3D gridPos; // Grid position
            protected QuaternionD gridRot; // Grid rotation
            protected Vector3D gridStagePos; // Grid staging position
            protected Vector3D dockPos; // Dock position
            protected QuaternionD dockRot; // Dock rotation
            protected Vector3D dockStagePos; // Dock staging position
            protected MatrixD tfM; // Transform matrix
            protected Vector3D tfCurPos; // Transformed position
            protected Vector3D targPos; // Target position (in grid)
            protected bool haveGridPos; // Flag for grid position setting
            protected bool haveGridRot; // Flag for grid rotation setting
            protected bool haveGridStagePos; // Flag for grid staging position setting
            protected bool haveDockPos; // Flag for dock position setting
            protected bool haveDockRot; // Flag for dock rotation setting
            protected bool haveDockStagePos; // Flag for dock staging position setting
            protected bool haveTargPos; // flag for the target position setting
            protected double angle; // Differential angle for rotation Actually cos of angle between actual and target vector
            protected string mode; // the current mode
            protected int subMode; // the current sub mode
            protected bool cont; // continue with drilling cycles
            protected bool pause; // program pause
            protected int shaftN; // Shaft Number
            protected bool reverse; // Reverse mode
            protected GyroControl gyroCtl; // Gyro control module
            protected ThrusterControl thrCtl; // Thruster control module
            protected OreCargoControl crgCtl; // Cargo control module
            protected LogControl logCtl; // Log control module
            protected DisplayControl dspCtl; // display control module
            
            // "Constant" Properties -------------------------------------------
            public float DockDistErr
            {
                set { dockDErr = value; }
            }
            public float DockStageDist
            {
                set { dockStageDist = value; }
            }
            public float GridStageDist
            {
                set { gridStageDist = value; }
            }
            public float ApDErr
            {
                set { apDErr = value; }
            }
            public double RotAErr
            {
                set { rotAErr = value; }
            }
            public float RotPct
            {
                set { rPct = value; }
            }
            public float GOPwr
            {
                set { gOPwr = value; }
            }
            public double ShipWidth
            {
                set { shipWidth = value; }
            }
            public int ShaftsPerRun
            {
                set { shaftPerRun = value; }
            }
            public int GridSideLength
            {
                set { gridSide = value; }
            }
            public bool ReturnOnFull
            {
                set { returnIfFull = value; }
            }
            public bool EjectStone
            {
                set { ejectStone = value; }
            }
            public VRage.MyFixedPoint MinUrReactor
            {
                set { minUr = value; }
            }

            // Properties ------------------------------------------------------
            // Constructor -----------------------------------------------------
            public MiningAutoPilotShip()
            {
                dockDErr = 0.25f;
                dockStageDist = 100f;
                gridStageDist = 100f;
                apDErr = 5.0f;
                rotAErr = 0.999999d;
                rPct = 0.75f;
                gOPwr = 1.0f;
                shipWidth = 8;
                shaftPerRun = 1;
                gridSide = 20;
                clockTickCount = 5;
                returnIfFull = false;
                ejectStone = true;
                minUr = 60;
                initialized = false;

                haveGridPos = false; // initialize the flag
                haveGridRot = false; // initialize the flag
                haveGridStagePos = false; // initialize the flag
                haveDockPos = false; // initialize the flag
                haveDockRot = false; // initialize the flag
                haveDockStagePos = false; // initialize the flag
                haveTargPos = false; // initialize the flag

                mode = "Idle"; // set mode to idle (unknown state)
                subMode = 0; // set submode to 0

                blockList = new List<IMyTerminalBlock>(); // initialize the list
                thrusterList = new List<IMyTerminalBlock>(); // initialize the list
                gyroList = new List<IMyTerminalBlock>(); // initialize the list
                connectorList = new List<IMyTerminalBlock>(); // initialize the list
                cargoList = new List<IMyTerminalBlock>(); // initialize the list
                ejectorList = new List<IMyTerminalBlock>(); // initialize the list
                drillList = new List<IMyTerminalBlock>(); // initialize the list
                LCDList = new List<IMyTerminalBlock>(); // initialize the list
                reactorList = new List<IMyTerminalBlock>(); // initialize the list

                curPos = new Vector3D();
                curRot = new QuaternionD();

                cont = false;
                pause = false;

                gyroCtl = new GyroControl();
                thrCtl = new ThrusterControl();
                crgCtl = new OreCargoControl();
                logCtl = new LogControl();
                dspCtl = new DisplayControl();
            } // MiningAutoPilotShip constructor

            public MiningAutoPilotShip (Program p) : this()
            {
                pgm = p; // set the program
            }

            // Public Methods --------------------------------------------------
            public void Initialize(IMyGridTerminalSystem gts, IMyProgrammableBlock pb)
            {
                int i = 0; // counter for looping

                Me = pb; // set the programmable block

                thrusterList.Clear(); // clear block list
                gts.GetBlocksOfType<IMyThrust>(thrusterList, b => b.CubeGrid == pb.CubeGrid); // get all thrusters

                gyroList.Clear(); // clear block list
                gts.GetBlocksOfType<IMyGyro>(gyroList, b => b.CubeGrid == pb.CubeGrid); // get all gyros

                connectorList.Clear(); // clear connector list
                gts.GetBlocksOfType<IMyShipConnector>(connectorList, b => b.CubeGrid == pb.CubeGrid); // get all connectors

                cargoList.Clear(); // clear cargo list
                gts.GetBlocksOfType<IMyCargoContainer>(cargoList, b => b.CubeGrid == pb.CubeGrid); // get all cargo containers

                ejectorList.Clear(); // clear ejector list
                blockList.Clear(); // clear block list
                gts.GetBlocksOfType<IMyShipConnector>(blockList, b => b.CubeGrid == pb.CubeGrid); // get all connectors

                for (i = 0; i < blockList.Count; i++)
                {
                    if (blockList[i].DefinitionDisplayNameText == "ConnectorSmall")
                    {
                        ejectorList.Add(blockList[i]); // add the block to the list
                    }
                } // for blocklist
                blockList.Clear(); // re-clear the block list

                drillList.Clear(); // clear drill list
                gts.GetBlocksOfType<IMyShipDrill>(drillList, b => b.CubeGrid == pb.CubeGrid); // get list of drills

                LCDList.Clear(); // clear LCD list
                gts.GetBlocksOfType<IMyTextPanel>(LCDList, b => b.CubeGrid == pb.CubeGrid); // get list of lcd's

                reactorList.Clear(); // clear reactor list
                gts.GetBlocksOfType<IMyReactor>(reactorList, b => b.CubeGrid == pb.CubeGrid); // get list of reactors

                gts.GetBlocksOfType<IMyShipController>(blockList, b => b.CubeGrid == pb.CubeGrid); // get list of RC's
                for (i = 0; i < blockList.Count;i++)
                {
                    if (blockList[i].CustomData.Contains("MAPRC"))
                    {
                        rc = blockList[i] as IMyRemoteControl; // get the block
                        break; // break the loop
                    }
                } // for i
                blockList.Clear(); // clear the block list

                if (rc == null)
                {
                    throw new Exception("Init: No RC Block.\n");
                }

                // TODO: set primary connector
                for (i = 0; i < connectorList.Count; i++)
                {
                    if (connectorList[i].CustomData.Contains("MAPdockconnector"))
                    {
                        connector = connectorList[i] as IMyShipConnector; // get the block
                        break; // break the loop
                    }
                } // for i

                if (connector == null)
                {
                    throw new Exception("Init: No Connector Block.\n");
                }


                // TODO: set primary sensor
                gts.GetBlocksOfType<IMySensorBlock>(blockList, b => b.CubeGrid == pb.CubeGrid); // get list of RC's
                for (i = 0; i < blockList.Count; i++)
                {
                    if (blockList[i].CustomData.Contains("MAPsensor"))
                    {
                        sensor = blockList[i] as IMySensorBlock; // get the block
                        break; // break the loop
                    }
                } // for i
                blockList.Clear(); // clear the block list

                if (sensor == null)
                {
                    throw new Exception("Init: No Sensor Block.\n");
                }

                sensor.BackExtend = 50; // set max
                sensor.FrontExtend = 50; // set max
                sensor.LeftExtend = 50; // set max
                sensor.RightExtend = 50; // set max
                sensor.TopExtend = 50; // set max
                sensor.BottomExtend = 50; // set max
                sensor.DetectAsteroids = true; // set detect asteroids
                sensor.DetectEnemy = false; // do not detect enemy
                sensor.DetectFloatingObjects = false; // do not detect floating objects
                sensor.DetectFriendly = false; // do not detect friendly objects
                sensor.DetectLargeShips = false; // do not detect large ships
                sensor.DetectNeutral = false; // do not detect neutral objects
                sensor.DetectOwner = false; // do not detect owner
                sensor.DetectPlayers = false; // do not detect players
                sensor.DetectSmallShips = false; // do not detect small grids
                sensor.DetectStations = false; // do not detect stations
                sensor.DetectSubgrids = false; // do not detect subgrids

                // TODO: set primary timer MAPtimer
                gts.GetBlocksOfType<IMyTimerBlock>(blockList, b => b.CubeGrid == pb.CubeGrid); // get list of RC's
                for (i = 0; i < blockList.Count; i++)
                {
                    if (blockList[i].CustomData.Contains("MAPtimer"))
                    {
                        timer = blockList[i] as IMyTimerBlock; // get the block
                        break; // break the loop
                    }
                } // for i
                blockList.Clear(); // clear the block list

                if (timer == null)
                {
                    throw new Exception("Init: No Timer Block.\n");
                }

                gyroCtl.RotationError = rotAErr;

                thrCtl.Initialize(thrusterList, rc); // initialize thruster control

                logCtl.Initialize(LCDList, Me); // initialize log control

                dspCtl.Initialize(LCDList); // initialize display control

                initialized = true;
            } // Initialize method

            public void Argument(string arg)
            {
                string[] arglist; // list of arguments
                string[] keypairlist; // list of keypairs from argument
                string key; // key string
                string value; // value string
                int i = 0; // loop counter

                arglist = arg.Split(';'); // split on ;

                for (i = 0; i < arglist.Length; i++)
                {
                    if (arglist[i].Contains(":")) // check if value
                    {
                        pgm.Echo("arglist has :\n");
                        keypairlist = arglist[i].Split(':'); // split on :
                        key = keypairlist[0]; // get the key
                        value = keypairlist[1]; // get the value
                    } // if contains :
                    else
                    {
                        pgm.Echo("arglist has no :\n");
                        key = arglist[i]; // grab the key (no value)
                        value = ""; // no value
                    } // else contains :

                    pgm.Echo("Argument\nkey: " + key + "\nvalue: " + value + "\n");

                    switch (key.ToUpper())
                    {
                        case "PAUSE":
                            logCtl.AddLog("Argument: PAUSE");
                            PauseCmd(value); // Run the PauseCmd method
                            break;
                        case "SETDOCK":
                            logCtl.AddLog("Argument: SETDOCK");
                            SetDockCmd(value); // Run the SetDockCmd method
                            break;
                        case "SETGRID":
                            logCtl.AddLog("Argument: SETGRID");
                            SetGridCmd(value); // Run the SetGridCmd method
                            break;
                        case "START":
                            logCtl.AddLog("Argument: START");
                            Delay(1); // start the timer (just in case)
                            break;
                        case "CONTINUE":
                            logCtl.AddLog("Argument: CONTINUE");
                            ContinueCmd(value); // Run the ContinueCmd method
                            break;
                        case "COMPLETE":
                            logCtl.AddLog("Argument: COMPLETE");
                            //TODO : Still need logic here to allow the cycle to complete? or something?
                            //CompleteCmd(arg); // Run the CompleteCmd method
                            break;
                        case "":
                            //do nothing
                            break;
                        case "RESET":
                            logCtl.AddLog("Argument: RESET");
                            data = ""; // reset
                            break;
                        case "IDLE":
                            mode = "Idle"; // set mode to idle
                            break;
                        default:
                            logCtl.AddLog("Argument: Unknown Argument");
                            break; // unrecognized command
                    } // switch keypairlist[0]
                    logCtl.Update(); // write the log
                } // for i 
            } // Argument method

            public void Run(IMyGridTerminalSystem gts)
            {
                if (!initialized) // if system isn't initialized, quit
                {
                    logCtl.AddLog("Ship is not initialized.");
                    logCtl.Update();
                    return;
                }

                if (!(haveDockPos && haveDockRot && haveDockStagePos && haveGridPos && haveGridRot && haveGridStagePos))
                {
                    logCtl.AddLog("Required co-ordinates not set.");
                    logCtl.AddLog("haveDockPos: " + haveDockPos.ToString());
                    logCtl.AddLog("haveDockRot: " + haveDockRot.ToString());
                    logCtl.AddLog("haveDockStagePos: " + haveDockStagePos.ToString());
                    logCtl.AddLog("haveGridPos: " + haveGridPos.ToString());
                    logCtl.AddLog("haveGridRot: " + haveGridRot.ToString());
                    logCtl.AddLog("haveGridStagePos: " + haveGridStagePos.ToString());
                    logCtl.Update();
                    return;
                }

                blockList.Clear(); // clear list of blocks
                blockList.AddRange(drillList); // add drills
                blockList.AddRange(cargoList); // add cargo containers

                crgCtl.Update(blockList); // update the cargo statistics

                blockList.Clear(); // clear the list of blocks

                curPos = rc.GetPosition(); // get the current position

                switch (mode)
                {
                    case "Docked":
                        RunMode_Docked(gts);
                        break;
                    case "Undock":
                        RunMode_Undock();
                        break;
                    case "ToGrid":
                        RunMode_ToGrid();
                        break;
                    case "GridAlign":
                        RunMode_GridAlign();
                        break;
                    case "Drill":
                        RunMode_Drill();
                        break;
                    case "ClearShaft":
                        RunMode_ClearShaft();
                        break;
                    case "ReturnToBase":
                        RunMode_ReturnToBase();
                        break;
                    case "Dock":
                        RunMode_Dock();
                        break;
                    case "Idle":
                        mode = "ToGrid"; // set mode ToGrid
                        subMode = 0; // reset submode
                        break;
                    default:
                        break;
                }

                UpdateDisplay();
                logCtl.Update(); // update logs
            } // Run method

            public void Save (ref string storage)
            {
                if (haveGridPos)
                {
                    UpdateVecKey("gridPos", gridPos); // update gridPos value
                }
                if (haveGridRot)
                {
                    UpdateQuatKey("gridRot", gridRot); // update gridRot value
                }
                if (haveGridStagePos)
                {
                    UpdateVecKey("gridStagePos", gridStagePos); // update gridStagePos value
                }
                if (haveDockPos)
                {
                    UpdateVecKey("dockPos", dockPos); // update dockPos value
                }
                if (haveDockRot)
                {
                    UpdateQuatKey("dockRot", dockRot); // update dockRot value
                }
                if (haveDockStagePos)
                {
                    UpdateVecKey("dockStagePos", dockStagePos); // update dockStagePos value
                }
                UpdateVecKey("targPos", targPos); // update targPos value
                UpdateKey("mode", mode); // update mode value
                UpdateKey("subMode", subMode.ToString()); // update subMode value
                UpdateKey("cont", cont.ToString()); // update cont value
                UpdateKey("pause", pause.ToString()); // update pause value
                UpdateKey("shaftN", shaftN.ToString()); // update shaftN value

                if (initialized)
                {
                    storage = GetData(); // send the data back
                } // if initialized
            } // Save method

            public void Load (string storage)
            {
                Vector3D? v = null; // for grabbing nullable vectors
                QuaternionD? q = null; // for grabbing nullable quaternions

                SetData(storage); // set the data string to what was provided

                v = GetVecKey("gridPos"); // get the gridPos value
                if (v.HasValue)
                {
                    gridPos = v.Value; // set the value
                    haveGridPos = true; // note the flag
                    pgm.Echo("getveckey gridpos hasvalue");
                }
                pgm.Echo("getveckey gridpos complete\n");

                q = GetQuatKey("gridRot"); // get the gridRot value
                if (q.HasValue)
                {
                    gridRot = q.Value; // set the value
                    haveGridRot = true; // note the flag
                }

                v = GetVecKey("gridStagePos"); // get the gridStagePos value
                if (v.HasValue)
                {
                    gridStagePos = v.Value; // set the value
                    haveGridStagePos = true; // note the flag
                }

                v = GetVecKey("dockPos"); // get the dockPos value
                if (v.HasValue)
                {
                    dockPos = v.Value; // set the value
                    haveDockPos = true; // note the flag
                }

                q = GetQuatKey("dockRot"); // get the dockRot value
                if (q.HasValue)
                {
                    dockRot = q.Value; // set the value
                    haveDockRot = true; // note the flag
                }

                v = GetVecKey("dockStagePos"); // get the dockStagePos value
                if (v.HasValue)
                {
                    dockStagePos = v.Value; // set the value
                    haveDockStagePos = true; // note the flag
                }

                v = GetVecKey("targPos"); // get the targPos value
                if (v.HasValue)
                {
                    targPos = v.Value; // set the value
                    haveTargPos = true; // note the flag
                }
                mode = GetKey("mode"); // get the mode value
                int.TryParse(GetKey("subMode"), out subMode); // get the subMode value
                bool.TryParse(GetKey("cont"), out cont); // get the cont value
                bool.TryParse(GetKey("pause"), out pause); // get the pause value
                int.TryParse(GetKey("shaftN"), out shaftN); // get the shaftN value
            } // Load method

            // Run Mode Methods ------------------------------------------------

            protected void RunMode_Docked(IMyGridTerminalSystem gts)
            {
                if (subMode == 0)
                {
                    UnloadCargo(gts); // Unload the Cargo Module
                    ReactorRefuel(gts); // Refuel the Reactor
                    subMode = 1; // Move to next SubMode
                    logCtl.AddLog("RM_Docked - Unload/Refuel\n");
                } // if SubMode 0

                if (cont && !pause)
                {
                    mode = "Undock"; // Set Mode to Undock
                    subMode = 0; // Reset SubMode
                    Delay(1); // Delay the Program 1 s
                    logCtl.AddLog("RM_Docked - Moving to Undock\n");
                } // if Cont && !Pause
            } // RunMode_Docked

            protected void RunMode_Undock()
            {
                switch (subMode)
                {
                    case 0: UnlockConnector(); // Unlock the connector
                        subMode = 1; // set to next mode
                        logCtl.AddLog("RM_Undock - Unlock/Power Connector\n");
                        break; // break out
                    case 1: rc.ClearWaypoints(); // Clear waypoints
                        rc.ApplyAction("DockingMode_Off"); // Turn off Docking Mode
                        rc.ApplyAction("CollisionAvoidance_Off"); // Turn of collision avoidance (makes difficult to undock)
                        rc.ApplyAction("Forward"); // set direction for RC to forward
                        rc.AddWaypoint(dockStagePos, "Undock Staging Location"); // set the waypoint
                        rc.ApplyAction("AutoPilot_On"); // turn on the autopilot
                        logCtl.AddLog("RM_Undock - AP Set\n");
                        break;
                    case 2: // error checking
                        if (rc.IsAutoPilotEnabled)
                        {
                            subMode = 3; // Move on to next step
                        }
                        else
                        {
                            subMode = 1; // Go back and try again
                            logCtl.AddLog("RM_Undock - AP Error!\n");
                        }
                        break;
                    case 3: // checking distance
                        if (Vector3D.DistanceSquared(dockStagePos,curPos) < (apDErr * apDErr))
                        {
                            rc.ApplyAction("AutoPilot_Off"); // Turn off autopilot
                            rc.ApplyAction("DockingMode_Off"); // Turn off Docking Mode
                            rc.ClearWaypoints(); // Clear all waypoints
                            mode = "ToGrid"; // change mode to "ToGrid"
                            subMode = 0; // reset submode
                            logCtl.AddLog("RM_Undock - Moving to ToGrid\n");
                        } // if distance
                        break;
                } // switch submode

                Delay(1); // Delay 1s
            } // RunMode_Undock

            protected void RunMode_ToGrid()
            {
                int time = 1; // time for delay

                switch (subMode)
                {
                    case 0: // configure autopilot
                        time = 1;
                        rc.ClearWaypoints(); // Clear waypoints
                        rc.ApplyAction("CollisionAvoidance_On"); // Turn on collision avoidance
                        rc.ApplyAction("Forward"); // Configure forward direction
                        rc.AddWaypoint(gridStagePos, "Grid Staging Location"); // set the waypoint
                        rc.ApplyAction("AutoPilot_On"); // turn on the autopilot
                        subMode = 1; // set the next mode
                        logCtl.AddLog("RM_ToGrid - AP Set\n");
                        break;
                    case 1: // error checking
                        time = 1;
                        if (rc.IsAutoPilotEnabled)
                        {
                            subMode = 2; // move to next step
                        }
                        else
                        {
                            subMode = 0; // move to previous step
                            logCtl.AddLog("RM_ToGrid - AP Error!\n");
                        }
                        break;
                    case 2: // distance checking
                        time = 5;
                        if (Vector3D.DistanceSquared(gridStagePos,curPos) < (1000 * 1000))
                        {
                            time = 1; // set time to 1
                            subMode = 3; // advance to next submode
                        }
                        break;
                    case 3: // checking
                        time = 1;
                        if (Vector3D.DistanceSquared(gridStagePos,curPos) < (apDErr * apDErr))
                        {
                            rc.ApplyAction("AutoPilot_Off"); // turn off autopilot
                            rc.ClearWaypoints(); // clear the waypoints
                            mode = "GridAlign"; // set next mode
                            subMode = 0; // reset submode
                            logCtl.AddLog("RM_ToGrid - Moving to GridAlign\n");
                        }
                        break;
                } // switch subMode

                Delay(time); // set the delay, run the program again
            } // RunMode_ToGrid

            protected void RunMode_GridAlign()
            {
                double horDist = 0, verDist = 0; // horizontal and vertical distance (offset)
                float maxSpd = 3f; // maximum speed
                double maxHorDist = 0.1, maxVerDist = 0.1; // maximum Horizontal and Vertical Distance from target
                float maxHorAcc = 0.1f, maxVerAcc = 0.1f, maxFwdAcc = 1f; // maximum horizontal, vertical and forward accelleration

                if (subMode == 0)
                {
                    logCtl.AddLog("RM_GridAlign - Start Setup\n");
                    // Check Direction
                    if ((shaftPerRun > 1) && (shaftN % 2 == 0)) // check for direction
                    {
                        reverse = true; // set reverse
                    }

                    // Set Target Z based on direction
                    if (reverse)
                    {
                        targPos.Z = 0; // set target Z to 0 (grid entry)
                    }
                    else
                    {
                        targPos.Z = -1000; // set target Z to 1000 meters beyond grid
                    }

                    // Create Transformation Matrix
                    tfM = MatrixD.CreateFromQuaternion(gridRot) * MatrixD.Invert(MatrixD.CreateTranslation(gridPos));

                    // Set Gyro rotation data
                    gyroCtl.RotationError = rotAErr; // set rotation error
                    gyroCtl.SetRotation(gridRot, reverse); // set rotation q and reverse

                    // Kill Drills
                    TBOnOff(drillList, false);

                    subMode = 1; // set subMode to 1
                } // if subMode 0

                // Set Up Ship Rotation
                angle = gyroCtl.Align(rc, gyroList);

                // Convert Current location to in-grid location
                tfCurPos.X = Vector3D.Dot(tfM.Translation + curPos, tfM.Right);
                tfCurPos.Y = Vector3D.Dot(tfM.Translation + curPos, tfM.Up);
                tfCurPos.Z = Vector3D.Dot(tfM.Translation + curPos, tfM.Backward);

                // Calculate Horizontal and Vertical distance
                horDist = Math.Abs((tfCurPos.X - targPos.X));
                verDist = Math.Abs((tfCurPos.Y - targPos.Y));

                logCtl.AddLog("horDist: " + horDist.ToString());
                logCtl.AddLog("verDist: " + verDist.ToString());

                // Set Max Horizontal Acceleration
                if (horDist > 5) { maxHorAcc = 1.0f; }
                if (horDist > 25) { maxHorAcc = 5.0f; }

                // Set Max Vertical Acceleration
                if (verDist > 5) { maxVerAcc = 1.0f; }
                if (verDist > 25) { maxVerAcc = 5.0f; }

                if (angle >= rotAErr)
                {
                    if (horDist > maxHorDist)
                    {
                        // enable horizontal thrusters
                        if ((reverse && tfCurPos.X < targPos.X) || (!reverse && tfCurPos.X > targPos.X))
                        {
                            thrCtl.SetThrust(rc, thrCtl.STARBOARD, maxHorAcc); // enable thrusters
                        }
                        else
                        {
                            thrCtl.SetThrust(rc, thrCtl.PORT, maxHorAcc); // enable thrusters
                        }
                    }
                    else
                    {
                        thrCtl.SetThrust(rc, thrCtl.PORT + thrCtl.STARBOARD, 0); // disable hor thr
                    }

                    if (verDist > maxVerDist)
                    {
                        // enable verticle thrusters
                        if (tfCurPos.Y < targPos.Y)
                        {
                            thrCtl.SetThrust(rc, thrCtl.VENTRAL, maxVerAcc); // enable thrusters
                        }
                        else
                        {
                            thrCtl.SetThrust(rc, thrCtl.DORSAL, maxVerAcc); // enable thrusters
                        }
                    }
                    else
                    {
                        thrCtl.SetThrust(rc, thrCtl.DORSAL + thrCtl.VENTRAL, 0); // disable ver thr
                    }

                    if (horDist <= maxHorDist && verDist <= maxVerDist)
                    {
                        if (!sensor.LastDetectedEntity.IsEmpty()) // check sensor
                        {
                            thrCtl.SetThrust(rc, thrCtl.ALL, 0); // turn off all thrusters
                            mode = "Drill"; // set mode to drill
                            subMode = 0; // reset submode
                            logCtl.AddLog("RM_GridAlign - Moving to Drill\n");
                        }
                        else
                        {
                            if (rc.GetShipSpeed() < .80 * maxSpd)
                            {
                                maxFwdAcc = 1.0f; // accel to 1 m/s
                            }
                            else if (rc.GetShipSpeed() < maxSpd)
                            {
                                maxFwdAcc = 0.05f; // accel to 0.05 m/s
                            }
                            else if (rc.GetShipSpeed() >= maxSpd)
                            {
                                maxFwdAcc = 0.0f; // accel to 0.0 m/s
                            }

                            thrCtl.SetThrust(rc, thrCtl.FORWARD, maxFwdAcc); // enable thrusters
                        }
                    }
                    else
                    {
                        thrCtl.SetThrust(rc, thrCtl.AFT + thrCtl.FORWARD, 0); // disable fwd/rev thr
                    }
                }
                else
                {
                    // shut down all thrusters
                    thrCtl.SetThrust(rc, thrCtl.ALL, 0); // turn off all thrusters
                }

                Delay(); // Trigger the program again
            } // RunMode_GridAlign

            protected void RunMode_Drill()
            {
                double horDist = 0, verDist = 0; // horizontal and vertical distance (offset)
                float maxSpd = 3f; // maximum speed
                double maxHorDist = 0.1, maxVerDist = 0.1; // maximum Horizontal and Vertical Distance from target
                float maxHorAcc = 0.1f, maxVerAcc = 0.1f, maxFwdAcc = 1f; // maximum horizontal, vertical and forward accelleration
                bool asteroidDetected = false; // Determines if asteroid is detected
                int shaftX, shaftY; // values for calculating x,y for shaft grid
                bool pause = false; // for delaying the delay

                if (subMode == 0)
                {
                    logCtl.AddLog("RM_Drill - Setup\n");
                    // Check Direction
                    if ((shaftPerRun > 1) && (shaftN % 2 == 0)) // check for direction
                    {
                        reverse = true; // set reverse
                    }

                    // Set Target Z based on direction
                    if (reverse)
                    {
                        targPos.Z = 0; // set target Z to 0 (grid entry)
                    }
                    else
                    {
                        targPos.Z = -1000; // set target Z to 1000 meters beyond grid
                    }

                    // Create Transformation Matrix
                    tfM = MatrixD.CreateFromQuaternion(gridRot) * MatrixD.Invert(MatrixD.CreateTranslation(gridPos));

                    // Set Gyro rotation data
                    gyroCtl.RotationError = rotAErr; // set rotation error
                    gyroCtl.SetRotation(gridRot, reverse); // set rotation q and reverse

                    // Enable Drills
                    TBOnOff(drillList, true);

                    if (ejectStone)
                    {
                        TBOnOff(ejectorList, true); // enable ejectors
                        subMode = 1; // set subMode to 1
                        logCtl.AddLog("RM_Drill - Ejectors Enabled\n");
                    } // if ejectstone
                    else
                    {
                        subMode = 2; // set subMode to 2
                    } // else ejectstone
                } // if subMode 0

                if (subMode == 1)
                {
                    if ((crgCtl.VolPct > 80) && returnIfFull)
                    {
                        TBOnOff(ejectorList, false); // turn off
                        subMode = 2; // set subMode to 2
                        logCtl.AddLog("RM_Drill - Ejectors Disabled, 80% cargo\n");
                    }
                }

                asteroidDetected = !sensor.LastDetectedEntity.IsEmpty(); // get sensor info (shortcut)

                // Set Up Ship Rotation
                angle = gyroCtl.Align(rc, gyroList);

                // Convert Current location to in-grid location
                tfCurPos.X = Vector3D.Dot(tfM.Translation + curPos, tfM.Right);
                tfCurPos.Y = Vector3D.Dot(tfM.Translation + curPos, tfM.Up);
                tfCurPos.Z = Vector3D.Dot(tfM.Translation + curPos, tfM.Backward);

                // Calculate Horizontal and Vertical distance
                horDist = Math.Abs((tfCurPos.X - targPos.X));
                verDist = Math.Abs((tfCurPos.Y - targPos.Y));

                if (angle >= rotAErr)
                {
                    if (horDist > maxHorDist)
                    {
                        // enable horizontal thrusters
                        if ((reverse && tfCurPos.X < targPos.X) || (!reverse && tfCurPos.X > targPos.X))
                        {
                            thrCtl.SetThrust(rc, thrCtl.PORT, maxHorAcc); // enable thrusters
                        }
                        else
                        {
                            thrCtl.SetThrust(rc, thrCtl.STARBOARD, maxHorAcc); // enable thrusters
                        }
                    }
                    else
                    {
                        thrCtl.SetThrust(rc, thrCtl.PORT + thrCtl.STARBOARD, 0); // disable hor thr
                    }

                    if (verDist > maxVerDist)
                    {
                        // enable verticle thrusters
                        if (tfCurPos.Y < targPos.Y)
                        {
                            thrCtl.SetThrust(rc, thrCtl.DORSAL, maxVerAcc); // enable thrusters
                        }
                        else
                        {
                            thrCtl.SetThrust(rc, thrCtl.VENTRAL, maxVerAcc); // enable thrusters
                        }
                    }
                    else
                    {
                        thrCtl.SetThrust(rc, thrCtl.DORSAL + thrCtl.VENTRAL, 0); // disable ver thr
                    }

                    if (horDist <= maxHorDist && verDist <= maxVerDist)
                    {
                        if (!asteroidDetected) // check sensor
                        {
                            thrCtl.SetThrust(rc, thrCtl.ALL, 0); // turn off all thrusters
                            shaftN++; // increase shaft number
                            GetSpiralXY(shaftN, gridSide, out shaftX, out shaftY); // get shaft location
                            targPos.X = shaftX * shipWidth; // get actual x co-ord for shaft (grid)
                            targPos.Y = shaftY * shipWidth; // get actual y co-ord for shaft (grid)

                            subMode = 0; // reset submode

                            if ((shaftN % shaftPerRun) == 0) // completed runs?
                            {
                                thrCtl.SetThrust(rc, thrCtl.ALL, 0); // disable thrusters
                                mode = "ReturnToBase"; // Return to Base
                                logCtl.AddLog("RM_Drill - Moving to ReturnToBase\n");
                            }
                            else
                            {
                                thrCtl.SetThrust(rc, thrCtl.ALL, 0); // disable thrusters
                                mode = "GridAlign"; // set mode to drill
                                logCtl.AddLog("RM_Drill - Moving to GridAlign\n");
                            }

                            TBOnOff(ejectorList, false); // turn off ejectors
                            TBOnOff(drillList, false); // turn off drills
                        }
                        else if ((crgCtl.VolPct > 99.0f) && returnIfFull)
                        {
                            if (ejectStone)
                            { pause = true; } // wait before running the PB again

                            thrCtl.SetThrust(rc, thrCtl.ALL, 0); // disable thrusters
                            TBOnOff(ejectorList, false); // turn off ejectors
                            TBOnOff(drillList, false); // turn off drills
                            mode = "ClearShaft"; // Change mode
                            subMode = 0; // reset submode
                            logCtl.AddLog("RM_Drill - Moving to ClearShaft\n");
                        }
                        else
                        {
                            if (rc.GetShipSpeed() < .80 * maxSpd)
                            {
                                maxFwdAcc = 1.0f; // accel to 1 m/s^2
                            }
                            else if (rc.GetShipSpeed() < maxSpd)
                            {
                                maxFwdAcc = 0.05f; // accel to 0.05 m/s^2
                            }
                            else if (rc.GetShipSpeed() >= maxSpd)
                            {
                                maxFwdAcc = 0.0f; // accel to 0.0 m/s^2
                            }
                            else if (rc.GetShipSpeed() < 1.0f)
                            {
                                maxFwdAcc = 3.0f; // accel to 3.0 m/s^2
                            }

                            thrCtl.SetThrust(rc, thrCtl.AFT, maxFwdAcc); // enable thrusters
                        }
                    }
                    else
                    {
                        thrCtl.SetThrust(rc, thrCtl.AFT + thrCtl.FORWARD, 0); // disable fwd/rev thr
                    }
                }
                else
                {
                    // shut down all thrusters
                    thrCtl.SetThrust(rc, thrCtl.ALL, 0); // turn off all thrusters
                }

                if (pause)
                { Delay(10); } // delay the program
                else
                { Delay(); } // Trigger the program again
            } // RunMode_Drill

            protected void RunMode_ClearShaft()
            {
                double horDist = 0, verDist = 0; // horizontal and vertical distance (offset)
                float maxSpd = 3f; // maximum speed
                double maxHorDist = 0.1, maxVerDist = 0.1; // maximum Horizontal and Vertical Distance from target
                float maxHorAcc = 0.1f, maxVerAcc = 0.1f, maxFwdAcc = 1f; // maximum horizontal, vertical and forward accelleration

                if (subMode == 0)
                {
                    logCtl.AddLog("RM_ClearShaft - Setup\n");
                    // Check Direction
                    if ((shaftPerRun > 1) && (shaftN % 2 == 0)) // check for direction
                    {
                        reverse = true; // set reverse
                    }

                    // Set Target Z based on direction
                    if (reverse)
                    {
                        targPos.Z = -1000; // set target Z to 1000 meters beyond grid
                    }
                    else
                    {
                        targPos.Z = 0; // set target Z to 0 (grid entry)
                    }

                    // Create Transformation Matrix
                    tfM = MatrixD.CreateFromQuaternion(gridRot) * MatrixD.Invert(MatrixD.CreateTranslation(gridPos));

                    // Set Gyro rotation data
                    gyroCtl.RotationError = rotAErr; // set rotation error
                    gyroCtl.SetRotation(gridRot, reverse); // set rotation q and reverse

                    // Kill Drills
                    TBOnOff(drillList, false);

                    subMode = 1; // set subMode to 1
                } // if subMode 0

                // Set Up Ship Rotation
                angle = gyroCtl.Align(rc, gyroList);

                // Convert Current location to in-grid location
                tfCurPos.X = Vector3D.Dot(tfM.Translation + curPos, tfM.Right);
                tfCurPos.Y = Vector3D.Dot(tfM.Translation + curPos, tfM.Up);
                tfCurPos.Z = Vector3D.Dot(tfM.Translation + curPos, tfM.Backward);

                // Calculate Horizontal and Vertical distance
                horDist = Math.Abs((tfCurPos.X - targPos.X));
                verDist = Math.Abs((tfCurPos.Y - targPos.Y));

                // Set Max Horizontal Acceleration
                if (horDist > 5) { maxHorAcc = 1.0f; }
                if (horDist > 25) { maxHorAcc = 5.0f; }

                // Set Max Vertical Acceleration
                if (verDist > 5) { maxVerAcc = 1.0f; }
                if (verDist > 25) { maxVerAcc = 5.0f; }

                if (angle >= rotAErr)
                {
                    if (horDist > maxHorDist)
                    {
                        // enable horizontal thrusters
                        if ((reverse && tfCurPos.X < targPos.X) || (!reverse && tfCurPos.X > targPos.X))
                        {
                            thrCtl.SetThrust(rc, thrCtl.PORT, maxHorAcc); // enable thrusters
                        }
                        else
                        {
                            thrCtl.SetThrust(rc, thrCtl.STARBOARD, maxHorAcc); // enable thrusters
                        }
                    }
                    else
                    {
                        thrCtl.SetThrust(rc, thrCtl.PORT + thrCtl.STARBOARD, 0); // disable hor thr
                    }

                    if (verDist > maxVerDist)
                    {
                        // enable verticle thrusters
                        if (tfCurPos.Y < targPos.Y)
                        {
                            thrCtl.SetThrust(rc, thrCtl.DORSAL, maxVerAcc); // enable thrusters
                        }
                        else
                        {
                            thrCtl.SetThrust(rc, thrCtl.VENTRAL, maxVerAcc); // enable thrusters
                        }
                    }
                    else
                    {
                        thrCtl.SetThrust(rc, thrCtl.DORSAL + thrCtl.VENTRAL, 0); // disable ver thr
                    }

                    if (horDist <= maxHorDist && verDist <= maxVerDist)
                    {
                        if (!sensor.LastDetectedEntity.IsEmpty()) // check sensor
                        {
                            thrCtl.SetThrust(rc, thrCtl.ALL, 0); // turn off all thrusters
                            mode = "ReturnToBase"; // set mode to drill
                            subMode = 0; // reset submode
                            logCtl.AddLog("RM_ClearShaft - Moving to ReturnToBase\n");
                        }
                        else
                        {
                            if (rc.GetShipSpeed() < .80 * maxSpd)
                            {
                                maxFwdAcc = 1.0f; // accel to 1 m/s
                            }
                            else if (rc.GetShipSpeed() < maxSpd)
                            {
                                maxFwdAcc = 0.05f; // accel to 0.05 m/s
                            }
                            else if (rc.GetShipSpeed() >= maxSpd)
                            {
                                maxFwdAcc = 0.0f; // accel to 0.0 m/s
                            }

                            thrCtl.SetThrust(rc, thrCtl.FORWARD, maxFwdAcc); // enable thrusters
                        }
                    }
                    else
                    {
                        thrCtl.SetThrust(rc, thrCtl.AFT + thrCtl.FORWARD, 0); // disable fwd/rev thr
                    }
                }
                else
                {
                    // shut down all thrusters
                    thrCtl.SetThrust(rc, thrCtl.ALL, 0); // turn off all thrusters
                }

                Delay(); // Trigger the program again

            } // RunMode_ClearShaft

            protected void RunMode_ReturnToBase()
            {
                int time = 1; // time for delay

                switch (subMode)
                {
                    case 0: // configure autopilot
                        time = 1;
                        rc.ClearWaypoints(); // Clear waypoints
                        rc.ApplyAction("CollisionAvoidance_On"); // Turn on collision avoidance
                        rc.ApplyAction("Forward"); // Configure forward direction
                        rc.AddWaypoint(dockStagePos, "Grid Staging Location"); // set the waypoint
                        rc.ApplyAction("AutoPilot_On"); // turn on the autopilot
                        subMode = 1; // set the next mode
                        logCtl.AddLog("RM_ReturnToBase - AP Set\n");
                        break;
                    case 1: // error checking
                        time = 1;
                        if (rc.IsAutoPilotEnabled)
                        {
                            subMode = 2; // move to next step
                        }
                        else
                        {
                            subMode = 0; // move to previous step
                            logCtl.AddLog("RM_ReturnToBase - AP Error!\n");
                        }
                        break;
                    case 2: // distance checking
                        time = 5;
                        if (Vector3D.DistanceSquared(dockStagePos, curPos) < (1000 * 1000))
                        {
                            time = 1; // set time to 1
                            subMode = 3; // advance to next submode
                        }
                        break;
                    case 3: // checking
                        time = 1;
                        if (Vector3D.DistanceSquared(dockStagePos, curPos) < (apDErr * apDErr))
                        {
                            rc.ApplyAction("AutoPilot_Off"); // turn off autopilot
                            rc.ClearWaypoints(); // clear the waypoints
                            mode = "Dock"; // set next mode
                            subMode = 0; // reset submode
                            logCtl.AddLog("RM_ReturnToBase - Moving to Dock\n");
                        }
                        break;
                } // switch subMode

                Delay(time); // set the delay, run the program again

            } // RunMode_ReturnToBase

            protected void RunMode_Dock()
            {
                double horDist = 0, verDist = 0; // horizontal and vertical distance (offset)
                float maxSpd = 3f; // maximum speed
                double maxHorDist = 0.1, maxVerDist = 0.1; // maximum Horizontal and Vertical Distance from target
                float maxHorAcc = 0.1f, maxVerAcc = 0.1f, maxFwdAcc = 1f; // maximum horizontal, vertical and forward accelleration
                int time = 0; // delay time
                Vector3D tPos = new Vector3D(0, 0, 0); // target position (do not want to use global one)

                time = 0; // reset delay time

                if (subMode == 0)
                {
                    logCtl.AddLog("RM_Dock - Setup\n");
                    tPos.Z = 0; // set target Z to 0 (grid entry)

                    // Create Transformation Matrix
                    tfM = MatrixD.CreateFromQuaternion(dockRot) * MatrixD.Invert(MatrixD.CreateTranslation(dockPos));

                    // Set Gyro rotation data
                    gyroCtl.RotationError = rotAErr; // set rotation error
                    gyroCtl.SetRotation(dockRot); // set rotation q

                    subMode = 1; // set subMode to 1
                } // if subMode 0

                // Set Up Ship Rotation
                angle = gyroCtl.Align(rc, gyroList);

                // Convert Current location to in-grid location
                tfCurPos.X = Vector3D.Dot(tfM.Translation + curPos, tfM.Right);
                tfCurPos.Y = Vector3D.Dot(tfM.Translation + curPos, tfM.Up);
                tfCurPos.Z = Vector3D.Dot(tfM.Translation + curPos, tfM.Backward);

                // Calculate Horizontal and Vertical distance
                horDist = Math.Abs((tfCurPos.X - tPos.X));
                verDist = Math.Abs((tfCurPos.Y - tPos.Y));

                // Set Max Horizontal Acceleration
                if (horDist > 5) { maxHorAcc = 1.0f; }
                if (horDist > 25) { maxHorAcc = 5.0f; }

                // Set Max Vertical Acceleration
                if (verDist > 5) { maxVerAcc = 1.0f; }
                if (verDist > 25) { maxVerAcc = 5.0f; }

                if (subMode == 2)
                {
                    if (connector.Status == MyShipConnectorStatus.Connectable)
                    {
                        connector.ApplyAction("Lock"); // lock the connector
                    }
                    if (connector.Status == MyShipConnectorStatus.Connected)
                    {
                        mode = "Docked"; // set mode to docked
                        subMode = 0; // 
                        logCtl.AddLog("RM_Dock - Moving to Docked\n");
                    }
                }

                if (angle >= rotAErr && subMode == 1)
                {
                    if (horDist > maxHorDist)
                    {
                        // enable horizontal thrusters
                        if ((tfCurPos.X < tPos.X) || (tfCurPos.X > tPos.X))
                        {
                            thrCtl.SetThrust(rc, thrCtl.PORT, maxHorAcc); // enable thrusters
                        }
                        else
                        {
                            thrCtl.SetThrust(rc, thrCtl.STARBOARD, maxHorAcc); // enable thrusters
                        }
                    }
                    else
                    {
                        thrCtl.SetThrust(rc, thrCtl.PORT + thrCtl.STARBOARD, 0); // disable hor thr
                    }

                    if (verDist > maxVerDist)
                    {
                        // enable verticle thrusters
                        if (tfCurPos.Y < tPos.Y)
                        {
                            thrCtl.SetThrust(rc, thrCtl.DORSAL, maxVerAcc); // enable thrusters
                        }
                        else
                        {
                            thrCtl.SetThrust(rc, thrCtl.VENTRAL, maxVerAcc); // enable thrusters
                        }
                    }
                    else
                    {
                        thrCtl.SetThrust(rc, thrCtl.DORSAL + thrCtl.VENTRAL, 0); // disable ver thr
                    }

                    if (horDist <= maxHorDist && verDist <= maxVerDist)
                    {
                        //if (Vector3D.DistanceSquared(dockPos, curPos) > dockDErr * dockDErr)
                        if ((targPos.Z - curPos.Z) > dockDErr)
                        {
                            if (rc.GetShipSpeed() < .80 * maxSpd)
                            {
                                maxFwdAcc = 1.0f; // accel to 1 m/s
                            }
                            else if (rc.GetShipSpeed() < maxSpd)
                            {
                                maxFwdAcc = 0.05f; // accel to 0.05 m/s
                            }
                            else if (rc.GetShipSpeed() >= maxSpd)
                            {
                                maxFwdAcc = 0.0f; // accel to 0.0 m/s
                            }

                            thrCtl.SetThrust(rc, thrCtl.FORWARD, maxFwdAcc); // enable thrusters
                        }
                        else
                        {
                            thrCtl.SetThrust(rc, thrCtl.ALL, 0); // disable all thrusters
                            connector.ApplyAction("ONOFF_ON"); // Turn on the connector
                            subMode = 2; // set next submode
                            time = 10; // set the delay time to 10
                            logCtl.AddLog("RM_Dock - Thruster Shutdown, Try Connect\n");
                        }
                    }
                    else
                    {
                        thrCtl.SetThrust(rc, thrCtl.AFT + thrCtl.FORWARD, 0); // disable fwd/rev thr
                    }
                }
                else
                {
                    // shut down all thrusters
                    thrCtl.SetThrust(rc, thrCtl.ALL, 0); // turn off all thrusters
                }

                Delay(time); // Trigger the program again
            } // RunMode_Dock

            // Supporting Methods ----------------------------------------------

            protected void SetDockCmd(string arg)
            {
                if (arg == "")
                {
                    dockPos = rc.GetPosition(); // get the current position as the dock position
                    haveDockPos = true; // set the flag

                    dockRot = QuaternionD.CreateFromRotationMatrix(rc.WorldMatrix.GetOrientation()); // get the current rotation
                    haveDockRot = true; // set the flag

                    dockStagePos = rc.WorldMatrix.GetOrientation().Forward; // get the initial forward vector
                    dockStagePos *= dockStageDist; // multiply the vector by the staging distance
                    dockStagePos += dockPos; // add the current position to the distance vector
                    haveDockStagePos = true; // set the flag
                    logCtl.AddLog("Added current position to dock variables.");
                }
                else
                {

                }
            } // SetDockCmd method

            protected void SetGridCmd (string arg)
            {
                pgm.Echo("SetGrid\n");
                if (arg == "")
                {
                    pgm.Echo("No arg\n");
                    gridPos = rc.GetPosition(); // get the current position as the dock position
                    haveGridPos = true; // set the flag

                    gridRot = QuaternionD.CreateFromRotationMatrix(rc.WorldMatrix.GetOrientation()); // get the current rotation
                    haveGridRot = true; // set the flag

                    gridStagePos = rc.WorldMatrix.GetOrientation().Backward; // get the initial forward vector
                    gridStagePos *= gridStageDist; // multiply the vector by the staging distance
                    gridStagePos += gridPos; // add the current position to the distance vector
                    haveGridStagePos = true; // set the flag
                    logCtl.AddLog("Added current position to grid variables.");
                }
                else
                {
                    pgm.Echo("Argument\n");
                }

            } // SetGridCmd method

            protected void PauseCmd (string arg)
            {
                if (arg == "")
                {
                    pause = !pause; // swap pause
                }
                else if (arg == "TRUE" || arg == "YES" || arg == "ON")
                {
                    pause = true; // pause it
                } // if arg TRUE/YES/ON
                else if (arg == "FALSE" || arg == "NO" || arg == "OFF")
                {
                    pause = false; // turn pause off
                } // else arg FALSE/NO/OFF

                if (pause)
                {
                    PauseShip(); // pause ship tasks
                } // if pause
                else
                {
                    UnpauseShip(); // unpause ship tasks
                    Delay(1); // start the timer
                } // else pause
            } // PauseCmd method

            protected void ContinueCmd (string arg)
            {
                if (arg == "")
                {
                    cont = !cont; // swap cont
                } // if ""
                else if (arg == "TRUE" || arg == "YES" || arg == "ON")
                {
                    cont = true; // continue on
                } // else if TRUE/YES/ON
                else if (arg == "FALSE" || arg == "NO" || arg == "OFF")
                {
                    cont = false; // continue off
                } // else if FALSE/NO/OFF
            } // ContinueCmd method

            protected void CompleteCmd(string arg)
            {
                // TODO : Cycle complete logic (i.e. docked ship)
                RemoveKey("gridPos"); // remove the key
                haveGridPos = false; // reset the flag
                RemoveKey("gridRot"); // remove the key
                haveGridRot = false; // reset the flag
                RemoveKey("gridStagePos"); // remove the key
                haveGridStagePos = false; // reset the flag
                RemoveKey("dockPos"); // remove the key
                haveDockPos = false; // reset the flag
                RemoveKey("dockRot"); // remove the key
                haveDockRot = false; // reset the flag
                RemoveKey("dockStagePos"); // remove the key
                haveDockStagePos = false; // reset the flag
            } // CompleteCmd method

            protected void PauseShip ()
            {
                switch (mode)
                {
                    case "Docked":
                        // nothing to do
                        break;
                    case "Undock":
                        rc.ApplyAction("AutoPilot_Off"); // Turn off autopilot
                        rc.ApplyAction("DockingMode_Off"); // Turn off Docking Mode
                        rc.ClearWaypoints(); // Clear all waypoints
                        subMode = 1; // reset submode to 1 (when unpaused, re-enable AP)
                        break;
                    case "ToGrid":
                        rc.ApplyAction("AutoPilot_Off"); // turn off autopilot
                        rc.ClearWaypoints(); // clear the waypoints
                        subMode = 0; // reset submode
                        break;
                    case "GridAlign":
                        gyroCtl.Stop(gyroList); // stop all gyros
                        thrCtl.SetThrust(rc, thrCtl.ALL, 0); // stop all thrusters
                        subMode = 0; // reset submode
                        break;
                    case "Drill":
                        gyroCtl.Stop(gyroList); // stop all gyros
                        thrCtl.SetThrust(rc, thrCtl.ALL, 0); // stop all thrusters
                        TBOnOff(drillList, false);
                        TBOnOff(ejectorList, false);
                        subMode = 0; // reset submode
                        break;
                    case "ClearShaft":
                        gyroCtl.Stop(gyroList); // stop all gyros
                        thrCtl.SetThrust(rc, thrCtl.ALL, 0); // stop all thrusters
                        break;
                    case "ReturnToBase":
                        rc.ApplyAction("AutoPilot_Off"); // turn off autopilot
                        rc.ClearWaypoints(); // clear the waypoints
                        subMode = 0; // reset submode
                        break;
                    case "Dock":
                        gyroCtl.Stop(gyroList); // stop all gyros
                        thrCtl.SetThrust(rc, thrCtl.ALL, 0); // stop all thrusters
                        subMode = 0; // reset submode
                        break;
                    default:
                        break;
                } // switch mode

            } // PauseShip method

            protected void UnpauseShip ()
            {
                /*switch (mode)
                {
                    case "Docked":
                        break;
                    case "Undock":
                        break;
                    case "ToGrid":
                        break;
                    case "GridAlign":
                        break;
                    case "Drill":
                        break;
                    case "ClearShaft":
                        break;
                    case "ReturnToBase":
                        break;
                    case "Dock":
                        break;
                    default:
                        break;
                } // switch mode
                */
            } // UnpauseShip method

            protected void RunUpdate()
            {

            } // RunUpdate method

            protected void UpdateDisplay()
            {
                string s = ""; // temp string for formatting

                // Drone Status Screen(s)
                s = "Drone Status\nMode: " + mode + "\nSubmode: " + subMode.ToString() + "\n"; // Mode, submode

                if (pause)
                {
                    s += "Paused\n"; // Paused
                }

                if (cont)
                {
                    s += "Continue\n"; // Continue
                }

                dspCtl.WriteDroneStatus(s); // write it

                // Grid Info Screen(s)
                if ((mode == "GridAlign") || (mode == "Drill") || (mode == "ClearShaft"))
                {
                    s = "Grid Info - Asteroid\nShaft #: " + shaftN.ToString() + "\nX: " + targPos.X.ToString() + " Y: " + targPos.Y.ToString() + " Z: " + targPos.Z.ToString();
                }
                else if (mode == "Dock")
                {
                    s = "Grid Info - Docking\nWorld Location:\nX: " + Math.Round(dockPos.X, 2).ToString() + " Y: " + Math.Round(dockPos.Y, 2).ToString() + " Z: " + Math.Round(dockPos.Z, 2).ToString() + "\n";
                }
                else
                {
                    s = "Grid Info\nNo Grid Assigned";
                }
                dspCtl.WriteGridInfo(s); // write it

                // World Status Screen(s)
                s = "World Status\nX: " + Math.Round(curPos.X,2).ToString() + "\nY: " + Math.Round(curPos.Y,2).ToString() + "\nZ: " + Math.Round(curPos.Z,2) + "\nSpeed: " + rc.GetShipSpeed().ToString();
                dspCtl.WriteWorldStatus(s); // write it

                // Grid Status Screen(s)
                s = "Grid Position\nX: " + Math.Round(tfCurPos.X,2).ToString() + "\nY: " + Math.Round(tfCurPos.Y,2).ToString() + "\nZ: " + Math.Round(tfCurPos.Z,2) + "\nRotation: " + angle.ToString();
                dspCtl.WriteGridStatus(s); // write it

                // Cargo Status Screen(s)
                s = "Cargo Status\nCurrent Volume: " + crgCtl.CurVol.ToString("N") + "\nMax Volume: " + crgCtl.MaxVol.ToString("N") + "\n% Full: " + crgCtl.VolPct.ToString();
                dspCtl.WriteCargoStatus(s); // write it

                // Cargo Inventory Screen(s)
                dspCtl.WriteCargoInventory(crgCtl.GetInvStr()); // write it
            } // UpdateDisplay method

            protected void Delay(int sec = 0)
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

            protected void UnloadCargo(IMyGridTerminalSystem gts)
            {
                List<IMyTerminalBlock> destcargoList = new List<IMyTerminalBlock>();
                int i = 0;
                int j = 0;
                int k = 0;

                blockList.Clear(); // clear all blocks from our block list
                blockList.AddRange(cargoList); // add local cargo blocks
                blockList.AddRange(drillList); // add local drills
                blockList.AddRange(ejectorList); // add local ejectors
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

            protected void ReactorRefuel(IMyGridTerminalSystem gts)
            {
                int i = 0; // counter
                //int j = 0; // counter
                //IMyTerminalBlock reactor = null;
                //IMyTerminalBlock sourceReactor = null;

                /* TODO: This function needs to be cleaned up. It needs to support moving
                 * uranium from multiple reactors TO multiple reactors. It also needs to be able
                 * to move a varying amount to not empty a source reactor, and only move the required
                 * amount to the target reactor
                 * */

                //VRage.MyFixedPoint test;
                //float x = 10f;

                //test = (VRage.MyFixedPoint)x;

                /* Could set minimums for reactor data in the CustomData for each reactor
                 * like "RefuelSource:60" or similar, then have the refuel code help with that.
                 * */

                blockList.Clear();
                gts.GetBlocksOfType<IMyReactor>(blockList, g => g.CubeGrid != rc.CubeGrid);

                for (i = 0; i < blockList.Count; i++)
                {
                    if (blockList[i].GetInventory(0).GetItems()[0].Amount < 2 * minUr)
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
                    if (reactorList[i].GetInventory(0).GetItems()[0].Amount < minUr)
                    {

                    }
                }

            } // ReactorRefuel method

            protected void UnlockConnector()
            {
                connector.ApplyAction("Unlock");
                connector.ApplyAction("OnOff_Off");
            } // UnlockConnector method

            protected void TBOnOff(List<IMyTerminalBlock> blockList, bool onoff = true)
            {
                string cmd = ""; // Command to apply
                int i = 0; // counter for block list

                if (onoff)
                { cmd = "OnOff_On"; } // turn block on
                else
                { cmd = "OnOff_Off"; } // turn block off

                for (i = 0; i < blockList.Count; i++)
                {
                    blockList[i].ApplyAction(cmd); // apply the command
                } // for loop
            } //TBOnOff()

            protected void GetSpiralXY (int p, int n, out int X, out int Y)
            {
                /* This method was shamelessly stolen from the script that Pennywise built for his
                 * automated mining drone(s). 
                 * http://steamcommunity.com/profiles/76561198124730542/myworkshopfiles/?appid=244850
                 */

                int positionX = 0, positionY = 0, direction = 0, stepsCount = 1, stepPosition = 0, stepChange = 0;
                X = 0;
                Y = 0;
                for (int i = 0; i < n * n; i++)
                {
                    if (i == p)
                    {
                        X = positionX;
                        Y = positionY;
                        return;
                    }
                    if (stepPosition < stepsCount)
                    {
                        stepPosition++;
                    }
                    else
                    {
                        stepPosition = 1;
                        if (stepChange == 1)
                        {
                            stepsCount++;
                        }
                        stepChange = (stepChange + 1) % 2;
                        direction = (direction + 1) % 4;
                    }
                    if (direction == 0) { positionY++; }
                    else if (direction == 1) { positionX--; }
                    else if (direction == 2) { positionY--; }
                    else if (direction == 3) { positionX++; }
                }
            } // GetSpiralXY method

        } // MiningAutoPilotShip class
    } // Program partial class
} // Namespace
