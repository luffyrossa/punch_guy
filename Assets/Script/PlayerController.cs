using UnityEngine;
using UnityEngine.UI; // Import the namespace to use UI elements
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f; // Player movement speed
    public float pickUpDistance = 2f; // Maximum distance to pick up an enemy
    public Transform carryPosition; // Position where enemies will be stacked
    public Animator animator; // Reference to the player's Animator
    public Transform coinTarget; // Target where enemies are destroyed to give coins
    public float coinTargetRadius = 1.5f; // Radius of the coinTarget to destroy enemies
    public float stackHeight = 1f; // Height of enemy stacking
    public int stackCapacity = 5; // Maximum stacking capacity
    public float throwForce = 10f; // Force of throwing enemies
    public Transform throwTarget; // Target for throwing enemies
    public Text moneyText; // Reference to UI text to display earned money
    public Button changeColorButton; // Reference to button for color change
    public Color[] playerColors; // Array of available colors for the player
    public Renderer playerRenderer; // Player's renderer to change color

    private bool isCarryingEnemy = false;
    private List<GameObject> stackedEnemies = new List<GameObject>();
    private Vector3 lastMoveDirection;
    private bool isMoving = false;
    private bool isPunching = false;
    private int money = 0; // Variable to track earned money

    void Start()
    {
        // Bind the ChangeColorOnClick method to the button click event
        if (changeColorButton != null)
        {
            changeColorButton.onClick.AddListener(ChangeColorOnClick);
        }
    }

    void Update()
    {
        // Player movement by touch (for mobile devices)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // Calculate movement direction based on touch
            Vector3 touchPosition = Camera.main.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 10f));
            touchPosition.y = transform.position.y; // Maintain player's height
            Vector3 moveDirection = (touchPosition - transform.position).normalized;

            // Move the player in the calculated direction
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);

            // Update run animation state in Animator
            animator.SetBool("IsRunning", true);

            // Rotate player towards movement direction
            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                lastMoveDirection = moveDirection;
                isMoving = true;
            }
            else
            {
                isMoving = false;
            }

            // Apply inertia to stacked enemies' movement
            ApplyStackedCharactersInertia();
        }
        else
        {
            // If no touch, return to Idle animation
            animator.SetBool("IsRunning", false);
            isMoving = false;
        }

        // Input for punch on screen tap (for mobile devices)
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Punch();
        }
    }

    void Punch()
    {
        if (!isPunching)
        {
            isPunching = true;
            animator.SetTrigger("Punch"); // Activate punch animation

            // Check collision with nearby enemies tagged as "Enemy"
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickUpDistance);
            foreach (Collider col in hitColliders)
            {
                if (col.CompareTag("Enemy"))
                {
                    // Pick up the enemy
                    StackCharacter(col.gameObject);
                    break; // Pick only one enemy at a time (adjustable as needed)
                }
            }

            Invoke("ResetPunch", 1f); // Reset punch state after 1 second
        }
    }

    void StackCharacter(GameObject enemy)
    {
        if (stackedEnemies.Count >= stackCapacity) return;

        Rigidbody enemyRigidbody = enemy.GetComponent<Rigidbody>();
        if (enemyRigidbody != null)
        {
            enemyRigidbody.isKinematic = true;
        }

        Collider enemyCollider = enemy.GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        enemy.transform.parent = carryPosition;
        enemy.transform.localPosition = new Vector3(0, stackHeight * stackedEnemies.Count, 0); // Adjust height based on stacked count
        enemy.transform.localRotation = Quaternion.identity;
        stackedEnemies.Add(enemy);

        isCarryingEnemy = true;

        // Update stacked capacity
        UpdateStackCapacity();
    }

    void UpdateStackCapacity()
    {
        // Logic to update stack capacity, if necessary
        // Here you can implement any additional logic related to stack capacity
        Debug.Log("Enemy stacked! Current capacity: " + stackedEnemies.Count);
    }

    void ApplyStackedCharactersInertia()
    {
        if (isMoving)
        {
            foreach (GameObject enemy in stackedEnemies)
            {
                Rigidbody enemyRigidbody = enemy.GetComponent<Rigidbody>();
                if (enemyRigidbody != null && enemyRigidbody.isKinematic)
                {
                    Vector3 velocity = (transform.position - enemy.transform.position).normalized * moveSpeed;
                    enemyRigidbody.velocity = velocity;
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if player passed over coinTarget
        if (other.CompareTag("CoinTarget"))
        {
            if (isCarryingEnemy && stackedEnemies.Count > 0)
            {
                // Throw stacked enemies into coinTarget
                ThrowStackedEnemies();
            }
        }
    }

    void ThrowStackedEnemies()
    {
        List<GameObject> enemiesToRemove = new List<GameObject>(); // Temporary list to store enemies to remove

        foreach (GameObject enemy in stackedEnemies)
        {
            Rigidbody enemyRigidbody = enemy.GetComponent<Rigidbody>();
            if (enemyRigidbody != null)
            {
                enemyRigidbody.isKinematic = false;
                enemyRigidbody.velocity = Vector3.zero;
                enemyRigidbody.AddForce((throwTarget.position - enemy.transform.position).normalized * throwForce, ForceMode.Impulse);
            }

            Collider enemyCollider = enemy.GetComponent<Collider>();
            if (enemyCollider != null)
            {
                enemyCollider.enabled = true;
            }

            // Add enemy to removal list
            enemiesToRemove.Add(enemy);

            // Calculate money earned and add to total
            money += 100; // Example: earn 100 coins per enemy thrown into coinTarget
            UpdateMoneyText();
        }

        // Remove enemies from main list and destroy them
        foreach (GameObject enemyToRemove in enemiesToRemove)
        {
            stackedEnemies.Remove(enemyToRemove);
            Destroy(enemyToRemove, 2.0f); // Destroy enemy after 2 seconds
        }

        // Clear stacked enemies list
        stackedEnemies.Clear();
        isCarryingEnemy = false;
    }

    void UpdateMoneyText()
    {
        // Update UI text with current money
        if (moneyText != null)
        {
            moneyText.text = "Money: " + money.ToString();
        }
    }

    void ChangeColorOnClick()
    {
        // Check if enough money to change color (every 100 coins)
        if (money >= 100)
        {
            ChangePlayerColor();
            money -= 100; // Reduce spent money for color change
            UpdateMoneyText();
        }
    }

    void ChangePlayerColor()
    {
        // Logic to change player's color
        if (playerRenderer != null && playerColors.Length > 0)
        {
            // Choose a random color from available colors array
            Color newColor = playerColors[Random.Range(0, playerColors.Length)];
            playerRenderer.material.color = newColor;
            Debug.Log("Changed player color to: " + newColor.ToString());
        }
    }

    void ResetPunch()
    {
        isPunching = false;
    }
}
