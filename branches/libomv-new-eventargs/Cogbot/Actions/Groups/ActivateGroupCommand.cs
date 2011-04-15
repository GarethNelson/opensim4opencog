using System;
using System.Collections.Generic;
using System.Threading;
using OpenMetaverse;
using OpenMetaverse.Packets;
using System.Text;

namespace cogbot.Actions
{
    /// <summary>
    /// Changes Avatars currently active group
    /// </summary>
    public class ActivateGroupCommand : Command, BotPersonalCommand
    {
        private ManualResetEvent GroupsEvent = new ManualResetEvent(false);
        string activeGroup;

        public ActivateGroupCommand(BotClient testClient)
        {
            Name = "activategroup";
            Description = "Set a group as active. Usage: activategroup GroupName";
            Category = CommandCategory.Groups;
        }
        public override CmdResult Execute(string[] args, UUID fromAgentID, OutputDelegate WriteLine)
        {
            if (args.Length < 1)
                return ShowUsage();

            activeGroup = string.Empty;

            string groupName = String.Empty;
            for (int i = 0; i < args.Length; i++)
                groupName += args[i] + " ";
            groupName = groupName.Trim();

            UUID groupUUID = Client.GroupName2UUID(groupName);
            if (UUID.Zero != groupUUID)
            {
                NetworkManager.PacketCallback pcallback = new NetworkManager.PacketCallback(AgentDataUpdateHandler);
                Client.Network.RegisterCallback(PacketType.AgentDataUpdate, pcallback);

                Console.WriteLine("setting " + groupName + " as active group");
                Client.Groups.ActivateGroup(groupUUID);
                GroupsEvent.WaitOne(30000, false);

                Client.Network.UnregisterCallback(PacketType.AgentDataUpdate, pcallback);
                GroupsEvent.Reset();

                /* A.Biondi 
                 * TODO: Handle titles choosing.
                 */

                if (String.IsNullOrEmpty(activeGroup))
                    return Failure(Client.ToString() + " failed to activate the group " + groupName);

                return Success("Active group is now " + activeGroup);
            }
            return Failure(Client.ToString() + " doesn't seem to be member of the group " + groupName);
        }

        private void AgentDataUpdateHandler(Packet packet, Simulator sim)
        {
            AgentDataUpdatePacket p = (AgentDataUpdatePacket)packet;
            if (p.AgentData.AgentID == Client.Self.AgentID)
            {
                activeGroup = Utils.BytesToString(p.AgentData.GroupName) + " ( " + Utils.BytesToString(p.AgentData.GroupTitle) + " )";
                GroupsEvent.Set();
            }
        }
    }
}