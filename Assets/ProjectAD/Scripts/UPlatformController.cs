using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAD
{
    public class UPlatformController : MonoBehaviour
    {
        public Vector3 Velocity {
            get => m_velocity;
            set => m_velocity = value;
        }
        public Quaternion AngularVelocity
        {
            get => Quaternion.AngleAxis(m_rotateSpeed, m_rotateAxis);
        }
        public float RotateSpeed
        {
            get => m_rotateSpeed;
            set => m_rotateSpeed = value;
        }
        public Vector3 RotateAxis
        {
            get => m_rotateAxis;
            set => m_rotateAxis = value;
        }

        [SerializeField] private Vector3 m_velocity;
        [SerializeField] private Vector3 m_rotateAxis;
        [SerializeField] private float m_rotateSpeed;

        private Rigidbody m_rigidbody;

        private Vector3 m_initPos;
        private Quaternion m_initRot;

        private Vector3 m_nextPos;
        private Quaternion m_nextRot;


        private void Awake ()
        {
            m_rigidbody = GetComponent<Rigidbody>();

            KinematicPhysicsSystem.RegisterPlatformController(this);
        }
        private void OnDestroy ()
        {
            KinematicPhysicsSystem.UnregisterPlatformController(this);
        }


        public virtual void UpdateVelocity (float deltaTime)
        {
            
        }

        public void Simulate (float deltaTime)
        {
            // Initialize
            m_initPos = m_rigidbody.position;
            m_initRot = m_rigidbody.rotation;

            m_nextPos = m_initPos;
            m_nextRot = m_initRot;


            m_nextPos += m_velocity * deltaTime;
            m_nextRot *= Quaternion.AngleAxis(m_rotateSpeed * deltaTime, m_rotateAxis);

            m_rigidbody.position = m_nextPos;
            m_rigidbody.rotation = m_nextRot;
        }

        public void SimulateCommit ()
        {
            m_rigidbody.position = m_initPos;
            m_rigidbody.rotation = m_initRot;

            m_rigidbody.MovePosition(m_nextPos);
            m_rigidbody.MoveRotation(m_nextRot);
        }
    }
}