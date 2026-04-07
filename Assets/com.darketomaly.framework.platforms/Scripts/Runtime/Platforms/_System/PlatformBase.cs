using System;
using PlayFab.ClientModels;
using UnityEngine;

namespace Framework.Platforms
{
    public class PlayerProfileData
    {
        public string DisplayName;
        public readonly PropertyValue<string> AvatarUrl = new();
    }
    
    public abstract class PlatformBase : MonoBehaviour
    {
        // Platform and backend initialization.
        // Each of these should be re-attempted upon failure

        // 3. Get custom player data.  TO DO
        // 4. Get platform avatar sprite.  TO DO
        // 5. Get platform display name and mark as logged in.  TO DO
        // 6. Get platform avatar url and update playfab avatar.  TO DO

        /// <summary>
        /// Data grabbed from the platform that will be used to update Playfab's data
        /// </summary>
        public PlayerProfileData LocalPlayerProfileData { get; protected set; } = new();

        public static PlatformBase Instance { get; private set; }

        public PlatformSettings m_Settings;

        public bool IsInitialized { get; private set; }

        protected virtual void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // Initialize platform

            ErrorHandler.Execute("Platform Initialization", onFailure =>
            {
                Initialize(sessionTicket =>
                {
                    PlatformManager.Instance.SessionTicket = sessionTicket;
                    MarkAsInitialized();
                }, onFailure);
            });
            
            FrameworkProjectConfig.Instance.SetDiscordStreamData(GetDisplayName(), GetPlatformUserId(), PlatformManager.Instance.m_Platform.ToString(), Application.version);
        }

        private void OnEnable()
        {

        }

        private void OnDisable()
        {
            Shutdown();
            IsInitialized = false;
        }

        private void Update()
        {
            OnUpdate();
        }

        private void MarkAsInitialized()
        {
            ErrorHandler.Execute("Get avatar sprite", onFailure =>
            {
                GetAvatar(img =>
                {
                    //UIBindings.Instance.Avatar.Value = Sprite.Create(img, new Rect (0, 0, 128, 128), new Vector2 ());
                }, onFailure);
            });
            
            FrameworkProjectConfig.Instance.SetDiscordStreamData(GetDisplayName(), GetPlatformUserId(), PlatformManager.Instance.m_Platform.ToString(), Application.version);

            // Mark as initialized, avatar sprite is not essential and will be grabbed in the background
            OnInitialized();
            IsInitialized = true;
            PlatformManager.OnInitialized?.Invoke();
        }

        public abstract void GetAvatarUrl(Action<string> onSuccess, Action onFailure);

        protected abstract void GetAvatar(Action<Texture2D> onSuccess, Action onFailure);

        /// <param name="onInitialized">Contains the session ticket.</param>
        protected abstract void Initialize(Action<string> onInitialized, Action onFailure);

        protected abstract void OnInitialized();

        protected abstract void OnUpdate();

        protected abstract void Shutdown();

        public abstract string GetPlatformUserId();

        public abstract string GetDisplayName();

        public abstract void BackendLogin(GetPlayerCombinedInfoRequestParams combinedInfoRequestParams, Action<LoginResult> onLoginResult, Action onFailure);

        public void IncrementAchievementStatValue(string key)
        {
            // Handling with internal calls to avoid repeating init checks elsewhere
            // Can be handled once in base and never need to rely on caller or implementation to handle it.
            
            if (IsInitialized)
                Internal_IncrementAchievementStatValue(key);
        }

        protected abstract void Internal_IncrementAchievementStatValue(string key);

        public void UnlockAchievement(string key)
        {
            if (IsInitialized)
                Internal_UnlockAchievement(key);
        }

        protected abstract void Internal_UnlockAchievement(string key);
    }
}