using RenSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinSounds
{
    public class JoinSounds : RenSharpEventClass
    {
        //Settings
        public static string ACmdTriggers = "!jsa|!joinsndadmin|!asnd";
        public static string CmdTriggers = "!js|!joinsnd|!snd";
        public static string DBFile = "JoinSounds.json";
        public static float MaxDelay = 10.0f;
        public static float MinDelay = 1.0f;
        public static int AdminLevel = 4;
        public static int ClearLevel = 6;
        public static bool GameLogEnabled = true;
        public static System.Collections.Generic.List<string> DisableList = new System.Collections.Generic.List<string>();

        //Rest of plugin.
        public JoinSounds()
        {

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing); 
        }

        public override void UnmanagedAttach()
        {
            RegisterEvent(DAEventType.PlayerJoin);
            RegisterEvent(DAEventType.SettingsLoaded);
        }

        public override void SettingsLoadedEvent()
        {
            IDASettingsClass settings = DASettingsManager.GetSettings("da.ini");
            UnregisterChatCommand(ACmdTriggers);
            UnregisterChatCommand(CmdTriggers);
            IINISection section = settings.GetSection("JoinSounds");
            foreach(IINIEntry entry in section.EntryList)
            {
                switch(entry.Entry)
                {
                    case "CommandsTriggers":
                        CmdTriggers = entry.Value;
                        break;
                    case "AdminCommandsTriggers":
                        ACmdTriggers = entry.Value;
                        break;
                    case "DatabaseFile":
                        DBFile = entry.Value;
                        break;
                    case "MaxDelay":
                        MaxDelay = (float)Convert.ToDouble(entry.Value.Replace(".",","));
                        break;
                    case "MinDelay":
                        MinDelay = (float)Convert.ToDouble(entry.Value.Replace(".", ","));
                        break;
                    case "DisableList":
                        //DA.HostMessage(entry.Value);
                        DisableList = entry.Value.Split('|').ToList();
                        break;
                    case "AdminLevel":
                        AdminLevel = Convert.ToInt32(entry.Value);
                        break;
                    case "ClearLevel":
                        ClearLevel = Convert.ToInt32(entry.Value);
                        break;
                    case "GameLog":
                        GameLogEnabled = Convert.ToBoolean(entry.Value);
                        break;
                    default:
                        Engine.ConsoleOutput($"[Join Sounds] Invalid entry detected under Join Sounds section in configuration file!\n" +
                                             $"[Join Sounds] Key: \"{entry.Entry}\" | Value: \"{entry.Value}\"\n");
                        break;

                }
            }
            if(!File.Exists(JoinSounds.DBFile))
            {
                File.WriteAllText(JoinSounds.DBFile, "[\r\n\r\n]");
            }
            PlayersDatabase.ReloadSettings();
            RegisterChatCommand(JoinSoundsGenericCommand, CmdTriggers, 1);
            RegisterChatCommand(JoinSoundsAdminGenericCommand, ACmdTriggers, 1);
        }

        public override void TimerExpired(int number, object data)
        {
            var Data = (KeyValuePair<IcPlayer, PlayerClass>)data;
            DA.Create2DSound(Data.Value.Sound);
            if (GameLogEnabled)
            {
                DALogManager.WriteGameLog($"_JOINSND {Data.Value.Name} {Data.Value.Sound}");
            }
        }

        public override void PlayerJoinEvent(IcPlayer player)
        {
            PlayerClass Player = PlayersDatabase.GetPlayer(player.PlayerName);
            if (Player != null)
            {
                if (!DisableList.Contains(Player.Sound.ToLower()))
                {
                    if (Player.Delay < MinDelay && Player.Delay > MaxDelay)
                    {
                        DA.PrivateColorMessage(player, Color.Cyan, $"[Join Sounds] Server settings are changed and incompatible setting detected. Your delay has been reset to minimum. Please update your delay with \"{CmdTriggers.Split('|')[0]} delay\".");
                        Player.Delay = MinDelay;
                        PlayersDatabase.Update(player.PlayerName, Player);
                    }
                    StartTimer(player.PlayerId, FloatToTime(Player.Delay), false, new KeyValuePair<IcPlayer, PlayerClass>(player, Player));
                }
                else
                {
                    DA.PrivateColorMessage(player, Color.Cyan, $"[Join Sounds] Server settings are changed and incompatible setting detected. Your join sound is no longer available. Please update your sound with \"{CmdTriggers.Split('|')[0]} set\". Your join sound is removed.");
                    PlayersDatabase.Remove(player.PlayerName);
                }
            }
        }

        public TimeSpan FloatToTime(float Value)
        {
            return TimeSpan.FromSeconds(Value);
        }

        bool JoinSoundsGenericCommand(IcPlayer player, string command, IDATokenClass text, TextMessageEnum chatType, object data)
        {
            int Size = text.Size; //Had to use like that cuz it is not a RenSharp problem :/P/PP
            if (text[1].ToLower() == "set" || text[1].ToLower() == "change")
            {
                if (Size > 1)
                {
                    if (!DisableList.Contains(text[2].ToLower()))
                    {
                        var Pl = PlayersDatabase.GetPlayer(player.PlayerName);
                        if (Pl != null)
                        {
                            Pl.Sound = text[2];
                            PlayersDatabase.Update(Pl.Name, Pl);
                            Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] Your join sound has been updated with \"{text[2]}\".");
                        }
                        else
                        {
                            Pl = new PlayerClass();
                            Pl.Name = player.PlayerName;
                            Pl.Sound = text[2];
                            Pl.Delay = MinDelay;
                            PlayersDatabase.Add(Pl);
                            Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] Your join sound has successfully aded with \"{text[2]}\" sound. You can adjust your play delay with \"{CmdTriggers.Split('|')[0]} delay\" command.");
                        }
                    }
                    else
                    {
                        Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] This sound is not allowed: {text[2]}.");
                    }
                }
                else
                {
                    DA.PagePlayer(player, $"Usage: {CmdTriggers.Split('|')[0]} set <sound filename>");
                }
            }
            else if (text[1].ToLower() == "delete" || text[1].ToLower() == "del" || text[1].ToLower() == "rem" || text[1].ToLower() == "remove")
            {
                PlayersDatabase.Remove(player.PlayerName);
                Engine.SendMessagePlayer(player.GameObj, Color.Cyan, "[Join Sounds] Your join sound has successfully removed.");
            }
            else if (text[1].ToLower() == "delay")
            {
                if (Size > 1)
                {
                    if (Convert.ToDouble(text[2]) <= MaxDelay && Convert.ToDouble(text[2]) >= MinDelay)
                    {
                        var Pl = PlayersDatabase.GetPlayer(player.PlayerName);
                        Pl.Delay = (float)Convert.ToDouble(text[2]);
                        PlayersDatabase.Update(Pl.Name, Pl);
                        Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] Your sound delay has been updated with \"{Convert.ToDouble(text[2].Replace(".", ","))}\".");
                    }
                    else
                    {
                        DA.PagePlayer(player, $"Usage: Your delay value must be between {MinDelay} and {MaxDelay}.");
                    }
                }
                else
                {
                    DA.PagePlayer(player, $"Usage: {CmdTriggers.Split('|')[0]} delay <decimal>");
                }
            }
            else if (text[1].ToLower() == "help")
            {
                Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] {CmdTriggers.Split('|')[0]} set <music filename>: Sets or changes your join sound.");
                Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] {CmdTriggers.Split('|')[0]} delay <decimal>: Changes your delay time before join sound will be played.");
                Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] {CmdTriggers.Split('|')[0]} remove: Removes your join sound.");
            }
            return true;
        }

        bool JoinSoundsAdminGenericCommand(IcPlayer player, string command, IDATokenClass text, TextMessageEnum chatType, object data)
        {
            int Size = text.Size; //Had to use like that cuz of a RenSharp problem :/P/PP
            if ((int)player.DAPlayer.AccessLevel >= AdminLevel)
            {
                if (text[1].ToLower() == "set" || text[1].ToLower() == "change")
                {
                    if (Size > 2)
                    {
                        if (!DisableList.Contains(text[3].ToLower()))
                        {
                            var Pl = PlayersDatabase.GetPlayer(text[2]);
                            if (Pl != null)
                            {
                                Pl.Sound = text[3];
                                PlayersDatabase.Update(Pl.Name, Pl);
                                Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] {text[2]}'s join sound has been updated with \"{text[3]}\".");
                            }
                            else
                            {
                                Pl = new PlayerClass();
                                Pl.Name = player.PlayerName.ToLower();
                                Pl.Sound = text[3];
                                Pl.Delay = MinDelay;
                                PlayersDatabase.Add(Pl);
                                Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] {text[2]}'s join sound has successfully added with \"{text[3]}\" sound. Delay is set to minimum by default.");
                            }
                        }
                        else
                        {
                            Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] This sound is not allowed: {text[3]}.");
                        }
                    }
                    else
                    {
                        DA.PagePlayer(player, $"Usage: {ACmdTriggers.Split('|')[0]} set <player name> <sound filename>");
                    }
                }
                else if (text[1].ToLower() == "delete" || text[1].ToLower() == "del" || text[1].ToLower() == "rem" || text[1].ToLower() == "remove")
                {
                    if (Size > 1)
                    {
                        PlayersDatabase.Remove(player.PlayerName);
                        Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] {text[2]}'s join sound has successfully removed.");
                    }
                    else
                    {
                        DA.PagePlayer(player, $"Usage: {ACmdTriggers.Split('|')[0]} remove <player name>");
                    }
                }
                else if (text[1].ToLower() == "delay")
                {
                    if (Size > 2)
                    {
                        if (Convert.ToDouble(text[3]) <= MaxDelay && Convert.ToDouble(text[3]) >= MinDelay)
                        {
                            var Pl = PlayersDatabase.GetPlayer(player.PlayerName);
                            Pl.Delay = (float)Convert.ToDouble(text[3]);
                            PlayersDatabase.Update(Pl.Name, Pl);
                            Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] {text[2]}'s sound delay has been updated with \"{Convert.ToDouble(text[3])}\".");
                        }
                        else
                        {
                            DA.PagePlayer(player, $"[Join Sounds] Delay value must be between {MinDelay} and {MaxDelay}.");
                        }
                    }
                    else
                    {
                        DA.PagePlayer(player, $"Usage: {ACmdTriggers.Split('|')[0]} delay <player name> <decimal>");
                    }
                }
                else if(text[1].ToLower() == "backup" || text[1].ToLower() == "bkup")
                {
                    string BackupName = $"{Path.GetFileNameWithoutExtension(JoinSounds.DBFile)}-{DateTime.Now.ToString("yyyyMMddHHmmss")}-COMMAND.{Path.GetExtension(JoinSounds.DBFile)}";
                    PlayersDatabase.Backup(BackupName);
                    Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] A backup of current database has been saved as \"{BackupName}\".");
                }
                else if (text[1].ToLower() == "clear")
                {
                    if ((int)player.DAPlayer.AccessLevel >= JoinSounds.ClearLevel)
                    {
                        if (Size < 2)
                        {
                            Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] This command is very sensitive. Please use \"{ACmdTriggers.Split('|')[0]} clear CONFIRM\" to clear database.");
                        }
                        else if (text[2] == "CONFIRM")
                        {
                            string BackupName = $"{Path.GetFileNameWithoutExtension(JoinSounds.DBFile)}-{DateTime.Now.ToString("yyyyMMddHHmmss")}-CLEARBKUP.{Path.GetExtension(JoinSounds.DBFile)}";
                            PlayersDatabase.Backup(BackupName);
                            PlayersDatabase.Clear();
                            PlayersDatabase.Save();
                            Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] Player database is successfully removed. A backup of it saved as \"{BackupName}\".");
                        }
                        else
                        {
                            Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] Invalid text entered. Please use \"{ACmdTriggers.Split('|')[0]} clear CONFIRM\" to clear database.");
                        }
                    }
                    else
                    {
                        DA.PagePlayer(player.GameObj, "You don't have access to use this command.");
                    }
                }
                else if (text[1].ToLower() == "reload")
                {
                    PlayersDatabase.ReloadSettings();
                    Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] Database is successfully reloaded!");
                }
                else if (text[1].ToLower() == "save")
                {
                    PlayersDatabase.Save();
                    Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] Database is successfully saved!");
                }
                else if (text[1].ToLower() == "dlist")
                {
                    DA.PagePlayer(player, "Full list of disabled sounds:");
                    foreach (string s in DisableList)
                    {
                        DA.PagePlayer(player, s);
                    }
                }
                else if (text[1].ToLower() == "show")
                {
                    if (text.Size > 1)
                    {
                        PlayerClass Player = PlayersDatabase.GetPlayer(text[2]);
                        if (Player != null)
                        {
                            Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] Player data of {Player.Name} - Sound: {Player.Sound} | Delay: {Player.Delay}");
                        }
                        else
                        {
                            Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] Failed to find player {text[2]}");
                        }
                    }
                    else
                    {
                        DA.PagePlayer(player, $"Usage: {ACmdTriggers.Split('|')[0]} show <player name>");
                    }
                }
                else if (text[1].ToLower() == "help")
                {
                    Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] {ACmdTriggers.Split('|')[0]} set <player name> <music filename>: Sets or changes players join sound.");
                    Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] {ACmdTriggers.Split('|')[0]} delay <player name> <decimal>: Changes your delay time before join sound will be played.");
                    Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] {ACmdTriggers.Split('|')[0]} remove <player name>: Removes your join sound.");
                    Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] {ACmdTriggers.Split('|')[0]} clear: Clears the player database.");
                    Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] {ACmdTriggers.Split('|')[0]} dlist: Lists all disabled sounds.");
                    Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] {ACmdTriggers.Split('|')[0]} reload: Reloads the player database.");
                    Engine.SendMessagePlayer(player.GameObj, Color.Cyan, $"[Join Sounds] {ACmdTriggers.Split('|')[0]} save: Saves current player datas. This is automatically ran when a value changes.");
                }
            }
            else
            {
                DA.PagePlayer(player.GameObj, "You don't have access to use this command.");
            }
            return false;
        }
    }
}
