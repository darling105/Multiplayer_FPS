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
    [SyncVar] public bool isDeath;

    [Header("Movement Settings")]
    [SerializeField] float PlayerSpeed;
    [SerializeField] float PlayerSpeedMax;
    // [SerializeField] private float jumpHeight = 2f;

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
    Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (isLocalPlayer)
        {
            //It is local player.

            //Setup FPS camera.
            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0, 0.7f, 0);
            Camera.main.transform.rotation = Quaternion.identity;

            CanvasManager.instance.ChangePlayerState(true);
            CanvasManager.instance.UpdateHP(100, 100);
            CanvasManager.instance.localPlayer = this;
            CanvasManager.instance.AmmoCountText.text = ammoCount.ToString() + "/" + ammoCountMax.ToString();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            foreach (GameObject go in disableOnClient)
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
        if (Input.GetAxis("Horizontal") != Mathf.Epsilon || Input.GetAxis("Vertical") != Mathf.Epsilon)
        {
            Vector3 movementDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            movementDirection *= PlayerSpeed;
            movementDirection = Vector3.ClampMagnitude(movementDirection, PlayerSpeed);

            if (rb.velocity.magnitude < PlayerSpeedMax)
                rb.AddRelativeForce(movementDirection * Time.deltaTime * 100);
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
            if (ammoCount > 0 && !isDeath)
            {
                // Thực hiện lệnh bắn
                CmdRaycastAttack(Camera.main.transform.forward, Camera.main.transform.position);
            }
        }
    }
    private void Reload()
    {

    }
    [Command]
    private void CmdRaycastAttack(Vector3 clientCam, Vector3 clientCamPos)
    {
        if (ammoCount > 0 && !isDeath)
        {
            ammoCount--;
            TargetShoot();

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
        //Shoot success and change UI
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
        TargetGotDame();
        if (health < 1)
        {
            Die();
            NetworkClient.spawned[shooterID].GetComponent<PlayerMovementController>().kills++;
            NetworkClient.spawned[shooterID].GetComponent<PlayerMovementController>().TargetGotKill();

        }
    }
    [Server]
    public void Die()
    {
        deaths++;
        isDeath = true;
        Debug.Log("Player Die");
        TargetDie();
        RpcPlayerDie();
    }
    [Command]
    public void CmdRespawn()
    {
        if (isDeath)
        {
            health = healthMax;
            ammoCount = ammoCountMax;
            isDeath = false;
            TargetRespawn();
            RpcPlayerRespawn();
        }

    }
    [TargetRpc]
    void TargetRespawn()
    {
        CanvasManager.instance.ChangePlayerState(true);
        CanvasManager.instance.UpdateHP(health, healthMax);
        Cursor.lockState = CursorLockMode.Locked;
        transform.position = NetworkManager.singleton.GetStartPosition().position;

    }
    [TargetRpc]
    void TargetDie()
    {
        CanvasManager.instance.ChangePlayerState(!isDeath);
        Cursor.lockState = CursorLockMode.None;
        Debug.Log("You died");


    }

    [TargetRpc]
    void TargetGotKill()
    {
        Debug.Log("You got kill");
    }

    [TargetRpc]
    void TargetGotDame()
    {
        CanvasManager.instance.UpdateHP(health, healthMax);
        Debug.Log("We got hit");
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

    [ClientRpc]
    void RpcPlayerRespawn()
    {
        GetComponent<Collider>().enabled = true;
        foreach (GameObject item in disableOnDeath)
        {
            item.SetActive(true);
        }
    }
}



