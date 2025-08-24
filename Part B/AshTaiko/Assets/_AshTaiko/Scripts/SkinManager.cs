using System;
using UnityEngine;

namespace AshTaiko
{
    public class SkinManager : MonoBehaviour
    {
        public static SkinManager Instance { get; private set; }

        [SerializeField]
        private Color _donColor;
        
        [SerializeField]
        private Color _kaColor;

        public Color GetColor(SkinElement element)
        {
            return element switch
            {
                SkinElement.Don => _donColor,
                SkinElement.Ka => _kaColor,
                _ => Color.white
            };
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
