using System;
using UnityEngine;

namespace AshTaiko
{
    [Serializable]
    public class MenuPage
    {
        [SerializeField]
        private GameObject _pageObject;

        public void Hide()
        {
            _pageObject.SetActive(false);
        }

        public void Show()
        {
            _pageObject.SetActive(true);
        }
    }
}