using System.Collections.Generic;
using Cogbot.World;
using OpenMetaverse;
using MushDLR223.ScriptEngines;

namespace Cogbot.Actions.Pathfinder
{
    public class UnmeshPrim : Cogbot.Actions.Command, SystemApplicationCommand
    {
        public UnmeshPrim(BotClient client)
        {
            Name = GetType().Name;
        }

        public override void MakeInfo()
        {
            Description = "Unmeshes all prims and removes collision planes.";
            Category = Cogbot.Actions.CommandCategory.Movement;
            Parameters = CreateParams("targets", typeof (PrimSpec), "The objects to " + Name);
        }

        public override CmdResult ExecuteRequest(CmdRequest args)
        {
            int argsUsed;
            ICollection<SimObject> objs;
            bool rightNow = true;
            if (!args.TryGetValue("targets", out objs))
            {
                objs = (ICollection<SimObject>) WorldSystem.GetAllSimObjects();
                rightNow = false;
            }
            WriteLine("Unmeshing " + objs.Count);
            foreach (SimObject o2 in objs)
            {
                SimObjectPathFinding o = o2.PathFinding;

                o.IsWorthMeshing = true;
                if (rightNow)
                {
                    o.RemoveCollisions();
                }
                else
                {
                    o.RemoveCollisions();
                }
            }
            if (rightNow)
            {
                TheSimAvatar.GetSimRegion().GetPathStore(TheSimAvatar.SimPosition).RemoveAllCollisionPlanes();
            }
            else
            {
                TheSimAvatar.GetSimRegion().GetPathStore(TheSimAvatar.SimPosition).RemoveAllCollisionPlanes();
            }

            return TheBotClient.ExecuteCommand("meshinfo", args.CallerAgent, args.Output, args.CmdFlags);
        }
    }
}