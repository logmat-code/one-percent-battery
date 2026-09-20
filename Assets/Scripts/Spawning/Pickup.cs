using System;
using OnePercentBattery.Meta;
using OnePercentBattery.Player;
using UnityEngine;

namespace OnePercentBattery.Spawning
{
    public enum PickupType
    {
        Coin,
        Energy,
        Shield,
        Magnet
    }

    /// <summary>One component covers all pickup types; only the field(s) relevant to <see cref="Type"/> are used.</summary>
    public sealed class Pickup : MonoBehaviour
    {
        [SerializeField] private PickupType type = PickupType.Coin;
        [SerializeField] private int coinValue = 1;
        [SerializeField] private float energyRestorePercent = 15f;
        [SerializeField] private float shieldSeconds = 5f;

        public PickupType Type => type;

        /// <summary>Set by the spawner right after activation so this instance returns to the correct pool.</summary>
        public Action<Pickup> OnCollected;

        private PlayerController _player;
        private float _magnetPullElapsedSeconds;

        /// <summary>Set by the spawner right after activation - only Coin uses it (see Update), to home in on the player while a Magnet is active.</summary>
        public void SetPlayer(PlayerController player) => _player = player;

        private void OnEnable()
        {
            _magnetPullElapsedSeconds = 0f;
        }

        private void Update()
        {
            if (type != PickupType.Coin || _player == null) return;

            PlayerMagnet magnet = _player.Magnet;
            if (!magnet.IsActive)
            {
                _magnetPullElapsedSeconds = 0f;
                return;
            }

            Vector2 playerPos = _player.transform.position;
            float distance = Vector2.Distance(transform.position, playerPos);
            if (distance > magnet.Radius)
            {
                _magnetPullElapsedSeconds = 0f;
                return;
            }

            // Coins have no Rigidbody2D (see PlayerController.Awake's note on trigger pairs), so a
            // plain, smooth transform move is the correct tool here - MoveTowards can't overshoot, and
            // the coin is collected the moment it overlaps the player's own collider, through the same
            // OnTriggerEnter2D -> Collect() path a coin the player simply runs into already uses.
            _magnetPullElapsedSeconds += Time.deltaTime;
            float speed = magnet.PullSpeed + magnet.Acceleration * _magnetPullElapsedSeconds;
            transform.position = Vector2.MoveTowards(transform.position, playerPos, speed * Time.deltaTime);
        }

        public void Collect(PlayerController player)
        {
            switch (type)
            {
                case PickupType.Coin:
                    CurrencyManager.Instance.AddCoinsForRun(coinValue);
                    break;
                case PickupType.Energy:
                    player.Battery.RestoreEnergy(energyRestorePercent);
                    break;
                case PickupType.Shield:
                    player.Battery.GrantShield(shieldSeconds);
                    break;
                case PickupType.Magnet:
                    player.Magnet.Activate();
                    break;
            }

            OnCollected?.Invoke(this);
        }
    }
}
