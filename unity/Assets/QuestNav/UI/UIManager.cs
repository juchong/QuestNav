using System.Net;
using System.Net.Sockets;
using QuestNav.Core;
using QuestNav.Network;
using QuestNav.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuestNav.UI
{
    /// <summary>
    /// Interface for UI management.
    /// </summary>
    public interface IUIManager
    {
        /// <summary>
        /// Gets the current team number.
        /// </summary>
        string TeamNumber { get; }

        /// <summary>
        /// Initializes the UI manager with required UI components.
        /// </summary>
        /// <param name="teamInput">Input field for team number</param>
        /// <param name="ipAddressText">Text for IP address display</param>
        /// <param name="conStateText">Text for connection state display</param>
        /// <param name="teamUpdateButton">Button for updating team number</param>
        /// <param name="networkConnection">Network connection reference for updating state</param>
        void Initialize(TMP_InputField teamInput, TMP_Text ipAddressText, TMP_Text conStateText, 
                      Button teamUpdateButton, INetworkTableConnection networkConnection);

        /// <summary>
        /// Updates the IP address text display.
        /// </summary>
        void UpdateIPAddressText();

        /// <summary>
        /// Updates the connection state text display.
        /// </summary>
        void UpdateConStateText();

        /// <summary>
        /// Sets the input box placeholder text with the current team number.
        /// </summary>
        /// <param name="team">The team number to display</param>
        void SetInputBox(string team);

        /// <summary>
        /// Updates the team number based on user input.
        /// </summary>
        void UpdateTeamNumber();
    }

    /// <summary>
    /// Manages UI elements and user interactions for the QuestNav application.
    /// </summary>
    public class UIManager : MonoBehaviour, IUIManager
    {
        #region Fields
        /// <summary>
        /// Input field for team number entry
        /// </summary>
        private TMP_InputField teamInput;

        // <summary>
        /// IP address text
        /// </summary>
        private TMP_Text ipAddressText;

        // <summary>
        /// ConState text
        /// </summary>
        private TMP_Text conStateText;

        /// <summary>
        /// Button to update team number
        /// </summary>
        private Button teamUpdateButton;

        /// <summary>
        /// Reference to network connection
        /// </summary>
        private INetworkTableConnection networkConnection;

        /// <summary>
        /// Current team number
        /// </summary>
        private string teamNumber = "";

        /// <summary>
        /// Holds the detected local IP address of the HMD
        /// </summary>
        private string myAddressLocal = "0.0.0.0";
        #endregion

        #region Properties
        /// <summary>
        /// Gets the current team number.
        /// </summary>
        public string TeamNumber => teamNumber;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initializes the UI manager with required UI components.
        /// </summary>
        /// <param name="teamInput">Input field for team number</param>
        /// <param name="ipAddressText">Text for IP address display</param>
        /// <param name="conStateText">Text for connection state display</param>
        /// <param name="teamUpdateButton">Button for updating team number</param>
        /// <param name="networkConnection">Network connection reference for updating state</param>
        public void Initialize(TMP_InputField teamInput, TMP_Text ipAddressText, TMP_Text conStateText, 
                             Button teamUpdateButton, INetworkTableConnection networkConnection)
        {
            this.teamInput = teamInput;
            this.ipAddressText = ipAddressText;
            this.conStateText = conStateText;
            this.teamUpdateButton = teamUpdateButton;
            this.networkConnection = networkConnection;

            teamNumber = PlayerPrefs.GetString("TeamNumber", QuestNavConstants.Network.DEFAULT_TEAM_NUMBER);
            SetInputBox(teamNumber);
            teamInput.Select();
            
            teamUpdateButton.onClick.AddListener(UpdateTeamNumber);
            teamInput.onSelect.AddListener(OnInputFieldSelected);
            
            UpdateIPAddressText();
            UpdateConStateText();
        }

        /// <summary>
        /// Updates the team number based on user input and triggers an asynchronous connection reset.
        /// </summary>
        public void UpdateTeamNumber()
        {
            QueuedLogger.Log("[QuestNav] Updating Team Number");
            teamNumber = teamInput.text;
            PlayerPrefs.SetString("TeamNumber", teamNumber);
            PlayerPrefs.Save();
            SetInputBox(teamNumber);

            // Update the connection with new team number
            networkConnection.UpdateTeamNumber(teamNumber);
        }

        /// <summary>
        /// Updates the default IP address shown in the UI with the current HMD IP address
        /// </summary>
        public void UpdateIPAddressText()
        {
            // Get the local IP
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    myAddressLocal = ip.ToString();
                    TextMeshProUGUI ipText = ipAddressText as TextMeshProUGUI;
                    if (myAddressLocal == "127.0.0.1")
                    {
                        ipText.text = "No Adapter Found";
                    }
                    else
                    {
                        ipText.text = myAddressLocal;
                    }
                }
                break;
            }
        }

        /// <summary>
        /// Updates the connection state text display.
        /// </summary>
        public void UpdateConStateText()
        {
            TextMeshProUGUI conText = conStateText as TextMeshProUGUI;
            conText.text = networkConnection.ConnectionStateMessage;
        }

        /// <summary>
        /// Sets the input box placeholder text with the current team number.
        /// </summary>
        /// <param name="team">The team number to display</param>
        public void SetInputBox(string team)
        {
            teamInput.text = "";
            TextMeshProUGUI placeholderText = teamInput.placeholder as TextMeshProUGUI;
            if (placeholderText != null)
            {
                placeholderText.text = "Current: " + team;
            }
            else
            {
                QueuedLogger.LogError("Placeholder is not assigned or not a TextMeshProUGUI component.");
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Event handler for when the input field is selected
        /// </summary>
        /// <param name="text">The current text in the input field</param>
        private void OnInputFieldSelected(string text)
        {
            QueuedLogger.Log("[QuestNav] Input Selected");
        }
        #endregion
    }
}