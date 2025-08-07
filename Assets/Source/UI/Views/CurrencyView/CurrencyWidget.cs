using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrawlLine.Player;

namespace BrawlLine.UI
{
    public class CurrencyWidget : MonoBehaviour
    {
        [Header("Currency Widget UI")]
        [SerializeField] private Button currencyButton;
        [SerializeField] private TextMeshProUGUI amountText;
        
        [Header("Currency Configuration")]
        [SerializeField] private CurrencyType currencyType;
        
        private System.Action<CurrencyType> onCurrencyClicked;
        
        private void Awake()
        {
            if (currencyButton != null)
            {
                currencyButton.onClick.AddListener(OnCurrencyButtonClicked);
            }
        }
        
        private void OnDestroy()
        {
            if (currencyButton != null)
            {
                currencyButton.onClick.RemoveListener(OnCurrencyButtonClicked);
            }
        }
        
        public void Initialize(CurrencyType type, System.Action<CurrencyType> clickCallback)
        {
            currencyType = type;
            onCurrencyClicked = clickCallback;
            UpdateDisplay();
        }
        
        public void UpdateDisplay()
        {
            if (amountText != null)
            {
                int amount = PlayerModel.GetCurrency(currencyType);
                amountText.text = FormatCurrencyAmount(amount);
            }
        }
        
        private string FormatCurrencyAmount(int amount)
        {
            if (amount >= 1000000)
            {
                return $"{amount / 1000000f:F1}M";
            }
            else if (amount >= 1000)
            {
                return $"{amount / 1000f:F1}K";
            }
            else
            {
                return amount.ToString();
            }
        }
        
        private void OnCurrencyButtonClicked()
        {
            onCurrencyClicked?.Invoke(currencyType);
        }
        
        public CurrencyType GetCurrencyType()
        {
            return currencyType;
        }
        
        public void SetInteractable(bool interactable)
        {
            if (currencyButton != null)
            {
                currencyButton.interactable = interactable;
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (currencyButton == null)
            {
                currencyButton = GetComponent<Button>();
                
                if (currencyButton == null)
                {
                    Debug.LogWarning("Currency button is not assigned and could not be found", this);
                }
            }
            
            if (amountText == null)
            {
                Debug.LogWarning("Amount text is not assigned", this);
            }
        }
#endif
    }
}