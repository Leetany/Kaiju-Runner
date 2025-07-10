using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jino
{
    public class CharacterSelectionUI : MonoBehaviour
    {
        private const int NUM_OF_CHARACTERS = 10;

        public static CharacterSelectionUI Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }


    }
}


