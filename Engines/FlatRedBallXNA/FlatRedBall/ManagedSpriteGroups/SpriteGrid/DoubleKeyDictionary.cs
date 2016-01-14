using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.ManagedSpriteGroups
{
    public class DoubleKeyDictionary<TValue> 
    {
        private List<int> mKey1s = new List<int>();
        private List<int> mKey2s = new List<int>();

        private Dictionary<int, Dictionary<int, TValue>> _key1Dict;
        private Dictionary<int, Dictionary<int, TValue>> _key2Dict;

        private List<Dictionary<int, TValue>> _unusedDict = new List<Dictionary<int, TValue>>();

        private Dictionary<int, TValue> GetPooledDict()
        {
            if (_unusedDict.Count > 0)
            {
                Dictionary<int, TValue> returnValue = _unusedDict[0];
                _unusedDict.RemoveAt(0);
                return returnValue;
            }
            else
            {
                return new Dictionary<int, TValue>();
            }
        }

        private void ReleaseDictIntoPool(Dictionary<int, TValue> release)
        {
            release.Clear();
            _unusedDict.Add(release);
        }

        public enum Key
        {
            Key1,
            Key2
        }

        public DoubleKeyDictionary()
        {
            _key1Dict = new Dictionary<int, Dictionary<int, TValue>>();
            _key2Dict = new Dictionary<int, Dictionary<int, TValue>>();

            Clear();
        }

        private int _nonDefaultCount;
        private int _key1Count;
        private int _key2Count;

        private int _minKey1;
        private int _maxKey1;
        private int _minKey2;
        private int _maxKey2;

        public int MinKey1 { get { return _minKey1; } }
        public int MaxKey1 { get { return _maxKey1; } }
        public int MinKey2 { get { return _minKey2; } }
        public int MaxKey2 { get { return _maxKey2; } }

        public int NonDefaultCount { get { return _nonDefaultCount; } }
        public int TotalCount { get { return _key1Count * _key2Count; } }
        public int Key1Count { get { return _key1Count; } }
        public int Key2Count { get { return _key2Count; } }

        public List<int> Key1s { get { return mKey1s; } }
        public List<int> Key2s { get { return mKey2s; } }
        public Dictionary<int, Dictionary<int, TValue>>.ValueCollection Key1Values { get { return _key1Dict.Values; } }
        public Dictionary<int, Dictionary<int, TValue>>.ValueCollection Key2Values { get { return _key2Dict.Values; } }

        public TValue this[int key1, int key2] 
        {
            get
            {
                return _key1Dict[key1][key2];
            }
            set
            {
                if (Object.Equals(value, default(TValue)) && !Object.Equals(_key1Dict[key1][key2], default(TValue)))
                {
                    _nonDefaultCount++;
                }
                else if (!Object.Equals(value, default(TValue)) && Object.Equals(_key1Dict[key1][key2], default(TValue)))
                {
                    _nonDefaultCount--;
                }

                _key1Dict[key1][key2] = value;
                _key2Dict[key2][key1] = value;
            }
        }

        public Dictionary<int, TValue>.ValueCollection GetKey1Values(int key1)
        {
            return _key1Dict[key1].Values;
        }

        public Dictionary<int, TValue>.ValueCollection GetKey2Values(int key2)
        {
            return _key2Dict[key2].Values;
        }

        public void AddKey1(int key1)
        {
            if (!_key1Dict.ContainsKey(key1))
            {
                Dictionary<int, TValue> key1Value = GetPooledDict();

                //Fill new Key1 with Key2 values
                foreach(int key2 in mKey2s)
                {
                    key1Value.Add(key2, default(TValue));
                }

                _key1Dict.Add(key1, key1Value);
                mKey1s.Add(key1);

                //Fill Key2 values with new Key1
                foreach (int key2 in mKey2s)
                {
                    _key2Dict[key2].Add(key1, default(TValue));
                }

                //Set Min/Max
                if (key1 <_minKey1)
                    _minKey1 = key1;

                if (key1 > _maxKey1)
                    _maxKey1 = key1;

                _key1Count++;
            }
        }

        public void AddKey2(int key2)
        {
            if (!_key2Dict.ContainsKey(key2))
            {
                Dictionary<int, TValue> key2Value = GetPooledDict();

                //Fill new Key2 with Key1 values
                foreach (int key1 in mKey1s)
                {
                    key2Value.Add(key1, default(TValue));
                }

                _key2Dict.Add(key2, key2Value);
                mKey2s.Add(key2);

                //Fill Key1 values with new Key2
                foreach (int key1 in mKey1s)
                {
                    _key1Dict[key1].Add(key2, default(TValue));
                }

                //Set Min/Max
                if (key2 < _minKey2)
                    _minKey2 = key2;

                if (key2 > _maxKey2)
                    _maxKey2 = key2;

                _key2Count++;
            }
        }

        public void Clear()
        {
            _key1Dict.Clear();
            _key2Dict.Clear();

            _nonDefaultCount = 0;
            _key2Count = 0;
            _key1Count = 0;

            _minKey1 = int.MaxValue;
            _maxKey1 = int.MinValue;

            _minKey2 = int.MaxValue;
            _maxKey2 = int.MinValue;

            mKey1s.Clear();
            mKey2s.Clear();
        }

        public bool ContainsRowKey(int key1)
        {
            return _key1Dict.ContainsKey(key1);
        }

        public bool ContainsColKey(int key2)
        {
            return _key2Dict.ContainsKey(key2);
        }

        public bool ContainsKey(int key1, int key2)
        {
            if(!_key1Dict.ContainsKey(key1))
                return false;

            return _key1Dict[key1].ContainsKey(key2);
        }

        public bool ContainsValue(TValue value)
        {
            foreach (int key1 in mKey1s)
            {
                if (_key1Dict[key1].ContainsValue(value))
                {
                    return true;
                }
            }

            return false;
        }

        public Dictionary<int, Dictionary<int, TValue>>.Enumerator GetRowEnumerator()
        {
            return _key1Dict.GetEnumerator();
        }

        public Dictionary<int, Dictionary<int, TValue>>.Enumerator GetColEnumerator()
        {
            return _key2Dict.GetEnumerator();
        }

        public bool RemoveKey1(int key1)
        {
            if (!_key1Dict.ContainsKey(key1))
            {
                return false;
            }

            ReleaseDictIntoPool(_key1Dict[key1]);
            _key1Dict.Remove(key1);

            foreach (int key2 in mKey2s)
            {
                _key2Dict[key2].Remove(key1);
            }

            _key1Count--;
            mKey1s.Remove(key1);

            if (key1 == _minKey1)
            {
                _minKey1 = int.MaxValue;
                foreach (int checkKey in mKey1s)
                {
                    if (checkKey < _minKey1)
                        _minKey1 = checkKey;
                }
            }

            if (key1 == _maxKey1)
            {
                _maxKey1 = int.MinValue;
                foreach (int checkKey in mKey1s)
                {
                    if (checkKey > _maxKey1)
                        _maxKey1 = checkKey;
                }
            }

            return true;
        }

        public bool RemoveKey2(int key2)
        {
            if (!_key2Dict.ContainsKey(key2))
            {
                return false;
            }

            ReleaseDictIntoPool(_key2Dict[key2]);
            _key2Dict.Remove(key2);

            foreach (int key1 in mKey1s)
            {
                _key1Dict[key1].Remove(key2);
            }

            _key2Count--;
            mKey2s.Remove(key2);

            if (key2 == _minKey2)
            {
                _minKey2 = int.MaxValue;
                foreach (int checkKey in mKey2s)
                {
                    if (checkKey < _minKey2)
                        _minKey2 = checkKey;
                }
            }

            if (key2 == _maxKey2)
            {
                _maxKey2 = int.MinValue;
                foreach (int checkKey in mKey2s)
                {
                    if (checkKey > _maxKey2)
                        _maxKey2 = checkKey;
                }
            }

            return true;
        }

        public bool TryGetValue(int row, int col, out TValue value)
        {
            Dictionary<int, TValue> rowValue;

            if (_key1Dict.TryGetValue(row, out rowValue))
            {
                TValue getValue = default(TValue);

                if (rowValue.TryGetValue(col, out getValue))
                {
                    value = getValue;
                    return true;
                }
                else
                {
                    value = getValue;
                    return false;
                }
            }
            else
            {
                value = default(TValue);
                return false;
            }
        }

        public void PopulateList<T>(IList<T> list) where T : TValue
        {
            if (list == null) return;

            list.Clear();

            foreach(int key1 in mKey1s)
            {
                foreach (int key2 in mKey2s)
                {
                    TValue value = this[key1, key2];

                    if (!Object.Equals(value, default(TValue)))
                        list.Add((T)value);
                }
            }
        }
    }
}
