using UnityEngine;
using System.Collections;
using TMPro;

[System.Serializable]
public class DatosArma
{
    public string nombre = "Pistola";
    public GameObject modelo;
    public float danio = 1f;
    public float cadencia = 0.3f;
    public int tamanoCargador = 12;
    public int municionReserva = 48;
    public float tiempoRecarga = 1.2f;
    public bool automatica = false;
    public int proyectiles = 1;
    public float dispersion = 0f;
    public AudioClip sonidoDisparo;
    [HideInInspector] public int balasEnCargador;
    [HideInInspector] public Vector3 posicionBase;
}

public class Disparo : MonoBehaviour
{
    public DatosArma[] armas;
    public TMP_Text textoMunicion;
    public float alcance = 100f;
    public float retroceso = 0.06f;

    [Header("Balas visibles")]
    public Color colorBala = new Color(1f, 0.85f, 0.3f, 1f);
    public float velocidadBala = 200f;
    public float largoBala = 2f;
    public float grosorBala = 0.06f;

    int actual = 0;
    float proximoDisparo = 0f;
    bool recargando = false;
    Coroutine rutinaRecarga;

    AudioSource fuente;
    AudioClip sonidoRecarga;
    AudioClip sonidoVacio;
    Light luzDestello;
    Material materialBala;

    void Awake()
    {
        // Si la lista esta vacia, crea las 3 armas automaticamente
        if (armas == null || armas.Length == 0)
        {
            armas = new DatosArma[3];

            armas[0] = new DatosArma();
            armas[0].nombre = "Pistola";
            armas[0].danio = 1.5f;
            armas[0].cadencia = 0.35f;
            armas[0].tamanoCargador = 12;
            armas[0].municionReserva = 48;
            armas[0].tiempoRecarga = 1.2f;
            armas[0].automatica = false;
            armas[0].proyectiles = 1;
            armas[0].dispersion = 0f;

            armas[1] = new DatosArma();
            armas[1].nombre = "Rifle";
            armas[1].danio = 1f;
            armas[1].cadencia = 0.1f;
            armas[1].tamanoCargador = 30;
            armas[1].municionReserva = 120;
            armas[1].tiempoRecarga = 2f;
            armas[1].automatica = true;
            armas[1].proyectiles = 1;
            armas[1].dispersion = 0.02f;

            armas[2] = new DatosArma();
            armas[2].nombre = "Escopeta";
            armas[2].danio = 1f;
            armas[2].cadencia = 0.9f;
            armas[2].tamanoCargador = 6;
            armas[2].municionReserva = 24;
            armas[2].tiempoRecarga = 2.2f;
            armas[2].automatica = false;
            armas[2].proyectiles = 8;
            armas[2].dispersion = 0.12f;
        }

        // Busca los cubos de las armas por nombre si no estan conectados
        string[] nombresModelos = { "ModeloPistola", "ModeloRifle", "ModeloEscopeta" };
        for (int i = 0; i < armas.Length && i < nombresModelos.Length; i++)
        {
            if (armas[i].modelo == null)
            {
                Transform t = transform.Find(nombresModelos[i]);
                if (t != null) armas[i].modelo = t.gameObject;
            }
            if (armas[i].modelo != null)
            {
                armas[i].posicionBase = armas[i].modelo.transform.localPosition;
            }
        }

        // Busca el texto de municion si no esta conectado
        if (textoMunicion == null)
        {
            GameObject obj = GameObject.Find("TextoMunicion");
            if (obj != null) textoMunicion = obj.GetComponent<TMP_Text>();
        }

        // Prepara el audio
        fuente = GetComponent<AudioSource>();
        if (fuente == null) fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;

        // Carga los sonidos desde Assets/Resources/Sonidos
        string[] nombresSonidos = { "disparo_pistola", "disparo_rifle", "disparo_escopeta" };
        for (int i = 0; i < armas.Length && i < nombresSonidos.Length; i++)
        {
            if (armas[i].sonidoDisparo == null)
            {
                armas[i].sonidoDisparo = Resources.Load<AudioClip>("Sonidos/" + nombresSonidos[i]);
            }
        }
        sonidoRecarga = Resources.Load<AudioClip>("Sonidos/recarga");
        sonidoVacio = Resources.Load<AudioClip>("Sonidos/vacio");

        // Material de las balas: usa Resources/Materiales/Bala si existe
        materialBala = Resources.Load<Material>("Materiales/Bala");
        if (materialBala == null)
        {
            materialBala = new Material(Shader.Find("Sprites/Default"));
        }

        // Crea la luz del destello
        GameObject objLuz = new GameObject("DestelloLuz");
        objLuz.transform.SetParent(transform);
        objLuz.transform.localPosition = new Vector3(0.3f, -0.2f, 1f);
        luzDestello = objLuz.AddComponent<Light>();
        luzDestello.type = LightType.Point;
        luzDestello.color = new Color(1f, 0.8f, 0.4f);
        luzDestello.intensity = 5f;
        luzDestello.range = 8f;
        luzDestello.enabled = false;

        foreach (DatosArma a in armas) a.balasEnCargador = a.tamanoCargador;
    }

    void Start()
    {
        EquiparArma(0);
    }

    void Update()
    {
        if (armas == null || armas.Length == 0) return;

        if (Input.GetKeyDown(KeyCode.Alpha1) && armas.Length > 0) EquiparArma(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) && armas.Length > 1) EquiparArma(1);
        if (Input.GetKeyDown(KeyCode.Alpha3) && armas.Length > 2) EquiparArma(2);

        if (!recargando)
        {
            DatosArma arma = armas[actual];

            if (Input.GetKeyDown(KeyCode.R))
            {
                Recargar();
            }
            else
            {
                bool apretando = arma.automatica ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");
                if (apretando && Time.time >= proximoDisparo)
                {
                    if (arma.balasEnCargador > 0)
                    {
                        Disparar(arma);
                    }
                    else if (arma.municionReserva > 0)
                    {
                        Recargar();
                    }
                    else
                    {
                        proximoDisparo = Time.time + 0.4f;
                        ReproducirSonido(sonidoVacio);
                    }
                }
            }
        }

        // El arma vuelve suavemente a su posicion despues del retroceso
        DatosArma actualArma = armas[actual];
        if (actualArma.modelo != null)
        {
            Transform m = actualArma.modelo.transform;
            m.localPosition = Vector3.Lerp(m.localPosition, actualArma.posicionBase, Time.deltaTime * 15f);
        }

        ActualizarTexto();
    }

    void Disparar(DatosArma arma)
    {
        proximoDisparo = Time.time + arma.cadencia;
        arma.balasEnCargador--;

        ReproducirSonido(arma.sonidoDisparo);
        StartCoroutine(Destello());

        if (arma.modelo != null)
        {
            arma.modelo.transform.localPosition = arma.posicionBase - new Vector3(0f, 0f, retroceso);
        }

        // Punto desde donde sale la bala visible (la punta del arma)
        Vector3 boca = transform.position + transform.forward * 0.5f;
        if (arma.modelo != null)
        {
            boca = arma.modelo.transform.position + transform.forward * (arma.modelo.transform.localScale.z * 0.5f);
        }

        for (int i = 0; i < arma.proyectiles; i++)
        {
            Vector3 direccion = transform.forward;
            direccion += transform.right * Random.Range(-arma.dispersion, arma.dispersion);
            direccion += transform.up * Random.Range(-arma.dispersion, arma.dispersion);
            direccion = direccion.normalized;

            Vector3 destino = transform.position + direccion * alcance;

            RaycastHit golpe;
            if (Physics.Raycast(transform.position, direccion, out golpe, alcance))
            {
                destino = golpe.point;
                CrearImpacto(golpe.point);

                Slime slime = golpe.collider.GetComponent<Slime>();
                if (slime != null) slime.RecibirDanio(arma.danio);
            }

            CrearBala(boca, destino);
        }
    }

    void CrearBala(Vector3 inicio, Vector3 fin)
    {
        GameObject obj = new GameObject("Bala");
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = grosorBala;
        lr.endWidth = 0f;
        lr.material = materialBala;
        lr.startColor = colorBala;
        lr.endColor = new Color(colorBala.r, colorBala.g, colorBala.b, 0f);
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.SetPosition(0, inicio);
        lr.SetPosition(1, inicio);

        Destroy(obj, 2f);
        StartCoroutine(MoverBala(obj, lr, inicio, fin));
    }

    IEnumerator MoverBala(GameObject obj, LineRenderer lr, Vector3 inicio, Vector3 fin)
    {
        float distanciaTotal = Vector3.Distance(inicio, fin);
        Vector3 dir = (fin - inicio).normalized;
        float recorrido = 0f;

        while (recorrido < distanciaTotal + largoBala)
        {
            if (lr == null) yield break;

            recorrido += velocidadBala * Time.deltaTime;
            float cabeza = Mathf.Min(recorrido, distanciaTotal);
            float cola = Mathf.Max(0f, recorrido - largoBala);

            lr.SetPosition(0, inicio + dir * cabeza);
            lr.SetPosition(1, inicio + dir * cola);
            yield return null;
        }

        if (obj != null) Destroy(obj);
    }

    void CrearImpacto(Vector3 punto)
    {
        GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(esfera.GetComponent<Collider>());
        esfera.transform.position = punto;
        esfera.transform.localScale = Vector3.one * 0.15f;

        Renderer r = esfera.GetComponent<Renderer>();
        r.material = materialBala;
        r.material.color = colorBala;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Destroy(esfera, 0.1f);
    }

    void ReproducirSonido(AudioClip clip)
    {
        if (clip == null) return;
        fuente.pitch = Random.Range(0.95f, 1.05f);
        fuente.PlayOneShot(clip);
    }

    IEnumerator Destello()
    {
        luzDestello.enabled = true;
        yield return new WaitForSeconds(0.05f);
        luzDestello.enabled = false;
    }

    void Recargar()
    {
        DatosArma arma = armas[actual];
        if (arma.balasEnCargador >= arma.tamanoCargador || arma.municionReserva <= 0) return;
        rutinaRecarga = StartCoroutine(RutinaRecarga(arma));
    }

    IEnumerator RutinaRecarga(DatosArma arma)
    {
        recargando = true;
        ReproducirSonido(sonidoRecarga);
        yield return new WaitForSeconds(arma.tiempoRecarga);

        int necesarias = arma.tamanoCargador - arma.balasEnCargador;
        int cargadas = Mathf.Min(necesarias, arma.municionReserva);
        arma.balasEnCargador += cargadas;
        arma.municionReserva -= cargadas;
        recargando = false;
    }

    void EquiparArma(int indice)
    {
        if (rutinaRecarga != null) StopCoroutine(rutinaRecarga);
        recargando = false;
        if (luzDestello != null) luzDestello.enabled = false;
        actual = indice;

        for (int i = 0; i < armas.Length; i++)
        {
            if (armas[i].modelo != null) armas[i].modelo.SetActive(i == indice);
        }
    }

    public void RellenarMunicion()
    {
        foreach (DatosArma a in armas)
        {
            a.balasEnCargador = a.tamanoCargador;
            a.municionReserva += a.tamanoCargador * 2;
        }
    }

    void ActualizarTexto()
    {
        if (textoMunicion == null) return;
        DatosArma a = armas[actual];

        if (recargando) textoMunicion.text = a.nombre + "\nRecargando...";
        else textoMunicion.text = a.nombre + "\n" + a.balasEnCargador + " / " + a.municionReserva;
    }
}