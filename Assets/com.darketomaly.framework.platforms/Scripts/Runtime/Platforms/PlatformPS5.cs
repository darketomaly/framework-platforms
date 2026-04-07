using System;
using PlayFab.ClientModels;
using UnityEngine;

namespace Framework.Platforms
{
    public class PlatformPS5 : PlatformBase
    {
        protected override void Initialize(Action<string> onInitialized, Action onFailure)
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
            return null;
        }

        public override void GetAvatarUrl(Action<string> onSuccess, Action onFailure)
        {

        }

        protected override void GetAvatar(Action<Texture2D> onSuccess, Action onFailure)
        {

        }

        public override void BackendLogin(GetPlayerCombinedInfoRequestParams combinedInfoRequestParams, Action<LoginResult> onLoginResult, Action onFailure)
        {

        }

        protected override void Internal_IncrementAchievementStatValue(string key)
        {

        }

        protected override void Internal_UnlockAchievement(string key)
        {

        }
    }
}