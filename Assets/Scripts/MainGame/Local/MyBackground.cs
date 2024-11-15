using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyBackground : MonoBehaviour
{
    [SerializeField] private GameObject groundPrefab;
    private Color groundOriginalColor;
    private float groundWidth;
    private float groundHeight;
    [SerializeField] bool ColorGround;

    [SerializeField] private List<GameObject> groundInstances;
    [SerializeField] private float backgroundZ;
    [SerializeField] private float rowNum = 5;
    [SerializeField] private float colNum = 5;

    private Vector3 cameraTopLeftCorner
    {
        get
        {
            Vector3 cameraPos = Camera.main.transform.position;
            return new Vector3(cameraPos.x - Camera.main.aspect * Camera.main.orthographicSize, cameraPos.y + Camera.main.orthographicSize, backgroundZ);
        }
    }

    private Vector3 getGroundTopLeftCorner(GameObject groundPrefab)
    {
        return new Vector3(groundPrefab.transform.position.x - groundWidth / 2, groundPrefab.transform.position.y + groundHeight / 2, backgroundZ);
    }

    private void setGroundTopLeftCorner(GameObject groundPrefab, Vector3 targetPosition)
    {
        Vector3 changeVector = targetPosition - getGroundTopLeftCorner(groundPrefab);
        groundPrefab.transform.position += changeVector;
    }

    private void Awake()
    {
        SpriteRenderer spriteRenderer = groundPrefab.GetComponent<SpriteRenderer>();
        groundOriginalColor = spriteRenderer.color;
        groundWidth = spriteRenderer.bounds.size.x;
        groundHeight = spriteRenderer.bounds.size.y;

        groundInstances = new List<GameObject>();
        for (int i = 0; i < rowNum * colNum; i++) { groundInstances.Add(Instantiate(groundPrefab, this.transform)); }

        this.transform.position = cameraTopLeftCorner;
        SetGroundPositions();
    }

    private void SetGroundPositions()
    {
        Vector3 currentPosition = new Vector3(transform.position.x - groundWidth, transform.position.y + groundHeight, backgroundZ);
        int currentIndex = 0;

        for (int row = 0; row < rowNum; row++)
        {
            for (int col = 0; col < colNum; col++, currentIndex++)
            {
                setGroundTopLeftCorner(groundInstances[currentIndex], currentPosition);
                currentPosition = new Vector3(currentPosition.x + groundWidth, currentPosition.y, backgroundZ);

                if (ColorGround) groundInstances[currentIndex].GetComponent<SpriteRenderer>().color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1);
                else groundInstances[currentIndex].GetComponent<SpriteRenderer>().color = groundOriginalColor;
            }
            currentPosition = new Vector3(transform.position.x - groundWidth, currentPosition.y - groundHeight, backgroundZ);
        }
    }

    private void LateUpdate()
    {
        if (cameraTopLeftCorner.x <= transform.position.x - groundWidth ||
            cameraTopLeftCorner.x >= transform.position.x + groundWidth)
            transform.position = new Vector3(cameraTopLeftCorner.x, transform.position.y, backgroundZ);
        if (cameraTopLeftCorner.y <= transform.position.y - groundHeight ||
            cameraTopLeftCorner.y >= transform.position.y + groundWidth)
            transform.position = new Vector3(transform.position.x, cameraTopLeftCorner.y, backgroundZ);
    }
}
