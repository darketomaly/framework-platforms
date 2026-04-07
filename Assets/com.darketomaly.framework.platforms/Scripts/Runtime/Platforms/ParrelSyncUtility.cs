using UnityEngine;

namespace ParrelSync
{
    public static class ParrelSyncUtility
    {
        public static bool ForceStandalone()
        {
            #if UNITY_EDITOR
            Debug.Log($"Clone argument: {ParrelSync.ClonesManager.GetArgument()}");
            return ParrelSync.ClonesManager.IsClone();
            #else
            return false;
            #endif
        }

        public static string GetCloneArgument()
        {
            #if UNITY_EDITOR
            string arg = ParrelSync.ClonesManager.GetArgument();
            #else
            string arg = null;
            #endif

            if (string.IsNullOrEmpty(arg))
            {
                // To do
                // Build index?
                
                return "original";
            }
            else
            {
                return arg;
            }
        }
    }
}