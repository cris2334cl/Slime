using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class VidaJugador : MonoBehaviour
{
    public float vida = 100f;

    [Header("Game Over (opcional, puedes dejarlos vacios)")]
    public GameObject panelGameOver;
    public TMP_Text textoFinal;

    [Header("Efecto de dano")]
    public Color colorDano = new Color(0.9f, 0f, 0f, 1f);
    public float intensidadFlash = 1f;
    public float duracionFlash = 0.6f;
    public float intensidadTemblor = 0.08f;
    public float duracionTemblor = 0.25f;
    public float vidaBaja = 30f;

    bool muerto = false;
    Image vineta;
    Image tinte;
    float alphaFlash = 0f;
    Transform camaraTransform;
    Vector3 posicionCamaraBase;
    Coroutine rutinaTemblor;
    AudioSource fuente;
    AudioClip sonidoDano;

    void Start()
    {
        // Crea el canvas del efecto
        GameObject objCanvas = new GameObject("CanvasDano");
        Canvas canvas = objCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        // Tinte rojo suave sobre toda la pantalla
        tinte = CrearImagen(objCanvas.transform, "TinteRojo", null);

        // Viñeta: bordes rojos, centro transparente
        vineta = CrearImagen(objCanvas.transform, "VinetaRoja", CrearSpriteVineta());

        // Busca la camara para el temblor
        Camera cam = GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            camaraTransform = cam.transform;
            posicionCamaraBase = camaraTransform.localPosition;
        }

        // Audio del golpe (opcional: Assets/Resources/Sonidos/dano)
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        sonidoDano = Resources.Load<AudioClip>("Sonidos/dano");

        PintarEfecto(0f);
    }

    Image CrearImagen(Transform padre, string nombre, Sprite sprite)
    {
        GameObject obj = new GameObject(nombre);
        obj.transform.SetParent(padre, false);
        Image img = obj.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return img;
    }

    Sprite CrearSpriteVineta()
    {
        int tam = 128;
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float dx = (x / (tam - 1f) - 0.5f) * 2f;
                float dy = (y / (tam - 1f) - 0.5f) * 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((d - 0.35f) / 0.9f);
                a = a * a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f));
    }

    void PintarEfecto(float alpha)
    {
        if (vineta == null || tinte == null) return;

        vineta.color = new Color(colorDano.r, colorDano.g, colorDano.b, alpha);
        tinte.color = new Color(colorDano.r, colorDano.g, colorDano.b, alpha * 0.25f);

        // Pequeno "latido": la viñeta crece un poco al recibir el golpe
        float escala = 1f + alpha * 0.08f;
        vineta.rectTransform.localScale = new Vector3(escala, escala, 1f);
    }

    void Update()
    {
        if (muerto) return;

        // El destello se va apagando solo
        alphaFlash = Mathf.MoveTowards(alphaFlash, 0f, Time.deltaTime * intensidadFlash / duracionFlash);

        // Con poca vida, los bordes laten en rojo suave
        float alphaBaja = 0f;
        if (vida <= vidaBaja)
        {
            float pulso = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
            alphaBaja = Mathf.Lerp(0.15f, 0.45f, pulso);
        }

        PintarEfecto(Mathf.Max(alphaFlash, alphaBaja));
    }

    public void RecibirDanio(float cantidad)
    {
        if (muerto) return;

        vida -= cantidad;

        if (vida <= 0)
        {
            vida = 0;
            Morir();
            return;
        }

        alphaFlash = intensidadFlash;

        if (sonidoDano != null)
        {
            fuente.pitch = Random.Range(0.95f, 1.05f);
            fuente.PlayOneShot(sonidoDano);
        }

        if (camaraTransform != null)
        {
            if (rutinaTemblor != null) StopCoroutine(rutinaTemblor);
            rutinaTemblor = StartCoroutine(Temblor());
        }
    }

    IEnumerator Temblor()
    {
        float t = 0f;
        while (t < duracionTemblor)
        {
            t += Time.deltaTime;
            float fuerza = intensidadTemblor * (1f - t / duracionTemblor);
            camaraTransform.localPosition = posicionCamaraBase + Random.insideUnitSphere * fuerza;
            yield return null;
        }
        camaraTransform.localPosition = posicionCamaraBase;
        rutinaTemblor = null;
    }

    void Morir()
    {
        muerto = true;

        if (rutinaTemblor != null) StopCoroutine(rutinaTemblor);
        if (camaraTransform != null) camaraTransform.localPosition = posicionCamaraBase;
        PintarEfecto(0f);

        // Si tienes panel de Game Over, lo muestra. Si no, reinicia la partida.
        if (panelGameOver != null)
        {
            JugadorFPS mov = GetComponent<JugadorFPS>();
            if (mov != null) mov.enabled = false;

            Disparo disparo = GetComponentInChildren<Disparo>();
            if (disparo != null) disparo.enabled = false;

            if (textoFinal != null) textoFinal.text = "GAME OVER\nPuntos: " + Puntaje.puntos;
            panelGameOver.SetActive(true);

            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Reiniciar();
        }
    }

    public void Reiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}