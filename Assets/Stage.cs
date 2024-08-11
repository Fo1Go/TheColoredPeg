using System.Collections.Generic;
using UnityEngine;

public class Stage
{
    public static Dictionary<Color, string> ColorToColorName = new Dictionary<Color, string>()
    {
        {new Color(0.9f, 0.05f, 0f),      "Red"     }, 
        {new Color(0.05f, 0.9f, 0f),      "Green"   }, 
        {new Color(0.05f, 0.05f, 0.9f),   "Blue"    }, 
        {new Color(0.9f, 0.05f, 0.9f),    "Magenta" }, 
        {new Color(0.9f, 0.9f, 0f),       "Yellow"  }, 
        {new Color(0.05f, 0.9f, 0.9f),    "Cyan"    }, 
        {new Color(0.9f, 0.9f, 0.9f),     "White"   }, 
        {new Color(0.05f, 0.05f, 0.05f),  "Black"   }  
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