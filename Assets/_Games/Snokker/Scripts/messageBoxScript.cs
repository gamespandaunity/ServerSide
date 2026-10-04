using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class messageBoxScript : MonoBehaviour
{
	public static string msgText = string.Empty;

	public static string msgConfirmCallback1 = string.Empty;

	public static string msgConfirmCallback2 = string.Empty;

	public static MESSAGE_TYPE messageType = MESSAGE_TYPE.OK;

	public static string[] msgBtnsText = new string[2] { "CANCEL", "YES" };

	private Text messageTextComp;

	private LayoutElement messageTextLayoutElementComp;

	private Text btnTextComp1;

	private Text btnTextComp2;

	private GameObject btnObj1;

	private GameObject btnObj2;

	private GameObject okBtnObj;

	private void Awake()
	{
		messageTextComp = GetComponent<Text>();
		messageTextLayoutElementComp = GetComponent<LayoutElement>();
		btnTextComp1 = GameObject.Find("Canvas/AllParent/MessageBox/BG/Buttons/Btn1/Text").GetComponent<Text>();
		btnTextComp2 = GameObject.Find("Canvas/AllParent/MessageBox/BG/Buttons/Btn2/Text").GetComponent<Text>();
		btnObj1 = GameObject.Find("Canvas/AllParent/MessageBox/BG/Buttons/Btn1");
		btnObj2 = GameObject.Find("Canvas/AllParent/MessageBox/BG/Buttons/Btn2");
		okBtnObj = GameObject.Find("Canvas/AllParent/MessageBox/BG/Buttons/OkBtn");
	}

	private void OnEnable()
	{
		if (messageType == MESSAGE_TYPE.OK)
		{
			btnObj1.SetActive(false);
			btnObj2.SetActive(false);
			okBtnObj.SetActive(true);
		}
		else
		{
			btnObj1.SetActive(true);
			btnObj2.SetActive(true);
			okBtnObj.SetActive(false);
			btnTextComp1.text = msgBtnsText[0];
			btnTextComp2.text = msgBtnsText[1];
		}
		StartCoroutine(resizeTheMessage());
	}

	private IEnumerator resizeTheMessage()
	{
		messageTextComp.text = msgText;
		yield return new WaitForEndOfFrame();
		messageTextLayoutElementComp.preferredHeight = messageTextComp.preferredHeight;
	}
}
