using System;
using System.Collections.Generic;
using Cogbot.World;
using OpenMetaverse;
using MushDLR223.ScriptEngines;

namespace Cogbot.Actions.Objects
{
    public class DropCommand : Command, BotPersonalCommand
    {
        public DropCommand(BotClient testClient)
        {
            Name = "drop";
        }

        public override void MakeInfo()
        {
            Description = "drops a specified attachment into the world";
            Details = "drop <prim|attachmentPoint> example: /drop LeftHand ";
            Category = CommandCategory.Objects;
            Parameters = CreateParams("targets", typeof (PrimSpec), "The objects to " + Name);
        }

        public override CmdResult ExecuteRequest(CmdRequest args)
        {
            if (args.Length < 1)
                return ShowUsage();

            int argsUsed;
            List<SimObject> PS = WorldSystem.GetPrimitives(args, out argsUsed);
            if (IsEmpty(PS))
            {
                object obj;
                if (TryEnumParse(typeof (AttachmentPoint), args, 0, out argsUsed, out obj))
                {
                    AttachmentPoint detachFrom = (AttachmentPoint) obj;
                    foreach (var s in TheSimAvatar.Children)
                    {
                        Primitive prim = s.Prim;
                        if (prim != null)
                        {
                            if (s.Prim.PrimData.AttachmentPoint == detachFrom)
                            {
                                AddSuccess(string.Format("[dropping @ {0} Offset: {1}] {2}",
                                                         prim.PrimData.AttachmentPoint,
                                                         prim.Position, s));
                                Client.Objects.DropObject(s.GetSimulator(), s.LocalID);
                            }
                        }
                    }
                    return SuccessOrFailure();
                }
                return Failure("Cannot find objects from " + args.str);
            }
            foreach (var found in PS)
            {
                Client.Objects.DropObject(found.GetSimulator(), found.LocalID);
                AddSuccess("dropping " + found);
            }
            return SuccessOrFailure();
        }
    }
}