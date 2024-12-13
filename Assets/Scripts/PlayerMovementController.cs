using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


public class PlayerMovementController : NetworkBehaviour, IDamageable
{
    [Header("Player Stats")]
    [SyncVar] public int health = 100;
    [SyncVar] public int healthMax = 100;
    [SyncVar] public int kills;
    [SyncVar] public int deaths;
    [SyncVar] private bool isDead = false;

    [Header("Movement Settings")]
    public float playerSpeed = 5f;
    public float jumpHeight = 2f;
    public float groundDistance = 0.4f;
    public float gravity = -9.81f;
    public Transform groundCheck;
    public LayerMask groundMask;

    private Rigidbody rb;
    private bool isGrounded;

    [Header("Look Settings")]
    [SerializeField] float sensitivityX = 1f;
    [SerializeField] float sensitivityY = 1f;
    [SerializeField] float maxCameraX = 80f;
    [SerializeField] float minCameraX = -80f;
    float xRotation;

    [Header("Weapon")]

    [SyncVar] int ammoCountMax = 100;
    [SyncVar] public int ammoCount = 100;
    [SerializeField] Transform weaponMuzzle;
    [SerializeField] Transform weaponArm;
    [SerializeField] int weaponDamage = 2;
    [SerializeField] GameObject bulletHolePrefab;
    [SerializeField] GameObject bulletFXPrefab;
    [SerializeField] GameObject bulletBloodFXPrefab;

    [Header("GFX")]
    [SerializeField] GameObject[] disableOnClient;
    [SerializeField] GameObject[] disableOnDeath;
    [Header("VFX")]
    AudioSource audioSource;
    [SerializeField] protected AudioClip shootClip;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        if (isLocalPlayer)
        {
            //It is local player.

            //Setup FPS camera.
            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0, 0.7f, 0.5f);
            Camera.main.transform.rotation = Quaternion.identity;

            CanvasManager.instance.ChangePlayerState(true);
            CanvasManager.instance.UpdateHP(100, 100);
            CanvasManager.instance.localPlayer = this;
            CanvasManager.instance.AmmoCountText.text = ammoCount.ToString() + "/" + ammoCountMax.ToString();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            foreach (var go in disableOnClient)
            {
                go.SetActive(false);
            }
        }
        else
        {
            //Its not local player.
            rb.isKinematic = true;
        }
    }
    void Update()
    {
        if (!isLocalPlayer)
            return;
        Move();
        Fire();

    }


    private void Move()
    {
        //Movement
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Horizontal movement
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        Vector3 moveVelocity = move * playerSpeed;

        rb.velocity = new Vector3(moveVelocity.x, rb.velocity.y, moveVelocity.z);

        // Jumping
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, Mathf.Sqrt(jumpHeight * -2f * gravity), rb.velocity.z);
        }

        // Apply gravity manually (if needed, usually handled by Rigidbody)
        if (!isGrounded)
        {
            rb.AddForce(Vector3.up * gravity * Time.deltaTime, ForceMode.Acceleration);
        }
        //Rotation
        float mouseX = Input.GetAxis("Mouse X") * sensitivityX * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivityY * Time.deltaTime;
        transform.Rotate(Vector3.up, mouseX);
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minCameraX, maxCameraX);
        weaponArm.localEulerAngles = new Vector3(xRotation, 0, 0);
        Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }
    private void Fire()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Nếu còn đạn và nhân vật chưa chết
            if (ammoCount > 0 && !isDead)
            {
                // Thực hiện lệnh bắn
                CmdRaycastAttack(Camera.main.transform.forward, Camera.main.transform.position);
                audioSource.PlayOneShot(shootClip);
            }
        }
    }
    private void Reload()
    {

    }
    [Command]
    private void CmdRaycastAttack(Vector3 clientCam, Vector3 clientCamPos)
    {

        if (!isDead)
        {
            // ammoCount--;
            // TargetShoot();

            Ray ray = new Ray(clientCamPos, clientCam * 500);
            Debug.DrawRay(clientCamPos, clientCam * 500, Color.red, 2f);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("Server: Player shot: " + hit.collider.name);
                if (hit.collider.CompareTag("Player"))
                {
                    RpcPlayerFiredEntity(GetComponent<NetworkIdentity>().netId, hit.collider.GetComponent<NetworkIdentity>().netId, hit.point, hit.normal);
                    hit.collider.GetComponent<PlayerMovementController>().Damage(weaponDamage, GetComponent<NetworkIdentity>().netId);

                }
                else
                {
                    RpcPlayerFired(GetComponent<NetworkIdentity>().netId, hit.point, hit.normal);
                }

            }
        }
    }

    [ClientRpc]
    void RpcPlayerFired(uint shooterID, Vector3 impactPos, Vector3 impactRot)
    {
        Instantiate(bulletHolePrefab, impactPos + impactRot * 0.1f, Quaternion.LookRotation(impactRot));
        Instantiate(bulletFXPrefab, impactPos, Quaternion.LookRotation(impactRot));
        NetworkClient.spawned[shooterID].GetComponent<PlayerMovementController>().MuzzleFlash();
    }
    [ClientRpc]
    void RpcPlayerFiredEntity(uint shooterID, uint targetID, Vector3 impactPos, Vector3 impactRot)
    {
        Instantiate(bulletHolePrefab, impactPos + impactRot * 0.1f, Quaternion.LookRotation(impactRot), NetworkClient.spawned[targetID].transform);
        Instantiate(bulletBloodFXPrefab, impactPos, Quaternion.LookRotation(impactRot));
        NetworkClient.spawned[shooterID].GetComponent<PlayerMovementController>().MuzzleFlash();
    }

    [TargetRpc]
    void TargetShoot()
    {
        CanvasManager.instance.AmmoCountText.text = ammoCount.ToString() + "/" + ammoCountMax.ToString();
    }

    public void MuzzleFlash()
    {
        weaponMuzzle.GetComponent<ParticleSystem>().Play();
    }
    [Server]    
    public void Damage(int amount, uint shooterID)
    {
        health -= amount;
        RpcTakeDame(health);
        if (health < 1)
        {
            Die();
            NetworkClient.spawned[shooterID].GetComponent<PlayerMovementController>().kills++;
        }
    }
    public void Die()
    {

        deaths++;
        isDead = true;
        Debug.Log("Player Dieeeeeeeeeeeeeeeeeeeeee");
        RpcPlayerDie();
        HoiSinh();
    }
    [ClientRpc]
    void RpcTakeDame(int newHealth)
    {
        if (isOwned)
        {
            CanvasManager.instance.UpdateHP(newHealth, healthMax);
            Debug.Log("We got hit");
        }
    }
    [ClientRpc]
    void RpcPlayerDie()
    {
        GetComponent<Collider>().enabled = false;
        foreach (GameObject item in disableOnDeath)
        {
            item.SetActive(false);
        }
    }
    public bool IsDead()
    {
        return isDead;
    }

    public IEnumerator ReSpawn()
    {
        yield return new WaitForSeconds(5);
        isDead = false;
        health = healthMax;
        CanvasManager.instance.UpdateHP(health, healthMax);
        transform.position = NetworkManager.singleton.GetStartPosition().position;
        GetComponent<Collider>().enabled = true;
        foreach (var item in disableOnDeath)
        {
            item.SetActive(true);
        }
    }
    [ClientRpc]
    public void HoiSinh()
    {
        StartCoroutine(ReSpawn());
    }

}