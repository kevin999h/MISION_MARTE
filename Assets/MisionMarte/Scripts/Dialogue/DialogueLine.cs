using UnityEngine;

namespace MisionMarte
{
    /// <summary>Una línea de conversación: quién habla, color del nombre y qué dice.
    /// Puedes usar {HEROE} y {COMPANERA} y se reemplazan por Ligi / Mariale según el personaje elegido.</summary>
    [System.Serializable]
    public struct DialogueLine
    {
        public string speakerName;
        public Color nameColor;
        [TextArea(2, 5)] public string text;

        public DialogueLine(string speaker, Color color, string text)
        {
            speakerName = speaker; nameColor = color; this.text = text;
        }
    }
}
