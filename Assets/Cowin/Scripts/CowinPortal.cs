using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using JSON.SimpleJSON;
using UnityEngine;
using UnityEngine.UI;

public class CowinPortal : MonoBehaviour
{
    public string url;
    public float interval = 1;
    public InputField inputURL;
    public InputField InputField;
    public GameObject allResponses, uniqueResponses;
    public Text dialogText;
    public Toggle Eighteen, FortyFive, FirstDose, SecondDose;

    private string lastResponse;
    private Coroutine _coroutine;

    private void Start()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = 10;

        OnURLSubmit(PlayerPrefs.GetString("URL", url));
        inputURL.text = url;
        Refresh();
    }

    public void Refresh()
    {
        if (_coroutine != null) StopCoroutine(_coroutine);

        _coroutine = StartCoroutine(Fetch());
    }

    public void OnURLSubmit(string url)
    {
        PlayerPrefs.SetString("URL", url);
        PlayerPrefs.Save();
        this.url = url;
    }

    public void IntervalInput(string interval)
    {
        try
        {
            this.interval = float.Parse(interval);
        }
        catch (Exception e)
        {
            this.interval = 5;
        }
    }

    IEnumerator Fetch()
    {
        while (true)
        {
            ColorBlock colorBlock = ColorBlock.defaultColorBlock;

            string data = GetResponse(url);
            if (AnalyseData(data))
            {
                colorBlock.normalColor = Color.green;
                lastResponse = null;
            }

            InputField result = Instantiate(InputField.gameObject, allResponses.transform).GetComponent<InputField>();
            result.text = DateTime.Now + " : " + data;

            if (lastResponse != data)
            {
                Debug.Log(data);
                result = Instantiate(InputField.gameObject, uniqueResponses.transform).GetComponent<InputField>();
                result.text = DateTime.Now + " : " + data;
                result.colors = colorBlock;
                lastResponse = data;
            }

            yield return new WaitForSeconds(interval);
        }
    }

    public void Clear(Transform content)
    {
        foreach (Transform child in content)
        {
            if (child.GetComponent<Button>()) continue;

            Destroy(child.gameObject);
        }

        lastResponse = null;
    }

    bool AnalyseData(string data)
    {
        HideReport();
        try
        {
            JSONObject json = JSONNode.Parse(data) as JSONObject;

            JSONArray centers = json["centers"].AsArray;

            List<string> availableNames = new List<string>();

            foreach (var center in centers)
            {
                foreach (var session in center.Value["sessions"])
                {
                    int capacity = session.Value["available_capacity"].AsInt;
                    if (capacity > 0)
                    {
                        // Vaccine available
                        bool agePass = (Eighteen.isOn && session.Value["min_age_limit"].AsInt == 18) ||
                                       (FortyFive.isOn && session.Value["min_age_limit"].AsInt == 45);

                        bool dosePass = (FirstDose.isOn && session.Value["available_capacity_dose1"].AsInt > 0) ||
                                        (SecondDose.isOn && session.Value["available_capacity_dose2"].AsInt > 0);

                        if (agePass && dosePass)
                        {
                            availableNames.Add(center.Value["name"]);
                        }
                    }
                }
            }

            if (availableNames.Count > 0)
            {
                ShowReport("Vaccine available at " + string.Join(",\n", availableNames));
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        return false;
    }

    private void ShowReport(string message)
    {
        dialogText.transform.parent.gameObject.SetActive(true);
        dialogText.text = message;
    }

    private void HideReport()
    {
        dialogText.transform.parent.gameObject.SetActive(false);
    }

    string GetResponse(string link)
    {
        try
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(link);

            HttpWebResponse response = (HttpWebResponse) request.GetResponse();
            Stream resStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(resStream);

            string data = reader.ReadToEnd();
            return data;
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }
}