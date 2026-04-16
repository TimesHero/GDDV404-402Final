using System.Collections;
using System.IO;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class AssetBundleLoader : MonoBehaviour
{
    public string bundleName;
    public string variantName;

    private const string BUNDLE_NAME_FOR_GEM_UI = "icons/gems";
    [SerializeField] private string greenGemAssetName = "1";
    [SerializeField] private string redGemAssetName = "2";
    public PurchaseFufillment GemManager;

    public string[] assetNames;
    public Image[] images;

    void Start()
    {
        StartCoroutine(LoadBundleFromURL());
    }

    private IEnumerator LoadBundleFromURL()
    {
        string extension = string.IsNullOrEmpty(variantName) ? string.Empty : '.' + variantName;
        string url = Path.Combine(Application.streamingAssetsPath, bundleName, extension);

        using UnityWebRequest webRequest = UnityWebRequestAssetBundle.GetAssetBundle(url);

        yield return webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to download AssetBundle: {webRequest.error}");
            yield break;
        }

        AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(webRequest);

        for (int i = 0; i < assetNames.Length; i++)
        {
            if (assetNames.Length > 0)
            { yield return StartCoroutine(LoadSpriteFromBundle(bundle, assetNames[i], images[i])); }
        }

        if (bundleName == BUNDLE_NAME_FOR_GEM_UI)
        {
            Debug.Log("I love you x1");

            GemManager = FindFirstObjectByType(typeof(PurchaseFufillment)) as PurchaseFufillment;

            yield return StartCoroutine(LoadSpriteFromBundle(bundle, greenGemAssetName, 1));
            yield return StartCoroutine(LoadSpriteFromBundle(bundle, redGemAssetName, 'r'));

            if (GemManager != null)
            {
                GemManager.UpdateGemsDisplay();
            }
        }

        bundle.Unload(false);
    }

    private IEnumerator LoadSpriteFromBundle(AssetBundle bundle, string assetName, Image icon)
    {
        AssetBundleRequest bundleRequest = bundle.LoadAssetAsync<Sprite>(assetName);
        yield return bundleRequest;

        if (bundleRequest.asset != null)
        {
            icon.sprite = (Sprite)bundleRequest.asset;
            Debug.Log($"Loaded {assetName} from {bundleName}");
        }
        else Debug.LogError($"Failed to load {assetName} from {bundleName}");
    }

    private IEnumerator LoadSpriteFromBundle(AssetBundle bundle, string assetName, int a)
    {
        AssetBundleRequest bundleRequest = bundle.LoadAssetAsync<Sprite>(assetName);
        yield return bundleRequest;

        if (bundleRequest.asset != null)
        {
            GemManager.greenGemSprite = (Sprite)bundleRequest.asset;
            Debug.Log($"Loaded {assetName} from {bundleName}");
        }
        else Debug.LogError($"Failed to load {assetName} from {bundleName}");
    }

    private IEnumerator LoadSpriteFromBundle(AssetBundle bundle, string assetName, char b)
    {
        AssetBundleRequest bundleRequest = bundle.LoadAssetAsync<Sprite>(assetName);
        yield return bundleRequest;

        if (bundleRequest.asset != null)
        {
            GemManager.redGemSprite = (Sprite)bundleRequest.asset;
            Debug.Log($"Loaded {assetName} from {bundleName}");
        }
        else Debug.LogError($"Failed to load {assetName} from {bundleName}");
    }
}
