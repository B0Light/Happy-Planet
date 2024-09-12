#if !UNITY_WEBGL || UNITY_EDITOR
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using System.Linq;
using EHTool.DBKit;
using Google.MiniJSON;

public class FirebaseConnector<T> : IDatabaseConnector<T> where T : IDictionaryable<T> {

    DatabaseReference docRef;

    ISet<CallbackMethod<IList<T>>> _allListener;
    ISet<CallbackMethod<string>> _fallbackListener;

    IDictionary<CallbackMethod<T>, ISet<int>> _recordListener;

    bool _databaseExist = false;

    public bool IsDatabaseExist()
    {
        return _databaseExist;
    }

    public void Connect(string authName, string databaseName)
    {
        docRef = FirebaseDatabase.DefaultInstance.RootReference.Child(databaseName).Child(authName);

        _allListener = new HashSet<CallbackMethod<IList<T>>>();
        _fallbackListener = new HashSet<CallbackMethod<string>>();

        _recordListener = new Dictionary<CallbackMethod<T>, ISet<int>>();
    }

    public void AddRecord(T record)
    {

        // ���߿� ���� �ʿ�
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "0" , record }
        };

        docRef.UpdateChildrenAsync(updates).ContinueWithOnMainThread(task => {
            Debug.Log("AddRecord");
        });
    }

    public void UpdateRecordAt(T record, int idx)
    {
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { idx.ToString(), record.ToDictionary() },
        };

        updates.Add((idx + 1).ToString(), null);
        docRef.UpdateChildrenAsync(updates);

        if (!_databaseExist)
        {
            _databaseExist = true;

        }
    }

    public void GetAllRecord(CallbackMethod<IList<T>> callback, CallbackMethod<string> fallback)
    {
        if (_allListener.Count > 0)
        {
            _allListener.Add(callback);
            _fallbackListener.Add(fallback);
            return;
        }

        _allListener.Add(callback);
        _fallbackListener.Add(fallback);

        docRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {

            DataSnapshot snapshot = task.Result;

            _databaseExist = snapshot.Exists;

            if (snapshot.Exists)
            {

                List<T> data = new List<T>();


                int beforeIdx = 0;

                foreach (DataSnapshot child in snapshot.Children)
                {

                    if (int.Parse(child.Key) != beforeIdx++) break;

                    Dictionary<string, object> tmp = child.Value as Dictionary<string, object>;

                    T temp = default;
                    temp.SetValueFromDictionary(tmp);

                    data.Add(temp);

                }

                foreach (CallbackMethod<IList<T>> cb in _allListener)
                {
                    cb?.Invoke(data);
                }
            }
            else
            {

                foreach (CallbackMethod<string> cb in _fallbackListener)
                {
                    cb?.Invoke(string.Format("Document {0} does not exist!", snapshot.Key.ToString()));
                }

            }

            _allListener = new HashSet<CallbackMethod<IList<T>>>();
            _fallbackListener = new HashSet<CallbackMethod<string>>();

        });
    }

    // GetRecordAll���� ��� ���ڵ� �޾ƿ��� �ű⼭ ���ϴ°� ã�ƿ��� �����
    // ��ȿ������ ��������� �� ���ӿ��� �̰� ����ϴ� �� �ϳ��ۿ� ���(GameManagerData�ε� �̰͵� Firebase �Ⱦ� ����) �ϴ��� �̷��� ��
    public void GetRecordAt(CallbackMethod<T> callback, CallbackMethod<string> fallback, int idx)
    {
        if (!_recordListener.ContainsKey(callback))
        {
            _recordListener.Add(callback, new HashSet<int>());
        }

        _recordListener[callback].Add(idx);

        CallbackMethod<IList<T>> thisCallback = (IList<T> data) =>
        {
            if (idx < data.Count) {
                callback?.Invoke(data[idx]);
                return;
            }

            fallback?.Invoke("No Idx");
        };

        if (_allListener.Count > 0)
        {
            _allListener.Add(thisCallback);
            return;
        }

        GetAllRecord(thisCallback, fallback);

    }

}
#endif