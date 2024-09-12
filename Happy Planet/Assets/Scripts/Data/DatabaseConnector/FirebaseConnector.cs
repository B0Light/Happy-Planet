#if !UNITY_WEBGL || UNITY_EDITOR
using EHTool.DBKit;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using UnityEngine;

public class FirebaseConnector<T> : IDatabaseConnector<T> where T : IDictionaryable<T> {

    DatabaseReference docRef;

    CallbackMethod<IList<T>> _allListener;
    CallbackMethod<string> _fallbackListener;

    bool _databaseExist = false;

    public bool IsDatabaseExist()
    {
        return _databaseExist;
    }

    public void Connect(string authName, string databaseName)
    {
        docRef = FirebaseDatabase.DefaultInstance.RootReference.Child(databaseName).Child(authName);

        _allListener = null;
        _fallbackListener = null;

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
        if (_allListener != null)
        {
            _allListener += callback;
            _fallbackListener += fallback;
            return;
        }

        _allListener = callback;
        _fallbackListener = fallback;

        docRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {

            DataSnapshot snapshot = task.Result;

            _databaseExist = snapshot.Exists;

            if (snapshot.Exists)
            {
                List<T> data = new List<T>();

                int expectIdx = 0;

                foreach (DataSnapshot child in snapshot.Children)
                {
                    if (!int.TryParse(child.Key, out int idx)) continue;
                    if (idx < 0) continue;
                    if (idx != expectIdx++) break;

                    Dictionary<string, object> tmp = child.Value as Dictionary<string, object>;

                    T temp = default;
                    temp.SetValueFromDictionary(tmp);

                    data.Add(temp);

                }

                _allListener?.Invoke(data);
            }
            else
            {
                _fallbackListener?.Invoke(string.Format("Document {0} does not exist!", snapshot.Key.ToString()));

            }

            _allListener = null;
            _fallbackListener = null;

        });
    }

    // GetRecordAll���� ��� ���ڵ� �޾ƿ��� �ű⼭ ���ϴ°� ã�ƿ��� �����
    // ��ȿ������ ��������� �� ���ӿ��� �̰� ����ϴ� �� �ϳ��ۿ� ���(GameManagerData�ε� �̰͵� Firebase �Ⱦ� ����) �ϴ��� �̷��� ��
    public void GetRecordAt(CallbackMethod<T> callback, CallbackMethod<string> fallback, int idx)
    {

        CallbackMethod<IList<T>> thisCallback = (IList<T> data) =>
        {
            if (idx < data.Count) {
                callback?.Invoke(data[idx]);
                return;
            }

            fallback?.Invoke("No Idx");
        };

        GetAllRecord(thisCallback, fallback);

    }

}
#endif