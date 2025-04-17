using System.Collections.Generic;
using UnityEngine;
using PDollarGestureRecognizer;

public class MovementRecognizer : MonoBehaviour
{
    public Transform movementSource;
    public float newPositionThresholdDistance = 0.05f;
    public GameObject debugCubePrefab;
    public bool creationMode = true;
    public string newGestureName;
    public float recognitionThreshold = 0.9f;  
    public List<Gesture> trainingSet = new List<Gesture>();

    private bool isMoving = false;
    private List<Vector3> positionsList = new List<Vector3>();

    void Update()
    {
        if(isMoving)
            UpdateMovement();        
    }

    public void StartMovement()
    {
        isMoving = true;
        positionsList.Clear();

        AddPointFromMovementSource();
    }

    void AddPointFromMovementSource()
    {
        positionsList.Add(movementSource.position);
        if (debugCubePrefab)
        {
            GameObject spawnedDebugCube = Instantiate(debugCubePrefab, movementSource.position, Quaternion.identity);
            Destroy(spawnedDebugCube, 5);
        }
    }

    void UpdateMovement()
    {
        Vector3 lastPosition = positionsList[positionsList.Count - 1];

        if (Vector3.Distance(movementSource.position, lastPosition) > newPositionThresholdDistance)
        {
            AddPointFromMovementSource();
        }
    }

   public (string, float) EndMovement()
{
    isMoving = false;
    AddPointFromMovementSource();

    // 포인트가 충분하지 않을 경우
    if (positionsList.Count < 5)
    {
        Debug.LogWarning("Insufficient points for gesture recognition.");
        return (null, 0f);
    }

    // 포인트 배열 생성
    Point[] pointArray = new Point[positionsList.Count];
    for (int i = 0; i < positionsList.Count; i++)
    {
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(positionsList[i]);
        pointArray[i] = new Point(screenPoint.x, screenPoint.y, 0);
    }

    Gesture newGesture = new Gesture(pointArray);

    if (creationMode)
    {
        newGesture.Name = newGestureName;
        trainingSet.Add(newGesture);
        return (null, 0f);
    }
    else
    {
        // 제스처 세트가 비어 있는지 확인
        if (trainingSet.Count == 0)
        {
            Debug.LogWarning("No gestures in training set for recognition.");
            return (null, 0f);
        }

        // 제스처 인식
        Result result = PointCloudRecognizer.Classify(newGesture, trainingSet.ToArray());

        if (result.Score > recognitionThreshold)
        {   
            //Debug.Log($"Gesture recognized: {result.GestureClass}, Score: {result.Score}");
            return (result.GestureClass, result.Score);
        }
        else{
            //Debug.Log("Gesture recognition failed. No match found.");
            return (null, result.Score);
        }
    }
}


}
