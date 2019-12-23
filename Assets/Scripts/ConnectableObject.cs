using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Datas
{
    public enum ConnectableObjectState
    {
        Idle,
        Selected,
        WaitForConnection,
    }

    class ConnectableObject
    {
        public Action<ConnectableObject> OnPlatformClick = null;
        public Action<ConnectableObject> OnConnectorClick = null;

        private Color defaultGrayColor = new Color(191, 197, 214, 255) / 255f;
        private Color yellowColor = new Color(255, 243, 36, 255) / 255f;
        private Color blueColor = new Color(0, 183, 244, 255) / 255f;

        private ConnectableObjectView view = null;

        private GameObject root = null;
        public GameObject Root
        {
            get
            {
                return root;
            }
            set
            {
                root = value;
            }
        }

        private int id = -1;
        public int Id
        {
            get
            {
                return id;
            }
        }

        public ConnectableObject(int id, GameObject root)
        {
            this.id = id;
            this.root = root;
            view = root.GetComponent<ConnectableObjectView>();
            if (view == null)
            {
                view = root.AddComponent<ConnectableObjectView>();
            }
        }

        private ConnectableObjectState state = ConnectableObjectState.Idle;
        public ConnectableObjectState State
        {
            get 
            {
                return state;
            }
            set
            {
                state = value;
                UpdateState();
            }
        }

        private void UpdateState()
        {
            switch (state)
            {
                case ConnectableObjectState.Idle:
                    SetColor(Color.gray);
                    break;

                case ConnectableObjectState.Selected:
                    SetColor(Color.yellow);
                    break;

                case ConnectableObjectState.WaitForConnection:
                    SetColor(Color.blue);
                    break;
            }
        }

        private void SetColor(Color color)
        {
            view.ConnectorRenderer.material.color = color;
        }

        public void ProcessClick(Transform clickedObject)
        {
            if (clickedObject == view.ConnectorTransform)
            {
                OnConnectorClick?.Invoke(this);
            }
            else if (clickedObject == view.PlatformTransform)
            {
                OnPlatformClick?.Invoke(this);
            }
        }
    }
}
