/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollower : MonoBehaviour
{

    public Transform cameraTransform; // Referência à Transform da câmera
    public float followDistance = 10f;

    public bool invertX = false; // Inverter movimento no eixo X
    public bool invertY = false; // Inverter movimento no eixo Y
    public bool invertZ = false; // Inverter movimento no eixo Z

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
            if (cameraTransform != null)
        {

            // Calcular a posição para seguir onde a câmera está olhando
            Vector3 followPosition = cameraTransform.position + cameraTransform.forward * followDistance;
            print(followPosition);

            // Atualizar a posição do objeto
            followPosition.x *= invertX ? -1 : 1;
            followPosition.y *= invertY ? -1 : 1;
            followPosition.z *= invertZ ? -1 : 1;
            transform.position = followPosition;
        }
    }
}
*/