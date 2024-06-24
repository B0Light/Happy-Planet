using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;
using System;
using System.Linq;
using System.Runtime.InteropServices;

static class FirestoreWebGLBridge {

    [DllImport("__Internal")]
    public static extern void WebGLOnInit(string path, string firebaseConfigValue);

    [DllImport("__Internal")]
    public static extern void WebGLAddRecord(string path, string recordJson);

    [DllImport("__Internal")]
    public static extern void WebGLUpdateRecordAt(string path, string recordJson, int idx);
    [DllImport("__Internal")]
    public static extern void WebGLGetAllRecord(string path, string objectName, string callback, string fallback);
}

public class FirestoreWebGLConnector<T> : MonoBehaviour, IDatabaseConnector<T> {

    DocumentReference docRef;

    ISet<CallbackMethod<IList<T>>> _allListener;
    IDictionary<CallbackMethod<T>, ISet<int>> _recordListener;

    public bool IsDatabaseExist()
    {
        return false;
    }

    public void Connect(string databaseName)
    {
        _allListener = new HashSet<CallbackMethod<IList<T>>>();
        _recordListener = new Dictionary<CallbackMethod<T>, ISet<int>>();

        FirestoreWebGLBridge.WebGLOnInit("path", "path");
    }

    public void AddRecord(T record)
    {
        FirestoreWebGLBridge.WebGLAddRecord("test", JsonUtility.ToJson(record));
    }

    public void UpdateRecordAt(T record, int idx)
    {
        FirestoreWebGLBridge.WebGLUpdateRecordAt("test", JsonUtility.ToJson(record), idx);
    }

    public void GetAllRecord(CallbackMethod<IList<T>> callback)
    {
        if (_allListener.Count > 0)
        {
            _allListener.Add(callback);
            return;
        }

        _allListener.Add(callback);

        FirestoreWebGLBridge.WebGLGetAllRecord("test", gameObject.name, "Callback", "Fallback");
    }

    public void Callback(string json) { 
        
    }

    public void Fallback(string json) { 
        
    }

    // GetRecordAll���� ��� ���ڵ� �޾ƿ��� �ű⼭ ���ϴ°� ã�ƿ��� �����
    // ��ȿ������ ��������� �� ���ӿ��� �̰� ����ϴ� �� �ϳ��ۿ� ���(GameManagerData�ε� �̰͵� Firestore �Ⱦ� ����) �ϴ��� �̷��� ��
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
        foreach (KeyValuePair<CallbackMethod<T>, ISet<int>> callback in _recordListener)
        {
            foreach (int idx in callback.Value)
            {
                callback.Key(data[idx]);

            }
        }

        _recordListener = new Dictionary<CallbackMethod<T>, ISet<int>>();
    }
}