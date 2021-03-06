using System;
using System.Collections.Generic;
using System.Reflection;
using MushDLR223.ScriptEngines;
using MushDLR223.Utilities;
using OpenMetaverse;
using Cogbot.Actions;
using Cogbot.World;

#if USE_SAFETHREADS
using Thread = MushDLR223.Utilities.SafeThread;
#endif
//using RadegastTab = Radegast.SleekTab;

// older LibOMV
//using TeleportFlags = OpenMetaverse.AgentManager.TeleportFlags;
//using TeleportStatus = OpenMetaverse.AgentManager.TeleportStatus;

namespace Cogbot
{
    public partial class BotClient
    {
        public string MasterName
        {
            get
            {
                if (string.IsNullOrEmpty(_lastMasterSet) && UUID.Zero != _lastMasterKey)
                {
                    MasterName = "" + _lastMasterKey;
                }
                if (_lastMasterSet==null)
                {
                    foreach (string s in MasterNames)
                    {
                        return s;   
                    }
                }
                return _lastMasterSet;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    UUID found;
                    if (UUID.TryParse(value, out found))
                    {
                        MasterKey = found;
                        return;
                    }
                    _lastMasterSet = value;
                    SetSecurityLevel(OWNERLEVEL, value, BotPermissions.Owner);
                    found = WorldSystem.GetUserID(value);
                    if (found != UUID.Zero)
                    {
                        MasterKey = found;
                    }
                }
            }
        }
        public ICollection<string> MasterNames
        {
            get
            {
                IEqualityComparer<string> InsensitiveString = new KeyStringComparer();
                var list = new HashSet<string>(InsensitiveString);
                lock (SecurityLevelsByName)
                {
                    foreach (var kv in SecurityLevelsByName)
                    {
                        if ((kv.Value & BotPermissions.Master) != 0)
                        {
                            list.Add(kv.Key);
                        }
                    }
                }
                lock (SecurityLevels)
                {
                    foreach (var kv in SecurityLevels)
                    {
                        if ((kv.Value & BotPermissions.Master) != 0)
                        {
                            string _masterName = WorldSystem.GetUserName(kv.Key) ?? "" + kv.Key;
                            list.Add(_masterName);

                        }
                    }
                }
                return list;
            }
            set
            {
                foreach (string v in value)
                {
                    if (!string.IsNullOrEmpty(v))
                    {
                        UUID found;
                        if (UUID.TryParse(v, out found))
                        {
                            SetSecurityLevel(found, v, BotPermissions.Master);
                            continue;
                        }
                        found = WorldSystem.GetUserID(v);
                        if (found != UUID.Zero)
                        {
                            SetSecurityLevel(found, v, BotPermissions.Master);
                            continue;
                        }
                        SetSecurityLevel(OWNERLEVEL, v, BotPermissions.Master);
                    }
                }
            }
        }

        // permissions "NextOwner" means banned "Wait until they are an owner before doing anything!"
        public Dictionary<UUID, BotPermissions> SecurityLevels = new Dictionary<UUID, BotPermissions>();
        public Dictionary<string, BotPermissions> SecurityLevelsByName = new Dictionary<string, BotPermissions>();

        private UUID _lastMasterKey = UUID.Zero;
        public UUID MasterKey
        {
            get
            {
                if (UUID.Zero == _lastMasterKey && !string.IsNullOrEmpty(_lastMasterSet))
                {
                    UUID found = WorldSystem.GetUserID(_lastMasterSet);
                    if (found != UUID.Zero)
                    {
                        MasterKey = found;
                    }
                }
                if (_lastMasterKey == null)
                {
                    foreach (var s in MasterKeys)
                    {
                        return s;
                    }
                }
                return _lastMasterKey;
            }
            set
            {
                if (UUID.Zero != value)
                {
                    _lastMasterKey = value;
                    if (string.IsNullOrEmpty(_lastMasterSet))
                    {
                        string maybe = WorldSystem.GetUserName(value);
                        if (!string.IsNullOrEmpty(maybe)) MasterName = maybe;
                    }
                    lock (SecurityLevels)
                    {
                        if (!SecurityLevels.ContainsKey(value)) SecurityLevels[value] = BotPermissions.Master;
                        else SecurityLevels[value] |= BotPermissions.Master;
                    }
                }
            }
        }

        public ICollection<UUID> MasterKeys
        {
            get
            {
                var list = new HashSet<UUID>();
                lock (SecurityLevelsByName)
                {
                    foreach (var kv in SecurityLevelsByName)
                    {
                        if ((kv.Value & BotPermissions.Master) != 0)
                        {
                            var _masterName = WorldSystem.GetUserID(kv.Key);
                            if (_masterName != UUID.Zero) list.Add(_masterName);

                        }
                    }
                }
                lock (SecurityLevels)
                {
                    foreach (var kv in SecurityLevels)
                    {
                        if ((kv.Value & BotPermissions.Master) != 0)
                        {
                            list.Add(kv.Key);
                        }
                    }
                }
                return list;
            }
            set
            {
                foreach (UUID found in value)
                {
                    if (found != UUID.Zero)
                    {
                        string named = WorldSystem.GetUserName(found);
                        SetSecurityLevel(found, named, BotPermissions.Master);
                        continue;
                    }
                }
            }
        }
        public bool AllowObjectMaster = true;

        public BotPermissions GetSecurityLevel(UUID uuid, string name)
        {
            BotPermissions bp = BotPermissions.None;
            if (uuid != UUID.Zero)
            {
                lock (SecurityLevels)
                    if (SecurityLevels.TryGetValue(uuid, out bp))
                    {
                        return bp;
                    }
            }
            if (!string.IsNullOrEmpty(name))
            {
                lock (SecurityLevelsByName)
                    if (SecurityLevelsByName.TryGetValue(name, out bp))
                    {
                        return bp;
                    }
            }
            else if (Friends.FriendList != null)
            {
                FriendInfo fi;
                if (Friends.FriendList.TryGetValue(uuid, out fi))
                {
                    BotPermissions start = RecognizedFriendSecurityLevel;
                    if (fi.CanModifyMyObjects)
                    {
                        start |= BotPermissions.Trusted;
                    }
                    if (!fi.CanSeeMeOnMap)
                    {
                        start |= BotPermissions.Friend;
                    }
                    return start;
                }
            }
            else if (GroupMembers != null && GroupMembers.ContainsKey(uuid))
            {
                return RecognizedGroupSecurityLevel;
            }
            return StrangerSecurityLevel;
        }

        public BotPermissions RecognizedFriendSecurityLevel = BotPermissions.Friend;
        public BotPermissions RecognizedGroupSecurityLevel = BotPermissions.SameGroup;
        public BotPermissions UnrecognizedGroupSecurityLevel = BotPermissions.UnknownGroup;
        public BotPermissions StrangerSecurityLevel = BotPermissions.Stranger;
        
        private BotPermissions GetSecurityLevel(InstantMessage im)
        {
            BotPermissions perms = GetSecurityLevel(im.FromAgentID, im.FromAgentName);
            return perms;
        }

        public void SetSecurityLevel(UUID uuid, string name, BotPermissions perms)
        {
            BotPermissions bp;
            if (uuid != UUID.Zero)
            {
                lock (SecurityLevels) SecurityLevels[uuid] = perms;
            }
            if (!string.IsNullOrEmpty(name))
            {
                // dont take whitepaces
                name = name.Trim();
                if (name != "") lock (SecurityLevelsByName) SecurityLevelsByName[name] = perms;
            }
        }

        public readonly static UUID OWNERLEVEL = UUID.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        public static UUID SessionToCallerId(object callerSession)
        {
            if (callerSession is BotClient) return OWNERLEVEL;
            if (callerSession == null)
            {
                return UUID.Zero;
            }
            UUID callerId = UUID.Zero;
            if (callerSession is UUID) callerId = (UUID)callerSession;
            if (callerId != UUID.Zero) return callerId;
            CmdRequest request = callerSession as CmdRequest;
            if (request != null) return SessionToCallerId(request.CallerAgent);
            return OWNERLEVEL;
            throw new NotImplementedException();
        }

        void Self_OnChat(object sender, ChatEventArgs e)
        {
            InstantMessageDialog Dialog = InstantMessageDialog.MessageFromAgent;
            switch (e.SourceType)
            {
                case ChatSourceType.System:
                    break;
                case ChatSourceType.Agent:
                    break;
                case ChatSourceType.Object:
                    Dialog = InstantMessageDialog.MessageFromObject;
                    break;
            }
            UUID regionID = UUID.Zero;
            if (e.Simulator != null)
            {
                regionID = e.Simulator.RegionID;
            }
            Self_OnMessage(e.FromName, e.SourceID, e.OwnerID,
                           e.Message, UUID.Zero, false,
                           regionID, e.Position,
                           Dialog, e.Type, e);
            ;

        }

        private void Self_OnInstantMessage(object sender, InstantMessageEventArgs e)
        {
            InstantMessage im = e.IM;
            WorldObjects.GetMemberValues("", im);
            ChatType Type = ChatType.Normal;
            switch (im.Dialog)
            {
                case InstantMessageDialog.StartTyping:
                    Type = ChatType.StartTyping;
                    break;
                case InstantMessageDialog.StopTyping:
                    Type = ChatType.StopTyping;
                    break;
            }
            Self_OnMessage(im.FromAgentName, im.FromAgentID, im.ToAgentID,
                           im.Message, im.IMSessionID, im.GroupIM,
                           im.RegionID, im.Position,
                           im.Dialog, Type, e);
        }

        private void Self_OnMessage(string FromAgentName, UUID FromAgentID, UUID ToAgentID,
            string Message, UUID IMSessionID, bool GroupIM,
            UUID RegionID, Vector3 Position,
            InstantMessageDialog Dialog, ChatType Type, EventArgs origin)
        {
            if (Dialog == InstantMessageDialog.GroupNotice)
            {
                GroupIM = true;
            }

            BotPermissions perms = GetSecurityLevel(FromAgentID, FromAgentName);

            // Received an IM from someone that is authenticated
            if (Type == ChatType.OwnerSay)
            {
                perms |= BotPermissions.Owner;
            }

            bool displayedMessage = false;
            if (origin is ChatEventArgs && Message.Length > 0 && Dialog == InstantMessageDialog.MessageFromAgent)
            {
                WriteLine(String.Format("{0} says, \"{1}\".", FromAgentName, Message));
                PosterBoard["/posterboard/onchat"] = Message;
                if (FromAgentName == Self.Name)
                {
                    PosterBoard["/posterboard/onchat-said"] = Message;
                }
                else
                {
                    PosterBoard["/posterboard/onchat-heard"] = Message;
                }
            }

            bool groupIM = GroupIM && GroupMembers != null && GroupMembers.ContainsKey(FromAgentID) ? true : false;


            switch (Dialog)
            {
                case InstantMessageDialog.MessageBox:
                    break;
                case InstantMessageDialog.GroupInvitation:
                    if ((perms & BotPermissions.AcceptGroupAndFriendRequests) != 0)
                    {
                        string groupName = Message;
                        int found = groupName.IndexOf("Group:");
                        if (found > 0) groupName = groupName.Substring(found + 6);
                        Self.InstantMessage(Self.Name, FromAgentID, string.Empty, IMSessionID,
                                            InstantMessageDialog.GroupInvitationAccept, InstantMessageOnline.Offline,
                                            Self.SimPosition,
                                            UUID.Zero, new byte[0]);
                        found = groupName.IndexOf(":");
                        if (found > 0)
                        {
                            groupName = groupName.Substring(0, found).Trim();
                            ExecuteCommand("joingroup " + groupName, CMDFLAGS.NoResult);
                        }
                    }
                    break;
                case InstantMessageDialog.InventoryOffered:
                    break;
                case InstantMessageDialog.InventoryAccepted:
                    break;
                case InstantMessageDialog.InventoryDeclined:
                    break;
                case InstantMessageDialog.GroupVote:
                    break;
                case InstantMessageDialog.TaskInventoryOffered:
                    break;
                case InstantMessageDialog.TaskInventoryAccepted:
                    break;
                case InstantMessageDialog.TaskInventoryDeclined:
                    break;
                case InstantMessageDialog.NewUserDefault:
                    break;
                case InstantMessageDialog.SessionAdd:
                    break;
                case InstantMessageDialog.SessionOfflineAdd:
                    break;
                case InstantMessageDialog.SessionGroupStart:
                    break;
                case InstantMessageDialog.SessionCardlessStart:
                    break;
                case InstantMessageDialog.SessionSend:
                    break;
                case InstantMessageDialog.SessionDrop:
                    break;
                case InstantMessageDialog.BusyAutoResponse:
                    break;
                case InstantMessageDialog.ConsoleAndChatHistory:
                    break;
                case InstantMessageDialog.Lure911:
                case InstantMessageDialog.RequestTeleport:
                    if ((perms & BotPermissions.AcceptTeleport) != 0)
                    {
                        TheSimAvatar.StopMoving();
                        if (RegionID != UUID.Zero)
                        {
                            if (!displayedMessage)
                            {
                                DisplayNotificationInChat("TP to Lure from " + FromAgentName);
                                displayedMessage = true;
                            }
                            SimRegion R = SimRegion.GetRegion(RegionID, gridClient);
                            if (R != null)
                            {
                                Self.Teleport(R.RegionHandle, Position);
                                return;
                            }
                        }
                        DisplayNotificationInChat("Accepting TP Lure from " + FromAgentName);
                        displayedMessage = true;
                        Self.TeleportLureRespond(FromAgentID, IMSessionID, true);
                    }
                    break;
                case InstantMessageDialog.AcceptTeleport:
                    break;
                case InstantMessageDialog.DenyTeleport:
                    break;
                case InstantMessageDialog.GodLikeRequestTeleport:
                    break;
              //  case InstantMessageDialog.CurrentlyUnused:
                //    break;
                case InstantMessageDialog.GotoUrl:
                    break;
                case InstantMessageDialog.Session911Start:
                    break;
                case InstantMessageDialog.FromTaskAsAlert:
                    break;
                case InstantMessageDialog.GroupNotice:
                    break;
                case InstantMessageDialog.GroupNoticeInventoryAccepted:
                    break;
                case InstantMessageDialog.GroupNoticeInventoryDeclined:
                    break;
                case InstantMessageDialog.GroupInvitationAccept:
                    break;
                case InstantMessageDialog.GroupInvitationDecline:
                    break;
                case InstantMessageDialog.GroupNoticeRequested:
                    break;
                case InstantMessageDialog.FriendshipOffered:
                    if ((perms & BotPermissions.AcceptGroupAndFriendRequests) != 0)
                    {
                        DisplayNotificationInChat("Accepting Friendship from " + FromAgentName);
                        Friends.AcceptFriendship(FromAgentID, IMSessionID);
                        displayedMessage = true;
                    }
                    break;
                case InstantMessageDialog.FriendshipAccepted:
                    break;
                case InstantMessageDialog.FriendshipDeclined:
                    break;
                case InstantMessageDialog.StartTyping:
                    break;
                case InstantMessageDialog.StopTyping:
                    break;
                case InstantMessageDialog.MessageFromObject:
                case InstantMessageDialog.MessageFromAgent:
                    // message from self
                    if (FromAgentName == GetName()) return;
                    // message from system
                    if (FromAgentName == "System") return;
                    // message from others
                    CommandInstance ci;
                    if (Commands.TryGetValue("im", out ci))
                    {
                        var whisper = ci.WithBotClient as Cogbot.Actions.Communication.ImCommand;
                        if (whisper != null)
                        {
                            whisper.currentAvatar = FromAgentID;
                            whisper.currentSession = IMSessionID;
                        }
                    }
                    var cea = origin as ChatEventArgs;
                    if ((perms & BotPermissions.ExecuteCommands) != 0)
                    {
                        OutputDelegate WriteLine;
                        if (origin is InstantMessageEventArgs)
                        {
                            WriteLine = new OutputDelegate(
                                (string text, object[] ps) =>
                                    {
                                        string reply0 = DLRConsole.SafeFormat(text, ps);
                                        InstantMessage(FromAgentID, reply0, IMSessionID);
                                    });
                        }
                        else
                        {
                            WriteLine = new OutputDelegate(
                                (string text, object[] ps) =>
                                    {
                                        string reply0 = DLRConsole.SafeFormat(text, ps);
                                        Talk(reply0, 0, Type);
                                    });
                        }
                        string cmd = Message;
                        CMDFLAGS needResult = CMDFLAGS.Console;
                        if (cmd.StartsWith("cmcmd "))
                        {
                            cmd = cmd.Substring(6);
                            WriteLine("");
                            WriteLine(string.Format("invokecm='{0}'", cmd));
                            ClientManager.DoCommandAll(cmd, FromAgentID, WriteLine);
                        }
                        else if (cmd.StartsWith("cmd "))
                        {
                            cmd = cmd.Substring(4);
                            WriteLine(string.Format("invoke='{0}'", cmd));
                            var res = ExecuteCommand(cmd, FromAgentID, WriteLine, needResult);
                            WriteLine("iresult='" + res + "'");
                        }
                        else if (cmd.StartsWith("/") || cmd.StartsWith("@"))
                        {
                            cmd = cmd.Substring(1);
                            WriteLine("");
                            WriteLine(string.Format("invoke='{0}'", cmd));
                            var res = ExecuteCommand(cmd, FromAgentID, WriteLine, needResult);
                            WriteLine("iresult='" + res + "'");
                        }
                    }
                    if (cea != null && cea.AudibleLevel == ChatAudibleLevel.Barely) return;
                    break;
                default:
                    break;
            }
            //if (Dialog != InstantMessageDialog.MessageFromAgent && Dialog != InstantMessageDialog.MessageFromObject)
            {
                string debug = String.Format("{0} {1} {2} {3} {4} {5}: {6}",
                                             groupIM ? "GroupIM" : "IM", Dialog, Type, perms, FromAgentID, FromAgentName,
                                             Helpers.StructToString(origin));
                if (!displayedMessage)
                {
                    DisplayNotificationInChat(debug);
                    displayedMessage = true;
                }
            }
        }
        public bool AcceptAllInventoryItems = false;
        private string _lastMasterSet;

        public static bool HasPermission(BotPermissions them, BotPermissions testFor)
        {
            if (((them & BotPermissions.Ignore) != 0) && (testFor & BotPermissions.Ignore) == 0) return false;
            return (them & testFor) != 0;
        }

        private void Inventory_OnInventoryObjectReceived(object sender, InventoryObjectOfferedEventArgs e)
        {
            if (AcceptAllInventoryItems)
            {
                e.Accept = true;
                return; // accept everything}
            }
            BotPermissions them = GetSecurityLevel(e.Offer);
            if (HasPermission(them, BotPermissions.AcceptInventory))
            {
                e.Accept = true;
                return;
            }
            e.Accept = false;
        }
    }
    /// <summary>
    ///  
    /// </summary>
    [Flags]
    public enum BotPermissions : uint
    {
        None = 0,
        ChatWith = 1,
        AcceptInventory = 2,
        ExecuteCommands = 4,
        ExecuteCode = 8,
        AcceptGroupAndFriendRequests = 16,
        AcceptTeleport = 32,
        SendDebugTo = 64,
        IsMaster = 128,
        Ignore = 256,

        Stranger = ChatWith,
        SameGroup = Friend,
        UnknownGroup = Stranger,
        Friend = AcceptInventory | ChatWith | AcceptGroupAndFriendRequests | AcceptTeleport,
        Trusted = Friend | ExecuteCommands,
        Owner = Trusted | ExecuteCode | IsMaster,
        Master = Owner | SendDebugTo | IsMaster
    }

}