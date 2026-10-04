using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;
//using UnityEngine.Experimental.UIElements;
using UnityEngine.UI;

public class LocalizationData : MonoBehaviour
{
	[FormerlySerializedAs("DATALIST")] public List<string> dataList = new List<string>();

	[FormerlySerializedAs("refList")] public List<string> referenceList = new List<string>();

	[FormerlySerializedAs("PreloaderFonts")] public Font[] preloaderFonts = new Font[9];

	[FormerlySerializedAs("data")] public string[] languageData;

	[FormerlySerializedAs("buttons")] public Button[] languageButtons;


	public const byte ENGLISH_LANGUAGE = 1;

	

	
	public static LocalizationData localizationInstance;

	[FormerlySerializedAs("tempTitleText")] [HideInInspector]
	
	public string temporaryTitleText = "Select your language!";

	[FormerlySerializedAs("tempNoteText")] public string temporaryNoteText ="<size=50>Note:</size> You can change language at any time from the setting screen.";

	[FormerlySerializedAs("OKTextArray")] public string okButtonText ="OK";

	public void Awake()
	{
		localizationInstance = this;
		DontDestroyOnLoad(this);
		
		
	}

	
	public void Start()
	{
		
			loadLocalizationData();
		
	}

	public List<string> ReadMyFile(string fileName)
	{
		List<string> list = new List<string>();
		string empty = string.Empty;
		//Debug.Log("got");
		TextAsset textAsset = Resources.Load<TextAsset>(fileName);
		//Debug.Log(fileName);
		//Debug.Log(textAsset.text);
		if (textAsset != null)
		{
			//Debug.Log("got level ");
			using StreamReader streamReader = new StreamReader(new MemoryStream(textAsset.bytes));
			//Debug.Log("got sr");
			string item;
			while ((item = streamReader.ReadLine()) != null)
			{
				list.Add(item);
			}
			return list;
		}
		return list;
	}

	private void loadLocalizationData()
	{
		PlayerPrefs.SetInt("LastUsedLanguage", 1);
		if (languageData != null)
		{
			languageData = null;
			Resources.UnloadUnusedAssets();
		}
		string text = "Localization_EN";
		string fileName = "Reference";
		//Constants_M.Log(text);
		if (dataList.Count > 0)
		{
			dataList.Clear();
		}
		dataList = ReadMyFile(text);
		if (referenceList.Count > 0)
		{
			referenceList.Clear();
		}
		referenceList = ReadMyFile(fileName);
		referenceList = referenceList.ConvertAll((string d) => d.ToUpper());
		if (dataList != null)
		{
			//Debug.Log("assigned languageIndex:" );
			return;
		}
		ConstantsData_M.Log(2);
		//Debug.Log("file not found:" + text);
	}

	

	public void loadTheSelectedLanguageFromResources()
	{
		loadLocalizationData();
	}

	public string getText(int index)
	{
		string result = string.Empty;
		if (dataList != null && index >= 0 && index < dataList.Count)
		{
			result = dataList[index];
		}
		return result;
	}

	public string removeTheLastWord(string name)
	{
		string[] array = name.Split(" "[0]);
		if (array.Length > 1)
		{
			int num = array[array.Length - 1].Length + 1;
			string result = name.Remove(name.Length - num, num);
			array = null;
			return result;
		}
		return array[0];
	}

	public string removeAllTags(string temp)
	{
		string text = temp;
		while (temp.Contains("<") && temp.Contains(">"))
		{
			int num = temp.IndexOf("<");
			int num2 = temp.IndexOf(">");
			temp = temp.Remove(num, num2 - num + 1);
		}
		return temp;
	}
}
