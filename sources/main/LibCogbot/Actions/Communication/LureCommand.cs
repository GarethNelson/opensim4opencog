﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using cogbot.TheOpenSims;
using OpenMetaverse;

using MushDLR223.ScriptEngines;

namespace cogbot.Actions.Communication
{
    class LureCommand : Command, BotPersonalCommand
    {
        public LureCommand(BotClient testClient)
        {
            Name = "lure";
            Description = "Send a lure to a user. Usage: lure FirstName LastName";
            Category = CommandCategory.Friends;
            Parameters = new[] { new NamedParam(typeof(SimAvatar), typeof(UUID)) };
        }


        public override CmdResult Execute(string[] args, UUID fromAgentID, OutputDelegate WriteLine)
        {
            int argsUsed;
            List<SimObject> PS = WorldSystem.GetPrimitives(args, out argsUsed);
            if (!IsEmpty(PS))
            {
                int nfound = 0;
                foreach (var prim in PS)
                {
                    Client.Self.SendTeleportLure(prim.ID);
                    Success(Name + ": " + prim);
                    nfound++;
                }
                if (nfound > 0) return Success(Name + " found: " + nfound + " object/agent(s)");
            }
            string user = string.Join(" ", args);
            UUID id = WorldSystem.GetUserID(user);
            if (id == UUID.Zero) return Failure("Cannot find " + user);
            Client.Self.SendTeleportLure(id);
            return Success("teleport Lure sent to " + user);
        }
    }
}
