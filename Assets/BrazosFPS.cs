using UnityEngine;
using UnityEngine.SceneManagement;

public class BrazosFPS : MonoBehaviour
{
    [Header("Aspecto")]
    public Color colorManga = new Color(0.25f, 0.32f, 0.22f, 1f);
    public Color colorPiel = new Color(0.93f, 0.72f, 0.58f, 1f);
    public float grosorBrazo = 0.09f;
    public float radioMano = 0.06f;
    public bool ocultarCapsula = true;

    [Header("Posiciones (relativas a la camara)")]
    public Vector3 hombroDerecho = new Vector3(0.62f, -0.8f, 0.15f);
    public Vector3 manoDerecha = new Vector3(0.32f, -0.26f, 0.55f);
    public Vector3 hombroIzquierdo = new Vector3(-0.5f, -0.8f, 0.2f);
    public Vector3 manoIzquierda = new Vector3(0.18f, -0.25f, 0.9f);

    [Header("Animacion")]
    public float fuerzaCaminar = 1f;
    public float fuerzaMouse = 1f;
    public float fuerzaDisparo = 1f;

    class Brazo
    {
        public Transform manga;
        public Transform mano;
    }

    Brazo derecho;
    Brazo izquierdo;
    GameObject raizBrazos;
    CharacterController controlador;

    float fase = 0f;
    float caminar = 0f;
    float retroceso = 0f;
    float tCambio = 1f;
    float tRecarga = 1f;
    Vector3 sway = Vector3.zero;

    // Se agrega solo a la camara del jugador, no hay que ponerlo en ningun objeto
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        Agregar();
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        Agregar();
    }

    static void Agregar()
    {
        JugadorFPS jugador = FindFirstObjectByType<JugadorFPS>();
        if (jugador == null) return;

        Camera cam = jugador.GetComponentInChildren<Camera>();
        if (cam == null) return;

        if (cam.GetComponent<BrazosFPS>() == null)
        {
            cam.gameObject.AddComponent<BrazosFPS>();
        }
    }

    void Start()
    {
        JugadorFPS jugador = GetComponentInParent<JugadorFPS>();
        if (jugador != null)
        {
            controlador = jugador.GetComponent<CharacterController>();

            if (ocultarCapsula)
            {
                MeshRenderer mr = jugador.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }
        }

        raizBrazos = new GameObject("Brazos");
        raizBrazos.transform.SetParent(transform, false);

        derecho = CrearBrazo("Derecho");
        izquierdo = CrearBrazo("Izquierdo");
    }

    Brazo CrearBrazo(string nombre)
    {
        Brazo b = new Brazo();

        GameObject manga = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        manga.name = "Manga" + nombre;
        Destroy(manga.GetComponent<Collider>());
        manga.transform.SetParent(raizBrazos.transform, false);
        Pintar(manga, colorManga);
        b.manga = manga.transform;

        GameObject mano = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mano.name = "Mano" + nombre;
        Destroy(mano.GetComponent<Collider>());
        mano.transform.SetParent(raizBrazos.transform, false);
        mano.transform.localScale = Vector3.one * radioMano * 2f;
        Pintar(mano, colorPiel);
        b.mano = mano.transform;

        return b;
    }

    void Pintar(GameObject obj, Color color)
    {
        Renderer r = obj.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.material.color = color;
    }

    void Update()
    {
        if (Time.timeScale == 0f || derecho == null) return;

        float dt = Time.deltaTime;

        // --- Caminar ---
        float velocidad = 0f;
        if (controlador != null)
        {
            Vector3 v = controlador.velocity;
            v.y = 0f;
            velocidad = v.magnitude;
        }
        bool seMueve = velocidad > 0.5f;
        caminar = Mathf.Lerp(caminar, seMueve ? 1f : 0f, dt * 8f);
        if (seMueve) fase += dt * 9f;

        Vector3 bob = new Vector3(Mathf.Sin(fase) * 0.012f, Mathf.Sin(fase * 2f) * 0.01f, 0f);
        bob *= caminar * fuerzaCaminar;

        // --- Balanceo con el mouse ---
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");
        Vector3 objetivo = new Vector3(-mx * 0.012f, -my * 0.012f, 0f) * fuerzaMouse;
        objetivo = Vector3.ClampMagnitude(objetivo, 0.05f);
        sway = Vector3.Lerp(sway, objetivo, dt * 10f);

        // --- Disparo, recarga y cambio de arma ---
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            if (Input.GetButtonDown("Fire1")) retroceso = 1f;
            else if (Input.GetButton("Fire1")) retroceso = Mathf.Max(retroceso, 0.4f);

            if (Input.GetKeyDown(KeyCode.R)) tRecarga = 0f;

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Alpha3))
            {
                tCambio = 0f;
            }
        }

        retroceso = Mathf.Lerp(retroceso, 0f, dt * 14f);
        tCambio = Mathf.Min(1f, tCambio + dt / 0.35f);
        tRecarga = Mathf.Min(1f, tRecarga + dt / 1.1f);

        float bajadaCambio = Mathf.Sin(Mathf.PI * tCambio) * 0.12f;
        float bajadaRecarga = Mathf.Sin(Mathf.PI * tRecarga) * 0.2f;
        Vector3 retrocesoVec = new Vector3(0f, 0.01f, -0.05f) * retroceso * fuerzaDisparo;

        Vector3 comun = bob + sway + retrocesoVec + new Vector3(0f, -bajadaCambio, 0f);

        Vector3 posManoD = manoDerecha + comun;
        Vector3 posManoI = manoIzquierda + comun + new Vector3(0f, -bajadaRecarga, -bajadaRecarga * 0.5f);

        PonerBrazo(derecho, hombroDerecho + comun * 0.3f, posManoD);
        PonerBrazo(izquierdo, hombroIzquierdo + comun * 0.3f, posManoI);
    }

    void PonerBrazo(Brazo b, Vector3 hombro, Vector3 mano)
    {
        Vector3 dir = mano - hombro;
        float largo = dir.magnitude;
        if (largo < 0.001f) return;

        b.manga.localPosition = (hombro + mano) * 0.5f;
        b.manga.localRotation = Quaternion.FromToRotation(Vector3.up, dir / largo);
        b.manga.localScale = new Vector3(grosorBrazo, largo * 0.5f, grosorBrazo);
        b.mano.localPosition = mano;
    }
}