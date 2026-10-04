using UnityEngine;

public class RandomTableGenerator : MonoBehaviour
{
    public Material[] tableback; // Array to hold the materials
    public Material[] tableBorder; // Array to hold the materials
    public GameObject[] tableCom;
    [SerializeField] private int totalTable;
    public static RandomTableGenerator instance;
    void Start()
    {
       
        if(instance == null)
        {
            instance = this;
        }
        if(staticVariables.selectedTable==-1)
        {
            SetTable(0);
        }
        else
        {
            SetTable(staticVariables.selectedTable);
        }
       
        
     
    }

    public void SetTable(int x)
    {
        tableCom[0].GetComponent<Renderer>().material = tableback[x];
        tableCom[1].GetComponent<Renderer>().material = tableback[x];
        tableCom[2].GetComponent<Renderer>().material = tableBorder[x];
    }
}


