using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MageLock.Networking;
using MageLock.DependencyInjection;
using MageLock.Events;
using MageLock.Localization;

namespace MageLock.UI
{
    public class MatchmakingPopup : Popup
    {
        [Header("Matchmaking References")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private NetworkMode matchmakingMode = NetworkMode.Online;
        
        [Header("Scene Loading")]
        [SerializeField] private string gameSceneName = "GameScene";
        [SerializeField] private LocString loadingText;
        
        [Inject] private NetworkManagerCustom _networkManager;
        
        private float _matchmakingTime;
        private bool _isLobbyFull;
        
        public override void Initialize()
        {
            base.Initialize();
            
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelClicked);
            }
            
            StartMatchmaking();
        }
        
        private void OnEnable()
        {
            EventsBus.Subscribe<ClientConnectedEvent>(OnClientConnected);
            EventsBus.Subscribe<MatchmakingFailedEvent>(OnMatchmakingFailed);
        }
        
        private void OnDisable()
        {
            EventsBus.Unsubscribe<ClientConnectedEvent>(OnClientConnected);
            EventsBus.Unsubscribe<MatchmakingFailedEvent>(OnMatchmakingFailed);
        }
        
        private void StartMatchmaking()
        {
            if (_networkManager != null)
            {
                UpdatePlayerCount(_networkManager.ConnectedClientsIds.Count, _networkManager.MaxPlayers); 
                _networkManager.StartMatch(matchmakingMode);
            }
        }
        
        private void UpdatePlayerCount(int current, int max)
        {
            if (playerCountText != null)
            {
                playerCountText.text = $"{current}/{max}";
            }
        }
        
        private void OnLobbyFull()
        {
            if (cancelButton != null)
            {
                cancelButton.interactable = false;
            }
            
            LoadGameScene();
        }
        
        private void LoadGameScene()
        {
            if (_networkManager != null && _networkManager.IsHost && !_isLobbyFull)
            {
                _isLobbyFull = true;
                
                if (timerText != null)
                {
                    timerText.text = loadingText;
                }
                
                _networkManager.LoadSceneForAllClients(
                    gameSceneName,
                    onSuccess: OnSceneLoadSuccess,
                    onFailure: OnSceneLoadFailure
                );
            }
        }
        
        private void OnSceneLoadSuccess()
        {
            Debug.Log("Game scene loaded successfully for all clients!");
            
            base.Close();
        }
        
        private void OnSceneLoadFailure(string errorMessage)
        {
            Debug.LogError($"Failed to load game scene: {errorMessage}");
            
            if (_networkManager != null)
            {
                _networkManager.CancelMatchmaking();
            }
            
            base.Close();
        }
        
        private void UpdateTimer()
        {
            if (!_isLobbyFull)
            {
                _matchmakingTime += Time.deltaTime;
                UpdateTimerDisplay();
            }
        }
        
        private void UpdateTimerDisplay()
        {
            if (timerText != null && !_isLobbyFull)
            {
                int minutes = Mathf.FloorToInt(_matchmakingTime / 60f);
                int seconds = Mathf.FloorToInt(_matchmakingTime % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }
        
        private void OnClientConnected(ClientConnectedEvent clientConnectedEvent)
        {
            UpdatePlayerCount(clientConnectedEvent.TotalClients, clientConnectedEvent.MaxPlayers);
            
            if (clientConnectedEvent.IsLobbyFull)
            {
                OnLobbyFull();
            }
        }
        
        private void OnMatchmakingFailed(MatchmakingFailedEvent _)
        {
            if (_networkManager != null)
            {
                _networkManager.CancelMatchmaking();
            }
            
            base.Close();
        }
        
        private void OnCancelClicked()
        {
            if (_networkManager != null)
            {
                _networkManager.CancelMatchmaking();
            }
            
            base.Close(); 
        }
        
        protected override void Update()
        {
            base.Update();
            UpdateTimer();
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(OnCancelClicked);
            }
        }
    }
}