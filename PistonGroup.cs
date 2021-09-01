using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    partial class Program
    {
        public class PistonGroup
        {
            enum Command { Extend, Retract }
            
            List<IMyExtendedPistonBase> Pistons;
            Command LastCommand;

            public PistonGroup(List<IMyExtendedPistonBase> pistons)
            {
                Pistons = pistons;
            }

            public PistonStatus Status { 
                get
                {
                    if (Pistons.All(p => p.Status == PistonStatus.Extended))
                        return PistonStatus.Extended;
                    if (Pistons.All(p => p.Status == PistonStatus.Retracted))
                        return PistonStatus.Retracted;
                    if (Pistons.Any(p => p.Status == PistonStatus.Retracting) || LastCommand == Command.Retract)
                        return PistonStatus.Retracting;
                    if (Pistons.Any(p => p.Status == PistonStatus.Extending) || LastCommand == Command.Extend)
                        return PistonStatus.Extending;

                    throw new Exception($"Cannot get status {string.Join(",", Pistons.Select(x=>x.Status))}");
                } 
            }

            public string CustomName { get { return string.Join(",", Pistons.Select(p => p.CustomName)); } }

            public void Extend()
            {
                LastCommand = Command.Extend;
                foreach(var piston in Pistons)
                {
                    if (piston.Status != PistonStatus.Extended)
                    {
                        piston.Extend();
                        break;
                    }
                }
            }

            public float Velocity
            {
                get
                {
                    return Pistons.FirstOrDefault()?.Velocity ?? 0;
                }
                set
                {
                    foreach (var piston in Pistons)
                        piston.Velocity = value;
                }
            }

            //public float ShareInertiaTensor
            //{
            //    get
            //    {
            //        return Pistons.FirstOrDefault()?.ShareInertiaTensor ?? 0;
            //    }
            //    set
            //    {
            //        foreach (var piston in Pistons)
            //            piston.ShareInertiaTensor = value;
            //    }
            //}

            public void Retract()
            {
                LastCommand = Command.Retract;
                foreach (var piston in Pistons)
                {
                    if (piston.Status != PistonStatus.Retracted)
                    {
                        piston.Retract();
                        break;
                    }
                }
            }
        }
    }
}
