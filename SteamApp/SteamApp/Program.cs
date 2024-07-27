using System;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SteamApiConsoleApp
{
    public class FriendListResponse
    {
        [JsonProperty("friendslist")]
        public FriendsList FriendsList { get; set; }
    }

    public class FriendsList
    {
        [JsonProperty("friends")]
        public List<Friend> Friends { get; set; }
    }

    public class Friend
    {
        [JsonProperty("steamid")]
        public string SteamId { get; set; }

        [JsonProperty("relationship")]
        public string Relationship { get; set; }

        [JsonProperty("friend_since")]
        public long FriendSince { get; set; }
    }
    class Program
    {
        /*

        Steam User ID Example : 76561199161083869

        Steam Game ID Example : 730
 
        */
        private static readonly string apiKey = "2EDCB9C589BAC36AEEA34FC390F24E0E";
        private static readonly string baseUrl = "http://api.steampowered.com/";
        private static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Please enter game id : ");
            string gameId = Console.ReadLine();

            Console.WriteLine("Please enter user Steam id : ");
            string steamId = Console.ReadLine();


            string result = await GetRecentlyPlayedGamesAsync(steamId);
            // Burda ne olduğunu çözemediğim bir hata var, süreyi yanlış gösteriyor.
            GetOwnedGamesAsync(steamId, gameId);
            var ownedGames = await GetOwnedGamesAsync(steamId, gameId);
            Console.WriteLine(ownedGames);
            string json = await GetFriendList(steamId);
            FriendListResponse friendListResponse = JsonConvert.DeserializeObject<FriendListResponse>(json);

            foreach (var friend in friendListResponse.FriendsList.Friends)
            {
                Console.WriteLine($"Steam ID: {friend.SteamId}, Relationship: {friend.Relationship}, Friend Since: {friend.FriendSince}");
            }


            var userInfo = await GetUserInfoAsync(steamId);
            Console.WriteLine(userInfo);
            string achievementsJson = await SteamApiClient.GetPlayerAchievementsAsync(steamId, gameId);
            var gameInfo = await GetGameInfoAsync(gameId);
            Console.WriteLine(gameInfo);
            if (achievementsJson != null)
            {
                Console.WriteLine("Player Achievements:");
                var achievements = ParseAchievements(achievementsJson);
                foreach (var achievement in achievements)
                {
                    Console.WriteLine(achievement);
                }
            }
            else
            {
                Console.WriteLine("Failed to retrieve player achievements.");
            }
        }
        private static async Task<string> GetUserInfoAsync(string steamId)
        {
            using (HttpClient client = new HttpClient())
            {

                string url = $"{baseUrl}ISteamUser/GetPlayerSummaries/v2/?key={apiKey}&steamids={steamId}";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(responseBody);
                JObject player = (JObject)json["response"]["players"][0];
                string personaName = player["personaname"].ToString();
                string profileUrl = player["profileurl"].ToString();
                string avatarUrl = player["avatar"].ToString();
                string loccountrycode = player["loccountrycode"].ToString();

                string steamID = player["steamid"].ToString();
                var profileState = player["profilestate"].ToString();
                var commentPermission = player["commentpermission"].ToString();
                var primaryClanId = player["primaryclanid"].ToString();
                var timeCreated = player["timecreated"].ToString();

                return $"Username : {personaName}\nProfil URL: {profileUrl}\nAvatar URL: {avatarUrl} \n Steam ID {steamID}\n , Profile State : {profileState} \n Comment Permission : {commentPermission}\n,Time Created : {timeCreated}\n, Primary Clan ID : {primaryClanId}\n ";
            }
        }
        private static async Task<string> GetFriendList(string steamId)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = $"{baseUrl}/ISteamUser/GetFriendList/v0001/?key={apiKey}&steamid={steamId}&relationship=friend";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                return responseBody;
            }
        }

        public static async Task<string> GetPlayerAchievementsAsync(string steamId, string gameId)
        {
            try
            {
                string url = $"{baseUrl}ISteamUserStats/GetPlayerAchievements/v0001/?appid={gameId}&key={apiKey}&steamid={steamId}";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected error: {e.Message}");
                return null;
            }
        }
        private static async Task<string> GetGameInfoAsync(string gameId)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = $"{baseUrl}ISteamUserStats/GetNumberOfCurrentPlayers/v1/?key={apiKey}&appid={gameId}";

                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(responseBody);

                return json.ToString();
                // **Json dosyasında cok bi sey yok sadece oyunda kac kisi var falan onu gosteriyor **
                //Dökümanda bununla alakalı başka şeyler de olması lazım, kontrol edip ona göre ekleme yapabilirim 


            }
        }

        public static async Task<string> GetOwnedGamesAsync(string steamId, string gameId)
        {
            try
            {
                string url = $"{baseUrl}IPlayerService/GetOwnedGames/v0001/?key={apiKey}&steamid={steamId}&format=json";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(responseBody);

                int gameCount = (int)json["response"]["game_count"];
                JArray games = (JArray)json["response"]["games"];
                if (games != null)
                {
                    foreach (JObject game in games)
                    {
                        if (game["appid"].ToString() == gameId)
                        {
                            var playtimeForever = game["playtime_forever"].ToString();
                            var playtime2weeks = game["playtime_2weeks"]?.ToString() ?? "0"; // 2 haftada eğer oynamadıysa burda devreye giriyo 
                            return $"Total Games: {gameCount}, Game ID: {gameId}, Playtime Forever: {playtimeForever} minutes, Playtime Last 2 Weeks: {playtime2weeks} minutes";
                        }
                    }
                }
                return $"Total Games: {gameCount}, Game with ID {gameId} not found in the user's library.";
            }
            catch (Exception e)
            {
                return $"Error: {e.Message}";
            }
        }

        public static async Task<string> GetRecentlyPlayedGamesAsync(string steamId)
        {
            string url = $"{baseUrl}IPlayerService/GetRecentlyPlayedGames/v0001/?key={apiKey}&steamid={steamId}&format=json";
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(responseBody);
            JArray games = (JArray)json["response"]["games"];
            if (games != null)
            {
                foreach (JObject game in games)
                {
                    var playtimeForever = game["playtime_forever"].ToString();
                    var playtime2weeks = game["playtime_2weeks"]?.ToString() ?? "0";
                    return $", Playtime Forever: {playtimeForever} minutes, Playtime Last 2 Weeks: {playtime2weeks} minutes";

                }
                {

                }
            }
            return responseBody;
        }
        private static string[] ParseAchievements(string json)
        {
            JObject data = JObject.Parse(json);
            JArray achievementsArray = (JArray)data["playerstats"]["achievements"];
            string[] achievements = new string[achievementsArray.Count];

            for (int i = 0; i < achievementsArray.Count; i++)
            {
                JObject achievement = (JObject)achievementsArray[i];
                string name = achievement["apiname"].ToString();
                bool achieved = achievement["achieved"].ToString() == "1";
                DateTime unlockTime = DateTimeOffset.FromUnixTimeSeconds((long)achievement["unlocktime"]).DateTime;

                achievements[i] = $"Name: {name}, Achieved: {achieved}, Unlock Time: {unlockTime}";
            }

            return achievements;
        }
    }

    public static class SteamApiClient
    {
        private static readonly string apiKey = "2EDCB9C589BAC36AEEA34FC390F24E0E";
        private static readonly string baseUrl = "http://api.steampowered.com/";
        private static readonly HttpClient client = new HttpClient();

        public static async Task<string> GetPlayerAchievementsAsync(string steamId, string gameId)
        {
            try
            {
                string url = $"{baseUrl}ISteamUserStats/GetPlayerAchievements/v0001/?appid={gameId}&key={apiKey}&steamid={steamId}";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected error: {e.Message}");
                return null;
            }
        }
    }
}
