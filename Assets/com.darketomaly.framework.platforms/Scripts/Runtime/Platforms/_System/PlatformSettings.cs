using UnityEngine;

namespace Framework.Platforms
{
    [CreateAssetMenu]
    public class PlatformSettings : ScriptableObject
    {
        public PlatformManager.Platform ID;
        public string DisplayName;
    }
}