using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{

    public BlockType blockType;

    public int xIndex;
    public int yIndex;

    public bool isMatched;
    private Vector2 currentPos;
    private Vector2 targetPos;

    public bool isMoving;

    public Block(int x, int y)
    {
        xIndex = x;
        yIndex = y;
    }

    public void SetPosition(int x, int y)
    {
        xIndex = x;
        yIndex = y;
    }

    //MoveToTarget
    public void MoveToTarget(Vector2 _targetPos)
    {
        StartCoroutine(MoveCoroutine(_targetPos));
    }
    //MoveCoroutine
    private IEnumerator MoveCoroutine(Vector2 targetPosition)
    {
        isMoving = true;
        float duration = 0.4f;

        Vector2 startPosition = transform.position;
        float elaspedTime = 0f;

        while (elaspedTime < duration)
        {
            float t = elaspedTime / duration;

            transform.position = Vector2.Lerp(startPosition, targetPosition, t);

            elaspedTime += Time.deltaTime;

            yield return null;
        }

        transform.position = targetPosition;
        isMoving = false;
    }
}

public enum BlockType
{
    Red,
    Blue,
    Purple,
    Green,
    Yellow
}
