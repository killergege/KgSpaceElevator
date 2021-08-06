using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    // detecter l'état
    // constantiser tout
    // plusieurs pistons sur le mm bras (chainer les pistons)
    // nb max d'itération ou 0

    partial class Program : MyGridProgram
    {
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area.
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts


        public enum Steps
        {
            RightDisconnect,
            PistonExtend,
            RightConnect,
            LeftDisconnect,
            PistonRetract,
            LeftConnect
        }

        private Steps CurrentStep;
        private IMyShipMergeBlock rightMergeBlock;
        private IMyShipMergeBlock leftMergeBlock;
        private IMyExtendedPistonBase leftPiston;
        private IMyExtendedPistonBase rightPiston;
        private IMyProjector projector;
        private IMyShipConnector connector;
        private List<IMyShipWelder> welders;
        private List<IMyShipGrinder> grinders;        

        private Steps PreviousStep;
        private int Wait = 0;
        private bool Initialized = false;
        private List<string> EchoStrings= new List<string>();

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            Runtime.UpdateFrequency = UpdateFrequency.None;
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (!Initialized)
                Initialize();

            //if ((updateSource & UpdateType.Terminal) != 0)
            if (argument == "pause" || argument == "stop")
            {
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
            if(argument == "start")
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                CurrentStep = CalculateCurrentStep();
            }

            // -----------------

            if(CurrentStep != PreviousStep)
                EchoStrings.Add($"CurrentStep = {CurrentStep}");
            PreviousStep = CurrentStep;

            switch (CurrentStep)
            {
                case Steps.RightDisconnect:
                    if (rightMergeBlock.Enabled)
                    {
                        EchoStrings.Add("Right Merge Disable");
                        rightMergeBlock.Enabled = false;
                        break;
                    }
                    //wait step ?
                    if (rightMergeBlock.Enabled == false)
                    {
                        EchoStrings.Add("Right Merge Disabled");
                        CurrentStep++;
                    }
                    break;
                case Steps.PistonExtend:
                    if (rightPiston.Status != PistonStatus.Extended && rightPiston.Status != PistonStatus.Extending)
                    {
                        EchoStrings.Add("Piston Extend");
                        rightPiston.Extend();
                    }
                    if (rightPiston.Status == PistonStatus.Extended) 
                    {
                        EchoStrings.Add("Piston extended");
                        CurrentStep++;
                    }
                    break;
                case Steps.RightConnect:
                    if (rightMergeBlock.Enabled == false)
                    {
                        EchoStrings.Add("Right Merge Enable");
                        rightMergeBlock.Enabled = true;
                        Wait = 10;
                        break;
                    }
                    //wait ?
                    if (rightMergeBlock.Enabled && rightMergeBlock.IsConnected && Wait == 0)
                    {
                        EchoStrings.Add("Right Merge Enabled");
                        CurrentStep++;
                    }
                    break;
                case Steps.LeftDisconnect:
                    if(connector.Status == MyShipConnectorStatus.Connected)
                    {
                        EchoStrings.Add("Connector Disconnect");
                        connector.Disconnect();
                    }
                    if(connector.Status != MyShipConnectorStatus.Connected && connector.Enabled)
                    {
                        EchoStrings.Add("Connector Disable");
                        connector.Enabled = false;
                    }
                    if (projector.Enabled)
                    {
                        EchoStrings.Add("Projector disable");
                        projector.Enabled = false;
                    }
                    if (leftMergeBlock.Enabled)
                    {
                        EchoStrings.Add("Left Merge Disable");
                        leftMergeBlock.Enabled = false;
                        break;
                    }
                    //wait ?
                    if (leftMergeBlock.Enabled == false && connector.Status != MyShipConnectorStatus.Connected && connector.Enabled == false)
                    {
                        EchoStrings.Add("Left Merge Disabled");
                        EchoStrings.Add("Connector Disconnected");
                        CurrentStep++;
                    }
                    break;
                case Steps.PistonRetract:
                    if (rightPiston.Status != PistonStatus.Retracted && rightPiston.Status != PistonStatus.Retracting)
                    {
                        EchoStrings.Add("Piston Retract");
                        rightPiston.Retract();
                    }
                    if (rightPiston.Status == PistonStatus.Retracted)
                    {
                        EchoStrings.Add("Piston Retracted");
                        CurrentStep++;
                    }
                    break;
                case Steps.LeftConnect:
                    if(connector.Enabled == false)
                    {
                        EchoStrings.Add("Connector enable");
                        connector.Enabled = true;
                    }
                    if(connector.Status == MyShipConnectorStatus.Connectable)
                    {
                        EchoStrings.Add("Connector Connect");
                        connector.Connect();
                    }
                    else
                    {
                        EchoStrings.Add("CONNECTOR CANNOT BE CONNECTED");
                    }
                    if (!projector.Enabled)
                    {
                        EchoStrings.Add("Projector Enable");
                        projector.Enabled = true;
                    }
                    if (leftMergeBlock.Enabled == false)
                    {
                        EchoStrings.Add("Left Merge Enable");
                        leftMergeBlock.Enabled = true;
                        break;
                    }
                    //wait ?
                    if (leftMergeBlock.Enabled && leftMergeBlock.IsConnected && connector.Status ==  MyShipConnectorStatus.Connected && connector.Enabled)
                    {
                        EchoStrings.Add("Left Merge Enabled");
                        EchoStrings.Add("Connector Connected");
                        CurrentStep++;
                    }                        
                    break;
                default:
                    EchoStrings.Clear();
                    EchoStrings.Add("Loop");
                    CurrentStep = 0;
                    break;
            }
            if(Wait > 0)
                Wait--;
            Echo(string.Join(Environment.NewLine, EchoStrings));
        }

        public Steps CalculateCurrentStep()
        {
            if (leftMergeBlock.Enabled && leftMergeBlock.IsConnected && connector.Status == MyShipConnectorStatus.Connected && connector.Enabled && rightPiston.Status == PistonStatus.Retracted && rightMergeBlock.IsConnected)
                return Steps.RightDisconnect;
            if (rightMergeBlock.Enabled == false && (rightPiston.Status == PistonStatus.Retracted || rightPiston.Status == PistonStatus.Extending))
                return Steps.PistonExtend;
            if (rightPiston.Status == PistonStatus.Extended && rightMergeBlock.Enabled == false)
                return Steps.RightConnect;
            if (rightMergeBlock.Enabled && rightMergeBlock.IsConnected && leftMergeBlock.Enabled 
                && (connector.Enabled || projector.Enabled))
                return Steps.LeftDisconnect;
            if (leftMergeBlock.Enabled == false && connector.Enabled == false && (rightPiston.Status == PistonStatus.Extended || rightPiston.Status == PistonStatus.Retracting))
                return Steps.PistonRetract;
            if (rightPiston.Status == PistonStatus.Retracted && connector.Status == MyShipConnectorStatus.Connectable)
                return Steps.LeftConnect;
            throw new Exception("Current step not found");
        }

        public void Initialize()
        {
            EchoStrings.Add("Init");
            rightMergeBlock = GetBlockFromGroup<IMyShipMergeBlock>("RIGHT");
            leftMergeBlock = GetBlockFromGroup<IMyShipMergeBlock>("LEFT");
            leftPiston = GetBlockFromGroup<IMyExtendedPistonBase>("PLEFT");
            rightPiston = GetBlockFromGroup<IMyExtendedPistonBase>("PRIGHT");

            projector = GetBlockFromGroup<IMyProjector>("PROJ");
            connector = GetBlockFromGroup<IMyShipConnector>("CONNECT");
            welders = GetBlocksFromGroup<IMyShipWelder>("Welders");
            grinders = GetBlocksFromGroup<IMyShipGrinder>("Grinders");

            EchoStrings.Add($"RMerge = {rightMergeBlock.CustomName}");
            EchoStrings.Add($"LMerge = {leftMergeBlock.CustomName}");
            EchoStrings.Add($"LPiston = {leftPiston.CustomName}");
            EchoStrings.Add($"RPiston = {rightPiston.CustomName}");
            EchoStrings.Add($"Projector = {projector.CustomName}");
            EchoStrings.Add($"Welders = {string.Join(",", welders.Select(w => w.CustomName))}");
            EchoStrings.Add($"Grinders = {string.Join(",", grinders.Select(w => w.CustomName))}");

            //Setup settings
            //leftPiston.Velocity = -0.5f;
            //rightPiston.Velocity = -0.5f;
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
            group.GetBlocksOfType(blocks);
            return blocks;
        }
    }
}
