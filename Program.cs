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
    partial class Program : MyGridProgram
    {
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area.
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts


        public enum Steps
        {
            Init,
            RightDisconnect,
            PistonExtend,
            RightConnect,
            LeftDisconnect,
            PistonRetract,
            LeftConnect
        }

        private Steps CurrentStep { get; set; }
        private IMyShipMergeBlock rightMergeBlock;
        private IMyShipMergeBlock leftMergeBlock;
        private IMyExtendedPistonBase leftPiston;
        private IMyExtendedPistonBase rightPiston;
        private IMyProjector projector;
        private IMyShipConnector connector;
        private List<IMyShipWelder> welders;
        private List<IMyShipGrinder> grinders;


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
            //if ((updateSource & UpdateType.Terminal) != 0)
            if(argument == "init")
            {
                Echo("Rerun");
                CurrentStep = Steps.Init;
            }
            if(argument =="pause")
            {
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
            if(argument == "start")
            {
                //Update10 marche et du coup c'est plus réactif, mais avec des glitches visuels chez moi donc peut-être risqué.
                Runtime.UpdateFrequency = UpdateFrequency.Update100; 
            }

            Echo($"CurrentStep = {CurrentStep}");
            switch (CurrentStep)
            {
                case Steps.Init:
                    Echo("Init");
                    rightMergeBlock = GetBlockFromGroup<IMyShipMergeBlock>("RIGHT");
                    leftMergeBlock = GetBlockFromGroup<IMyShipMergeBlock>("LEFT");
                    leftPiston = GetBlockFromGroup<IMyExtendedPistonBase>("PLEFT");
                    rightPiston = GetBlockFromGroup<IMyExtendedPistonBase>("PRIGHT");

                    projector = GetBlockFromGroup<IMyProjector>("PROJ");
                    connector = GetBlockFromGroup<IMyShipConnector>("CONNECT");
                    welders = GetBlocksFromGroup<IMyShipWelder>("Welders");
                    grinders = GetBlocksFromGroup<IMyShipGrinder>("Grinders");

                    Echo($"RMerge = {rightMergeBlock.CustomName}");
                    Echo($"LMerge = {leftMergeBlock.CustomName}");
                    Echo($"LPiston = {leftPiston.CustomName}");
                    Echo($"RPiston = {rightPiston.CustomName}");
                    Echo($"Projector = {projector.CustomName}");
                    Echo($"Welders = {string.Join(",", welders.Select(w => w.CustomName))}");
                    Echo($"Grinders = {string.Join(",", grinders.Select(w => w.CustomName))}");

                    //Setup settings
                    //leftPiston.Velocity = -0.5f;
                    //rightPiston.Velocity = -0.5f;
                    grinders.ForEach(g => g.Enabled = true);
                    welders.ForEach(w => w.Enabled = true);
                    //rightMergeBlock.Enabled = true;
                    //leftMergeBlock.Enabled = true;

                    //Check objects status to initialize to proper step !

                    CurrentStep++;
                    break;
                case Steps.RightDisconnect:
                    if (rightMergeBlock.Enabled)
                    {
                        Echo("Right Merge Disable");
                        rightMergeBlock.Enabled = false;
                        break;
                    }
                    //wait step ?
                    if (rightMergeBlock.Enabled == false)
                    {
                        Echo("Right Merge Disabled");
                        CurrentStep++;
                    }
                    break;
                case Steps.PistonExtend:
                    if (rightPiston.Status != PistonStatus.Extended && rightPiston.Status != PistonStatus.Extending)
                    {
                        Echo("Piston Extend");
                        rightPiston.Extend();
                    }
                    if (rightPiston.Status == PistonStatus.Extended) 
                    {
                        Echo("Piston extended");
                        CurrentStep++;
                    }
                    break;
                case Steps.RightConnect:
                    if (rightMergeBlock.Enabled == false)
                    {
                        Echo("Right Merge Enable");
                        rightMergeBlock.Enabled = true;
                        break;
                    }
                    //wait ?
                    if (rightMergeBlock.Enabled)
                    {
                        Echo("Right Merge Enabled");
                        CurrentStep++;
                    }
                    break;
                case Steps.LeftDisconnect:
                    if(connector.Status == MyShipConnectorStatus.Connected)
                    {
                        Echo("Connector Disconnect");
                        connector.Disconnect();
                    }
                    if(connector.Status != MyShipConnectorStatus.Connected && connector.Enabled)
                    {
                        Echo("Connector Disable");
                        connector.Enabled = false;
                    }
                    if (projector.Enabled)
                    {
                        Echo("Projector disable");
                        projector.Enabled = false;
                    }
                    if (leftMergeBlock.Enabled == true)
                    {
                        Echo("Left Merge Disable");
                        leftMergeBlock.Enabled = false;
                        break;
                    }
                    //wait ?
                    if (leftMergeBlock.Enabled == false && connector.Status != MyShipConnectorStatus.Connected && connector.Enabled == false)
                    {
                        Echo("Left Merge Disabled");
                        Echo("Connector Disconnected");
                        CurrentStep++;
                    }
                    break;
                case Steps.PistonRetract:
                    if (rightPiston.Status != PistonStatus.Retracted && rightPiston.Status != PistonStatus.Retracting)
                    {
                        Echo("Piston Retract");
                        rightPiston.Retract();
                    }
                    if (rightPiston.Status == PistonStatus.Retracted)
                    {
                        Echo("Piston Retracted");
                        CurrentStep++;
                    }
                    break;
                case Steps.LeftConnect:
                    if(connector.Enabled == false)
                    {
                        Echo("Connector enable");
                        connector.Enabled = true;
                    }
                    if(connector.Status == MyShipConnectorStatus.Connectable)
                    {
                        Echo("Connector Connect");
                        connector.Connect();
                    }
                    else
                    {
                        Echo("CONNECTOR CANNOT BE CONNECTED");
                    }
                    if (!projector.Enabled)
                    {
                        Echo("Projector Enable");
                        projector.Enabled = true;
                    }
                    if (leftMergeBlock.Enabled == false)
                    {
                        Echo("Left Merge Enable");
                        leftMergeBlock.Enabled = true;
                        break;
                    }
                    //wait ?
                    if (leftMergeBlock.Enabled && connector.Status ==  MyShipConnectorStatus.Connected && connector.Enabled)
                    {
                        Echo("Left Merge Enabled");
                        Echo("Connector Connected");
                        CurrentStep++;
                    }                        
                    break;
                default:
                    Echo("Loop");
                    CurrentStep = (Steps)1;
                    break;
            }
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
