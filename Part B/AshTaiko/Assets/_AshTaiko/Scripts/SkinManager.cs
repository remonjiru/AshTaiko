using System;
using UnityEngine;

namespace AshTaiko
{
    /// <summary>
    /// Manages visual skinning and color schemes for game elements.
    /// Provides centralized access to colors and visual properties for
    /// different note types and UI elements.
    /// </summary>
    public class SkinManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance providing global access to skin settings.
        /// </summary>
        public static SkinManager Instance { get; private set; }

        [SerializeField]
        private Color _donColor;
        
        [SerializeField]
        private Color _kaColor;

        /// <summary>
        /// Gets the color for a specific skin element.
        /// </summary>
        /// <param name="element">The skin element to get the color for.</param>
        /// <returns>The color for the specified element, or white if not found.</returns>
        public Color GetColor(SkinElement element)
        {
            return element switch
            {
                SkinElement.Don => _donColor,
                SkinElement.Ka => _kaColor,
                _ => Color.white
            };
        }

        /// <summary>
        /// Sets up the singleton instance.
        /// </summary>
        private void Awake()
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Represents different visual elements that can be skinned.
    /// </summary>
    public enum SkinElement
    {
        /// <summary>
        /// Don note visual element.
        /// </summary>
        Don,
        
        /// <summary>
        /// Ka note visual element.
        /// </summary>
        Ka,
    }
}
