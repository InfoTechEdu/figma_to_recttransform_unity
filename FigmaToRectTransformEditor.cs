using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;



/* BUGS TO FIX
 * 1. Cannot work with float data
 * 2. Working only with one Scree Resolution (1920x1080)
 * 
 */

public class FigmaToRectTransformEditor : EditorWindow
{
    private static EditorWindow _winInstance;
    private string figmaText = "Parse Figma data here...";
    private GameObject selectedGameObject;
    private bool relativeParent = false;

    int defaultScreenWidth = 1920;
    int defaultScreenHeight = 1080;

    [MenuItem("Figma/RectTransform Parser")]
    public static void ShowWindow()
    {
        _winInstance = GetWindow<FigmaToRectTransformEditor>(false, "Figma To RectTransform Parser");
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical();

        EditorGUILayout.LabelField(string.Format("Default Screen Resolution - {0}x{1}", defaultScreenWidth, defaultScreenHeight));

        relativeParent = EditorGUILayout.Toggle("Derrive relative parent", relativeParent);

        EditorGUILayout.LabelField("Figma data");
        figmaText = EditorGUILayout.TextField(figmaText, GUILayout.MinHeight(100));
        if (GUILayout.Button("Paste from Clipboard"))
        {
            figmaText = GUIUtility.systemCopyBuffer;
        }
        if (GUILayout.Button("Parse and Paste to Selected"))
        {
            FigmaData figmaData = parseFigmaData(figmaText);
            selectedGameObject = Selection.activeGameObject;
            if (selectedGameObject != null)
            {
                RectTransform selectedRectT = selectedGameObject.GetComponent<RectTransform>();
                if (selectedRectT != null)
                    FigmaToRectTransform(figmaData, selectedRectT, relativeParent);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private FigmaData parseFigmaData(string data)
    {
        FigmaData result = new FigmaData();
        Type figmaType = typeof(FigmaData);

        string[] properties = data.Split(new char[] {';'});
        properties = properties.GetAllWithoutLast();

        for (int i = 0; i < properties.Length; i++)
        {
            string p = properties[i];
            p = p.Trim();
            p = p.Replace(" ", "");

            Debug.LogWarning("P value is [" + p + "]");
            string pName = p.GetUntilOrEmpty();
            string pValueStr = p.GetBetweenTwoStrings(":", "px");
            float pValue = float.Parse(pValueStr);

            FieldInfo nextField = figmaType.GetField(pName);
            nextField.SetValue(result, pValue);
        }

        Debug.Log("Figma data was parsed! New FigmaData object - " + result.ToString());

        return result;
    }

    private void FigmaToRectTransform(FigmaData fData, RectTransform rectT, bool relativeParent = false)
    {
        rectT.sizeDelta = new Vector2(fData.width, fData.height);

        int screenWidth;
        int screenHeight;
        if(relativeParent)
        {
            screenWidth = (int)rectT.transform.parent.GetComponent<RectTransform>().sizeDelta.x;
            screenHeight = (int)rectT.transform.parent.GetComponent<RectTransform>().sizeDelta.y;
        }
        else
        {
            screenWidth = defaultScreenWidth;
            screenHeight = defaultScreenHeight;
        }
                  
        Vector2 newPos = new Vector2(
            (-screenWidth/2) + fData.left + (fData.width/2),
            (screenHeight/2) - fData.top - (fData.height/2));
        rectT.localPosition = newPos;

        Debug.LogWarning("offsetMin = " + rectT.offsetMin);
        Debug.LogWarning("offsetMax = " + rectT.offsetMax);
    }

}

public class FigmaData
{
    public float width;
    public float height;
    public float left;
    public float top;

    public override string ToString()
    {
        return string.Format("Figma Data [width = {0}, height = {1}, left = {2}, top = {3}]", width, height, left, top);
    }
}

//Also we can use Split(';')[0] for this task
static class ParseHelper
{
    public static string GetUntilOrEmpty(this string text, string stopAt = ":")
    {
        string result = string.Empty;

        if (!String.IsNullOrWhiteSpace(text))
        {
            int charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);

            if (charLocation > 0)
            {
                result = text.Substring(0, charLocation);
            }
        }

        Debug.LogWarning("Helper class. GetBetweenTwoStrings(). Result - " + result);

        return result;
    }

    public static string GetBetweenTwoStrings(this string text, string from, string to)
    {
        string result = String.Empty;

        if(!String.IsNullOrWhiteSpace(text))
        {
            int fromIndex = text.IndexOf(from) + from.Length;
            int toIndex = text.IndexOf(to);

            result = text.Substring(fromIndex, toIndex - fromIndex);
        }

        Debug.LogWarning("Helper class. GetBetweenTwoStrings(). Result - " + result);
        return result;
    }

    public static string[] GetAllWithoutLast(this string[] text)
    {
        string[] result = new string[text.Length - 1];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = text[i];
        }

        return result;
    }
}


//Not Already Used!
public static class RectTransformExtensions
{
    public static void SetLeftFromCenterAnchorTest(this RectTransform rt, float left)
    {

        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
    }

    public static void ResetAnchors(this RectTransform rt)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
    }

    public static void ResetPivot(this RectTransform rt)
    {
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    public static void SetPivotLeftTop(this RectTransform rt)
    {
        rt.pivot = new Vector2(0, 1);
    }

    public static void SetLeft(this RectTransform rt, float left)
    {
        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
    }

    public static void SetRight(this RectTransform rt, float right)
    {
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }

    public static void SetTop(this RectTransform rt, float top)
    {
        rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
    }

    public static void SetBottom(this RectTransform rt, float bottom)
    {
        rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
    }

    public static void SetAnchorsToLeftTop(this RectTransform rt)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
    }
}
