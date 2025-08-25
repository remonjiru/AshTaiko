using System;
using UnityEngine;

namespace AshTaiko
{
    /// <summary>
    /// Represents a single menu page that can be shown or hidden.
    /// Provides simple show/hide functionality for menu navigation.
    /// </summary>
    [Serializable]
    public class MenuPage
    {
        [SerializeField]
        private GameObject _pageObject;

        /// <summary>
        /// Hides the menu page by deactivating its GameObject.
        /// </summary>
        public void Hide()
        {
            _pageObject.SetActive(false);
        }

        /// <summary>
        /// Shows the menu page by activating its GameObject.
        /// </summary>
        public void Show()
        {
            _pageObject.SetActive(true);
        }
    }
}