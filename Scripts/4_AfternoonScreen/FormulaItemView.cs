using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FormulaItemView : MonoBehaviour
{
    [Header("Cocktail")]
    [SerializeField] private Image cocktailImage;
    [SerializeField] private TMP_Text cocktailNameText;

    [Header("Materials")]
    [SerializeField] private Image mat1Image;
    [SerializeField] private TMP_Text mat1NameText;
    [SerializeField] private Image mat2Image;
    [SerializeField] private TMP_Text mat2NameText;
    [SerializeField] private Image mat3Image;
    [SerializeField] private TMP_Text mat3NameText;

    public void Apply(RecipeData data)
    {
        if (data == null) return;
        // 名称
        if (cocktailNameText != null) cocktailNameText.text = data.displayName ?? "";
        // 图片
        if (cocktailImage != null) cocktailImage.sprite = LoadSprite(data.cocktailSpritePath);

        // 材料1..3
        ApplyMat(mat1Image, mat1NameText, data, 0);
        ApplyMat(mat2Image, mat2NameText, data, 1);
        ApplyMat(mat3Image, mat3NameText, data, 2);
    }

    private void ApplyMat(Image img, TMP_Text name, RecipeData d, int idx)
    {
        if (d == null) return;
        string n = (d.materialNames != null && d.materialNames.Count > idx) ? d.materialNames[idx] : string.Empty;
        string p = (d.materialSpritePaths != null && d.materialSpritePaths.Count > idx) ? d.materialSpritePaths[idx] : string.Empty;
        if (name != null) name.text = string.IsNullOrEmpty(n) ? "" : n;
        if (img != null) img.sprite = LoadSprite(p);
    }

    private Sprite LoadSprite(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        return Resources.Load<Sprite>(path);
    }
}


