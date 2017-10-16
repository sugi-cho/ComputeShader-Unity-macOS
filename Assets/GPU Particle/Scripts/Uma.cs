using UnityEngine;

public class Uma : MonoBehaviour
{
    public Texture posTex;
    public Texture normTex;
    public Texture colTex;

    GPUParticle particle;

    void Start(){
        InvokeRepeating("Emit", 0f, 10f);
    }

    void Emit()
    {
        if (particle == null)
            particle = GetComponent<GPUParticle>();
        particle.EmitWithTexture(posTex, normTex, colTex);
    }
}
