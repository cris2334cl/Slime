using UnityEngine;

public class JugadorFPS : MonoBehaviour
{
    public float velocidad = 6f;
    public float sensibilidad = 2f;
    public Transform camara;

    CharacterController controlador;
    float rotacionVertical = 0f;
    float velocidadY = 0f;

    void Start()
    {
        controlador = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensibilidad;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidad;
        transform.Rotate(0, mouseX, 0);
        rotacionVertical -= mouseY;
        rotacionVertical = Mathf.Clamp(rotacionVertical, -80f, 80f);
        camara.localRotation = Quaternion.Euler(rotacionVertical, 0, 0);

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 mov = (transform.right * x + transform.forward * z) * velocidad;

        if (controlador.isGrounded && velocidadY < 0) velocidadY = -2f;
        velocidadY += -20f * Time.deltaTime;
        mov.y = velocidadY;

        controlador.Move(mov * Time.deltaTime);
    }
}