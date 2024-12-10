using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FirstPersonMovement : MonoBehaviour
{
    public float initialSpeed = 100f; // Vitesse initiale du joueur
    public float speedGrowthFactor = 1.1f; // Facteur de croissance exponentielle de la vitesse
    public float dashSpeed = 300f; // Vitesse du dash
    public float dashDuration = 0.2f; // Durée du dash en secondes
    public float minY = 30f; // Limite basse pour l'axe Y
    public float maxY = 55f; // Limite haute pour l'axe Y
    public bool IsRunning;
    public List<Func<float>> speedOverrides;

    private bool isDashing = false;
    private Vector3 dashDirection;
    private float dashTimeRemaining;

    public Image fadeImage; // L'image qui va devenir noire
    public TMP_Text fadeText;
    public float fadeDuration = 2f; // Durée du fondu en secondes

    private bool isFading = false; // Vérifie si le fondu est en cours
    private float fadeTimer = 0f; // Chronomètre pour gérer le fondu

    Rigidbody rigidbody;
    ParticleSystem lesFlechesDeNaruto;

    void Awake()
    {
        // Initialisation des composants
        rigidbody = GetComponent<Rigidbody>();
        lesFlechesDeNaruto = FindFirstObjectByType<ParticleSystem>();
    }

    private void Start()
    {
        // Assurez-vous que l'image et le texte sont invisibles au départ
        fadeImage.color = new Color(0, 0, 0, 0); // Alpha à 0
        fadeText.color = new Color(1, 1, 1, 0); // Alpha à 0 pour le texte en blanc
    }

    void FixedUpdate()
    {
        if (!isFading)
        {
            // Calculer la vitesse actuelle (elle augmente exponentiellement avec le temps)
            float speed = initialSpeed * Mathf.Pow(speedGrowthFactor, Time.time);

            // Gestion du déplacement normal
            float velocityZ = speed;
            float velocityX = Input.GetAxis("Horizontal") * speed * 0.3f; // Vitesse fixe sur l'axe X
            float velocityY = Input.GetAxis("Vertical") * speed * 0.3f;

            // Limiter la position sur l'axe Y
            float newY = Mathf.Clamp(rigidbody.position.y + velocityY * Time.fixedDeltaTime, minY, maxY);

            if (isDashing)
            {
                rigidbody.velocity = dashDirection * dashSpeed;
                dashTimeRemaining -= Time.fixedDeltaTime;

                if (dashTimeRemaining <= 0)
                {
                    isDashing = false;
                }
            }
            else
            {
                rigidbody.velocity = new Vector3(velocityX, 0, velocityZ);
            }

            rigidbody.position = new Vector3(rigidbody.position.x, newY, rigidbody.position.z);
        }
        else
        {
            // Gérer le fade-in
            fadeTimer += Time.deltaTime;
            float alpha = Mathf.Clamp01(fadeTimer / fadeDuration);

            // Appliquer le fade à l'image et au texte
            fadeImage.color = new Color(0, 0, 0, alpha);
            fadeText.color = new Color(1, 1, 1, alpha);
        }
    }

    void StartDash()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        dashDirection = new Vector3(horizontalInput, verticalInput, 0f).normalized;

        if (dashDirection.magnitude > 0)
        {
            isDashing = true;
            dashTimeRemaining = dashDuration;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision détectée, démarrage du fade-in.");

        lesFlechesDeNaruto.Stop();
        rigidbody.velocity = Vector3.zero;

        isFading = true;
        fadeTimer = 0f; // Réinitialiser le timer pour le fade-in
    }
}
