using UnityEngine;
using UnityEngine.AI;

public class Slime : MonoBehaviour
{
    public float vida = 3f;
    public float danioAtaque = 10f;
    public float distanciaAtaque = 1.8f;
    public float tiempoEntreAtaques = 1f;

    [Header("Animacion de salto")]
    public float velocidadSalto = 5f;
    public float alturaSalto = 0.9f;
    public float duracionSalto = 0.55f;
    public float descansoMin = 0.3f;
    public float descansoMax = 0.7f;
    public bool crearOjos = true;

    enum Fase { Reposo, Agacharse, Saltando, Aterrizar }

    NavMeshAgent agente;
    Transform jugador;
    float proximoAtaque = 0f;

    Fase fase = Fase.Reposo;
    float tiempoFase = 0f;
    float duracionReposo = 0.5f;
    float duracionAgacharse = 0.15f;
    float duracionAterrizar = 0.15f;
    float desfase = 0f;
    Vector3 escalaBase;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        agente.speed = velocidadSalto;
        agente.acceleration = 60f;
        agente.angularSpeed = 360f;
        agente.isStopped = true;

        escalaBase = transform.localScale;
        desfase = Random.Range(0f, 6.28f);
        duracionReposo = Random.Range(0f, descansoMax);

        JugadorFPS j = FindFirstObjectByType<JugadorFPS>();
        if (j != null) jugador = j.transform;

        if (crearOjos) CrearOjos();
    }

    void Update()
    {
        if (jugador == null) return;

        agente.SetDestination(jugador.position);
        ActualizarSalto();
        Atacar();
    }

    void Atacar()
    {
        Vector3 dif = jugador.position - transform.position;
        dif.y = 0f;

        if (dif.magnitude <= distanciaAtaque && Time.time >= proximoAtaque)
        {
            VidaJugador v = jugador.GetComponent<VidaJugador>();
            if (v != null) v.RecibirDanio(danioAtaque);
            proximoAtaque = Time.time + tiempoEntreAtaques;
        }
    }

    void ActualizarSalto()
    {
        tiempoFase += Time.deltaTime;
        float altura = 0f;
        Vector3 escala = Vector3.one;

        switch (fase)
        {
            case Fase.Reposo:
            {
                // Respira suavemente mientras descansa
                float respiro = Mathf.Sin(Time.time * 5f + desfase) * 0.04f;
                escala = new Vector3(1f - respiro, 1f + respiro, 1f - respiro);
                if (tiempoFase >= duracionReposo) CambiarFase(Fase.Agacharse);
                break;
            }
            case Fase.Agacharse:
            {
                // Se aplasta para tomar impulso
                float t = Mathf.Clamp01(tiempoFase / duracionAgacharse);
                float y = Mathf.Lerp(1f, 0.65f, t);
                float xz = Mathf.Lerp(1f, 1.2f, t);
                escala = new Vector3(xz, y, xz);
                if (t >= 1f) CambiarFase(Fase.Saltando);
                break;
            }
            case Fase.Saltando:
            {
                // En el aire: estirado y avanzando
                float t = Mathf.Clamp01(tiempoFase / duracionSalto);
                altura = Mathf.Sin(t * Mathf.PI) * alturaSalto;
                float estiramiento = Mathf.Lerp(1.3f, 0.95f, t);
                float ancho = 1f / Mathf.Sqrt(estiramiento);
                escala = new Vector3(ancho, estiramiento, ancho);
                if (t >= 1f) CambiarFase(Fase.Aterrizar);
                break;
            }
            case Fase.Aterrizar:
            {
                // Cae aplastado y se recupera
                float t = Mathf.Clamp01(tiempoFase / duracionAterrizar);
                float suave = Mathf.SmoothStep(0f, 1f, t);
                float y = Mathf.Lerp(0.6f, 1f, suave);
                float xz = Mathf.Lerp(1.3f, 1f, suave);
                escala = new Vector3(xz, y, xz);
                if (t >= 1f) CambiarFase(Fase.Reposo);
                break;
            }
        }

        transform.localScale = Vector3.Scale(escalaBase, escala);
        agente.baseOffset = 0.5f * transform.localScale.y + altura;
    }

    void CambiarFase(Fase nueva)
    {
        fase = nueva;
        tiempoFase = 0f;

        if (nueva == Fase.Reposo)
        {
            duracionReposo = Random.Range(descansoMin, descansoMax);
        }

        // Solo avanza mientras esta en el aire
        agente.isStopped = (nueva != Fase.Saltando);
    }

    void CrearOjos()
    {
        for (int lado = -1; lado <= 1; lado += 2)
        {
            GameObject ojo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(ojo.GetComponent<Collider>());
            ojo.name = "Ojo";
            ojo.transform.SetParent(transform, false);
            ojo.transform.localPosition = new Vector3(0.2f * lado, 0.12f, 0.42f);
            ojo.transform.localScale = new Vector3(0.2f, 0.25f, 0.2f);
            ojo.GetComponent<Renderer>().material.color = Color.white;

            GameObject pupila = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(pupila.GetComponent<Collider>());
            pupila.name = "Pupila";
            pupila.transform.SetParent(ojo.transform, false);
            pupila.transform.localPosition = new Vector3(0f, 0f, 0.4f);
            pupila.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            pupila.GetComponent<Renderer>().material.color = Color.black;
        }
    }

    public void RecibirDanio(float cantidad)
    {
        vida -= cantidad;
        if (vida <= 0)
        {
            Puntaje.puntos += 10;
            Destroy(gameObject);
        }
    }
}