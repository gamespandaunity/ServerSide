using UnityEngine;

[CreateAssetMenu(fileName = "Car", menuName = "NewCarData")]
public class Car : ScriptableObject
{
    public string carName;
    public float engine; // Max Torgue , Normal 560
    public float maxSpeed; // Max Speed , Normal 350
    public float control; // Brakes , Normal 4000
    public int price;
}
