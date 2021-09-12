using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    // constantiser tout => https://github.com/malware-dev/MDK-SE/wiki/Handling-configuration-and-storage

    partial class Program : MyGridProgram
    {
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts

        public enum Steps
        {
            None,
            RightDisconnect,
            PistonExtend,
            RightConnect,
            LeftDisconnect,
            PistonRetract,
            LeftConnect,
            ReloadComponents
        }

        private Steps CurrentStep;
        private IMyShipMergeBlock rightMergeBlock;
        private IMyShipMergeBlock leftMergeBlock;
        private PistonGroup pistons;
        private IMyProjector projector;
        private IMyShipConnector connector;
        private IMyShipConnector baseConnector;
        private List<IMyShipWelder> welders;
        private List<IMyShipGrinder> grinders;
        private IMyTextPanel lcdPanel;
        private int iterations;
        private const int MAX_ITERATIONS = int.MaxValue;
        private const int MAX_DISTANCE = 50000;
        private float PISTON_EXTEND_SPEED = -0.3f;
        private float PISTON_RETRACT_SPEED = 0.7f;
        private int WAIT_FOR_RELOAD = 60; //seconds


        //Technical
        private Steps PreviousStep;
        private int Wait = 0;
        private bool Initialized = false;
        private DateTime StartTime = DateTime.MinValue;
        private double Distance = 0;
        private LogManager Logs;

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            Runtime.UpdateFrequency = UpdateFrequency.None;
            Logs = new LogManager(content => Echo(content));
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
        }

        /// <summary>
        /// "start" to start process
        /// "pause" or "stop" to pause it, use start to restart
        /// "init" to reinit iterations & variables.
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="updateSource"></param>
        public void Main(string argument, UpdateType updateSource)
        {
            if (!Initialized || argument == "init")
                Initialize();

            //if ((updateSource & UpdateType.Terminal) != 0)
            if (argument == "pause" || argument == "stop")
            {
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
            if (argument == "start")
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                CurrentStep = CalculateCurrentStep();
            }

            // -----------------

            if (CurrentStep != PreviousStep)
                Logs.Add($"CurrentStep = {CurrentStep}");
            PreviousStep = CurrentStep;

            switch (CurrentStep)
            {
                case Steps.RightDisconnect:
                    StartTime = DateTime.Now;
                    Distance = CalculateDistance(rightMergeBlock, Me);
                    if (rightMergeBlock.Enabled)
                    {
                        Logs.Add("Right Merge Disable");
                        rightMergeBlock.Enabled = false;
                        break;
                    }
                    if (!projector.Enabled)
                    {
                        Logs.Add("Projector Enable");
                        projector.Enabled = true;
                    }
                    if (rightMergeBlock.Enabled == false && projector.Enabled)
                    {
                        Logs.Add("Right Merge Disabled");
                        CurrentStep++;
                    }
                    break;
                case Steps.PistonExtend:                    
                    if (pistons.Status != PistonStatus.Extended)
                    {
                        //EchoStrings.Add("Piston Extend");
                        if(Math.Abs(pistons.Velocity) != Math.Abs(PISTON_EXTEND_SPEED))
                            pistons.Velocity = PISTON_EXTEND_SPEED;
                        pistons.Extend();
                    }
                    if (pistons.Status == PistonStatus.Extended)
                    {
                        Logs.Add("Piston extended");
                        CurrentStep++;
                    }
                    break;
                case Steps.RightConnect:
                    if (rightMergeBlock.Enabled == false)
                    {
                        Logs.Add("Right Merge Enable");
                        rightMergeBlock.Enabled = true;
                        Wait = 10;
                        break;
                    }
                    if (projector.RemainingBlocks > 0)
                    {                        
                        Logs.Add($"Could not build all blocks (remaining : {projector.RemainingBlocks}) !");
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                    }
                    if (rightMergeBlock.Enabled && rightMergeBlock.IsConnected && Wait == 0)
                    {
                        Logs.Add("Right Merge Enabled");
                        CurrentStep++;
                    }
                    break;
                case Steps.LeftDisconnect:
                    if (connector.Status == MyShipConnectorStatus.Connected)
                    {
                        Logs.Add("Connector Disconnect");
                        connector.Disconnect();
                    }
                    if (connector.Status != MyShipConnectorStatus.Connected && connector.Enabled)
                    {
                        Logs.Add("Connector Disable");
                        connector.Enabled = false;
                    }
                    if (projector.Enabled)
                    {
                        Logs.Add("Projector disable");
                        projector.Enabled = false;
                    }
                    if (leftMergeBlock.Enabled)
                    {
                        Logs.Add("Left Merge Disable");
                        leftMergeBlock.Enabled = false;
                        break;
                    }
                    if (leftMergeBlock.Enabled == false && connector.Status != MyShipConnectorStatus.Connected && connector.Enabled == false)
                    {
                        Logs.Add("Left Merge Disabled");
                        Logs.Add("Connector Disconnected");
                        CurrentStep++;
                    }
                    break;
                case Steps.PistonRetract:
                    if (pistons.Status != PistonStatus.Retracted)
                    {
                        //EchoStrings.Add("Piston Retract");
                        if (Math.Abs(pistons.Velocity) != Math.Abs(PISTON_RETRACT_SPEED))
                            pistons.Velocity = PISTON_RETRACT_SPEED;
                        pistons.Retract();
                    }
                    if (pistons.Status == PistonStatus.Retracted)
                    {
                        Logs.Add("Piston Retracted");
                        CurrentStep++;
                    }
                    break;
                case Steps.LeftConnect:
                    if (connector.Enabled == false)
                    {
                        Logs.Add("Connector enable");
                        connector.Enabled = true;
                    }
                    if (connector.Status == MyShipConnectorStatus.Connectable)
                    {
                        Logs.Add("Connector Connect");
                        connector.Connect();
                    }
                    else
                    {
                        Logs.Add("CONNECTOR CANNOT BE CONNECTED");
                    }
                    if (leftMergeBlock.Enabled == false)
                    {
                        Logs.Add("Left Merge Enable");
                        leftMergeBlock.Enabled = true;
                        break;
                    }
                    if (leftMergeBlock.Enabled && leftMergeBlock.IsConnected && connector.Status == MyShipConnectorStatus.Connected && connector.Enabled)
                    {
                        Logs.Add("Left Merge Enabled");
                        Logs.Add("Connector Connected");
                        CurrentStep++;
                    }
                    break;
                case Steps.ReloadComponents:
                    if (iterations % 5 != 0 || iterations == 0)
                    {
                        CurrentStep++;
                        Logs.Add("Skipped.");
                        break;
                    }

                    if (Wait > 0)
                        break;
                    if (Wait == 0 && baseConnector.Status == MyShipConnectorStatus.Connected)
                    {
                        Logs.Add("Finished reloading...");
                        baseConnector.Disconnect();                        
                        CurrentStep++;
                    }
                    else if (Wait == 0)
                    {
                        Logs.Add("Reloading...");
                        baseConnector.Connect();
                        Wait = WAIT_FOR_RELOAD * 10;
                    }
                    break;
                default:
                    var time = StartTime != DateTime.MinValue ? (DateTime.Now - StartTime).TotalSeconds as double? : null;
                    var speed = time != null ? ((CalculateDistance(rightMergeBlock, Me) - Distance) / time.Value) as double?:null;
                    Logs.Add($"Loop ({iterations}) - Time = {time?.ToString()??"?"} seconds. Speed = {speed?.ToString() ?? "?"} m/s");                    

                    iterations++;

                    if (iterations >= MAX_ITERATIONS)
                    {
                        Logs.Add("Max iteration reached");
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                    }
                    if (CalculateDistance(rightMergeBlock, Me) > MAX_DISTANCE)
                    {
                        Logs.Add("Max distance reached");
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                    }
                    CurrentStep = (Steps)1;
                    break;
            }
            if (Wait > 0)
                Wait--;
            Logs.Distance = CalculateDistance(rightMergeBlock, Me);
            Logs.Iteration = iterations;
            Logs.Echo(CurrentStep);           
        }

        public Steps CalculateCurrentStep()
        {
            if (leftMergeBlock.Enabled && leftMergeBlock.IsConnected && connector.Status == MyShipConnectorStatus.Connected && connector.Enabled && pistons.Status == PistonStatus.Retracted && rightMergeBlock.IsConnected)
                return Steps.RightDisconnect;
            if (rightMergeBlock.Enabled == false && pistons.Status != PistonStatus.Extended)
                return Steps.PistonExtend;
            if (pistons.Status == PistonStatus.Extended && rightMergeBlock.Enabled == false)
                return Steps.RightConnect;
            if (rightMergeBlock.Enabled && rightMergeBlock.IsConnected && leftMergeBlock.Enabled
                && (connector.Enabled || projector.Enabled))
                return Steps.LeftDisconnect;
            if (leftMergeBlock.Enabled == false && connector.Enabled == false && pistons.Status != PistonStatus.Retracted)
                return Steps.PistonRetract;
            if (pistons.Status == PistonStatus.Retracted && (connector.Enabled == false || connector.Status == MyShipConnectorStatus.Connectable))
                return Steps.LeftConnect;
            throw new Exception("Current step not found");
        }

        public void Initialize()
        {
            Logs.Add("Init");
            rightMergeBlock = GetBlockFromGroup<IMyShipMergeBlock>("RIGHT");
            leftMergeBlock = GetBlockFromGroup<IMyShipMergeBlock>("LEFT");
            pistons = new PistonGroup(GetBlocksFromGroup<IMyExtendedPistonBase>("PRIGHT"));

            projector = GetBlockFromGroup<IMyProjector>("PROJ");
            connector = GetBlockFromGroup<IMyShipConnector>("CONNECT");
            baseConnector = GetBlockFromGroup<IMyShipConnector>("BASECONNECTOR");
            welders = GetBlocksFromGroup<IMyShipWelder>("Welders");
            grinders = GetBlocksFromGroup<IMyShipGrinder>("Grinders");
            lcdPanel = GetBlockFromGroup<IMyTextPanel>("LCD");
            if(lcdPanel != null)
            {
                Logs.Add($"LCD Panel found");
                lcdPanel.ContentType = ContentType.TEXT_AND_IMAGE;
                Logs.SendLogs = content => { Echo(content); lcdPanel.WriteText(content); };
            }
            else
            {
                Logs.Add($"No LCD Panel");
                Logs.SendLogs = content => Echo(content);
            }

            Logs.Add($"RMerge = {rightMergeBlock.CustomName}");
            Logs.Add($"LMerge = {leftMergeBlock.CustomName}");
            Logs.Add($"RPiston = {pistons.CustomName}");
            Logs.Add($"Projector = {projector.CustomName}");
            Logs.Add($"Welders = {string.Join(",", welders.Select(w => w.CustomName))}");
            Logs.Add($"Grinders = {string.Join(",", grinders.Select(w => w.CustomName))}");
            Logs.Add($"Base connector = {baseConnector.CustomName}");

            iterations = 0;

            //Setup settings
            //leftPiston.Velocity = -0.5f;
            //pistons.Velocity = -0.4f;
            grinders.ForEach(g => g.Enabled = true);
            welders.ForEach(w => w.Enabled = true);
            //rightMergeBlock.Enabled = true;
            //leftMergeBlock.Enabled = true;

            Initialized = true;
        }

        public T GetBlockFromGroup<T>(string groupName) where T : class
        {
            var blocks = GetBlocksFromGroup<T>(groupName);
            return blocks.FirstOrDefault();
        }
        public List<T> GetBlocksFromGroup<T>(string groupName) where T : class
        {
            var group = GridTerminalSystem.GetBlockGroupWithName(groupName);
            var blocks = new List<T>();
            group?.GetBlocksOfType(blocks);
            return blocks;
        }

        public double CalculateDistance(IMyTerminalBlock source, IMyTerminalBlock target)
        {
            var sourcePosition = source.GetPosition();
            var targetPosition = target.GetPosition();
            return Math.Round(Vector3D.Distance(sourcePosition, targetPosition), 2);
        }
    }
}
