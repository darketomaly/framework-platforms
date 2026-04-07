using System;
using ParrelSync;
using UnityEngine;

namespace Framework.Platforms
{
    public class PlatformManager : MonoBehaviour
    {
        public static PlatformManager Instance;
        public static Action OnInitialized;

        [NonSerialized]
        public string SessionTicket;

        public Platform m_Platform;
        public State m_State;
        
        public string Prefix { get; private set; }

        public enum Platform
        {
            Standalone,
            Steam,
            PS5,
            Xbox
        }

        public enum State
        {
            Playtest,
            Demo,
            Retail
        }

        public bool Beta => m_State is State.Playtest;
        public bool Demo => m_State is State.Demo;

        #region Setup

        private void Awake()
        {
            Instance = this;

            // Override platform before initializing

            if (ParrelSyncUtility.ForceStandalone())
            {
                this.Log("Platform forced to Standalone.");
                m_Platform = Platform.Standalone;
            }
            
            // Activate active platform

            PlatformBase activePlatform = null;

            foreach (var pb in GetComponentsInChildren<PlatformBase>(true))
            {
                if (m_Platform == pb.m_Settings.ID)
                {
                    activePlatform = pb;
                    pb.gameObject.SetActive(true);
                    break;
                }
            }
            
            // Set platform prefix

            var displayName = activePlatform.m_Settings.DisplayName;

            Prefix = Instance.m_Platform switch
            {
                Platform.Steam when Instance.Beta => $"{displayName} Playtest",
                Platform.Steam when Instance.Demo => $"{displayName} Demo",
                _ => displayName
            };
        }

        #endregion
    }
}