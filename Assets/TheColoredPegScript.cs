using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using UnityEngine;
using Random = UnityEngine.Random;

public class TheColoredPegScript : MonoBehaviour
{
	public KMBombModule module;
	public KMBombInfo bomb;
	public KMAudio sound;
	public KMSelectable buttonYes;
	public KMSelectable buttonNo;

	private int _moduleID;
	private static int _moduleIDCounter = 1;
	private bool _isModuleSolved;
	private int _currentStage;
	private List<Stage> _stages;
	private List<List<string>> _answers;
	private List<GameObject> _stageLEDs;
	private List<GameObject> _coloredLEDs;
	private List<GameObject> _pegFaces;
	private GameObject _peg;
	private Material _ledOff;
	private Material _ledOn;

	private readonly Dictionary<string, Color> _possibleColors = new Dictionary<string, Color>()
	{
		{"R", new Color(0.9f, 0.05f, 0f)     },    // red
		{"G", new Color(0.05f, 0.9f, 0f)     },    // green
		{"B", new Color(0.05f, 0.05f, 0.9f)  },    // blue
		{"M", new Color(0.9f, 0.05f, 0.9f)   },    // magenta
		{"Y", new Color(0.9f, 0.9f, 0f)      },    // yellow
		{"C", new Color(0.05f, 0.9f, 0.9f)   },    // cyan
		{"W", new Color(0.9f, 0.9f, 0.9f)    },    // white
		{"K", new Color(0.05f, 0.05f, 0.05f) }     // black
	};
	private const string PossibleColorsStr = "RGBMYCWK";
	private const float PegSpeed = 9f;

	public void Awake ()
	{
		_moduleID = _moduleIDCounter++;
		buttonYes.OnInteract += delegate () { ButtonPress("Yes"); return false; };
		buttonNo.OnInteract += delegate () { ButtonPress("No"); return false; };
	}

	public void Start() {
		_isModuleSolved = false;
		_currentStage = 0;
		_answers = new List<List<string>>();
		_peg = module.transform.Find("Peg").gameObject;
		_stages = new List<Stage>();
		_stageLEDs = new List<GameObject>() {
			module.transform.Find("CounterStages").transform.Find("LedStage1").gameObject,
			module.transform.Find("CounterStages").transform.Find("LedStage2").gameObject,
			module.transform.Find("CounterStages").transform.Find("LedStage3").gameObject
		};
		_coloredLEDs = new List<GameObject>() {
			module.transform.Find("Leds").transform.Find("Led1").gameObject,
			module.transform.Find("Leds").transform.Find("Led2").gameObject,
			module.transform.Find("Leds").transform.Find("Led3").gameObject
		};
		_pegFaces = new List<GameObject>() {
			module.transform.Find("Peg").transform.Find("Faces").transform.Find("Face1").gameObject,
			module.transform.Find("Peg").transform.Find("Faces").transform.Find("Face2").gameObject,
			module.transform.Find("Peg").transform.Find("Faces").transform.Find("Face3").gameObject,
			module.transform.Find("Peg").transform.Find("Faces").transform.Find("Face4").gameObject,
			module.transform.Find("Peg").transform.Find("Faces").transform.Find("Face5").gameObject
		};
		_ledOff = Resources.Load<Material>("Materials/OffLed");
		_ledOn = Resources.Load<Material>("Materials/OnLed");
		for (int index = 0; index < _stageLEDs.Count; index++)
		{
			_stages.Add(GenerateStage());
		}
		SetStage(_stages[_currentStage]);
		UpdateStageCounter();
		_currentStage++;
		
		for (int stageCount = 0; stageCount < _stages.Count; stageCount++)
		{
			Stage stage = _stages[stageCount];
			Log(String.Format("For stage #{0}\nGenerated LEDs: {1}\nGenerated faces: {2}", 
				stageCount+1, 
				stage.LEDS(), 
				stage.Faces()));
		}

		CalculateAnswer();
		StartCoroutine(RotatePeg());
	}

	private void SetStage(Stage stage)
	{
		for (int index = 0; index < _coloredLEDs.Count; index++)
		{
			_coloredLEDs[index].GetComponent<Renderer>().material.color = stage.LEDsColors[index];
		}
		for (int index = 0; index < _pegFaces.Count; index++)
		{
			_pegFaces[index].GetComponent<Renderer>().material.color = stage.PegColors[index];
		}
	}

	private void UpdateStageCounter()
	{
		for (var index = 0; index < _stageLEDs.Count; index++)
		{
			if (index < _currentStage)
				_stageLEDs[index].GetComponent<Renderer>().material = _ledOn;
			else
				_stageLEDs[index].GetComponent<Renderer>().material = _ledOff;
		}
	}

	private Stage GenerateStage()
	{
		Stage stage = new Stage();
		for (int index = 0; index < _coloredLEDs.Count; index++)
		{
			stage.AddLedColor(ChooseColor());
		}
		for (int index = 0; index < _pegFaces.Count; index++)
		{
			stage.AddPegColor(ChooseColor());
		}
		return stage;
	}

	private IEnumerator RotatePeg()
	{
		float t = 0;
		while (true)
		{
			yield return null;
			t += Time.deltaTime;
			_peg.transform.localEulerAngles = new Vector3(_peg.transform.localEulerAngles.x, 
				_peg.transform.localEulerAngles.y, 
				t / PegSpeed * 360);
		} 
	}
	
	private Color ChooseColor()
	{
		return GetColor(PossibleColorsStr[Random.Range(0, PossibleColorsStr.Length)].ToString());
	} 

	private Color GetColor(string color)
	{
		return _possibleColors[color];
	}

	private void ButtonPress(string WhatPressed)
	{
		if (_isModuleSolved) return;
		if (WhatPressed.Equals("Yes"))
		{
			buttonYes.AddInteractionPunch(0.25f);
		}
		else
		{
			buttonNo.AddInteractionPunch(0.25f);
		}
		
		UpdateStageCounter();
		if (_currentStage == _stages.Count)
		{
			Solve();
			return;
		}
		SetStage(_stages[_currentStage]);
		_currentStage++;
	}

	private void CalculateAnswer()
	{
		List<bool> conditions = new List<bool>()
		{
			new HashSet<Color>(_stages[0].PegColors).Count == 3, // 0
			_stages[0].LEDsColors.Count(x => _stages[0].PegColors.Contains(x)) == 3, // 1
			_stages[0].PegColors.Count(x => x == _possibleColors["R"] ||  // 2
			                               x == _possibleColors["Y"] || 
			                               x == _possibleColors["M"] || 
			                               x == _possibleColors["W"]) > 2,
			bomb.IsIndicatorOn("FRQ"), // 3
			new HashSet<Color>(_stages[0].PegColors).Count == 5, // 4 
			_stages[0].LEDsColors.Count(x => x == _possibleColors["B"]) 
				+ _stages[0].PegColors.Count(x => x == _possibleColors["B"]) == 2, // 5
			bomb.GetSolvableModuleNames().Contains("Colour flash"), //  6
			bomb.GetBatteryCount() <= 4, // 7
			bomb.GetPorts().Contains("DVI"), // 8
			Int64.Parse(bomb.GetSerialNumber().Substring(bomb.GetSerialNumber().Length-1)) % 2 == 0, // 9
			_stages[0].PegColors.Contains(_possibleColors["K"]) && _stages[0].PegColors.Contains(_possibleColors["W"]), //10
		};
		
		string yes = "yes";
		string no = "no";
		string answer;
		if (conditions.Count(x => x) > 4)
		{
			answer = yes;
		}
		else
		{
			answer = no;
		}
		_answers.Add(new List<string>(){answer, "any"});
		Log(String.Format("Total fulfilled conditions is {0}. Correct button for stage 1 is: {1}", 
			conditions.Count(x => x), _answers[0][0]));
		string stringLog = "";
		for (int resultID = 0; resultID < conditions.Count; resultID++)
		{
			if (conditions[resultID])
			{
				stringLog += String.Format("Condition {0} is true\n", resultID + 1);
			}
		}

		Log(stringLog);

		// Красный канал это строка, зелёный это столбец, а синий множетель. 
		// 	Найдите число, пересечение в таблице, умножьте на множитель, и отсчитайте с начала таблицы это число. 
		// 	Если дошли до конца, продолжите с начала. В таблице левая верхняя координата это (0, 0). 
		// 	Если это число больше 6, нажмите на "YES", иначе на "No".
		// 	Нажмите на кнопку, когда последняя цифра таймера равняется появления красного канала на двух этапах, по модулю 9.

		// 5	1	4	2	9	7	8	3	6
		// 3	4	2	9	1	9	7	7	5
		// 2	7	1	7	5	6	3	1	7
		// 8	6	8	8	3	1	1	6	9
		// 7	2	9	5	4	3	4	5	1
		// 4	3	7	3	8	4	5	2	2
		// 6	8	6	4	7	5	2	9	3
		// 1	9	3	6	2	8	6	8	4
		// 9	5	5	1	6	2	9	4	5



		// На третьем этапе посчитайте два значения по формулам.
		// 	Значение 1: "(A [1] (B [2] С)) [4] C" 
		// Значение 2: "B [3] (C [4] (A [5] C))"
		// Где [n] это операция соответствующая n-ому цвету на колышке в порядке появления на модуле. A, B, C это значение цветов светодиодов.
		//
		// 	Цвет	Операция	Значение
		// 	Красный	Лог. ИЛИ	10110
		// Зелёный	Лог. И	11111
		// Синий	Искл. ИЛИ	00010
		// Мажента	Импликация	10001
		// Жёлтый	НЕ И	10111
		// Голубой	НЕ ИЛИ	11010
		// Белый	Искл. НЕ И	11100
		// Черный	Лог. И	01101
		// Если второе число получилось больше первого, то нажмите "NO", иначе нажмите "YES".
		// Нажмите кнопку когда меньшее из двух чисел является последней цифрой серийного номера
	}

	private void Incorrect()
	{
		module.HandleStrike();
	}

	private void Solve()
	{
		_isModuleSolved = true;
		module.HandlePass();
	}
	
	private void Log(string logString)
	{
		Debug.Log(String.Format("[TheColoredPeg #{0}] {1}", _moduleID, logString));
	}
}
