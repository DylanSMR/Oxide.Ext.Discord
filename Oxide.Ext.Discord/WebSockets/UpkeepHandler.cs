﻿using Oxide.Core;
using System;
using System.Timers;
using Oxide.Ext.Discord.DiscordObjects;
using System.Linq;

namespace Oxide.Ext.Discord.WebSockets
{
    public class UpkeepHandler
    {
        private DiscordClient Client;
        private Timer UpkeepTimer;
        private Timer GuildMemberRefreshTimer;
        private DateTime LastUpdate;

        public UpkeepHandler(DiscordClient client)
        {
            if (client == null) return;

            LastUpdate = DateTime.UtcNow;
            this.Client = client;

            UpkeepTimer = new Timer();
            UpkeepTimer.Elapsed += CheckForBeat;
            UpkeepTimer.AutoReset = true;
            UpkeepTimer.Interval = 1000;
            UpkeepTimer.Start();

            GuildMemberRefreshTimer = new Timer();
            GuildMemberRefreshTimer.Elapsed += GuildMemberRefresh;
            GuildMemberRefreshTimer.AutoReset = true;
            GuildMemberRefreshTimer.Interval = 60000;
            GuildMemberRefreshTimer.Start();
        }

        public void SendBeat()
        { 
            this.LastUpdate = DateTime.UtcNow;
        }

        public void Shutdown()
        {
            UpkeepTimer.Dispose();
            UpkeepTimer = null;

            GuildMemberRefreshTimer.Dispose();
            GuildMemberRefreshTimer = null;
        }

        private void CheckForBeat(object sender, ElapsedEventArgs args)
        {
            if (Math.Floor((DateTime.UtcNow - LastUpdate).TotalSeconds) > 3)
            {
                Interface.Oxide.LogInfo($"[Discord Ext] Discord connection closed (no heartbeat)");
                Discord.CloseClient(Client);
                Shutdown();
            }
        }

        private void GuildMemberRefresh(object sender, ElapsedEventArgs args)
        {
            Interface.Oxide.LogInfo($"Updating from {Client.DiscordServer.members.Count}");
            Client.DiscordServer.ListGuildMembers(Client, guildMembers =>
            {
                Client.DiscordServer.members = guildMembers.Select(x => new Member(x)).ToList();
                Interface.Oxide.LogInfo($"Updated to {Client.DiscordServer.members.Count}");
            });
        }
    }
}
