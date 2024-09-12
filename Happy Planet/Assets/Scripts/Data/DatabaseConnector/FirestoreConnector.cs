#if !UNITY_WEBGL || UNITY_EDITOR
using EHTool.DBKit;
using Firebase.Extensions;
using Firebase.Firestore;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FirestoreConnector<T> : IDatabaseConnector<T> where T : IDictionaryable<T> {

    DocumentReference docRef;

    CallbackMethod<IList<T>> _allListener;
    CallbackMethod<string> _fallbackListener;

    bool _databaseExist = false;

    public bool IsDatabaseExist()
    {
        return _databaseExist;
    }

    public void Connect(string authName, string databaseName)
    {
        docRef = FirebaseFirestore.DefaultInstance.Collection(databaseName).Document(authName);

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

        docRef.UpdateAsync(updates).ContinueWithOnMainThread(task => {
            Debug.Log("AddRecord");
        });
    }

    public void UpdateRecordAt(T record, int idx)
    {
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { idx.ToString(), record.ToDictionary() },
        };

        if (!_databaseExist)
        {
            docRef.SetAsync(updates);
            _databaseExist = true;
            return;

        }

        updates.Add((idx + 1).ToString(), FieldValue.Delete);
        docRef.UpdateAsync(updates);
    }

    public void GetAllRecord(CallbackMethod<IList<T>> callback, CallbackMethod<string> fallback)
    {
        if (_allListener != null)
        {
            _allListener += callback;
            _fallbackListener += fallback;
            return;
        }

        _allListener += callback;
        _fallbackListener += fallback;

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            DocumentSnapshot snapshot = task.Result;

            _databaseExist = snapshot.Exists;

            if (snapshot.Exists)
            {
                List<T> data = new List<T>();

                int expectIdx = 0;

                foreach (KeyValuePair<string, object> child in snapshot.ToDictionary().OrderBy(x => int.Parse(x.Key)))
                {
                    if (!int.TryParse(child.Key, out int idx)) continue;
                    if (idx < 0) continue;
                    if (idx != expectIdx++) break;

                    T temp = default;
                    temp.SetValueFromDictionary(child.Value as Dictionary<string, object>);

                    data.Add(temp);

                }

                _allListener?.Invoke(data);
            }
            else
            {
                _fallbackListener?.Invoke(string.Format("Document {0} does not exist!", snapshot.Id));

                Debug.Log(string.Format("Document {0} does not exist!", snapshot.Id));
            }

            _allListener = null;
            _fallbackListener = null;
        });
    }

    // GetRecordAll���� ��� ���ڵ� �޾ƿ��� �ű⼭ ���ϴ°� ã�ƿ��� �����
    // ��ȿ������ ��������� �� ���ӿ��� �̰� ����ϴ� �� �ϳ��ۿ� ���(GameManagerData�ε� �̰͵� Firestore �Ⱦ� ����) �ϴ��� �̷��� ��
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