using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowScript : MonoBehaviour
{
    public float speed = 3.5f;
    public int arrowDamage = 1;
    
    private WeaponScript weaponScript;

    void Start()
    {
        weaponScript = GetComponent<WeaponScript>();
        
        if (weaponScript == null)
        {
            weaponScript = gameObject.AddComponent<WeaponScript>();
            weaponScript.weaponDamage = arrowDamage;
            weaponScript.isSword = false;
        }
    }

    void Update()
    {
        MoveArrow();
        
        Destroy(gameObject, 5);
    }
    
    private void MoveArrow()
    {
        Vector3 rotation = transform.rotation.eulerAngles;
        
        if (rotation.z == 90)
        {
            transform.Translate(transform.up * -speed * Time.deltaTime);
        }
        else if (rotation.z == 270)
        {
            transform.Translate(transform.up * speed * Time.deltaTime);
        }
        else if (rotation.z == 0)
        {
            transform.Translate(transform.right * speed * Time.deltaTime);
        }
        else if (rotation.z == 180)
        {
            transform.Translate(transform.right * -speed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Obstacle"))
        {
            Destroy(gameObject);
        }
        
        // Note: Enemy collisions are handled by the WeaponScript component
    }
}
