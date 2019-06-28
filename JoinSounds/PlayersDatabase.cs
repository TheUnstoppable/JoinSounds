using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JoinSounds
{
    public class PlayerClass
    {
        public string Name;
        public string Sound;
        public float Delay;
    }

    public class PlayersCollection
    {
        public PlayersCollection()
        {
            PlayerList = new List<PlayerClass>();
        }
        public List<PlayerClass> PlayerList;
    }

    public class PlayersDatabase
    {
        public static PlayersCollection Players = new PlayersCollection();

        public static void Add(PlayerClass Data)
        {
            Players.PlayerList.Add(Data);
        }

        public static void Remove(string Name)
        {
            Players.PlayerList.RemoveAt(Players.PlayerList.FindIndex(x => x.Name.ToLower() == Name.ToLower()));
        }

        public static void Update(string Name, PlayerClass DataNew)
        {
            Players.PlayerList[Players.PlayerList.FindIndex(x => x.Name.ToLower() == Name.ToLower())] = DataNew;
            Save();
        }

        public static void Save()
        {
            string Text = "[\r\n";
            foreach (PlayerClass Player in Players.PlayerList)
            {
                Text += $"      {{\r\n      \"Name\": \"{Player.Name.ToLower()}\",\r\n      \"Sound\": \"{Player.Sound}\",\r\n      \"Delay\": {Player.Delay.ToString().Replace(",", ".")}\r\n   }},";
            }
            Text = Text.Remove(Text.Length - 1, 1);
            Text += "\r\n]";
            File.WriteAllText(JoinSounds.DBFile, Text);
            ReloadSettings();
        }

        public static void ReloadSettings()
        {
            Players.PlayerList = JsonConvert.DeserializeObject<List<PlayerClass>>(File.ReadAllText(JoinSounds.DBFile), new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            });
        }

        public static void Backup(string name)
        {
            File.Copy(JoinSounds.DBFile, name);
        }

        public static void Clear()
        {
            Players = new PlayersCollection();
        }

        public static PlayerClass GetPlayer(string Name)
        {
            try
            {
                return Players.PlayerList.First(x => x.Name.ToLower() == Name.ToLower());
            }
            catch(Exception)
            {
                return null;
            }
        }
    }
}
