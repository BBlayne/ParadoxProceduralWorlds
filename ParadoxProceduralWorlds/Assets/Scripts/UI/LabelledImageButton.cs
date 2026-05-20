using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class LabelledImageButton : MonoBehaviour
{
	[SerializeField]UnityEngine.UI.Image m_Image;

	[SerializeField]UnityEngine.UI.Button m_Button;

	private int m_CellIndex = -1;



	public int CellIndex
	{
		get { return m_CellIndex; }
		set { m_CellIndex = value; }
	}

	void Awake()
	{
		if (m_Image == null)
		{
			m_Image = GetComponent<UnityEngine.UI.Image>();
		}
	}

	private void OnEnable()
	{
		if (m_Button !=  null) 
		{
			m_Button.onClick.AddListener(HandleOnButtonClicked);
		}
	}

	private void OnDisable()
	{
		m_CellIndex = -1;

		if (m_Button !=  null) 
		{
			m_Button.onClick.RemoveAllListeners();
		}
	}

	public void SetButtonImage(UnityEngine.UI.Image image)
	{
		m_Image = image;
	}

	public void ToggleHideButton(bool bVisible)
	{
		gameObject.SetActive(bVisible);
	}

	void HandleOnButtonClicked()
	{
		// 
	}
}
