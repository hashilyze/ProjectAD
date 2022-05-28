using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAD
{
    public class Teleporter : MonoBehaviour
    {
        [SerializeField] private GameObject m_warpLocation;

        private void OnTriggerEnter (Collider other)
        {
            if (other.transform.TryGetComponent(out UCharacterController controller))
            {
                controller.Teleport(m_warpLocation.transform.position);
            }
        }
    }
}