using DG.Tweening; // Подключаем DoTween
using UnityEngine;
using UnityEngine.Tilemaps;

public class FireInstance
{
    public Vector3Int position;
    public int turnsLeft;
    private Tilemap fireTilemap;

    public FireInstance(Vector3Int position, Tilemap fireTilemap, int initialTurns)
    {
        this.position = position;
        this.fireTilemap = fireTilemap;
        this.turnsLeft = initialTurns;
    }

    public void UpdateFire()
    {
        if (turnsLeft > 0)
        {
            turnsLeft--;

            // Уменьшаем размер тайла вручную, создавая новую матрицу с изменённым масштабом
            float scaleFactor = 1f - (0.3f * (3 - turnsLeft)); // Уменьшаем на 10% каждый ход
            Matrix4x4 originalMatrix = fireTilemap.GetTransformMatrix(position);
            Vector3 originalScale = originalMatrix.lossyScale;

            // Анимация через DoTween для изменения масштаба
            DOTween.To(() => originalScale, newScale =>
            {
                Matrix4x4 newMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, newScale);
                fireTilemap.SetTransformMatrix(position, newMatrix);
            }, new Vector3(scaleFactor, scaleFactor, 1f), 0.5f).SetEase(Ease.OutQuad);

            if (turnsLeft <= 0)
            {
                // Удаляем тайл огня с задержкой после завершения анимации
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    fireTilemap.SetTile(position, null);
                });
            }
        }
    }

}