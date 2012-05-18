using System;
using OpenMetaverse;
using System.Collections.Generic;
using System.Threading;
using cogbot.TheOpenSims;

using MushDLR223.ScriptEngines;

namespace cogbot.Actions.Appearance
{
    public class AnimCommand : Command, BotPersonalCommand
    {
        public static bool NOSEARCH_ANIM = false;
        public AnimCommand(BotClient testClient)
        {
            TheBotClient = testClient;
            Name = "anim";
            Description = "List or do animation or gesture on Simulator.";
            Usage =
                @"Usage: anim // just lists anims currently ran
                  Usage: anim stopall +HOVER 5 +23423423423-4234234234-234234234-23423423 10 -CLAP  " +
                "// stop all current anims, begin hover, wait 5 seconds, begin clapping, wait 10 seconds, stop clapping ";

            Category = CommandCategory.Appearance;
            ParameterVersions = NamedParam.CreateParamVersions(
                NamedParam.CreateParams(),
                NamedParam.CreateParams(
                    NamedParam.Optional("stopall", typeof (bool), "stops all current anims"),
                    NamedParam.Optional("anim_0-N", typeof (SimAnimation), "+/-animuuid"),
                    NamedParam.Optional("seconds", typeof (int), "how long to pause for")
                    ));
            ResultMap = NamedParam.CreateParams(
                "ranSteps", typeof (List<string>), "list of ran animsteps",
                "message", typeof (string), "if success was false, the reason why",
                "success", typeof (bool), "true if command was successful");
        }

        public override CmdResult Execute(string[] args, UUID fromAgentID, OutputDelegate WriteLine)
        {
            if (args.Length < 1)
            {
                ICollection<string> list = WorldSystem.SimAssetSystem.GetAssetNames(AssetType.Animation);
                WriteLine(TheBotClient.argsListString(list));
                Dictionary<UUID,int> gestures = WorldSystem.TheSimAvatar.GetCurrentAnimDict();
                string alist = String.Empty;
                foreach (var anim in gestures)
                {
                    alist += WorldSystem.GetAnimationName(anim.Key);
                    alist += " ";
                    alist += anim.Value;
                    alist += Environment.NewLine;
                }
                WriteLine("Currently: {0}", alist);
                return ShowUsage();// " anim [seconds] HOVER [seconds] 23423423423-4234234234-234234234-23423423  +CLAP -JUMP STAND";           
            }
            int argStart = 0;
            string directive = args[0].ToLower();

            if (directive == "stopall")
            {
                Dictionary<UUID, bool> animations = new Dictionary<UUID, bool>();
                var anims = TheSimAvatar.GetCurrentAnims();
                foreach(var ani in anims) {                                                        
                    animations[ani] = false;
                }
                int knownCount = animations.Count;

                Client.Self.Animate(animations, true);
                argStart++;
            }
            
            int time = 1300; //should be long enough for most animations
            List<KeyValuePair<UUID, int>> amins = new List<KeyValuePair<UUID, int>>();
            base.SetWriteLine("message");
            for (int i = argStart; i < args.Length; i++)
            {
                int mode = 0;
                string a = args[i];
                if (String.IsNullOrEmpty(a)) continue;
                try
                {
                    float ia;
                    if (float.TryParse(a, out ia))
                    {
                        if (ia > 0.0)
                        {
                            time = (int)(ia * 1000);
                            amins.Add(new KeyValuePair<UUID, int>(UUID.Zero, time));
                            continue;
                        }
                    }
                }
                catch (Exception) { }
                char c = a.ToCharArray()[0];
                if (c == '-')
                {
                    mode = -1;
                    a = a.Substring(1);
                }
                else if (c == '+')
                {
                    mode = 1;
                    a = a.Substring(1);
                } else
                {
                    mode = 0;
                }
                UUID anim = WorldSystem.GetAssetUUID(a, AssetType.Animation);

                if (anim == UUID.Zero)
                {
                    try
                    {
                        if (a.Substring(2).Contains("-"))
                            anim = UUIDParse(a);
                    }
                    catch (Exception) { }
                }
                if (anim == UUID.Zero)
                {
                    WriteLine("skipping unknown animation " + a);
                    continue;
                }
                 if (mode==0)
                 {
                     amins.Add(new KeyValuePair<UUID, int>(anim, -1));
                     amins.Add(new KeyValuePair<UUID, int>(anim, +1));
                     amins.Add(new KeyValuePair<UUID, int>(anim, -1));
                     continue;
                 }
                amins.Add(new KeyValuePair<UUID,int>(anim,mode));
            }
            base.SetWriteLine("argStep");
            foreach (KeyValuePair<UUID, int> anim in amins)
            {
                try
                {
                    int val = anim.Value;
                    if (anim.Key == UUID.Zero)
                    {
                        Thread.Sleep(val);
                        continue;
                    }
                    switch (val)
                    {
                        case -1:
                            Client.Self.AnimationStop(anim.Key, true);
                            WriteLine("\nStop anim " + WorldSystem.GetAnimationName(anim.Key));
                            continue;
                        case +1:
                            Client.Self.AnimationStart(anim.Key, true);
                            WriteLine("\nStart anim " + WorldSystem.GetAnimationName(anim.Key));
                            continue;
                        default:
                            try
                            {
                                Client.Self.AnimationStart(anim.Key, true);
                                WriteLine("\nRan anim " + WorldSystem.GetAnimationName(anim.Key) + " for " + val / 1000 +
                                          " seconds.");
                                Thread.Sleep(val);
                            }
                            finally
                            {
                                Client.Self.AnimationStop(anim.Key, true);
                            }
                            continue;
                    }
                }
                catch (Exception e)
                {
                    return Failure("\nRan " + amins.Count + " amins but " + e); 
                }
            }
            if (NOSEARCH_ANIM)
            {
                String str = string.Join(" ", args);
                WriteLine("ANIM ECHO " + str);
                Success("\nStart anim " + str + "\n");
            }
            return Success("Ran " + amins.Count + " steps");
        }
    }
}
