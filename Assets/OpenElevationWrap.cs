using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class OpenElevationWrap
{
    private const string URL = "https://api.open-elevation.com/api/v1/lookup";
    private const string FileName = "elevation_data.json";

    [Serializable]
    private struct Location
    {
        public double latitude;
        public double longitude;

        public Location(double latitude, double longitude)
        {
            this.latitude = latitude;
            this.longitude = longitude;
        }
    }

    [Serializable]
    public struct Elevation
    {
        public double elevation;
        public double latitude;
        public double longitude;
    }

    [Serializable]
    private class Request
    {
        public List<Location> locations;

        public Request(List<Location> locations)
        {
            this.locations = locations;
        }
    }

    [Serializable]
    public class Response
    {
        public List<Elevation> results;
    }

    private List<Location> _locations;

    public OpenElevationWrap()
    {
        _locations = new List<Location>();
    }

    public void AddLocation(double latitude, double longitude)
    {
        _locations.Add(new Location(latitude, longitude));
    }

    public async Task<Response> GetElevationData()
    {
        var filePath = Path.Combine(Application.persistentDataPath, FileName);
        if (!File.Exists(filePath)) return await PostRequest();
        Debug.Log($"Loading cached elevation data from: {filePath}");
        return LoadResponseFromFile();
    }

    public async Task<Response> PostRequest()
    {
        if (_locations.Count == 0)
        {
            Debug.Log("No locations added");
            return null;
        }

        var json = JsonUtility.ToJson(new Request(_locations));

        var uwr = new UnityWebRequest(URL, "POST")
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        uwr.SetRequestHeader("Content-Type", "application/json");

        var operation = uwr.SendWebRequest();
        while (!operation.isDone)
        {
            await Task.Yield();
        }

        if (uwr.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
            return null;
        }

        var responseText = uwr.downloadHandler.text;
        Debug.Log("Received: " + responseText);

        SaveResponseToFile(responseText);

        return JsonUtility.FromJson<Response>(responseText);
    }

    private static void SaveResponseToFile(string jsonData)
    {
        var filePath = Path.Combine(Application.persistentDataPath, FileName);
        try
        {
            File.WriteAllText(filePath, jsonData);
            Debug.Log($"Saved elevation data to: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save file: {e.Message}");
        }
    }

    private static Response LoadResponseFromFile()
    {
        var filePath = Path.Combine(Application.persistentDataPath, FileName);
        try
        {
            var jsonData = File.ReadAllText(filePath);
            return JsonUtility.FromJson<Response>(jsonData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load file: {e.Message}");
            return null;
        }
    }
}
