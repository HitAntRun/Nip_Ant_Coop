using UnityEngine;
using UnityEngine.UI;

public class MenuTab : MonoBehaviour
{
    [System.Serializable]
    public class Tab
    {
        public Button button;
        public GameObject panel;
    }

    [SerializeField] Tab[] tabs;
    [SerializeField] float baseY = 300f;
    [SerializeField] float activeOffsetY = 26f;
    [SerializeField] int startIndex = 0;

    void Awake()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int idx = i;
            tabs[i].button.onClick.AddListener(() => Select(idx));
        }
    }

    void Start() => Select(startIndex);

    public void Select(int index)
    {
        for (int i = tabs.Length - 1; i >= 0; i--)
            if (i != index)
                tabs[i].button.transform.SetSiblingIndex(0);

        tabs[index].button.transform.SetAsLastSibling();

        for (int i = 0; i < tabs.Length; i++)
        {
            bool active = i == index;
            var rt = (RectTransform)tabs[i].button.transform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x,
                baseY + (active ? activeOffsetY : 0f));
            tabs[i].panel.SetActive(active);
        }
    }
}