using UnityEngine;

namespace AZUR
{
    public sealed class AzurGameSceneButtons : MonoBehaviour
    {
        public void ShowMaxDebugger()
        {
            Debug.Log("[AZUR] UI Action: Show MAX Debugger");
            AzurAds.ShowMediationDebugger();
        }

        public void ShowRewardedAd()
        {
            Debug.Log("[AZUR] UI Action: Show Rewarded");
            AzurAds.ShowRewarded("game_scene_rewarded");
        }
    }
}
