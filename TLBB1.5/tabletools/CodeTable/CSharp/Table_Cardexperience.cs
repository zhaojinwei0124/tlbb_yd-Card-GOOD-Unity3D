//This code create by CodeEngine mrd.cyou.com ,don't modify
using System;
 using System.Collections.Generic;
 using System.Collections;

namespace GCGame.Table{

[Serializable]
 public class Tab_Cardexperience : ITableOperate
{ private const string TAB_FILE_DATA = "cardexperience.txt";
 public enum _ID
 {
 INVLAID_INDEX=-1,
ID_CARD_EXP,
ID_TOTAL_EXP,
MAX_RECORD
 }
 public string GetInstanceFile(){return TAB_FILE_DATA; }

private int m_CardExp;
 public int CardExp { get{ return m_CardExp;}}

private int m_TotalExp;
 public int TotalExp { get{ return m_TotalExp;}}

public bool LoadTable(Hashtable _tab)
 {
 if(!TableManager.ReaderPList(GetInstanceFile(),SerializableTable,_tab))
 {
 throw TableException.ErrorReader("Load File{0} Fail!!!",GetInstanceFile());
 }
 return true;
 }
 public void SerializableTable(ArrayList valuesList,string skey,Hashtable _hash)
 {
 if (string.IsNullOrEmpty(skey))
 {
 throw TableException.ErrorReader("Read File{0} as key is Empty Fail!!!", GetInstanceFile());
 }

 if ((int)_ID.MAX_RECORD!=valuesList.Count)
 {
 throw TableException.ErrorReader("Load {0} error as CodeSize:{1} not Equal DataSize:{2}", GetInstanceFile(),_ID.MAX_RECORD,valuesList.Count);
 }
 Int32 nKey = Convert.ToInt32(skey);
 Tab_Cardexperience _values = new Tab_Cardexperience();
 _values.m_CardExp =  Convert.ToInt32(valuesList[(int)_ID.ID_CARD_EXP] as string);
_values.m_TotalExp =  Convert.ToInt32(valuesList[(int)_ID.ID_TOTAL_EXP] as string);

 _hash.Add(nKey,_values); }


}
}

