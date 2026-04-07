using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace Framework.Platforms
{
    public class Backend : MonoBehaviour
    {
        public static Backend Instance;
        public static bool Banned;
        
        public string EntityId { get; private set; } = "0";
        
        public string EntityType { get; private set; }

        public UserData LocalUserData { get; private set; }
        
        public PersistentUserData LocalPersistentUserData { get; private set; }

        private static string DataKey => Application.version;

        /// <summary> Returns news text and link </summary>
        public static event Action<string, string> OnTitleNewsFetched;
        private static Action<LoginResult> OnLogin;

        private LoginResult m_loginResult;
        private State m_currentState = State.Uninitialized;
        private bool m_loggedIn;
        
        public string TitleNewsText { get; private set; }
        
        public string TitleNewsLink { get; private set; }

        private void Awake()
        {
            Instance = this;
            PlatformManager.OnInitialized += OnPlatformInitialized;
        }

        private void OnPlatformInitialized()
        {
            SetState(State.LoggingIn);
        }

        private void SetState(State state)
        {
            m_currentState = state;

            switch (m_currentState)
            {
                case State.LoggingIn:
                    this.Log("Logging in.");
                    APILogin();
                    break;
                
                case State.FetchingTitleNews:
                    this.Log($"Retrieving title news.");
                    FetchTitleNews();
                    break;
                
                case State.Error:
                    this.LogError($"Error.");
                    break;
            }
        }
        
        private void APILogin()
        {
            PlayFabSettings.TitleId = FrameworkProjectConfig.Instance.GetModule<PlatformProjectConfigData>().PlayfabId;

            var combinedRequestParams = new GetPlayerCombinedInfoRequestParams
            {
                GetUserData = true,
            };
            
            ErrorHandler.Execute("Backend login", onFailure =>
            {
                PlatformBase.Instance.BackendLogin(combinedRequestParams, OnLoginResult, onFailure);
            }, 2);
        }

        private void OnLoginResult(LoginResult result)
        {
            this.Log($"Successfully logged in: {result.EntityToken.EntityToken}");

            EntityId = result.EntityToken.Entity.Id;
            EntityType = result.EntityToken.Entity.Type;
            
            // Attempt to grab values

            var versionedData = result.InfoResultPayload.UserData.TryGetValue(DataKey, out var versionedDataValue);
            var persistentData = result.InfoResultPayload.UserData.TryGetValue("PersistentData", out var persistentDataValue);
            var forceUpdateUserData = false;
            
            if (versionedData)
            {
                LocalUserData = JsonUtility.FromJson<UserData>(versionedDataValue.Value);
                LocalUserData.Validate();
            }
            else
            {
                LocalUserData = null;
            }

            if (persistentData)
            {
                LocalPersistentUserData = JsonUtility.FromJson<PersistentUserData>(persistentDataValue.Value);
                
                #if UNITY_EDITOR

                this.LogImportant("Marking playfab account as developer");

                if (!LocalPersistentUserData.m_Developer)
                {
                    LocalPersistentUserData.m_Developer = true;
                    forceUpdateUserData = true;
                }
                
                #endif
            }
            else
            {
                LocalPersistentUserData = null;
            }
            
            // If one of the values were not found, create them and update backend
            
            if(!versionedData || !persistentData || forceUpdateUserData)
            {
                this.LogError("No player data found, creating one.");

                LocalUserData ??= new UserData();
                LocalPersistentUserData ??= new PersistentUserData();
                
                #if UNITY_EDITOR

                this.LogImportant("Marking playfab account as developer");
                LocalPersistentUserData.m_Developer = true;
                
                #endif

                UpdateUserDataRequest request = new UpdateUserDataRequest
                {
                    Data = new Dictionary<string, string>
                    {
                        {DataKey, JsonUtility.ToJson(LocalUserData)},
                        {"PersistentData", JsonUtility.ToJson(LocalPersistentUserData)},
                    }
                };
                
                ErrorHandler.Execute("Update user data on login", onFailure =>
                {
                    PlayFabClientAPI.UpdateUserData(request,
                        _ =>
                        {
                            this.LogImportant("Updated player data.");
                        
                            LocalUserData.Validate();
                            AfterLogin(result);
                        }, error =>
                        {
                            this.LogError(error.GenerateErrorReport());
                            onFailure?.Invoke();
                        });
                });
            }
            else
            {
                AfterLogin(result);
            }
            
            SetState(State.FetchingTitleNews);
        }

        private void AfterLogin(LoginResult result)
        {
            // After playfab auth, update display name and avatar
            // And afterwards, confirm as logged in
            
            UpdateUserTitleDisplayNameRequest displayNameRequest = new();
            
            PlatformBase.Instance.LocalPlayerProfileData.DisplayName = PlatformBase.Instance.GetDisplayName();

            var targetDisplayName = PlatformBase.Instance.LocalPlayerProfileData.DisplayName;
            
            // Playfab display names must be between 3 and 25 characters

            if (string.IsNullOrEmpty(targetDisplayName) || targetDisplayName.Length < 3)
            {
                targetDisplayName = "xxx";
            }
            
            if (!string.IsNullOrEmpty(targetDisplayName) && targetDisplayName.Length > 25)
            {
                targetDisplayName = targetDisplayName[..Math.Min(25, targetDisplayName.Length)];
            }
            
            displayNameRequest.DisplayName = targetDisplayName;
            
            ErrorHandler.Execute("Update display name after login", onFailure1 =>
            {
                PlayFabClientAPI.UpdateUserTitleDisplayName(displayNameRequest, displayNameResult =>
                {
                    m_loggedIn = true;
                    m_loginResult = result;
                    OnLogin?.Invoke(result);
                    
                    // After login, get avatar url and update it async, not essential for startup
                    
                    ErrorHandler.Execute("Get avatar url and update it after login", onFailure2 =>
                    {
                        PlatformBase.Instance.GetAvatarUrl(url =>
                        {
                            if (!string.IsNullOrEmpty(url))
                            {
                                PlatformBase.Instance.LocalPlayerProfileData.AvatarUrl.Value = url;
                                this.Log($"Avatar url: {url}");
                        
                                UpdateAvatarUrlRequest updateUrlRequest = new();
                                updateUrlRequest.ImageUrl = url;
                                
                                PlayFabClientAPI.UpdateAvatarUrl(updateUrlRequest, avatarUrlResult =>
                                {
                                    this.Log("Updated avatar url");
                                }, error2 =>
                                {
                                    // If playfab failed to update avatar URL, re-grab it from the platform just it case
                                    // In other words, mark "Get avatar url after login" as failed
                                    
                                    this.LogError(error2.GenerateErrorReport());
                                    onFailure2?.Invoke();
                                });
                            }
                            else
                            {
                                // If grabbed avatar URL is empty
                                onFailure2?.Invoke();
                            }
                        }, onFailure2);
                    });
                }, error =>
                {
                    this.LogError(error.GenerateErrorReport());
                    DiscordDataStream.Push(error.GenerateErrorReport());
                    onFailure1?.Invoke();
                });
            });
        }
        
        private void FetchTitleNews()
        {
            ErrorHandler.Execute("Fetch title news", onFailure =>
            {
                PlayFabClientAPI.GetTitleNews(new GetTitleNewsRequest { Count = 3 }, 
                    result =>
                    {
                        foreach (TitleNewsItem titleNewsItem in result.News)
                        {
                            if (titleNewsItem.Title == Application.version)
                            {
                                this.Log($"Retrieved title news.");
                        
                                TitleNews parsedNews = JsonUtility.FromJson<TitleNews>(titleNewsItem.Body);

                                TitleNewsText = parsedNews.m_Alert;
                                TitleNewsLink = parsedNews.m_FeedbackLink;
                                
                                OnTitleNewsFetched?.Invoke(parsedNews.m_Alert, parsedNews.m_FeedbackLink);
                                return;
                            }
                        }
                
                        this.LogImportant("Couldn't find title news for this version.");
                    }, 
                    error =>
                    {
                        this.LogError($"Couldn't retrieve title news. {error.GenerateErrorReport()}");
                        onFailure?.Invoke();
                    });
            });
        }

        #region Public methods

        public static void OnLoginExecute(Action<LoginResult> methodToExecute)
        {
            if (Instance && Instance.m_loggedIn)
            {
                methodToExecute?.Invoke(Instance.m_loginResult);
            }
            else
            {
                OnLogin += methodToExecute;
            }
        }

        public static void Unsubscribe(Action<LoginResult> methodToUnsuscribe)
        {
            OnLogin -= methodToUnsuscribe;
        }

        public void UpdateAvatarImageUrl()
        {
            UpdateAvatarUrlRequest request = new UpdateAvatarUrlRequest();
            request.ImageUrl = PlatformBase.Instance.LocalPlayerProfileData.AvatarUrl.Value;
            
            PlayFabClientAPI.UpdateAvatarUrl(request, result =>
            {
                this.Log("Updated avatar image url.");
            }, OnFailure);
        }

        public static void UpdateUserData()
        {
            if (Instance && Instance.m_loggedIn)
            {
                UpdateUserDataRequest request = new UpdateUserDataRequest
                {
                    Data = new Dictionary<string, string>
                    {
                        {DataKey, JsonUtility.ToJson(Instance.LocalUserData)},
                    }
                };
                
                PlayFabClientAPI.UpdateUserData(request,
                    _ =>
                    {
                        Instance.LogImportant("Updated player data.");
                        Instance.LocalUserData.Validate();
                    }, Instance.OnFailure);
            }
            else
            {
                Debug.LogError("Backend is not ready to update player data.");
            }
        }
        
        public static void RequestPhotonToken(string photonAppId, Action<GetPhotonAuthenticationTokenResult> onSuccess, Action<string> onError)
        {
            Instance.Log("Requesting photon token...");

            PlayFabClientAPI.GetPhotonAuthenticationToken(new GetPhotonAuthenticationTokenRequest()
            {
                PhotonApplicationId = photonAppId
            }, result =>
            {
                onSuccess.Invoke(result);
            }, error =>
            {
                Instance.LogError(error.GenerateErrorReport());
                onError.Invoke(error.GenerateErrorReport());
            });
        }
        
        #endregion

        private void OnFailure(PlayFabError obj)
        {
            this.LogError(obj.GenerateErrorReport());
            SetState(State.Error);
        }
    }
}