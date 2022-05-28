using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ProjectAD
{
    public class SpringArm : MonoBehaviour
    {
        [Tooltip("Camera follows target")]
        [SerializeField] private GameObject m_target;
        [Tooltip("Atached camera at SpringArm")]
        [SerializeField] private Camera m_camera;

        [SerializeField] private float m_length = 15.0f;
        [SerializeField] private Vector3 m_armOffset;


        private void Awake ()
        {
            if(m_camera == null)
            {
                m_camera = GetComponentInChildren<Camera>();
            }
        }

        private void LateUpdate ()
        {
            if (m_target == null) return;

            transform.position = m_target.transform.position + Vector3.back * m_length + m_armOffset;
        }
    }
}