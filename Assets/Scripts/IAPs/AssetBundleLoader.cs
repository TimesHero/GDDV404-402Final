using System.Collections;
using System.IO;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;  //loading an asset bundle means technically loading it from a URL. A URL can mean internet, or somewhere on a local device.

public class AssetBundleLoader : MonoBehaviour
{
    public string bundleName;
    public string variantName;

    //very much spaghetti code but idk how else to do it
    //brain cells fail me
    private const string BUNDLE_NAME_FOR_GEM_UI = "icons/gems";
    [SerializeField] private string greenGemAssetName = "1";
    [SerializeField] private string redGemAssetName = "2";
    public PurchaseFufillment GemManager;

    //these two arrays need to line up, allegedly spaghetti code
    public string[] assetNames;
    public Image[] images;

    void Start()
    {
        StartCoroutine(LoadBundleFromURL());
    }

    private IEnumerator LoadBundleFromURL()
    {
        //loads all the assets in the requested bundle
        string extension = string.IsNullOrEmpty(variantName) ? string.Empty : '.' + variantName;
        string url = Path.Combine(Application.streamingAssetsPath, bundleName, extension);

        using UnityWebRequest webRequest = UnityWebRequestAssetBundle.GetAssetBundle(url);

        yield return webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to download AssetBundle: {webRequest.error}");
            yield break;
        }

        //downloads the bundle
        AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(webRequest);

        //once the bundle is downloaded, load all of the bundle's contents, if possible
        for (int i = 0; i < assetNames.Length; i++)
        {
            if (assetNames.Length > 0)
            { yield return StartCoroutine(LoadSpriteFromBundle(bundle, assetNames[i], images[i])); }
        }
        

        //special case for gem UI
        //TURBO SPAGHETTI CODE DON'T LET YOUR KIDS WATCH IT
        if (bundleName == BUNDLE_NAME_FOR_GEM_UI)
        {
            Debug.Log("I love you x1");

            GemManager = FindFirstObjectByType(typeof(PurchaseFufillment)) as PurchaseFufillment;

            //loads green gem via spaghetti code
            yield return StartCoroutine(LoadSpriteFromBundle(bundle, greenGemAssetName, 1));
            //loads red gem via spaghetti code
            yield return StartCoroutine(LoadSpriteFromBundle(bundle, redGemAssetName, 'r'));

            if (GemManager != null)
            {
                GemManager.UpdateGemsDisplay();
            }
        }
        

        //then unload the bundle for space saving purposes
        bundle.Unload(false);
    }

    //called from the LoadBundleFromURL coroutine
    //loads a sprite by name from the bundle. unpacking the bundle also takes time.
    //not a very scalable method, you would need to use this a lot of times.
    private IEnumerator LoadSpriteFromBundle(AssetBundle bundle, string assetName, Image icon)
    {
        //loads the asset from the bundle
        AssetBundleRequest bundleRequest = bundle.LoadAssetAsync<Sprite>(assetName);
        yield return bundleRequest;

        //if something is loaded, set it.
        if (bundleRequest.asset != null)
        {
            icon.sprite = (Sprite)bundleRequest.asset;
            Debug.Log($"Loaded {assetName} from {bundleName}");
        }
        else Debug.LogError($"Failed to load {assetName} from {bundleName}");
    }

    //overload for Green Gem parameter of thinge
    private IEnumerator LoadSpriteFromBundle(AssetBundle bundle, string assetName, int a)
    {
        //loads the asset from the bundle
        AssetBundleRequest bundleRequest = bundle.LoadAssetAsync<Sprite>(assetName);
        yield return bundleRequest;

        //if something is loaded, set it.
        if (bundleRequest.asset != null)
        {
            GemManager.greenGemSprite = (Sprite)bundleRequest.asset;
            Debug.Log($"Loaded {assetName} from {bundleName}");
        }
        else Debug.LogError($"Failed to load {assetName} from {bundleName}");
    }

    //overload for Red Gem parameter of thinge
    private IEnumerator LoadSpriteFromBundle(AssetBundle bundle, string assetName, char b)
    {
        //loads the asset from the bundle
        AssetBundleRequest bundleRequest = bundle.LoadAssetAsync<Sprite>(assetName);
        yield return bundleRequest;

        //if something is loaded, set it.
        if (bundleRequest.asset != null)
        {
            GemManager.redGemSprite = (Sprite)bundleRequest.asset;
            Debug.Log($"Loaded {assetName} from {bundleName}");
        }
        else Debug.LogError($"Failed to load {assetName} from {bundleName}");
    }

}


/*
using System.Collections;
using System.IO;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking; //loading an asset bundle means technically loading it from a URL. A URL can mean internet, or somewhere on a local device.

public class AssetBundleLoader : MonoBehaviour
{
    public string bundleName;
    public string variantName;

    //these two arrays need to line up
    public string[] assetNames;
    public Image[] icons;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(LoadBundleFromURL());
    }

    //loads bundle from URL
    //calls the LoadSpriteFromBundle coroutine
    private IEnumerator LoadBundleFromURL()
    {
        //? is a ternary operator, left is true : right is false
        string extention = string.IsNullOrEmpty(variantName) ? string.Empty : '.' + variantName;
        string url = Path.Combine(Application.streamingAssetsPath, bundleName, extention);

        //using = disposable: when this falls out of scope, this request is disposed of,
        //the connection is closed, and the connection and resources it uses are freed up.
        using UnityWebRequest webRequest = UnityWebRequestAssetBundle.GetAssetBundle(url); 

        yield return webRequest.SendWebRequest(); //delays the function until the web request is done, as the request takes time to finish.
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            //if unsuccessful
            Debug.LogError($"Failed to download AssetBundle: {webRequest.error}");
            yield break;
        }

        //if successfully retrieved, get it's content
        AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(webRequest);

        print($"bundle: {bundle}");
        //unpacking the bundle also takes time, use the LoadSpriteFromBundle coroutine
        for (int i = 0; i < assetNames.Length; i++) 
        {
            yield return StartCoroutine(LoadSpriteFromBundle(bundle, assetNames[i], icons[i]));
        }

        //unloads the bundle, but not the assets in the bundle (hence the false)
        bundle.Unload(false);
    }

    //called from the LoadBundleFromURL coroutine
    //loads a sprite by name from the bundle. unpacking the bundle also takes time.
    //not a very scalable method, you would need to use this a lot of times.
    private IEnumerator LoadSpriteFromBundle(AssetBundle bundle, string assetName, Image icon)
    {
        AssetBundleRequest bundleRequest = bundle.LoadAssetAsync<Sprite>(assetName);
        yield return bundleRequest;

        //if the load is successful
        if (bundleRequest.asset != null)
        {
            icon.sprite = (Sprite)bundleRequest.asset;
            Debug.Log($"Loaded {assetName} from {bundleName}");
        }
        else Debug.LogError($"Failed to load {assetName} from {bundleName}");
    }
}
*/
