#if STEAMWORKS

using System;
using System.Text;
using System.Threading.Tasks;

using Steamworks;

using PlayFab;
using PlayFab.ClientModels;

using UnityEngine;
using UnityEngine.Networking;

namespace Framework.Platforms
{
    public class PlatformSteam : PlatformBase
    {
        [System.Serializable]
        private class SteamPlayerResponse
        {
            public Response response;
        }

        [System.Serializable]
        private class Response
        {
            public Player[] players;
        }

        [System.Serializable]
        private class Player
        {
            public string avatarfull;
        }
        
        private AuthTicket m_authTicket;
        
        protected override void Awake()
        {
            base.Awake();
            Steamworks.SteamClient.RunCallbacks();

            SteamUserStats.OnAchievementProgress += OnAchievementProgress;
            SteamUserStats.OnUserStatsReceived += OnUserStatsReceived;
        }

        private void OnDestroy()
        {
            m_authTicket?.Cancel();
            m_authTicket = null;
            
            SteamUserStats.OnAchievementProgress -= OnAchievementProgress;
            SteamUserStats.OnUserStatsReceived -= OnUserStatsReceived;
        }
        
        private void OnUserStatsReceived(SteamId arg1, Result arg2)
        {
            this.Log($"<color=yellow>[STEAM ACHIEVEMENT]</color> Stat received result: {arg2}");

            if (arg2 == Result.OK)
            {
                this.Log($"<color=yellow>[STEAM ACHIEVEMENT]</color> Achivements recieved");

                foreach (var item in SteamUserStats.Achievements)
                {
                    this.Log($"<color=yellow>[STEAM ACHIEVEMENT]</color> {item.Identifier}");
                }
            }
        }

        #region Interface Implementation

        protected override async void Initialize(Action<string> onInitialized, Action onFailure)
        {
            try
            {
                Steamworks.SteamClient.Init(GlobalData.SteamAppId, true);

                this.Log("Attempting to get steam ticket");
                
                await Task.Delay(500);
                
                if (!SteamClient.IsValid || !SteamClient.IsLoggedOn)
                {
                    this.LogError("Not logged on!");
                    DiscordDataStream.Push("Not logged on");
                    Shutdown();
                    onFailure?.Invoke();
                    return;
                }

                m_authTicket = await SteamUser.GetAuthTicketForWebApiAsync("AzurePlayFab");

                if (m_authTicket == null || m_authTicket.Data == null || m_authTicket.Data.Length == 0)
                {
                    this.LogError("Invalid ticket.");
                    DiscordDataStream.Push("Invalid ticket");
                    Shutdown();
                    onFailure?.Invoke();
                    return;
                }

                StringBuilder sb = new();

                foreach (var data in m_authTicket.Data)
                {
                    sb.AppendFormat("{0:x2}", data);
                }

                onInitialized?.Invoke(sb.ToString());
            }
            catch ( System.Exception e )
            {
                this.LogError(e);
                
                Shutdown();
                onFailure?.Invoke();
            }
        }
        
        protected override void OnInitialized() { }
        
        protected override void OnUpdate() { }

        protected override void Shutdown()
        {
            m_authTicket?.Cancel();
            Steamworks.SteamClient.Shutdown();
        }
        
        public override string GetPlatformUserId()
        {
            try
            {
                return SteamClient.SteamId.AccountId.ToString();
            }
            catch (Exception e)
            {
                return "0";
            }    
        }

        public override string GetDisplayName()
        {
            try
            {
                return SteamClient.Name;
            }
            catch (Exception e)
            {
                return "Noname";
            }
        }

        public override void BackendLogin(GetPlayerCombinedInfoRequestParams combinedInfoRequestParams, Action<LoginResult> onLoginResult, Action onFailure)
        {
            this.Log($"Attempting to log in with steam. Session: {PlatformManager.Instance.SessionTicket}]");

            if (SteamClient.SteamId.AccountId == 6144)
            {
                this.LogError($"Banned steam id ({SteamClient.SteamId.AccountId})");
                onFailure?.Invoke();
                return;
            }

            var req = new LoginWithSteamRequest
            {
                CreateAccount = true,
                SteamTicket = PlatformManager.Instance.SessionTicket,
                TitleId = GlobalData.PlayfabTitleId,
                TicketIsServiceSpecific = true,
                InfoRequestParameters = combinedInfoRequestParams,
            };
            
            PlayFabClientAPI.LoginWithSteam(req, onLoginResult, error =>
            {
                var report = error.GenerateErrorReport();
                Backend.Banned = error.Error is PlayFabErrorCode.AccountBanned or PlayFabErrorCode.IpAddressBanned;
                
                DiscordDataStream.Push($"{report}", FrameworkProjectConfig.Instance.DiscordWebhookUrl2);
                this.LogError(report);

                if (Backend.Banned)
                {
                    // Do not fail, do not retry
                    this.Log("Banned, not doing anything");
                }
                else
                {
                    onFailure?.Invoke();
                }
            });
        }

        public override async void GetAvatarUrl(Action<string> onSuccess, Action onFailure)
        {
            string url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key=A8172D61D140F1CE7A371A99A0CFDF08&steamids={SteamClient.SteamId}";

            this.Log($"Checking avatar url on: {url}");

            try
            {
                var request = UnityWebRequest.Get(url);
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    this.Log(request.downloadHandler.text);
                    
                    SteamPlayerResponse data = JsonUtility.FromJson<SteamPlayerResponse>(request.downloadHandler.text);
                    var avatarFull = data.response.players[0].avatarfull;
                    onSuccess?.Invoke(avatarFull);
                }
                else
                {
                    this.LogError("Could not get avatar url");
                    onFailure?.Invoke();
                }
            }
            catch (Exception exception)
            {
                this.LogError(exception.Message);
                onFailure?.Invoke();
            }
        }

        protected override async void GetAvatar(Action<Texture2D> onSuccess, Action onFailure)
        {
            try
            {
                var img = await SteamFriends.GetLargeAvatarAsync(SteamClient.SteamId);

                if (img != null)
                {
                    onSuccess?.Invoke(img.Value.Covert());
                }
                else
                {
                    // To do
                    // Check what happens with player that have no profile picture set

                    onFailure?.Invoke();
                }
            }
            catch ( Exception e )
            {
                onFailure?.Invoke();
                Debug.LogError(e);
            }
        }

        #endregion

        #region Achievements
        
        private void OnAchievementProgress(Steamworks.Data.Achievement arg1, int arg2, int arg3)
        {
            this.Log($"<color=yellow>[STEAM ACHIEVEMENT]</color> {arg1.Identifier} {arg2 + arg3 == 0} ({arg2}/{arg3})");
        }

        protected override void Internal_IncrementAchievementStatValue(string key)
        {
            SteamUserStats.AddStat(key, 1);
            SteamUserStats.StoreStats();
        }

        protected override void Internal_UnlockAchievement(string key)
        {
            this.Log($"[STEAM ACHIEVEMENT] Trigger Achivement");

            foreach (var item in SteamUserStats.Achievements)
            {
                if (!item.State && item.Identifier == key)
                {
                    this.Log($"[STEAM ACHIEVEMENT] {key} Exists");
                    
                    item.Trigger(true);
                    SteamUserStats.StoreStats();
                    return;
                }
            }
        }

        #endregion
    }
}

#endif