﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using cogbot.Listeners;

using MushDLR223.ScriptEngines;

namespace cogbot.Actions.Search
{
    public class UUIDTypeCommand : Command, GridMasterCommand
    {
        public UUIDTypeCommand(BotClient testClient)
        {
            Name = "UUID Type";
            Description = "Resolve the type of Object the UUID represents.  Usage: uuidtype b06c0586-da9a-473d-aa94-ee3ab5606e4d";
            Category = CommandCategory.BotClient;
        }

        public override CmdResult Execute(string[] args, UUID fromAgentID, OutputDelegate WriteLine)
        {
            if (args.Length < 1) return ShowUsage();
            var v = WorldObjects.uuidTypeObject;
            UUID uuid = UUID.Zero;
            string botcmd = String.Join(" ", args, 0, args.Length).Trim();
            int argsUsed;
            UUIDTryParse(args,0, out uuid, out argsUsed);

            object obj = null;
            lock (v)
            {
                v.TryGetValue(uuid, out obj);
            }
            if (obj != null)
            {
                WriteLine("Object is of Type " + obj.GetType());
            }
            else
            {
                WriteLine("Object not found for UUID " + uuid);
            }
            return Success("Done with UUID " + uuid + " obj= " + obj);
        }
    }
}
