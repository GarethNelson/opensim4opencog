﻿using System;
using OpenMetaverse;
using cogbot.TheOpenSims;
using PathSystem3D.Navigation;

//using libsecondlife;

namespace cogbot.Actions
{
    class Use : Command
    {
        public Use(BotClient Client)
            : base(Client)
        {
            Description = "Use an item from inventory or world.";
            Parameters = new [] { new NamedParam(typeof(SimPosition), typeof(UUID))};
            Name = "Use..";
        }

        public override string acceptInput(string verb, Parser args, OutputDelegate WriteLine)
       {
        //   base.acceptInput(verb, args);
           string to_op = "";
           string objname = "";
           if (args.prepPhrases["to"].Length > 0)
           {
               to_op = args.prepPhrases["to"];
           }
           objname = args.objectPhrase;
           if (objname == "") {
               return ("$bot don't know what object to use.");
           }
           if (to_op == "") {
               Primitive prim;
               if (WorldSystem.tryGetPrim(objname, out prim))
               {
                   SimObject objToUse = WorldSystem.GetSimObject(prim);
                   if ((BotNeeds)TheSimAvatar["CurrentNeeds"]==null)
                   {
                       TheSimAvatar["CurrentNeeds"] = new BotNeeds(90.0f);
                   }
                   SimTypeUsage usage = objToUse.GetBestUse((BotNeeds)TheSimAvatar["CurrentNeeds"]);
                   if (usage==null)
                   {
                       //usage = new MoveToLocation(TheSimAvatar, objToUse);
                       return "$bot don't have a use for " + objToUse + " yet.";
                   }
                   TheSimAvatar.Do(usage,objToUse);
                   return "used " + objToUse;
               }
               return "$bot don't know what to do with " + objname; 
           }
           WriteLine("Trying to (" + to_op + ") with (" + objname + ")");
           TheBotClient.UseInventoryItem(to_op, objname);
           return "completed to (" + to_op + ") with (" + objname + ")";
           
       }
    }
}
