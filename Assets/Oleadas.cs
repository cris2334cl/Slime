using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class Oleadas : MonoBehaviour
{
    public GameObject prefabSlime;
    public Transform[] puntosSpawn;
    public TMP_Text textoInfo;
    public float pausaEntreOleadas = 4f;

    int oleada = 0;
    bool esperando = false;
    float tiempoSiguiente = 0f;

    void Start()
    {
        Puntaje.puntos = 0;
        IniciarOleada();
    }

    void Update()
    {
        int vivos = FindObjectsByType<Slime>(FindObjectsSortMode.None).Length;

        if (!esperando && vivos == 0)
        {
            esperando = true;
            tiempoSiguiente = Time.time + pausaEntreOleadas;
        }

        if (esperando && Time.time >= tiempoSiguiente)
        {
            IniciarOleada();
        }

        if (textoInfo != null)
        {
            VidaJugador v = FindFirstObjectByType<VidaJugador>();
            int vida = 0;
            if (v != null) vida = Mathf.Max(0, Mathf.RoundToInt(v.vida));

            textoInfo.text = "Oleada: " + oleada + "\nSlimes: " + vivos + "\nVida: " + vida + "\nPuntos: " + Puntaje.puntos;
        }
    }

    void IniciarOleada()
    {
        esperando = false;
        oleada++;

        if (oleada > 1)
        {
            Disparo d = FindFirstObjectByType<Disparo>();
            if (d != null) d.RellenarMunicion();
        }

        int cantidad = 3 + oleada * 2;

        for (int i = 0; i < cantidad; i++)
        {
            Transform punto = puntosSpawn[Random.Range(0, puntosSpawn.Length)];
            Vector3 desfase = new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
            Vector3 deseada = punto.position + desfase;

            NavMeshHit golpe;
            if (NavMesh.SamplePosition(deseada, out golpe, 5f, NavMesh.AllAreas))
            {
                Instantiate(prefabSlime, golpe.position, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning("Spawn fuera del NavMesh: " + deseada);
            }
        }
    }
}