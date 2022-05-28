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

        // Event Binders
        public void OnMove (InputAction.CallbackContext ctx)
        {
            Vector2 direction = ctx.ReadValue<Vector2>();

            for(int cur = 0, cnt = m_playerList.Count; cur != cnt; ++cur)
            {
                Vector3 dir = new Vector3(direction.x, 0f, direction.y);
                dir = Quaternion.FromToRotation(Vector3.up, m_playerList[cur].transform.up) * dir;
                //dir = m_playerList[cur].transform.rotation * dir;
                m_playerList[cur].InputMove(dir);
            }
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