using System;
using ParrelSync;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace Framework.Platforms
{
    public class PlatformStandalone : PlatformBase
    {
        protected override void Initialize(Action<string> onInitialized,  Action onFailure)
        {
            onInitialized?.Invoke(null);
        }

        protected override void OnInitialized()
        {

        }

        public override string GetPlatformUserId()
        {
            return string.Empty;
        }

        protected override void OnUpdate()
        {

        }

        protected override void Shutdown()
        {

        }

        public override string GetDisplayName()
        {
#if UNITY_EDITOR

            return $"{Environment.UserName}-{ParrelSyncUtility.GetCloneArgument()}";

#else
            
            return $"{Environment.UserName}";
            
#endif
        }

        public override void GetAvatarUrl(Action<string> onSuccess, Action onFailure)
        {

        }

        protected override void GetAvatar(Action<Texture2D> onSuccess, Action onFailure)
        {

        }

        public override void BackendLogin(GetPlayerCombinedInfoRequestParams combinedInfoRequestParams, Action<LoginResult> onLoginResult, Action onFailure)
        {
            LoginWithCustomIDRequest request = new LoginWithCustomIDRequest
            {
                CreateAccount = true,

#if UNITY_EDITOR
                CustomId = $"{SystemInfo.deviceUniqueIdentifier}-{ParrelSyncUtility.GetCloneArgument()}",
#else
                CustomId = $"{SystemInfo.deviceUniqueIdentifier}",
#endif

                TitleId = GlobalData.PlayfabTitleId,
                InfoRequestParameters = combinedInfoRequestParams,
            };

            PlayFabClientAPI.LoginWithCustomID(request, onLoginResult, error =>
            {
                this.LogError(error.GenerateErrorReport());
                onFailure?.Invoke();
            });
        }

        protected override void Internal_IncrementAchievementStatValue(string key)
        {

        }

        protected override void Internal_UnlockAchievement(string key)
        {

        }
    }
}