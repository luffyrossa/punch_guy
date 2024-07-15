using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Referência ao personagem
    public Vector3 offset;   // Deslocamento inicial da câmera em relação ao personagem

    void Start()
    {
        // Calcula o deslocamento inicial baseado na posição inicial da câmera e do personagem
        offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        // Atualiza a posição da câmera para seguir o personagem mantendo o deslocamento constante
        transform.position = target.position + offset;
    }
}
