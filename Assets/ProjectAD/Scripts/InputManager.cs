using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAD
{
    public class InputManager : MonoBehaviour
    {
        private static InputManager s_singleton = null;
        private static readonly List<UCharacterController> m_playerList = new List<UCharacterController>();
        [SerializeField] private SpringArm m_springArm;

        [Header("Camera")]
        [SerializeField] private bool m_lookAngle;
        [SerializeField] private bool m_lookRotate;
        [Range(0.0f, 1.0f)]
        [SerializeField] private float m_sensitive = 0.1f;

        private Vector2 m_inputDir;
        

        private void UpdateMove ()
        {
            for (int cur = 0, cnt = m_playerList.Count; cur != cnt; ++cur)
            {
                Vector3 dir = new Vector3(m_inputDir.x, 0f, m_inputDir.y);
                dir = Quaternion.AngleAxis(m_springArm.Rotate, Vector3.up) * dir;
                m_playerList[cur].InputMove(dir);
            }
        }

        // Event Binders
        public void OnMove (InputAction.CallbackContext ctx)
        {
            Vector2 direction = ctx.ReadValue<Vector2>();
            m_inputDir = direction;

            UpdateMove();
        }
        public void OnJump(InputAction.CallbackContext ctx)
        {
            switch (ctx.phase)
            {
            case InputActionPhase.Started:
                for (int cur = 0, cnt = m_playerList.Count; cur != cnt; ++cur)
                {
                    m_playerList[cur].InputJump();
                }
                break;
            case InputActionPhase.Canceled:
                for (int cur = 0, cnt = m_playerList.Count; cur != cnt; ++cur)
                {
                    m_playerList[cur].InputStopJump();
                }
                break;
            }
        }

        public void OnLook (InputAction.CallbackContext ctx)
        {
            if (m_springArm == null)
            {
                m_springArm = FindObjectOfType<SpringArm>();
            }

            Vector2 delta = ctx.ReadValue<Vector2>() * m_sensitive;
            
            if (!m_lookRotate)
            {
                m_springArm.Rotate += delta.x;
            }

            if (!m_lookAngle)
            {
                m_springArm.Angle += delta.y;
            }
            

            UpdateMove();
        }


        public static InputManager GetInstance ()
        {
            if(s_singleton == null)
            {
                s_singleton = FindObjectOfType<InputManager>();
                if(s_singleton != null)
                {
                    DontDestroyOnLoad(s_singleton.gameObject);
                }
            }
            return s_singleton;
        }


        public static void RegisterPlayer (UCharacterController player)
        {
            if (!m_playerList.Contains(player))
            {
                m_playerList.Add(player);
            }
        }
        public static void UnregisterPlayer (UCharacterController player)
        {
            if (m_playerList.Contains(player))
            {
                m_playerList.Remove(player);
            }
        }

        private void Awake ()
        {
            if (s_singleton == null)
            {
                s_singleton = this;
                DontDestroyOnLoad(s_singleton.gameObject);
            }
            else
            {
                if(s_singleton != this)
                {
                    Destroy(s_singleton.gameObject);
                }
            }
        }
    }
}