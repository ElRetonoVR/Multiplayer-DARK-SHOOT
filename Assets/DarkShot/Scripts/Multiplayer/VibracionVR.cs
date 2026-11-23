using System.Collections;
using UnityEngine;

// Vibracion de los mandos con apagado automatico.
// OVRInput.SetControllerVibration mantiene la vibracion hasta que se apaga, asi que se usa una corrutina.
// Uso: VibracionVR.Pulso(OVRInput.Controller.RTouch, 0.05f);
public class VibracionVR : MonoBehaviour
{
    static VibracionVR instancia;
    float finIzquierda;
    float finDerecha;

    public static void Pulso(OVRInput.Controller mano, float segundos, float amplitud = 0.6f, float frecuencia = 0.8f)
    {
        if (instancia == null)
        {
            GameObject go = new GameObject("VibracionVR");
            instancia = go.AddComponent<VibracionVR>();
        }
        instancia.Iniciar(mano, segundos, amplitud, frecuencia);
    }

    void Iniciar(OVRInput.Controller mano, float segundos, float amplitud, float frecuencia)
    {
        float fin = Time.unscaledTime + segundos;
        if (mano == OVRInput.Controller.LTouch) finIzquierda = fin; else finDerecha = fin;
        OVRInput.SetControllerVibration(frecuencia, amplitud, mano);
        StartCoroutine(Apagar(mano, fin));
    }

    // Solo apaga si ningun pulso posterior alargo el tiempo de esa mano
    IEnumerator Apagar(OVRInput.Controller mano, float fin)
    {
        yield return new WaitForSecondsRealtime(segundosHasta(fin));
        float finActual = mano == OVRInput.Controller.LTouch ? finIzquierda : finDerecha;
        if (Time.unscaledTime >= finActual - 0.001f)
        {
            OVRInput.SetControllerVibration(0f, 0f, mano);
        }
    }

    static float segundosHasta(float fin)
    {
        return Mathf.Max(0f, fin - Time.unscaledTime);
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }
}
