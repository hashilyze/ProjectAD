using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ProjectAD
{
    [ExecuteAlways]
    public class SpringArm : MonoBehaviour
    {
        public float Length
        {
            get => m_length;
            set => m_length = value;
        }

        public float Angle
        {
            get => m_angle;
            set => m_angle = Mathf.Clamp(value, 0.0f, 90.0f);
        }

        public float Rotate
        {
            get => m_rotate;
            set
            {
                float rate = (value + 360.0f) / 360.0f - ((int)value + 360) / 360;
                m_rotate = rate * 360.0f;
            }
        }


        [Tooltip("Camera follows target")]
        [SerializeField] private GameObject m_target;

        [Tooltip("Distance from target")]
        [SerializeField] private float m_length = 15.0f;
        [Tooltip("Degree of camera's roll")]
        [Range(0.0f, 90.0f)]
        [SerializeField] private float m_angle = 35.0f;
        [Tooltip("Degree of camera's pitch")]
        [SerializeField] private float m_rotate = 0.0f;

        private void LateUpdate ()
        {
            if (m_target == null) return;

            Vector3 cameraPos =
                Quaternion.AngleAxis(m_rotate, Vector3.up)
                * Quaternion.AngleAxis(m_angle, Vector3.right)
                * (Vector3.back * m_length);

            transform.position = m_target.transform.position + cameraPos;

            transform.rotation = 
                Quaternion.AngleAxis(m_rotate, Vector3.up)
                * Quaternion.AngleAxis(m_angle, Vector3.right);
        }
    }
}