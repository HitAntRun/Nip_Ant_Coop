using UnityEngine;

public class MagnifierController : MonoBehaviour
{
    [Header("Refs")]
    public Camera mainCam;
    public Camera magnifierCam;
    public RectTransform magnifierRoot;
    public LayerMask floorLayer;
    private Canvas canvas;
    public RectTransform searchLight;
    public Vector2 searchLightOffset = new Vector2(60f, -60f);
    public RectTransform magnifierSprite;
    public Vector2 magnifierOffset = new Vector2(60f, -60f);
    public RectTransform lensArea; 

    [Header("Zoom")]
    public float normalSize = 1.5f;
    public Vector3 AimPoint { get; private set; }
    public bool AimValid { get; private set; }
    public float SearchJudgeRadiusPx =>
        searchCircleSize.y * 0.5f * canvas.scaleFactor / searchMagnification;

    [Header("Search Mode")]
    private Vector2 normalCircleSize;
    public Vector2 searchCircleSize = new Vector2(500, 500);
    public float searchMagnification = 4f;
    public GameObject searchBackground;
    private bool isSearchMode = false;
    public bool IsSearchMode => isSearchMode;
    
    [Header("Options")]
    public bool hideSystemCursor = false;
    
    void Start()
    {
        magnifierRoot.gameObject.SetActive(true);
        magnifierCam.enabled = true;

        canvas = magnifierRoot.GetComponentInParent<Canvas>();
        normalCircleSize = magnifierRoot.sizeDelta;
        normalSize = ComputeOneToOneSize();

        ApplyMode();
    }

    void Update()
    {
        Vector2 mouse = Input.mousePosition;
        magnifierRoot.position = mouse;
        if (searchLight && isSearchMode)
        {
            float r = magnifierRoot.sizeDelta.x * 0.5f * canvas.scaleFactor;
            searchLight.position = mouse + new Vector2(r, -r) * 0.7f + searchLightOffset;
        }

        if (magnifierSprite && !isSearchMode)
            magnifierSprite.position = mouse + magnifierOffset;
        AimMagnifierCamera(mouse);
    }
    
    public void GetJudgeCircle(out Vector2 center, out float radius)
    {
        if (isSearchMode)
        {
            center = magnifierRoot.position;
            radius = searchCircleSize.y * 0.5f * canvas.scaleFactor / searchMagnification;
        }
        else
        {
            center = lensArea.position;
            radius = lensArea.rect.width * 0.5f * canvas.scaleFactor;
        }
    }

    void ApplyMode()
    {
        if(searchBackground)
            searchBackground.SetActive(isSearchMode);
        if (searchLight)
            searchLight.gameObject.SetActive(isSearchMode);
        if(magnifierSprite)
            magnifierSprite.gameObject.SetActive(!isSearchMode);
        
        magnifierRoot.sizeDelta = isSearchMode ? searchCircleSize : normalCircleSize;
        magnifierCam.orthographicSize = isSearchMode ? ComputeSearchSize() : normalSize;
    }

    public void ToggleSearchMode()
    {
        isSearchMode = !isSearchMode;
        ApplyMode();
        TutorialController.instance?.NotifyMagnify();
    }

    void AimMagnifierCamera(Vector2 mouse)
    {
        Vector3 wp;
        Ray ray = mainCam.ScreenPointToRay(mouse);
        if (Physics.Raycast(ray, out RaycastHit hit, 2000f, floorLayer))
        {
            wp = hit.point;
            AimValid = true;
        }
        else
        {
            wp = mainCam.ScreenToWorldPoint(
                            new Vector3(mouse.x, mouse.y, mainCam.nearClipPlane + 20f));
            AimValid = false;
        }
        AimPoint = wp;
            
        
        magnifierCam.transform.rotation = mainCam.transform.rotation;
        float d = Vector3.Distance(mainCam.transform.position, wp);
        magnifierCam.transform.position = wp - magnifierCam.transform.forward * d;
    }

    float ComputeOneToOneSize()
    {
        float circlePx = magnifierRoot.rect.height * canvas.scaleFactor;
        return mainCam.orthographicSize * (circlePx / Screen.height);
    }

    float ComputeSearchSize()
    {
        float circlePx = searchCircleSize.y * canvas.scaleFactor;
        return mainCam.orthographicSize * (circlePx / Screen.height) / searchMagnification;
    }
}
