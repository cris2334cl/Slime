using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuPausa : MonoBehaviour
{
    static MenuPausa instancia;

    GameObject fondo;
    GameObject panelPrincipal;
    GameObject panelOpciones;
    TMP_Text txtFps;
    Button botonCalidad;
    Button botonPantalla;
    Button botonFps;
    Slider sVolumen;
    Slider sSens;
    Slider sFov;

    bool pausado = false;
    bool mostrarFps = false;
    bool pantallaCompleta = true;
    float fpsSuavizado = 60f;
    JugadorFPS jug;
    Disparo disp;

    // Se crea solo al iniciar el juego, no hay que ponerlo en ningun objeto
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (instancia != null) return;
        GameObject obj = new GameObject("MenuPausa");
        obj.AddComponent<MenuPausa>();
    }

    void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        instancia = this;
        DontDestroyOnLoad(gameObject);

        // Valores iniciales (guardados o actuales)
        jug = FindFirstObjectByType<JugadorFPS>();
        Camera cam = Camera.main;

        float vol = Mathf.Clamp01(PlayerPrefs.GetFloat("vol", 1f));
        float sens = PlayerPrefs.HasKey("sens") ? PlayerPrefs.GetFloat("sens") : (jug != null ? jug.sensibilidad : 2f);
        float fov = PlayerPrefs.HasKey("fov") ? PlayerPrefs.GetFloat("fov") : (cam != null ? cam.fieldOfView : 60f);
        sens = Mathf.Clamp(sens, 0.5f, 6f);
        fov = Mathf.Clamp(fov, 60f, 110f);
        mostrarFps = PlayerPrefs.GetInt("fps", 0) == 1;
        pantallaCompleta = Screen.fullScreen;

        ConstruirUI(vol, sens, fov);
        AplicarAjustes();

        SceneManager.sceneLoaded += AlCargarEscena;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        pausado = false;
        fondo.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        jug = null;
        disp = null;
        AplicarAjustes();
    }

    // ---------------------------------------------------------------
    // Construccion del menu
    // ---------------------------------------------------------------

    void ConstruirUI(float vol, float sens, float fov)
    {
        GameObject objCanvas = new GameObject("CanvasPausa");
        objCanvas.transform.SetParent(transform, false);
        Canvas canvas = objCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler escalador = objCanvas.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920, 1080);
        escalador.matchWidthOrHeight = 0.5f;
        objCanvas.AddComponent<GraphicRaycaster>();

        // Contador de FPS (arriba a la derecha)
        txtFps = CrearTexto(objCanvas.transform, "TextoFPS", "", 28, TextAlignmentOptions.TopRight);
        RectTransform rFps = txtFps.rectTransform;
        rFps.anchorMin = new Vector2(1, 1);
        rFps.anchorMax = new Vector2(1, 1);
        rFps.pivot = new Vector2(1, 1);
        rFps.anchoredPosition = new Vector2(-20, -20);
        rFps.sizeDelta = new Vector2(300, 50);

        // Fondo oscuro que cubre la pantalla
        fondo = CrearPanel(objCanvas.transform, "Fondo", new Color(0f, 0f, 0f, 0.75f));
        Estirar(fondo.GetComponent<RectTransform>());

        // ----- Panel principal -----
        panelPrincipal = CrearColumna(fondo.transform, "PanelPrincipal");
        TMP_Text titulo = CrearTexto(panelPrincipal.transform, "Titulo", "PAUSA", 80, TextAlignmentOptions.Center);
        FijarTamano(titulo.gameObject, 500, 110);
        CrearBoton(panelPrincipal.transform, "Continuar", Reanudar);
        CrearBoton(panelPrincipal.transform, "Opciones", MostrarOpciones);
        CrearBoton(panelPrincipal.transform, "Reiniciar", Reiniciar);
        CrearBoton(panelPrincipal.transform, "Salir del juego", Salir);

        // ----- Panel de opciones -----
        panelOpciones = CrearColumna(fondo.transform, "PanelOpciones");
        TMP_Text tituloOp = CrearTexto(panelOpciones.transform, "Titulo", "OPCIONES", 64, TextAlignmentOptions.Center);
        FijarTamano(tituloOp.gameObject, 500, 80);

        botonCalidad = CrearBoton(panelOpciones.transform, "", CambiarCalidad);
        botonPantalla = CrearBoton(panelOpciones.transform, "", CambiarPantalla);
        botonFps = CrearBoton(panelOpciones.transform, "", CambiarFps);

        sVolumen = CrearSlider(panelOpciones.transform, "Volumen", 0f, 100f, vol * 100f, "0", v =>
        {
            AudioListener.volume = v / 100f;
            PlayerPrefs.SetFloat("vol", v / 100f);
        });

        sSens = CrearSlider(panelOpciones.transform, "Sensibilidad del mouse", 0.5f, 6f, sens, "0.0", v =>
        {
            PlayerPrefs.SetFloat("sens", v);
            AplicarAjustes();
        });

        sFov = CrearSlider(panelOpciones.transform, "Campo de visión", 60f, 110f, fov, "0", v =>
        {
            PlayerPrefs.SetFloat("fov", v);
            AplicarAjustes();
        });

        CrearBoton(panelOpciones.transform, "Volver", MostrarPrincipal);

        ActualizarTextos();
        panelPrincipal.SetActive(true);
        panelOpciones.SetActive(false);
        fondo.SetActive(false);
    }

    GameObject CrearPanel(Transform padre, string nombre, Color color)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(padre, false);
        obj.GetComponent<Image>().color = color;
        return obj;
    }

    void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    GameObject CrearColumna(Transform padre, string nombre)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform));
        obj.transform.SetParent(padre, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(600, 900);

        VerticalLayoutGroup vl = obj.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 14;
        vl.childAlignment = TextAnchor.MiddleCenter;
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = false;
        vl.childForceExpandHeight = false;
        return obj;
    }

    void FijarTamano(GameObject obj, float ancho, float alto)
    {
        LayoutElement le = obj.GetComponent<LayoutElement>();
        if (le == null) le = obj.AddComponent<LayoutElement>();
        le.preferredWidth = ancho;
        le.preferredHeight = alto;
    }

    TMP_Text CrearTexto(Transform padre, string nombre, string texto, float tamano, TextAlignmentOptions alineacion)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform));
        obj.transform.SetParent(padre, false);
        TextMeshProUGUI t = obj.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.fontSize = tamano;
        t.alignment = alineacion;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    Button CrearBoton(Transform padre, string texto, UnityAction accion)
    {
        GameObject obj = new GameObject("Boton " + texto, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(padre, false);

        Image img = obj.GetComponent<Image>();
        img.color = Color.white;

        Button b = obj.GetComponent<Button>();
        b.targetGraphic = img;
        ColorBlock c = b.colors;
        c.normalColor = new Color(0.22f, 0.26f, 0.32f, 0.95f);
        c.highlightedColor = new Color(0.3f, 0.5f, 0.85f, 1f);
        c.pressedColor = new Color(0.2f, 0.4f, 0.75f, 1f);
        c.selectedColor = new Color(0.22f, 0.26f, 0.32f, 0.95f);
        c.fadeDuration = 0.1f;
        b.colors = c;
        b.onClick.AddListener(accion);

        TMP_Text et = CrearTexto(obj.transform, "Texto", texto, 28, TextAlignmentOptions.Center);
        Estirar(et.rectTransform);

        FijarTamano(obj, 420, 52);
        return b;
    }

    void PonerTextoBoton(Button b, string texto)
    {
        TMP_Text t = b.GetComponentInChildren<TMP_Text>();
        if (t != null) t.text = texto;
    }

    Slider CrearSlider(Transform padre, string etiqueta, float min, float max, float valor, string formato, UnityAction<float> alCambiar)
    {
        TMP_Text txt = CrearTexto(padre, "Etiqueta " + etiqueta, "", 26, TextAlignmentOptions.Center);
        FijarTamano(txt.gameObject, 420, 34);

        GameObject raiz = new GameObject("Slider " + etiqueta, typeof(RectTransform));
        raiz.transform.SetParent(padre, false);
        FijarTamano(raiz, 420, 28);

        // Barra de fondo
        GameObject barra = CrearPanel(raiz.transform, "Barra", new Color(1f, 1f, 1f, 0.25f));
        RectTransform rBarra = barra.GetComponent<RectTransform>();
        rBarra.anchorMin = new Vector2(0f, 0.35f);
        rBarra.anchorMax = new Vector2(1f, 0.65f);
        rBarra.offsetMin = Vector2.zero;
        rBarra.offsetMax = Vector2.zero;

        // Relleno
        GameObject areaRelleno = new GameObject("AreaRelleno", typeof(RectTransform));
        areaRelleno.transform.SetParent(raiz.transform, false);
        RectTransform rArea = areaRelleno.GetComponent<RectTransform>();
        rArea.anchorMin = new Vector2(0f, 0.35f);
        rArea.anchorMax = new Vector2(1f, 0.65f);
        rArea.offsetMin = new Vector2(5f, 0f);
        rArea.offsetMax = new Vector2(-15f, 0f);

        GameObject relleno = CrearPanel(areaRelleno.transform, "Relleno", new Color(0.3f, 0.7f, 1f, 1f));
        RectTransform rRelleno = relleno.GetComponent<RectTransform>();
        rRelleno.anchorMin = Vector2.zero;
        rRelleno.anchorMax = new Vector2(0f, 1f);
        rRelleno.sizeDelta = new Vector2(10f, 0f);

        // Asa
        GameObject areaAsa = new GameObject("AreaAsa", typeof(RectTransform));
        areaAsa.transform.SetParent(raiz.transform, false);
        RectTransform rAreaAsa = areaAsa.GetComponent<RectTransform>();
        rAreaAsa.anchorMin = Vector2.zero;
        rAreaAsa.anchorMax = Vector2.one;
        rAreaAsa.offsetMin = new Vector2(10f, 0f);
        rAreaAsa.offsetMax = new Vector2(-10f, 0f);

        GameObject asa = CrearPanel(areaAsa.transform, "Asa", Color.white);
        RectTransform rAsa = asa.GetComponent<RectTransform>();
        rAsa.sizeDelta = new Vector2(20f, 0f);

        Slider s = raiz.AddComponent<Slider>();
        s.fillRect = rRelleno;
        s.handleRect = rAsa;
        s.targetGraphic = asa.GetComponent<Image>();
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = min;
        s.maxValue = max;
        s.wholeNumbers = (formato == "0");
        s.value = valor;

        txt.text = etiqueta + ": " + valor.ToString(formato);
        s.onValueChanged.AddListener(v =>
        {
            txt.text = etiqueta + ": " + v.ToString(formato);
            alCambiar(v);
        });
        return s;
    }

    // ---------------------------------------------------------------
    // Logica
    // ---------------------------------------------------------------

    void Update()
    {
        if (mostrarFps)
        {
            float actual = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            fpsSuavizado = Mathf.Lerp(fpsSuavizado, actual, 0.1f);
            txtFps.text = Mathf.RoundToInt(fpsSuavizado) + " FPS";
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (pausado)
            {
                if (panelOpciones.activeSelf) MostrarPrincipal();
                else Reanudar();
            }
            else if (PuedePausar())
            {
                Pausar();
            }
        }
    }

    bool PuedePausar()
    {
        if (Time.timeScale == 0f) return false;
        VidaJugador v = FindFirstObjectByType<VidaJugador>();
        return v == null || v.vida > 0f;
    }

    void Pausar()
    {
        pausado = true;

        jug = FindFirstObjectByType<JugadorFPS>();
        disp = FindFirstObjectByType<Disparo>();
        if (jug != null) jug.enabled = false;
        if (disp != null) disp.enabled = false;

        AsegurarEventSystem();

        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        fondo.SetActive(true);
        MostrarPrincipal();
    }

    void Reanudar()
    {
        pausado = false;
        fondo.SetActive(false);

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (jug != null) jug.enabled = true;
        if (disp != null) disp.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void MostrarPrincipal()
    {
        panelOpciones.SetActive(false);
        panelPrincipal.SetActive(true);
    }

    void MostrarOpciones()
    {
        ActualizarTextos();
        panelPrincipal.SetActive(false);
        panelOpciones.SetActive(true);
    }

    void Reiniciar()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void CambiarCalidad()
    {
        int total = QualitySettings.names.Length;
        int siguiente = (QualitySettings.GetQualityLevel() + 1) % total;
        QualitySettings.SetQualityLevel(siguiente, true);
        ActualizarTextos();
    }

    void CambiarPantalla()
    {
        pantallaCompleta = !pantallaCompleta;
        Screen.fullScreen = pantallaCompleta;
        ActualizarTextos();
    }

    void CambiarFps()
    {
        mostrarFps = !mostrarFps;
        PlayerPrefs.SetInt("fps", mostrarFps ? 1 : 0);
        if (!mostrarFps) txtFps.text = "";
        ActualizarTextos();
    }

    void ActualizarTextos()
    {
        PonerTextoBoton(botonCalidad, "Calidad gráfica: " + QualitySettings.names[QualitySettings.GetQualityLevel()]);
        PonerTextoBoton(botonPantalla, "Pantalla completa: " + (pantallaCompleta ? "Sí" : "No"));
        PonerTextoBoton(botonFps, "Mostrar FPS: " + (mostrarFps ? "Sí" : "No"));
    }

    void AplicarAjustes()
    {
        AudioListener.volume = sVolumen.value / 100f;

        JugadorFPS j = FindFirstObjectByType<JugadorFPS>();
        if (j != null) j.sensibilidad = sSens.value;

        Camera cam = Camera.main;
        if (cam != null) cam.fieldOfView = sFov.value;
    }

    void AsegurarEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }
}