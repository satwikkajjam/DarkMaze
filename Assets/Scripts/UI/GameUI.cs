using UnityEngine;

/// <summary>
/// Full-screen UI rendered via OnGUI for zero-dependency HUD.
/// Shows trinket count, stamina, flashlight battery, heartbeat indicator,
/// messages, death screen, win screen, and pause menu.
/// </summary>
public class GameUI : MonoBehaviour
{
    private GameManager gameManager;
    private TrinketManager trinketManager;
    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private PlayerFlashlight playerFlashlight;
    private Texture2D heartTexture;
    
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;
    private GUIStyle messageStyle;
    private GUIStyle hintStyle;
    private bool stylesInitialized;

    private float damageFlashAlpha;
    private float collectFlashAlpha;

    void Start()
    {
        gameManager = GameManager.Instance;
        trinketManager = FindFirstObjectByType<TrinketManager>();
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerController = playerObj.GetComponent<PlayerController>();
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            playerFlashlight = playerObj.GetComponentInChildren<PlayerFlashlight>();
        }

        if (trinketManager != null)
        {
            trinketManager.OnTrinketPickedUp += (t) => collectFlashAlpha = 1f;
        }

        if (playerHealth != null)
        {
            playerHealth.OnDamageTaken += (d) => damageFlashAlpha = 1f;
        }
    }

    void InitStyles()
    {
        if (stylesInitialized) return;
        stylesInitialized = true;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 48,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            normal = { textColor = new Color(0.8f, 0.8f, 0.9f) }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 24,
            fixedHeight = 50,
            fixedWidth = 250
        };

        messageStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = new Color(1f, 0.9f, 0.3f) }
        };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.5f, 0.5f, 0.6f) }
        };

        if (heartTexture == null)
        {
            heartTexture = new Texture2D(1, 1);
            heartTexture.SetPixel(0, 0, Color.white);
            heartTexture.Apply();
        }
    }

    void Update()
    {
        damageFlashAlpha = Mathf.Max(0, damageFlashAlpha - Time.deltaTime * 2f);
        collectFlashAlpha = Mathf.Max(0, collectFlashAlpha - Time.deltaTime * 3f);
    }

    void OnGUI()
    {
        InitStyles();

        if (gameManager == null) return;

        switch (gameManager.CurrentState)
        {
            case GameManager.GameState.Playing:
                DrawHUD();
                DrawMessages();
                DrawDamageFlash();
                DrawCollectFlash();
                break;
            case GameManager.GameState.Paused:
                DrawHUD();
                DrawPauseMenu();
                break;
            case GameManager.GameState.Dead:
                DrawDeathScreen();
                break;
            case GameManager.GameState.Won:
                DrawWinScreen();
                break;
        }
    }

    void DrawHUD()
    {
        float padding = 20f;
        float barWidth = 200f;
        float barHeight = 12f;

        // Minimap - upper left
        if (MiniMapCameraController.ActiveTexture != null)
        {
            float mapSize = 170f;
            Rect panelRect = new Rect(padding - 10f, padding - 6f, mapSize + 20f, mapSize + 38f);
            DrawPanel(panelRect, new Color(0f, 0f, 0f, 0.45f));
            DrawLabelWithShadow(new Rect(panelRect.x + 10f, panelRect.y + 6f, mapSize, 18f), "Maze Map", new GUIStyle(labelStyle)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.75f, 0.9f, 1f) }
            });

            Rect mapRect = new Rect(panelRect.x + 10f, panelRect.y + 28f, mapSize, mapSize);
            GUI.color = Color.white;
            GUI.DrawTexture(mapRect, MiniMapCameraController.ActiveTexture, ScaleMode.ScaleAndCrop, false);
        }

        // Trinket counter - top center
        if (trinketManager != null)
        {
            Rect trinketPanel = new Rect(Screen.width / 2f - 210, padding - 4f, 420, 68f);
            DrawPanel(trinketPanel, new Color(0f, 0f, 0f, 0.35f));

            GUIStyle trinketStyle = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.4f, 0.95f, 1f) }
            };

            string trinketText = $"Trinkets: {trinketManager.CollectedCount} / {trinketManager.RequiredTrinketsToWin} to win";
            string totalText = $"Total spawned: {trinketManager.TotalTrinkets}";
            DrawLabelWithShadow(new Rect(Screen.width / 2f - 190, padding + 2f, 380, 30), trinketText, trinketStyle);
            DrawLabelWithShadow(new Rect(Screen.width / 2f - 190, padding + 32f, 380, 24), totalText, new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                normal = { textColor = new Color(0.85f, 0.92f, 1f) }
            });
        }

        // Stamina bar - bottom left
        if (playerController != null)
        {
            float y = Screen.height - padding - barHeight - 30;

            DrawPanel(new Rect(padding - 10f, y - 30f, barWidth + 20f, 48f), new Color(0f, 0f, 0f, 0.3f));
            DrawLabelWithShadow(new Rect(padding, y - 20, 120, 20), "Stamina", labelStyle);

            // Background
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            GUI.DrawTexture(new Rect(padding, y, barWidth, barHeight), Texture2D.whiteTexture);
            
            // Fill
            Color staminaColor = playerController.StaminaPercent > 0.3f
                ? new Color(0.2f, 0.8f, 0.2f)
                : new Color(0.8f, 0.2f, 0.2f);
            GUI.color = staminaColor;
            GUI.DrawTexture(new Rect(padding, y, barWidth * playerController.StaminaPercent, barHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Hearts - bottom left above stamina
        if (playerHealth != null)
        {
            float heartY = Screen.height - padding - 150;
            DrawPanel(new Rect(padding - 10f, heartY - 30f, 170f, 48f), new Color(0f, 0f, 0f, 0.3f));
            DrawLabelWithShadow(new Rect(padding, heartY - 20f, 120f, 20f), "Hearts", labelStyle);

            for (int i = 0; i < playerHealth.maxHearts; i++)
            {
                Color heartColor = i < playerHealth.currentHearts ? new Color(1f, 0.3f, 0.35f) : new Color(0.25f, 0.25f, 0.28f);
                GUI.color = heartColor;
                GUI.Label(new Rect(padding + i * 22f, heartY + 2f, 20f, 20f), "♥", new GUIStyle(labelStyle)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = heartColor }
                });
            }

            GUI.color = Color.white;
        }

        // Flashlight battery - bottom left, above stamina
        if (playerFlashlight != null)
        {
            float y = Screen.height - padding - barHeight - 80;

            string flashText = playerFlashlight.isOn ? "Flashlight [ON]" : "Flashlight [OFF] (F)";
            DrawPanel(new Rect(padding - 10f, y - 30f, barWidth + 20f, 48f), new Color(0f, 0f, 0f, 0.3f));
            DrawLabelWithShadow(new Rect(padding, y - 20, 200, 20), flashText, labelStyle);

            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            GUI.DrawTexture(new Rect(padding, y, barWidth, barHeight), Texture2D.whiteTexture);

            GUI.color = new Color(1f, 0.9f, 0.3f);
            GUI.DrawTexture(new Rect(padding, y, barWidth * playerFlashlight.BatteryPercent, barHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Heartbeat indicator - bottom right
        if (playerHealth != null && playerHealth.heartbeatIntensity > 0.1f)
        {
            float pulse = Mathf.Sin(Time.time * (5f + playerHealth.heartbeatIntensity * 15f));
            float size = 20f + pulse * 10f * playerHealth.heartbeatIntensity;
            float alpha = playerHealth.heartbeatIntensity;

            GUI.color = new Color(0.8f, 0.1f, 0.1f, alpha);
            GUI.Label(new Rect(Screen.width - 80, Screen.height - 60, 60, 40),
                "♥", new GUIStyle(labelStyle)
                {
                    fontSize = (int)size,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.9f, 0.1f, 0.1f, alpha) }
                });
            GUI.color = Color.white;
        }

        // Interaction hint
        DrawInteractionHint();

        // Crosshair
        float crosshairSize = 4f;
        GUI.color = new Color(1f, 1f, 1f, 0.5f);
        GUI.DrawTexture(new Rect(Screen.width / 2f - crosshairSize / 2f, Screen.height / 2f - crosshairSize / 2f,
            crosshairSize, crosshairSize), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Crouch indicator
        if (playerController != null && playerController.IsCrouching)
        {
            DrawLabelWithShadow(new Rect(padding, Screen.height - padding - 130, 200, 20),
                "CROUCHING", new GUIStyle(labelStyle)
                {
                    normal = { textColor = new Color(0.5f, 0.8f, 0.5f) }
                });
        }

        // Sprint indicator  
        if (playerController != null && playerController.IsSprinting)
        {
            DrawLabelWithShadow(new Rect(padding, Screen.height - padding - 130, 200, 20),
                "SPRINTING", new GUIStyle(labelStyle)
                {
                    normal = { textColor = new Color(1f, 0.6f, 0.2f) }
                });
        }

        // Time
        if (gameManager != null)
        {
            int minutes = (int)(gameManager.playTime / 60f);
            int seconds = (int)(gameManager.playTime % 60f);
            DrawPanel(new Rect(Screen.width - 130, padding - 2f, 110, 30f), new Color(0f, 0f, 0f, 0.25f));
            DrawLabelWithShadow(new Rect(Screen.width - 120, padding, 100, 30),
                $"{minutes:00}:{seconds:00}", new GUIStyle(labelStyle)
                {
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 16
                });
        }
    }

    void DrawPanel(Rect rect, Color color)
    {
        Color prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = prev;
    }

    void DrawLabelWithShadow(Rect rect, string text, GUIStyle style)
    {
        GUIStyle shadowStyle = new GUIStyle(style);
        shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, shadowStyle);
        GUI.Label(rect, text, style);
    }

    void DrawInteractionHint()
    {
        // Check for nearby trinkets
        Trinket[] trinkets = FindObjectsByType<Trinket>(FindObjectsSortMode.None);
        Transform player = playerController?.transform;
        if (player == null) return;

        foreach (var trinket in trinkets)
        {
            if (trinket.IsCollected) continue;
            float dist = Vector3.Distance(player.position, trinket.transform.position);
            if (dist <= trinket.pickupRange + 0.5f)
            {
                Rect hintRect = new Rect(Screen.width / 2f - 130, Screen.height / 2f + 36, 260, 34);
                DrawPanel(hintRect, new Color(0f, 0f, 0f, 0.35f));
                DrawLabelWithShadow(new Rect(Screen.width / 2f - 120, Screen.height / 2f + 40, 240, 30),
                    "Press [E] to collect", new GUIStyle(hintStyle)
                    {
                        fontSize = 16,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = new Color(0.95f, 0.95f, 1f) }
                    });
                break;
            }
        }
    }

    void DrawMessages()
    {
        if (gameManager == null) return;
        string msg = gameManager.GetCurrentMessage();
        if (msg == null) return;

        float alpha = gameManager.GetMessageAlpha();
        messageStyle.normal.textColor = new Color(1f, 0.9f, 0.3f, alpha);
        GUI.Label(new Rect(Screen.width / 2f - 300, Screen.height * 0.3f, 600, 60), msg, messageStyle);
    }

    void DrawDamageFlash()
    {
        if (damageFlashAlpha <= 0) return;
        GUI.color = new Color(0.7f, 0f, 0f, damageFlashAlpha * 0.5f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawCollectFlash()
    {
        if (collectFlashAlpha <= 0) return;
        GUI.color = new Color(0.3f, 0.8f, 1f, collectFlashAlpha * 0.3f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawPauseMenu()
    {
        // Dim overlay
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;

        GUI.Label(new Rect(centerX - 150, centerY - 120, 300, 60), "PAUSED", titleStyle);

        if (GUI.Button(new Rect(centerX - 125, centerY - 25, 250, 50), "Resume", buttonStyle))
        {
            gameManager.ResumeGame();
        }

        if (GUI.Button(new Rect(centerX - 125, centerY + 40, 250, 50), "Restart", buttonStyle))
        {
            gameManager.RestartGame();
        }

        GUI.Label(new Rect(centerX - 150, centerY + 110, 300, 30),
            "A/D - Left/Right | W/S - Forward/Back | Shift - Sprint", hintStyle);
        GUI.Label(new Rect(centerX - 150, centerY + 135, 300, 30),
            "C - Crouch | F - Light | V - Light/Dark Mode | ESC - Pause", hintStyle);
    }

    void DrawDeathScreen()
    {
        // Strong dark overlay for readability
        GUI.color = new Color(0.03f, 0f, 0f, 0.93f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;

        Rect panelRect = new Rect(centerX - 320f, centerY - 190f, 640f, 360f);
        DrawPanel(panelRect, new Color(0f, 0f, 0f, 0.7f));
        DrawPanel(new Rect(panelRect.x + 8f, panelRect.y + 8f, panelRect.width - 16f, panelRect.height - 16f),
            new Color(0.18f, 0.04f, 0.04f, 0.35f));

        GUIStyle deathTitleStyle = new GUIStyle(titleStyle)
        {
            fontSize = 62,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.95f, 0.28f, 0.28f) }
        };
        DrawLabelWithShadow(new Rect(centerX - 280, centerY - 145, 560, 120), "YOU'VE BEEN\nCAUGHT", deathTitleStyle);
        titleStyle.normal.textColor = Color.white;

        GUIStyle deathMessageStyle = new GUIStyle(messageStyle)
        {
            fontSize = 44,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.93f, 0.93f, 0.93f) }
        };
        DrawLabelWithShadow(new Rect(centerX - 280, centerY - 20, 560, 60),
            "The echoes claimed another soul...", deathMessageStyle);

        if (GUI.Button(new Rect(centerX - 150, centerY + 65, 300, 62), "Try Again", new GUIStyle(buttonStyle)
        {
            fontSize = 30
        }))
        {
            gameManager.RestartGame();
        }
    }

    void DrawWinScreen()
    {
        // Dark overlay with slight gold tint
        GUI.color = new Color(0.03f, 0.03f, 0f, 0.9f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;

        Rect panelRect = new Rect(centerX - 320f, centerY - 200f, 640f, 390f);
        DrawPanel(panelRect, new Color(0f, 0f, 0f, 0.68f));
        DrawPanel(new Rect(panelRect.x + 8f, panelRect.y + 8f, panelRect.width - 16f, panelRect.height - 16f),
            new Color(0.2f, 0.2f, 0.05f, 0.2f));

        GUIStyle winTitleStyle = new GUIStyle(titleStyle)
        {
            fontSize = 60,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.3f, 1f, 0.5f) }
        };
        DrawLabelWithShadow(new Rect(centerX - 280, centerY - 145, 560, 90), "ESCAPED!", winTitleStyle);
        titleStyle.normal.textColor = Color.white;

        int minutes = (int)(gameManager.playTime / 60f);
        int seconds = (int)(gameManager.playTime % 60f);

        GUIStyle winMessageStyle = new GUIStyle(messageStyle)
        {
            fontSize = 34,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.95f, 0.95f, 0.95f) }
        };
        DrawLabelWithShadow(new Rect(centerX - 280, centerY - 60, 560, 46),
            $"Time: {minutes:00}:{seconds:00}", winMessageStyle);
        DrawLabelWithShadow(new Rect(centerX - 280, centerY - 15, 560, 46),
            $"Deaths: {gameManager.deaths}", winMessageStyle);

        if (GUI.Button(new Rect(centerX - 150, centerY + 70, 300, 62), "Play Again", new GUIStyle(buttonStyle)
        {
            fontSize = 30
        }))
        {
            gameManager.RestartGame();
        }

        DrawLabelWithShadow(new Rect(centerX - 260, centerY + 145, 520, 30),
            "You broke free from the Echo Maze!", new GUIStyle(hintStyle)
            {
                fontSize = 18,
                normal = { textColor = new Color(0.6f, 0.95f, 0.72f) }
            });
    }
}
