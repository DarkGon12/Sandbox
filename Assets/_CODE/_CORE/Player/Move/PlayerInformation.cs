using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine;

namespace Sandbox.Player
{
    public class PlayerInformation : MonoBehaviour
    {
        [SerializeField] private int _playerHealth = 200;

        private int _currentScene = 0;

        private void Start()
        {
            _currentScene = SceneManager.GetActiveScene().buildIndex;
        }

        public void TakeDamage(int damage)
        {
            _playerHealth -= damage;

            if (_playerHealth <= 0)
                StartCoroutine(WaitUntilDead());
        }

        private IEnumerator WaitUntilDead()
        {
            yield return new WaitForSeconds(4f);
            Dead();
        }

        private void Dead()
        {
            SceneManager.LoadScene(_currentScene);
        }
    }
}