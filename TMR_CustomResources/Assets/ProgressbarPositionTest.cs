using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class ProgressbarPositionTest : MonoBehaviour
{
    public RectTransform mainUIPanel;

    RectTransform healthbarTransform;
    RectTransform barRootTransform;
    RectTransform progressBarTransform;

    public (Vector3 bottomLeftOffset, Vector2 size) GetBarPositionAndSize()
    {
        //find and set parent to the center cluster
        if(progressBarTransform.parent.name != "BottomCenterCluster")
        {
            var cluster = mainUIPanel.transform.Find("SpringCanvas/BottomCenterCluster");
            if(cluster != null)
            {
                progressBarTransform.SetParent(cluster);
            }
            else
            {
                //fallback to the main panel
                progressBarTransform.SetParent(mainUIPanel.transform);
            }
        }

        Vector2 parentSize = progressBarTransform.parent.GetComponent<RectTransform>().rect.size;

        //fallback values in case healthbar is not found
        Vector2 size = new Vector2(Screen.width/3.5f, Screen.height/27f);
        Vector3 bottomLeftOffset = new Vector3(parentSize.x/2 - size.x/2, size.y * 6, 0);

        var healthBarRoot = mainUIPanel.transform.Find("SpringCanvas/BottomLeftCluster/BarRoots/HealthbarRoot");
        var barRoots = mainUIPanel.transform.Find("SpringCanvas/BottomLeftCluster/BarRoots");

        if (healthBarRoot == null || barRoots == null)
        {
            return (bottomLeftOffset, size);
        }

        if(healthbarTransform == null || barRootTransform == null)
        {
            healthbarTransform = healthBarRoot.GetComponent<RectTransform>();
            barRootTransform = barRoots.GetComponent<RectTransform>();
        }

        size = new Vector2(parentSize.x * 0.8f, healthbarTransform.rect.height);

        //use law of sines to get the depth of the healthbar after 6 degrees of rotation
        float depthOffset = barRootTransform.rect.width
                            / Mathf.Sin(90 * Mathf.Deg2Rad) 
                            * Mathf.Sin(-barRoots.parent.rotation.eulerAngles.y * Mathf.Deg2Rad);


        Debug.Log(healthbarTransform.GetBottomLeftOffset());
        bottomLeftOffset = new Vector3(parentSize.x/2 - size.x/2, healthbarTransform.GetBottomLeftOffset().y, depthOffset);

        return (bottomLeftOffset, size);
    }

    public void UpdatePositionAndSize()
    {
        var (bottomLeftOffset, size) = GetBarPositionAndSize();

        progressBarTransform.SetSizeInPixels(size.x, size.y);
        progressBarTransform.SetBottomLeftOffset(bottomLeftOffset.x, bottomLeftOffset.y);
        progressBarTransform.localScale = Vector3.one;
        progressBarTransform.localPosition = progressBarTransform.localPosition.SetZ(bottomLeftOffset.z);
    }

    void Update()
    {
        UpdatePositionAndSize();
    }

    void Start()
    {
        progressBarTransform = GetComponent<RectTransform>();
    }
}

