using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapEditor : MonoBehaviour
{
    private Territory currentTerritoryInflated;
    private Territory mainSelectedTerritory;
    private Camera m_Camera;

    private Vector3 mousePosLastFrame;

    private bool territorySelected = false;
    private float editingUIIntroTime;
    private float editingUIOutroTime;
    private bool selectingExtraNeighbour = false;
    private bool setupNeeded = false;

    [Header("Editor References")]
    public RectTransform editingUI;
    public AnimationCurve uiOpenCurve;
    public AnimationCurve uiCloseCurve;

    public TMP_InputField nameInput;
    public Image territoryImage;

    public TextMeshProUGUI neighboursList;
    public TextMeshProUGUI extraNeighboursList;

    private static Dictionary<int, string> territoryNamesList = null;
    private static Dictionary<int, List<int>> territoryExtraNeighbours = null;


    private void OnEnable()
    {
        m_Camera = Camera.main;
        territoryNamesList = LoadTerritoryNamesList();
        territoryExtraNeighbours = LoadExtraNeighboursList();

        territorySelected = false;
        selectingExtraNeighbour = false;
        setupNeeded = false;
    }

    private void Update()
    {
        if (territorySelected && !selectingExtraNeighbour)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseEditingUI();
                return;
            }

            if (editingUIIntroTime > 0.0f)
            {
                if (editingUIIntroTime == 1.0f && setupNeeded)
                {
                    setupNeeded = false;

                    //Setup
                    //Territory name
                    if (territoryNamesList.ContainsKey(mainSelectedTerritory.GetIndexInMap()))
                    {
                        nameInput.text = territoryNamesList[mainSelectedTerritory.GetIndexInMap()];
                    }
                    else
                    {
                        nameInput.text = mainSelectedTerritory.GetName();
                    }
                    //Neighbours list
                    //Normal neighbours list
                    //This never changes so we don't need to load anything from file
                    string neighbours = "Neighbours:\n";

                    foreach (Territory neighbour in mainSelectedTerritory.GetNeighbours())
                    {
                        neighbours += neighbour.GetName() + "\n";
                    }

                    neighboursList.text = neighbours;

                    //Extra neighbours list
                    if (!territoryExtraNeighbours.ContainsKey(mainSelectedTerritory.GetIndexInMap()))
                    {
                        territoryExtraNeighbours.Add(mainSelectedTerritory.GetIndexInMap(), new List<int>());
                    }

                    UpdateExtraNeighboursText();

                    territoryImage.sprite = mainSelectedTerritory.getCardSprite();
                }

                editingUIIntroTime -= Time.deltaTime * 2.0f;

                editingUI.anchoredPosition = Vector3.Lerp(Vector3.zero, Vector3.down * 3600, uiOpenCurve.Evaluate(editingUIIntroTime));
            }

            return;
        }

        if (editingUIOutroTime > 0.0f)
        {
            editingUIOutroTime -= Time.deltaTime * 2.0f;

            editingUI.anchoredPosition = Vector3.Lerp(Vector3.down * 3600, Vector3.zero, uiCloseCurve.Evaluate(editingUIOutroTime));
        }

        //Get map under territory
        Vector3 mousePosThisFrame = m_Camera.ScreenToWorldPoint(Input.mousePosition);
        mousePosThisFrame.z = 0;

        if (mousePosThisFrame != mousePosLastFrame)
        {
            Territory territory = Map.GetTerritoryUnderPosition(mousePosThisFrame);

            if (territory != currentTerritoryInflated)
            {
                //Defalte previous territory
                if (currentTerritoryInflated != null)
                {
                    currentTerritoryInflated.Deflate();
                }

                //Inflate current one
                if (territory != null)
                {
                    territory.Inflate();
                }

                currentTerritoryInflated = territory;
            }

            mousePosLastFrame = mousePosThisFrame;
        }

        if (currentTerritoryInflated != null && Input.GetMouseButtonDown(0))
        {
            if (selectingExtraNeighbour)
            {
                if (mainSelectedTerritory.GetNeighbours().Contains(currentTerritoryInflated) || mainSelectedTerritory.Equals(currentTerritoryInflated))
                {
                    AudioManagement.PlaySound("Refuse");
                    return;
                }

                //Add extra neighbour to list
                territoryExtraNeighbours[mainSelectedTerritory.GetIndexInMap()].Add(currentTerritoryInflated.GetIndexInMap());

                //Add to extra neighbour of new extra neighbour as well
                if (!territoryExtraNeighbours.ContainsKey(currentTerritoryInflated.GetIndexInMap()))
                {
                    territoryExtraNeighbours.Add(currentTerritoryInflated.GetIndexInMap(), new List<int>());
                }

                territoryExtraNeighbours[currentTerritoryInflated.GetIndexInMap()].Add(mainSelectedTerritory.GetIndexInMap());

                //update ui
                UpdateExtraNeighboursText();
            }
            else
            {
                territorySelected = true;
                mainSelectedTerritory = currentTerritoryInflated;
                mainSelectedTerritory.Deflate();

                currentTerritoryInflated = null;
                setupNeeded = true;
            }

            selectingExtraNeighbour = false;
            editingUIIntroTime = 1.0f;
            AudioManagement.PlaySound("ButtonPress");
        }
    }

    private void UpdateExtraNeighboursText()
    {
        string extraNeighboursString = "Extra Neighbours:\n";

        foreach (int neighbour in territoryExtraNeighbours[mainSelectedTerritory.GetIndexInMap()])
        {
            extraNeighboursString += Map.GetTerritory(neighbour).GetName() + "\n";
        }

        extraNeighboursList.text = extraNeighboursString;
    }

    public void CloseEditingUI()
    {
        territorySelected = false;
        selectingExtraNeighbour = false;
        editingUIOutroTime = 1.0f;
    }

    public void SaveButton()
    {
        if (territorySelected)
        {
            AudioManagement.PlaySound("ButtonPress");
            //Save territory to file
            if (!territoryNamesList.ContainsKey(mainSelectedTerritory.GetIndexInMap()))
            {
                territoryNamesList.Add(mainSelectedTerritory.GetIndexInMap(), "");
            }

            territoryNamesList[mainSelectedTerritory.GetIndexInMap()] = nameInput.text;

            SaveTerritoryNamesList();

            if (!territoryExtraNeighbours.ContainsKey(mainSelectedTerritory.GetIndexInMap()))
            {
                territoryExtraNeighbours.Add(mainSelectedTerritory.GetIndexInMap(), new List<int>());
            }

            SaveTerritoryExtraNeighbours();
        }
    }

    public void SelectExtraNeighbour()
    {
        AudioManagement.PlaySound("ButtonPress");
        selectingExtraNeighbour = true;
        editingUIOutroTime = 1.0f;
    }

    public void ResetExtraNeighbours()
    {
        AudioManagement.PlaySound("ButtonPress");
        //First remove connection from all other territories
        foreach (int neighbour in territoryExtraNeighbours[mainSelectedTerritory.GetIndexInMap()])
        {
            if (territoryExtraNeighbours.ContainsKey(neighbour))
            {
                territoryExtraNeighbours[neighbour].Remove(mainSelectedTerritory.GetIndexInMap());
            }
        }

        territoryExtraNeighbours[mainSelectedTerritory.GetIndexInMap()] = new List<int>();
        UpdateExtraNeighboursText();
    }

    private static string GetDataPathNames()
    {
        return Application.dataPath + "/territoryNames.json";
    }

    public static Dictionary<int, string> LoadTerritoryNamesList()
    {
        Dictionary<int, string> values = new Dictionary<int, string>();

        if (!File.Exists(GetDataPathNames()))
        {
            return values;
        }

        Debug.Log(GetDataPathNames());
        string data = File.ReadAllText(GetDataPathNames());

        SerializableDict<int, string> fileOutput = JsonUtility.FromJson<SerializableDict<int, string>>(data);

        //Convert file output into usable names list
        for (int i = 0; i < fileOutput.keyList.Count; i++)
        {
            values.Add(fileOutput.keyList[i], fileOutput.valueList[i]);
        }

        return values;
    }

    public static void SaveTerritoryNamesList()
    {
        Debug.Log(GetDataPathNames());

        //Convert territory name dict to list
        SerializableDict<int, string> dataToSave = new SerializableDict<int, string>();
        foreach (int key in territoryNamesList.Keys)
        {
            dataToSave.keyList.Add(key);
            dataToSave.valueList.Add(territoryNamesList[key]);
        }

        string data = JsonUtility.ToJson(dataToSave, true);
        File.WriteAllText(GetDataPathNames(), data);
    }

    private static string GetDataPathNeighbours()
    {
        return Application.dataPath + "/territoryNeighbours.json";
    }

    public static Dictionary<int, List<int>> LoadExtraNeighboursList()
    {
        Dictionary<int, List<int>> values = new Dictionary<int, List<int>>();

        if (!File.Exists(GetDataPathNeighbours()))
        {
            return values;
        }

        Debug.Log(GetDataPathNeighbours());
        string data = File.ReadAllText(GetDataPathNeighbours());

        SerializableDict<int, string> fileOutput = JsonUtility.FromJson<SerializableDict<int, string>>(data);

        for (int i = 0; i < fileOutput.keyList.Count; i++)
        {
            string[] outputValue = fileOutput.valueList[i].Split('-');
            List<int> extraNeighbours = new List<int>();

            if (!string.IsNullOrEmpty(outputValue[0]))
            {
                foreach (string value in outputValue)
                {
                    extraNeighbours.Add(int.Parse(value));
                }
            }

            values.Add(fileOutput.keyList[i], extraNeighbours);
        }

        return values;
    }

    public static void SaveTerritoryExtraNeighbours()
    {
        Debug.Log(GetDataPathNeighbours());

        //Convert extra neighbours dict to list
        SerializableDict<int, string> dataToSave = new SerializableDict<int, string>();
        foreach (int key in territoryExtraNeighbours.Keys)
        {
            dataToSave.keyList.Add(key);

            List<int> values = territoryExtraNeighbours[key];
            string output = "";

            if (values.Count > 0)
            {
                foreach (int value in values)
                {
                    output += value.ToString() + "-";
                }

                output = output.Remove(output.Length - 1);
            }

            dataToSave.valueList.Add(output);
        }

        string data = JsonUtility.ToJson(dataToSave, true);
        File.WriteAllText(GetDataPathNeighbours(), data);
    }

    [System.Serializable]
    private class SerializableDict<T, V>
    {
        public List<T> keyList = new List<T>();
        public List<V> valueList = new List<V>();
    }
}
