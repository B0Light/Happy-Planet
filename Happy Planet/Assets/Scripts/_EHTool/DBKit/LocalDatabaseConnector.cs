using Google.MiniJSON;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// �׽�Ʈ �� Firestore�� �������� �ʾ��� �� ���Ǵ� DB ������
// Firestore�� �����ϴ��� ĳ�� DB�� ����� �� ������
public class LocalDatabaseConnector<T> : IDatabaseConnector<T> {

    DataTable _data;
    private string _path;

    
    ISet<CallbackMethod<IList<T>>> _allListener;
    IDictionary<CallbackMethod<T>, ISet<int>> _recordListener;

    class DataTable {
        public List<T> value;
    }

    public bool IsDatabaseExist()
    {
        return File.Exists(_path);
    }

    private DataTable _GetDataTable() {


        if (_data == null)
        {
            string json;

            if (IsDatabaseExist())
                json = File.ReadAllText(_path);
            else
            {
                json = "{\"value\":[]}";
            }
            _data = JsonUtility.FromJson<DataTable>(json);
        }

        return _data;
    }

    public void Connect(string databaseName)
    {
#if UNITY_EDITOR
        _path = string.Format("{0}/{1}/{2}.json", Application.dataPath, "/Resources", databaseName);
#else
        _path = string.Format("{0}/{1}".json, Application.persistentDataPath, databaseName);
#endif
        _data = null;

            _allListener = new HashSet<CallbackMethod<IList<T>>>();
        _recordListener = new Dictionary<CallbackMethod<T>, ISet<int>>();
    }

    public void AddRecord(T record)
    {
        DataTable table = _GetDataTable();
        table.value.Add(record);

        string json = JsonUtility.ToJson(table, true);

        File.WriteAllText(_path, json);
    }

    public void UpdateRecordAt(T record, int idx)
    {
        DataTable table = _GetDataTable();
        int removeStartIdx = Mathf.Min(table.value.Count, idx);

        table.value.RemoveRange(removeStartIdx, table.value.Count - removeStartIdx);
        table.value.Add(record);

        string json = JsonUtility.ToJson(table, true);

        File.WriteAllText(_path, json);
    }

    public void GetAllRecord(CallbackMethod<IList<T>> callback)
    {
        _allListener.Add(callback);

        IList<T> data = _GetDataTable().value;

        foreach (CallbackMethod<IList<T>> cb in _allListener) {
            cb(data);
        }

        _allListener = new HashSet<CallbackMethod<IList<T>>>();
    }

    public void GetRecordAt(CallbackMethod<T> callback, int idx)
    {
        if (!_recordListener.ContainsKey(callback))
        {
            _recordListener.Add(callback, new HashSet<int>());
        }

        _recordListener[callback].Add(idx);

        if (_allListener.Count > 0)
        {
            _allListener.Add(Callback);
            return;
        }
        GetAllRecord(Callback);
    }

    public void Callback(IList<T> data)
    {
        foreach (KeyValuePair<CallbackMethod<T>, ISet<int>> callback in _recordListener) {
            foreach (int idx in callback.Value)
            {
                callback.Key(data[idx]);

            }
        }

        _recordListener = new Dictionary<CallbackMethod<T>, ISet<int>>();
    }
}