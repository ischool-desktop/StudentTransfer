using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Xml.Linq;
using System.ComponentModel;
using System.Xml;
using System.Collections;

namespace Campus
{
    /// <summary>
    /// 提供動態的 Xml 資料產生機制。
    /// </summary>
    public sealed class XmlObject : DynamicObject
    {
        #region 核心機制

        /// <summary>
        /// 是否允許同樣元素重複出現，例如多個同名的 Element。
        /// </summary>
        private bool UniqueMode { get; set; }

        private MemberName _name;
        private void SetName(string name)
        {
            _name = new MemberName(name);
        }

        /// <summary>
        /// 取得代表這個物件所屬的 Element 或 Attribute 名稱。
        /// </summary>
        /// <returns></returns>
        public string Name()
        {
            return _name.FullName;
        }

        /// <summary>
        /// 轉換為 Duplicate 模式。
        /// </summary>
        private void ToDuplicateMode()
        {
            if (!UniqueMode)
                return;

            UniqueMode = false;
            XmlObject newobj = new XmlObject(Name());
            newobj._string_value = _string_value;
            newobj._children = _children;
            newobj._xml_record_dictionary = _xml_record_dictionary;

            XmlRecordList.Add(newobj);
        }
        #endregion

        #region Overrides
        private XmlObject EnsureOneAndReturnLast()
        {
            if (XmlRecordList.Count <= 0)
                XmlRecordList.Add(new XmlObject(Name()));

            return XmlRecordList[Count() - 1];
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            MemberName mn = new MemberName(binder.Name);

            if (UniqueMode)
            {
                if (XmlRecordDictionary.ContainsKey(mn))
                    result = GetValues(mn);
                else
                    result = GetValue(mn);
            }
            else
                result = (EnsureOneAndReturnLast() as dynamic)[mn.FullName];

            return true;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            MemberName mn = new MemberName(binder.Name);

            if (UniqueMode)
                SetValue(mn, value);
            else
                (EnsureOneAndReturnLast() as dynamic)[mn.FullName] = value;

            return true;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            MemberName mn = new MemberName(indexes[0] + "");

            if (UniqueMode)
            {
                if (XmlRecordDictionary.ContainsKey(mn))
                    result = GetValues(mn);
                else
                    result = GetValue(mn);
            }
            else
                result = (EnsureOneAndReturnLast() as dynamic)[mn.FullName];

            return true;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            MemberName mn = new MemberName(indexes[0] + "");

            if (UniqueMode)
                SetValue(mn, value);
            else
                (EnsureOneAndReturnLast() as dynamic)[mn.FullName] = value;

            return true;
        }

        #endregion

        #region XmlObject

        #region 自動轉型
        public static implicit operator XmlObject(string xmlData)
        {
            XElement xelm = XElement.Parse(xmlData);
            return XmlObject.Parse(xelm);
        }

        public static implicit operator XmlObject(XElement xml)
        {
            if (xml == null)
                throw new ArgumentException("無法將 Null 物件轉型成 XmlObject.");

            return XmlObject.Parse(xml);
        }

        public static implicit operator XmlObject(XmlElement xml)
        {
            if (xml == null)
                throw new ArgumentException("無法將 Null 物件轉型成 XmlObject.");

            return XmlObject.Parse(xml.OuterXml);
        }

        public static implicit operator string(XmlObject obj)
        {
            if (obj == null)
                return string.Empty;
            else
                return obj._string_value;
        }
        #endregion

        #region Parser
        /// <summary>
        /// 產生 XData 物件。
        /// </summary>
        /// <returns></returns>
        public static XmlObject Parse(string xml)
        {
            return Parse(XElement.Parse(xml));
        }

        /// <summary>
        /// 產生 XData 物件。
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static XmlObject Parse(XElement xml)
        {
            dynamic root = new XmlObject(xml.Name.LocalName);

            foreach (XAttribute att in xml.Attributes())
                root["@" + att.Name.LocalName] = att.Value;

            Dictionary<string, int> nameCount = new Dictionary<string, int>();
            foreach (XElement elm in xml.Elements())
            {
                if (nameCount.ContainsKey(elm.Name.LocalName))
                    nameCount[elm.Name.LocalName]++;
                else
                    nameCount[elm.Name.LocalName] = 1;
            }

            Dictionary<string, bool> processed = new Dictionary<string, bool>();
            foreach (XElement elm in xml.Elements())
            {
                string elmName = elm.Name.LocalName;
                if (nameCount[elmName] > 1)
                {
                    if (processed.ContainsKey(elmName)) continue;

                    root[elmName] = XmlObject.ParseList(xml.Elements(elmName));
                }
                else
                {
                    if (elm.HasAttributes || elm.HasElements)
                        root[elmName] = XmlObject.Parse(elm);
                    else
                        root[elmName] = elm.Value;
                }
            }
            root._string_value = xml.Value;

            return root;
        }
        #endregion

        private XmlObject()
        {
            UniqueMode = true;
            _name = MemberName.Empty;
        }

        /// <summary>
        /// 建立。
        /// </summary>
        /// <param name="name">Element 名稱。</param>
        public XmlObject(string name)
            : this()
        {
            _name = new MemberName(name);
        }

        private string _string_value = string.Empty;

        private Dictionary<MemberName, XmlObject> _children = null;
        private Dictionary<MemberName, XmlObject> ChildrenDictionary
        {
            get
            {
                if (_children == null)
                    _children = new Dictionary<MemberName, XmlObject>();
                return _children;
            }
        }

        private Dictionary<MemberName, XmlObject> _xml_record_dictionary = null;
        private Dictionary<MemberName, XmlObject> XmlRecordDictionary
        {
            get
            {
                if (_xml_record_dictionary == null)
                    _xml_record_dictionary = new Dictionary<MemberName, XmlObject>();
                return _xml_record_dictionary;
            }
        }

        #region 輸出相關功能
        public List<string> Children()
        {
            List<string> result = new List<string>();

            foreach (MemberName each in ChildrenDictionary.Keys)
                result.Add(each.FullName);

            foreach (MemberName each in XmlRecordDictionary.Keys)
                result.Add(each.FullName);

            return result;
        }
        /// <summary>
        /// 取得數字資料。
        /// </summary>
        /// <returns></returns>
        public int IntVal()
        {
            return int.Parse(_string_value);
        }

        /// <summary>
        /// 取得數字資料，如果處理失敗則回傳預設值。
        /// </summary>
        /// <param name="defValue"></param>
        /// <returns></returns>
        public int IntVal(int defValue)
        {
            int val;
            if (int.TryParse(_string_value, out val))
                return val;
            else
                return defValue;
        }

        /// <summary>
        /// 取得數字資料，如果處理失敗則回傳預設值。
        /// </summary>
        /// <param name="defValue"></param>
        /// <returns></returns>
        public int IntVal(string defValue)
        {
            return IntVal(int.Parse(defValue));
        }

        /// <summary>
        /// 將資料轉換成 Xml，並格式化輸出。 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            XmlWriterSettings setting = new XmlWriterSettings();
            setting.OmitXmlDeclaration = true;
            setting.Indent = true;
            using (XmlWriter writer = XmlTextWriter.Create(sb, setting))
            {
                if (!UniqueMode)
                {
                    writer.WriteStartElement("_auto_generate_root");
                    foreach (XmlObject each in XmlRecordList)
                        each.Write(writer);
                    writer.WriteEndElement();
                }
                else
                    Write(writer);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 將資料轉換成 XLinq 格式。
        /// </summary>
        /// <returns></returns>
        private void Write(XmlWriter writer)
        {
            if (IsLeaf)
            {
                MemberName name = new MemberName(Name());

                if (name.IsAttribute)
                    writer.WriteAttributeString(name.Name, _string_value);
                else
                    writer.WriteElementString(name.Name, _string_value);
            }
            else
            {
                writer.WriteStartElement(Name() == null ? "_UndefineName" : Name());
                foreach (KeyValuePair<MemberName, XmlObject> each in ChildrenDictionary)
                {
                    if (each.Value.UniqueMode)
                        each.Value.Write(writer);
                    else
                    {
                        foreach (XmlObject record in each.Value.XmlRecordList)
                            record.Write(writer);
                    }
                }
                foreach (KeyValuePair<MemberName, XmlObject> xeach in XmlRecordDictionary)
                {
                    foreach (XmlObject each in xeach.Value.Each())
                        each.Write(writer);
                }
                writer.WriteEndElement();
            }
        }

        private bool IsLeaf
        {
            get { return ChildrenDictionary.Count <= 0 && XmlRecordDictionary.Count <= 0; }
        }
        #endregion

        #region 資料存取(Private)
        private void SetValue(MemberName name, object value)
        {
            if (_name.IsAttribute)
                throw new ArgumentException("這個節點只允許文字資料。");

            if (value == null)
            {
                if (XmlRecordDictionary.ContainsKey(name))
                    XmlRecordDictionary.Remove(name);

                if (ChildrenDictionary.ContainsKey(name))
                    ChildrenDictionary.Remove(name);

                return;
            }

            if (value is XmlObject)
            {
                XmlObject dl = value as XmlObject;
                dl.SetName(name.FullName);
                XmlRecordDictionary[name] = dl;
            }
            else
            {
                XmlObject newValue = null;

                if (value is XmlObject)
                {
                    newValue = value as XmlObject;

                    if (name.IsAttribute && !newValue._name.IsAttribute)
                        throw new ArgumentException("這個節點只允許文字資料。");

                    //防止自行建立的 XData.Name 與 MemberName 不相同。
                    //因為是否為 Xml 屬性的資訊在 Name 之中。
                    newValue._name = name;
                }
                else
                    newValue = new XmlObject(name.FullName) { _string_value = value + "" };

                ChildrenDictionary[name] = newValue;
            }
        }

        private XmlObject GetValue(MemberName name)
        {
            if (_children == null)
                _children = new Dictionary<MemberName, XmlObject>();

            if (!_children.ContainsKey(name))
                SetValue(name, string.Empty);

            return _children[name];
        }

        private XmlObject GetValues(MemberName name)
        {
            if (_xml_record_dictionary == null)
                _xml_record_dictionary = new Dictionary<MemberName, XmlObject>();

            if (!_xml_record_dictionary.ContainsKey(name))
                SetValue(name, new XmlObject());

            return _xml_record_dictionary[name];
        }
        #endregion
        #endregion

        #region XmlObjectList
        private List<XmlObject> XmlRecordList = new List<XmlObject>();

        internal static XmlObject ParseList(IEnumerable<XElement> xmlList)
        {
            XmlObject result = new XmlObject() { UniqueMode = false };
            result.SetName(xmlList.First().Name.LocalName);

            foreach (XElement elm in xmlList)
                result.XmlRecordList.Add(XmlObject.Parse(elm));

            return result;
        }

        #region Public Interface
        public XmlObject New()
        {
            ToDuplicateMode();
            XmlRecordList.Add(new XmlObject(Name()));

            return XmlRecordList[Count() - 1];
        }

        public XmlObject this[int index]
        {
            get
            {
                ToDuplicateMode();
                return XmlRecordList[index];
            }
        }

        public int Count()
        {
            ToDuplicateMode();
            return XmlRecordList.Count;
        }

        public void Remove(int index)
        {
            ToDuplicateMode();
            XmlRecordList.RemoveAt(index);
        }

        public void Clear()
        {
            ToDuplicateMode();
            XmlRecordList.Clear();
        }

        public IEnumerable<XmlObject> Each()
        {
            ToDuplicateMode();
            return XmlRecordList;
        }
        #endregion
        #endregion
    }

    #region MemberName
    internal struct MemberName
    {
        public static MemberName Empty = new MemberName(string.Empty);

        public MemberName(string name)
            : this()
        {
            if (name.StartsWith("@"))
            {
                IsAttribute = true;
                Name = name.Remove(0, 1);
            }
            else
            {
                IsAttribute = false;
                Name = name;
            }
        }

        public bool IsAttribute { get; set; }

        public string Name { get; set; }

        public string FullName
        {
            get
            {
                if (IsAttribute)
                    return "@" + Name;
                else
                    return Name;
            }
        }

        public override string ToString()
        {
            return FullName;
        }
    }
    #endregion
}
