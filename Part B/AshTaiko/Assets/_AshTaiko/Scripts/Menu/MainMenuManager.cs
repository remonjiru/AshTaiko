using UnityEngine;

namespace AshTaiko
{
    /// <summary>
    /// Manages the main menu navigation and page switching.
    /// Handles transitions between different menu pages (main, options, credits, chart select).
    /// </summary>
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

        /// <summary>
        /// Initializes the main menu and hides all other pages.
        /// </summary>
        private void Start()
        {
            HideAll();
            GoToMain();
        }

        /// <summary>
        /// Hides all menu pages to ensure clean state.
        /// </summary>
        private void HideAll()
        {
            _mainMenu.Hide();
            _optionsMenu.Hide();
            _creditsMenu.Hide();
            _chartSelectMenu.Hide();
        }

        /// <summary>
        /// Switches to the main menu page.
        /// </summary>
        public void GoToMain()
        {
            Debug.Log("Switching to Main Menu.");
            GoToMenu(_mainMenu);
        }

        /// <summary>
        /// Switches to the options menu page.
        /// </summary>
        public void GoToOptions()
        {
            Debug.Log("Switching to Options.");
            GoToMenu(_optionsMenu);
        }

        /// <summary>
        /// Switches to the credits menu page.
        /// </summary>
        public void GoToCredits()
        {
            Debug.Log("Switching to Credits.");
            GoToMenu(_creditsMenu);
        }

        /// <summary>
        /// Switches to the chart selection menu page.
        /// </summary>
        public void GoToChartSelect()
        {
            Debug.Log("Switching to Chart Select.");
            GoToMenu(_chartSelectMenu);
        }

        /// <summary>
        /// Exits the game application.
        /// </summary>
        public void ExitGame()
        {
            Application.Quit();
        }

        /// <summary>
        /// Switches to the specified menu page, hiding the current one.
        /// </summary>
        /// <param name="desiredMenu">The menu page to switch to.</param>
        private void GoToMenu(MenuPage desiredMenu)
        {
            if (_currentMenu != null) _currentMenu.Hide();

            desiredMenu.Show();
            _currentMenu = desiredMenu;
        }
    }
}
