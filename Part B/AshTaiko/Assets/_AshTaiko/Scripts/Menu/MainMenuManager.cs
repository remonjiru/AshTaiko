using UnityEngine;

namespace AshTaiko
{
    public class MainMenuManager : MonoBehaviour
    {
        private MenuPage _currentMenu;

        [SerializeField]
        private MenuPage _mainMenu;
        [SerializeField]
        private MenuPage _optionsMenu;
        [SerializeField]
        private MenuPage _creditsMenu;
        [SerializeField]
        private MenuPage _chartSelectMenu;

        private void Start()
        {
            HideAll();
            GoToMain();
        }

        private void HideAll()
        {
            _mainMenu.Hide();
            _optionsMenu.Hide();
            _creditsMenu.Hide();
            _chartSelectMenu.Hide();
        }

        public void GoToMain()
        {
            Debug.Log("Switching to Main Menu.");
            GoToMenu(_mainMenu);
        }

        public void GoToOptions()
        {
            Debug.Log("Switching to Options.");
            GoToMenu(_optionsMenu);
        }

        public void GoToCredits()
        {
            Debug.Log("Switching to Credits.");
            GoToMenu(_creditsMenu);
        }
        public void GoToChartSelect()
        {
            Debug.Log("Switching to Chart Select.");
            GoToMenu(_chartSelectMenu);
        }


        private void GoToMenu(MenuPage desiredMenu)
        {
            if (_currentMenu != null) _currentMenu.Hide();

            desiredMenu.Show();
            _currentMenu = desiredMenu;
        }
    }
}
