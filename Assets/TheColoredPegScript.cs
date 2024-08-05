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
	public KMBombInfo Bomb;
	public KMAudio sound;
	public KMSelectable ButtonYes;
	public KMSelectable ButtonNo;

	private static int ModuleIdCounter = 1;
	private int ModuleId;
	private bool ModuleSolved;
	private int CurrentStage;
	private List<List<string>> Answers;

	private List<GameObject> StageLEDs;
	private List<GameObject> ColoredLEDs;
	private List<GameObject> PegFaces;
	private GameObject Peg;
	private Material LedOff;
	private Material LedOn;

	private class Stage
	{
		public Dictionary<Color, string> ColorToColorName = new Dictionary<Color, string>()
		{
			{new Color(255, 0, 0)  , "Red"}, 
			{new Color(0, 255, 0)  , "Green"}, 
			{new Color(0, 0, 255)  , "Blue"}, 
			{new Color(255, 0, 255), "Magenta"}, 
			{new Color(255, 255, 0), "Yellow"}, 
			{new Color(0, 255, 255), "Cyan"}, 
			{new Color(255,255,255), "White"}, 
			{new Color(0,0,0)      , "Black"}  
		};
		public List<Color> LEDsColors;
		public List<Color> PegColors;

		public Stage()
		{
			LEDsColors = new List<Color>();
			PegColors = new List<Color>();
		}

		public void AddLedColor(Color color)
		{
			LEDsColors.Add(color);
		}

		public void AddPegColor(Color color)
		{
			PegColors.Add(color);
		}

		public string LEDS()
		{
			string leds = "";
			foreach (Color color in LEDsColors)
			{
				leds += ColorToColorName[color] + " ";
			}
			return leds;
		}

		public string Faces()
		{
			string faces = "";
			foreach (Color color in PegColors)
			{
				faces += ColorToColorName[color] + " ";
			}
			return faces;
		}
	}

	private List<Stage> Stages;

	private Dictionary<string, Color> PossibleColors = new Dictionary<string, Color>()
	{
		{"R", new Color(255, 0, 0)},      // red
		{"G", new Color(0, 255, 0)},      // green
		{"B", new Color(0, 0, 255)},      // blue
		{"M", new Color(255, 0, 255)},    // magenta
		{"Y", new Color(255, 255, 0)},    // yellow
		{"C", new Color(0, 255, 255)},    // cyan
		{"W", new Color(255,255,255)},    // white
		{"K", new Color(0,0,0)}           // black
	};
	private string PossibleColorsStr = "RGBMYCWK";
	private float PegSpeed = 15f;

	void Awake ()
	{
		ModuleId = ModuleIdCounter++;
		ButtonYes.OnInteract += delegate () { ButtonPress("Yes"); return false; };
		ButtonNo.OnInteract += delegate () { ButtonPress("No"); return false; };
	}

	void Start() {
		ModuleSolved = false;
		CurrentStage = 0;
		Answers = new List<List<string>>();
		Peg = module.transform.Find("Peg").gameObject;
		Stages = new List<Stage>();
		StageLEDs = new List<GameObject>() {
			module.transform.Find("CounterStages").transform.Find("LedStage1").gameObject,
			module.transform.Find("CounterStages").transform.Find("LedStage2").gameObject,
			module.transform.Find("CounterStages").transform.Find("LedStage3").gameObject
		};
		ColoredLEDs = new List<GameObject>() {
			module.transform.Find("Leds").transform.Find("Led1").gameObject,
			module.transform.Find("Leds").transform.Find("Led2").gameObject,
			module.transform.Find("Leds").transform.Find("Led3").gameObject
		};
		PegFaces = new List<GameObject>() {
			module.transform.Find("Peg").transform.Find("Faces").transform.Find("Face1").gameObject,
			module.transform.Find("Peg").transform.Find("Faces").transform.Find("Face2").gameObject,
			module.transform.Find("Peg").transform.Find("Faces").transform.Find("Face3").gameObject,
			module.transform.Find("Peg").transform.Find("Faces").transform.Find("Face4").gameObject,
			module.transform.Find("Peg").transform.Find("Faces").transform.Find("Face5").gameObject
		};
		LedOff = Resources.Load<Material>("Materials/OffLed");
		LedOn = Resources.Load<Material>("Materials/OnLed");
		for (int index = 0; index < StageLEDs.Count; index++)
		{
			Stages.Add(GenerateStage());
		}
		SetStage(Stages[CurrentStage]);
		UpdateStageCounter();
		CurrentStage++;
		
		for (int stageCount = 0; stageCount < Stages.Count; stageCount++)
		{
			Stage stage = Stages[stageCount];
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
		for (int index = 0; index < ColoredLEDs.Count; index++)
		{
			ColoredLEDs[index].GetComponent<Renderer>().material.color = stage.LEDsColors[index];
		}
		for (int index = 0; index < PegFaces.Count; index++)
		{
			PegFaces[index].GetComponent<Renderer>().material.color = stage.PegColors[index];
		}
	}

	private void UpdateStageCounter()
	{
		for (int index = 0; index < StageLEDs.Count; index++)
		{
			if (index < CurrentStage)
			{
				StageLEDs[index].GetComponent<Renderer>().material = LedOn;
			}
			else
			{
				StageLEDs[index].GetComponent<Renderer>().material = LedOff;
			}
		}
	}

	private Stage GenerateStage()
	{
		Stage stage = new Stage();
		for (int index = 0; index < ColoredLEDs.Count; index++)
		{
			stage.AddLedColor(ChooseColor());
		}
		for (int index = 0; index < PegFaces.Count; index++)
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
			Peg.transform.localEulerAngles = new Vector3(Peg.transform.localEulerAngles.x, Peg.transform.localEulerAngles.y, t / PegSpeed * 360);
		} 
	}
	
	private Color ChooseColor()
	{
		return GetColor(PossibleColorsStr[Random.Range(0, PossibleColorsStr.Length)].ToString());
	} 

	private Color GetColor(string color)
	{
		return PossibleColors[color];
	}

	void ButtonPress(string WhatPressed)
	{
		if (ModuleSolved) return;
		if (WhatPressed.Equals("Yes"))
		{
			ButtonYes.AddInteractionPunch(0.25f);
		}
		else
		{
			ButtonNo.AddInteractionPunch(0.25f);
		}
		
		UpdateStageCounter();
		if (CurrentStage == Stages.Count)
		{
			Solve();
			return;
		}
		SetStage(Stages[CurrentStage]);
		CurrentStage++;
	}

	private void CalculateAnswer()
	{
		List<bool> Conditions = new List<bool>()
		{
			new HashSet<Color>(Stages[0].PegColors).Count == 3, // 0
			Stages[0].LEDsColors.Count(x => Stages[0].PegColors.Contains(x)) == 3, // 1
			Stages[0].PegColors.Count(x => x == PossibleColors["R"] ||  // 2
			                               x == PossibleColors["Y"] || 
			                               x == PossibleColors["M"] || 
			                               x == PossibleColors["W"]) > 2,
			Bomb.IsIndicatorOn("FRQ"), // 3
			new HashSet<Color>(Stages[0].PegColors).Count == 5, // 4 
			Stages[0].LEDsColors.Count(x => x == PossibleColors["B"]) 
				+ Stages[0].PegColors.Count(x => x == PossibleColors["B"]) == 2, // 5
			Bomb.GetSolvableModuleNames().Contains("Colour flash"), //  6
			Bomb.GetBatteryCount() <= 4, // 7
			Bomb.GetPorts().Contains("DVI"), // 8
			Int64.Parse(Bomb.GetSerialNumber().Substring(Bomb.GetSerialNumber().Length-1)) % 2 == 0, // 9
			Stages[0].PegColors.Contains(PossibleColors["K"]) && Stages[0].PegColors.Contains(PossibleColors["W"]), //10
		};
		string yes = "yes";
		string no = "no";
		string answer;
		if (Conditions.Count(x => x) > 4)
		{
			answer = yes;
		}
		else
		{
			answer = no;
		}
		Answers.Add(new List<string>(){answer, "any"});

		Log(String.Format("Total fulfilled conditions is {0}. Correct button for stage 1 is: {1}", 
			Conditions.Count(x => x), Answers[0][0]));
		for (int resultID = 0; resultID < Conditions.Count; resultID++)
		{
			if (Conditions[resultID])
			{
				Log(String.Format("Condition {0} is true", resultID + 1));
			}
		}
		
		
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
		ModuleSolved = true;
		module.HandlePass();
	}
	
	private void Log(string LogString)
	{
		Debug.Log(String.Format("[TheColoredPeg #{0}] {1}", ModuleId, LogString));
	}

	void Update () {
		
	}
}
