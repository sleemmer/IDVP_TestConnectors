using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Datas
{
    class ConnectionBehaviour : MonoBehaviour
    {
        private Transform startRoot;
        private Transform endRoot;

        private bool completedConnection = false;

        public void SetStartRoot(Transform startRoot)
        {
            this.startRoot = startRoot;

            ActivateConnection(startRoot != null);
        }

        public void SetEndRoot(Transform endRoot)
        {
            this.endRoot = endRoot;

            ActivateConnection(endRoot != null);
        }

        private void Update()
        {
            UpdateConnection();
        }

        private void UpdateConnection()
        {
            if (startRoot == null)
                return;

            if (endRoot == null)
                return;

            Vector3 centerPos = (endRoot.position + startRoot.position) / 2f;
            Vector3 direction = endRoot.position - startRoot.position;

            transform.position = centerPos;
            transform.LookAt(startRoot, Vector3.up);
            transform.localScale = new Vector3(1, 1, direction.magnitude);
        }

        private void ActivateConnection(bool isActive)
        {
            if (isActive)
            {
                if (!gameObject.activeInHierarchy)
                {
                    gameObject.SetActive(true);
                }
            }
            else
            {
                if (gameObject.activeInHierarchy)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
