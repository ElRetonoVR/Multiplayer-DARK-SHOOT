using UnityEngine;

public class ChangeUI : MonoBehaviour
{
    public GameObject imagen1;
    public GameObject imagen2;
    public GameObject imagen3;
    public GameObject imagen4;
    public GameObject imagen5;
    public GameObject imagen6;

    public void CambiarImagen(int vida)
    {
        imagen1.SetActive(false);
        imagen2.SetActive(false);
        imagen3.SetActive(false);
        imagen4.SetActive(false);
        imagen5.SetActive(false);
        imagen6.SetActive(false);

        if (vida > 80)
            imagen1.SetActive(true);
        else if (vida > 60)
            imagen2.SetActive(true);
        else if (vida > 40)
            imagen3.SetActive(true);
        else if (vida > 20)
            imagen4.SetActive(true);
        else if (vida > 0)
            imagen5.SetActive(true);
        else
            imagen6.SetActive(true);
    }
}
