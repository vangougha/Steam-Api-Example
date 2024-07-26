using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SteamApiConsoleApp
{
    class Program
    {
        /*

        Steam User ID Example : 76561199161083869

        Steam Game ID Example : 341940

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

            GetOwnedGamesAsync(steamId, gameId);
            var ownedGames = await GetOwnedGamesAsync(steamId, gameId);
            Console.WriteLine(ownedGames);
            var userInfo = await GetUserInfoAsync(steamId);
            Console.WriteLine(userInfo);
            string achievementsJson = await SteamApiClient.GetPlayerAchievementsAsync(steamId, gameId);
            var gameInfo = await GetGameInfoTestAsync(gameId);
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

        private static async Task<string> GetGameInfoTestAsync(string gameId)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = $"{baseUrl}ISteamUserStats/GetNumberOfCurrentPlayers/v1/?key={apiKey}&appid={gameId}";

                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(responseBody);

                return json.ToString();
                // Json dosyasında cok bi sey yok sadece oyunda kac kisi var falan onu gosteriyor 
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

                // Oyunların listesine erişim
                JArray games = (JArray)json["response"]["games"];
                if (games != null)
                {
                    foreach (JObject game in games)
                    {
                        if (game["appid"].ToString() == gameId)
                        {
                            var playtimeForever = game["playtime_forever"].ToString();
                            var playtime2weeks = game["playtime_2weeks"]?.ToString() ?? "0"; //2 haftada eğer oynamadıysa burda devreye giriyo 
                            return $"Game ID: {gameId}, Playtime Forever: {playtimeForever} minutes, Playtime Last 2 Weeks: {playtime2weeks} minutes";
                        }
                    }
                }
                return $"Game with ID {gameId} not found in the user's library.";
            }
            catch (Exception e)
            {
                return $"Error: {e.Message}";
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
                // Log the exception or handle it as needed
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
            catch (Exception e)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Unexpected error: {e.Message}");
                return null;
            }
        }
        //JSON parçalama kısımlarını chat gpt yaptı bunu öğrenmem lazım
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

                return $"Kullanıcı Adı: {personaName}\nProfil URL: {profileUrl}\nAvatar URL: {avatarUrl}";
            }
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
                // Log the exception or handle it as needed
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
            catch (Exception e)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Unexpected error: {e.Message}");
                return null;
            }
        }
    }
}
