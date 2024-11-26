using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class Weapon : NetworkBehaviour
{
    [SerializeField] Transform cameraPosition;
 private void Update(){
    transform.position = cameraPosition.position;
 }
}
