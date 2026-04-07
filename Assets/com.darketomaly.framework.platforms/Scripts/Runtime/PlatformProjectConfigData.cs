using UnityEditor;
using UnityEngine;

namespace Framework
{
    public class PlatformProjectConfigData : FrameworkModuleData
    {
        [field: SerializeField] 
        public string PlayfabId { get; private set; }
        
        [field: SerializeField] 
        public string SteamAppId { get; private set; }
        
        [MenuItem("Tools/Framework/Inject platform project config")]
        private static void InjectIfNeeded()
        {
            var config = FrameworkProjectConfig.Instance;
            if (config == null) return;

            if (config.GetModule<PlatformProjectConfigData>() == null)
            {
                var playfabData = new PlatformProjectConfigData();
                config.AddModule<PlatformProjectConfigData>(playfabData);

                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();

                Debug.Log("[Framework] Added platform data to project config");
            }
        }
    }
}