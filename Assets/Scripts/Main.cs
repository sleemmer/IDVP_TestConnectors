using Assets.Scripts.Datas;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ApplicationState
{
    Idle,
    WaitForConnection,
    Drag,
}

public class Main : MonoBehaviour
{
    private const int CONNECTION_POOL_SIZE = 10;
    private const int START_DRAG_OFFSET_SQR = 25; // Считаем, что начался Drag, если смещение больше 5 пикселей

    public float Radius = 0;
    public int MaxObjectCount = 0;
    public Transform ConnectableRoot;
    public Transform ConnectionRoot;

    private ApplicationState state = ApplicationState.Idle;

    private List<ConnectableObject> connectableList = new List<ConnectableObject>();
    private List<ConnectionBehaviour> connectionListPool = new List<ConnectionBehaviour>();
    private Dictionary<string, ConnectionBehaviour> connectionListInUse = new Dictionary<string, ConnectionBehaviour>();

    private UnityEngine.Object connectableObj = null;
    private UnityEngine.Object connectionObj = null;

    private ConnectableObject selectedObject = null;
    private Vector3 worldPosOffset;
    private float screenZPos;
    private RaycastHit raycastHit;

    private Vector3 startDragPos;
    private bool isConnectionDragStarted = false;

    private GameObject fakeConnector = null;
    private ConnectionBehaviour fakeConnection = null;

    private void Start()
    {
        connectableObj = Resources.Load("Prefabs/Connectable");
        CreateConnectableObjects(MaxObjectCount);

        connectionObj = Resources.Load("Prefabs/Connection");
        CreateConnectionObjects(CONNECTION_POOL_SIZE);

        // Объект, к которому будет присоединяться "линия связи", когда делаем Drag от одного коннектора к другому
        fakeConnector = new GameObject("fakeConnector");
        fakeConnector.transform.SetParent(ConnectableRoot);

        // "Линия связи", которую тянем от выделенного коннектора к курсору
        fakeConnection = CreateConnectionObject();
        fakeConnection.SetStartRoot(null);
        fakeConnection.gameObject.name = "fakeConnection";
    }

    private void CreateConnectableObjects(int maxCount)
    {
        GameObject connectableGO;

        for (int i = 0; i < maxCount; i++)
        {
            connectableGO = GameObject.Instantiate(connectableObj) as GameObject;
            connectableGO.transform.SetParent(ConnectableRoot);
            connectableGO.transform.localPosition = GetRandomXZPosition(Radius);

            ConnectableObject obj = new ConnectableObject(i, connectableGO);
            obj.OnConnectorClick += OnConnectorClick;
            obj.OnPlatformClick += OnPlatformClick;

            connectableList.Add(obj);
            connectableList[i].State = ConnectableObjectState.Idle;
        }
    }

    private void CreateConnectionObjects(int maxCount)
    {
        for (int i = 0; i < maxCount; i++)
        {
            ConnectionBehaviour connection = CreateConnectionObject();
            connectionListPool.Add(connection);
        }
    }

    private ConnectionBehaviour CreateConnectionObject()
    {
        GameObject connectionGO = GameObject.Instantiate(connectionObj) as GameObject;
        connectionGO.transform.SetParent(ConnectionRoot);
        var connection = connectionGO.GetComponent<ConnectionBehaviour>();
        connection.SetStartRoot(null);

        return connection;
    }

    private Vector3 GetRandomXZPosition(float radius)
    {
        float randomAngle = UnityEngine.Random.Range(0, Mathf.PI * 2);
        float randomRadius = UnityEngine.Random.Range(0, radius);
        return new Vector3(Mathf.Cos(randomAngle) * randomRadius, 0, Mathf.Sin(randomAngle) * randomRadius);
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = screenZPos;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    private void OnConnectorClick(ConnectableObject clickedObject)
    {
        Debug.Log("fsv OnConnectorClick ID == " + clickedObject.Id);
        // Кликнули в коннектор
        switch (state)
        {
            case ApplicationState.Idle:
                clickedObject.State = ConnectableObjectState.Selected;
                selectedObject = clickedObject;

                screenZPos = Camera.main.WorldToScreenPoint(selectedObject.Root.transform.position).z;
                worldPosOffset = selectedObject.Root.transform.position - GetMouseWorldPosition();

                SetState(ApplicationState.WaitForConnection);
                break;

            case ApplicationState.WaitForConnection:
                // Устанавливаем связь между объектами и сбрасываем состояние приложения на Idle
                SetConnection(selectedObject, clickedObject);
                SetState(ApplicationState.Idle);
                break;
        }
    }

    private void OnPlatformClick(ConnectableObject clickedObject)
    {
        Debug.Log("fsv OnPlatformClick ID == " + clickedObject.Id);
        selectedObject = clickedObject;

        screenZPos = Camera.main.WorldToScreenPoint(selectedObject.Root.transform.position).z;
        worldPosOffset = selectedObject.Root.transform.position - GetMouseWorldPosition();

        SetState(ApplicationState.Drag);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            startDragPos = Input.mousePosition;
            //Обрабатываем клик
            CreateRaycast();
        }

        if (Input.GetKey(KeyCode.Mouse0))
        {
            if (selectedObject == null)
                return;

            switch (state)
            {
                case ApplicationState.Drag:
                    selectedObject.Root.transform.position = (GetMouseWorldPosition() + worldPosOffset);
                    break;

                case ApplicationState.WaitForConnection:
                    if ((Input.mousePosition - startDragPos).sqrMagnitude > START_DRAG_OFFSET_SQR && !isConnectionDragStarted)
                    {
                        Debug.Log("fsv StartConnectionDrag");
                        fakeConnection.SetStartRoot(selectedObject.Root.transform);
                        fakeConnection.SetEndRoot(fakeConnector.transform);
                        isConnectionDragStarted = true;
                    }

                    fakeConnector.transform.position = (GetMouseWorldPosition() + worldPosOffset);
                    break;
            }
        }

        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            switch (state)
            {
                case ApplicationState.Drag:
                    SetState(ApplicationState.Idle);
                    break;

                case ApplicationState.WaitForConnection:
                    if ((Input.mousePosition - startDragPos).sqrMagnitude > START_DRAG_OFFSET_SQR)
                    {
                        CreateRaycast();
                    }
                    break;
            }

            fakeConnection.SetStartRoot(null);
            isConnectionDragStarted = false;
        }
    }

    private bool CreateRaycast()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out raycastHit))
        {
            // Если во что-то попали, то объект кинет нам событие нужного типа
            for (int i = 0; i < connectableList.Count; i++)
            {
                connectableList[i].ProcessClick(raycastHit.transform);
            }

            return true;
        }
        else
        {
            // Если кликнули в пустоту, то сбрасываем состояние на дефолтное
            SetState(ApplicationState.Idle);

            return false;
        }
    }

    private void SetObjectsState(ConnectableObjectState objState)
    {
        for (int i = 0; i < connectableList.Count; i++)
        {
            if (connectableList[i] != selectedObject)
                connectableList[i].State = objState;
        }
    }

    private void SetState(ApplicationState appState)
    {
        state = appState;

        switch (state)
        {
            case ApplicationState.Idle:
                selectedObject = null;
                SetObjectsState(ConnectableObjectState.Idle);
                break;

            case ApplicationState.WaitForConnection:
                SetObjectsState(ConnectableObjectState.WaitForConnection);
                break;

            case ApplicationState.Drag:
                SetObjectsState(ConnectableObjectState.Idle);
                break;
        }
    }

    private void SetConnection(ConnectableObject startObject, ConnectableObject endObject)
    {
        if (startObject.Id == endObject.Id)
            return;

        string hash = startObject.Id.ToString() + endObject.Id.ToString();
        // если связь между этими двумя объектами уже установлена, то ничего не делаем
        if (connectionListInUse.ContainsKey(hash))
            return;

        ConnectionBehaviour connection = GetConnectionObjectFromPool();
        connection.SetStartRoot(startObject.Root.transform);
        connection.SetEndRoot(endObject.Root.transform);

        connectionListInUse.Add(hash, connection);
    }

    private ConnectionBehaviour GetConnectionObjectFromPool()
    {
        ConnectionBehaviour connection = null;
        if (connectionListPool.Count > 0)
        {
            int lastIndex = connectionListPool.Count - 1;
            connection = connectionListPool[lastIndex];
            connectionListPool.RemoveAt(lastIndex);

            return connection;
        }
        else
        {
            CreateConnectionObjects(CONNECTION_POOL_SIZE);
            return GetConnectionObjectFromPool();
        }
    }
}
