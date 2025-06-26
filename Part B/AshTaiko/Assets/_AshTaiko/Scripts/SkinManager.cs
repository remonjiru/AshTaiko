using System;
using UnityEngine;

namespace AshTaiko
{
    public class SkinManager : MonoBehaviour
    {
        public static SkinManager Instance;

        [SerializeField]
        private Color _donColor;
        [SerializeField]
        private Color _kaColor;

        public Color GetColor(SkinElement element)
        {
            Color colorToReturn = Color.white;
            switch (element)
            {
                case SkinElement.Don:
                    return _donColor;
                case SkinElement.Ka:
                    return _kaColor;
                default:
                    break;
            }
            return colorToReturn;
        }

        private void Awake()
        {
            Instance = this;
        }
    }

    public enum SkinElement
    {
        Don,
        Ka,
    }
}
