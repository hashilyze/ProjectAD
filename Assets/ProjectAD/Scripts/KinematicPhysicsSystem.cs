using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAD
{
    public class KinematicPhysicsSystem : MonoBehaviour
    {
        private static KinematicPhysicsSystem s_instance;
        
        private static readonly List<UCharacterController> m_characters = new List<UCharacterController>();
        private static readonly List<UPlatformController> m_platforms = new List<UPlatformController>();


        public static KinematicPhysicsSystem GetInstance ()
        {
            EnsureInstance();
            return s_instance;
        }

        public static void EnsureInstance ()
        {
            if (s_instance == null)
            {
                GameObject instanceObject = new GameObject("KinematicPhysicsSystem");
                s_instance = instanceObject.AddComponent<KinematicPhysicsSystem>();

                instanceObject.hideFlags = HideFlags.NotEditable;
                s_instance.hideFlags = HideFlags.NotEditable;

                DontDestroyOnLoad(instanceObject);
            }
        }

        public static void RegisterCharacterController (UCharacterController character)
        {
            if (!m_characters.Contains(character))
            {
                m_characters.Add(character);
            }
        }
        public static void UnregisterCharacterController (UCharacterController character)
        {
            m_characters.Remove(character);
        }


        public static void RegisterPlatformController (UPlatformController platform)
        {
            if (!m_platforms.Contains(platform))
            {
                m_platforms.Add(platform);
            }
        }
        public static void UnregisterPlatformController (UPlatformController platform)
        {
            m_platforms.Remove(platform);
        }

        private void Awake ()
        {
            if(s_instance == null)
            {
                s_instance = this;
                s_instance.gameObject.hideFlags = HideFlags.NotEditable;
                s_instance.hideFlags = HideFlags.NotEditable;

                DontDestroyOnLoad(s_instance.gameObject);
            }
            else
            {
                if(s_instance != this)
                {
                    Destroy(this.gameObject);
                }
            }
        }

        private void FixedUpdate ()
        {
            // Simulate Part.1
            {
                for (int cur = 0, cnt = m_platforms.Count; cur < cnt; ++cur)
                {
                    UPlatformController platform = m_platforms[cur];
                    platform.UpdateVelocity(Time.fixedDeltaTime);
                }

                for (int cur = 0, cnt = m_characters.Count; cur < cnt; ++cur)
                {
                    UCharacterController character = m_characters[cur];
                    character.SimulatePart1(Time.fixedDeltaTime);
                }
            }
            // Simulate Part.2
            {
                for (int cur = 0, cnt = m_platforms.Count; cur < cnt; ++cur)
                {
                    UPlatformController platform = m_platforms[cur];
                    platform.Simulate(Time.fixedDeltaTime);
                }

                for (int cur = 0, cnt = m_characters.Count; cur < cnt; ++cur)
                {
                    UCharacterController character = m_characters[cur];
                    character.SimulatePart2(Time.fixedDeltaTime);
                }
            }
            // Simulate Commit
            {
                for (int cur = 0, cnt = m_characters.Count; cur < cnt; ++cur)
                {
                    UCharacterController character = m_characters[cur];
                    character.SimulateCommit();
                }
                for (int cur = 0, cnt = m_platforms.Count; cur < cnt; ++cur)
                {
                    UPlatformController platform = m_platforms[cur];
                    platform.SimulateCommit();
                }
            }
        }
    }
}