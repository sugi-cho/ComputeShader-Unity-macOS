using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class GPUParticle : MonoBehaviour
{
    [System.Serializable]
    public struct ParticleData
    {
        public bool isActive;
        public Vector3 position;
        public Vector3 velocity;
        public Color color;
        public float size;
        public float duration;
    }
    [Header("Particle Params")]
    public int maxParticles = 1000000;
    public float lifeTime = 10f;
    public float startVelocity = 1f;
    public float damping = 10f;
    public float particleSize = 0.01f;

    [Header("Compute Shader")]
    public ComputeShader particleCompute;
    public string initFunc = "init";
    public string emitFunc = "emit";
    public string updateFunc = "update";

    public Material visualizer;

    const string propParticleBuffer = "_Particles";
    const string propPoolBuffer = "_Pool";
    const string propDeadBuffer = "_Dead";
    const string propActiveBuffer = "_Active";

    int numParticles;
    ComputeBuffer particleBuffer;
    ComputeBuffer activeBuffer;
    ComputeBuffer activeCountBuffer;
    ComputeBuffer poolBuffer;
    ComputeBuffer poolCountBuffer;
    int[] particleCounts;
    [SerializeField] int poolCount;
    [SerializeField] int currentCount;

    int initKernel;
    int emitKernel;
    int updateKernel;

    uint x;

    void Init()
    {
        uint y, z;
        initKernel = particleCompute.FindKernel(initFunc);
        emitKernel = particleCompute.FindKernel(emitFunc);
        updateKernel = particleCompute.FindKernel(updateFunc);
        particleCompute.GetKernelThreadGroupSizes(updateKernel, out x, out y, out z);

        numParticles = (int)((maxParticles / x) * x);

        particleBuffer = Helper.CreateComputeBuffer<ParticleData>(numParticles);

        activeBuffer = Helper.CreateComputeBuffer<int>(numParticles, ComputeBufferType.Append);
        activeBuffer.SetCounterValue(0);
        poolBuffer = Helper.CreateComputeBuffer<int>(numParticles, ComputeBufferType.Append);
        poolBuffer.SetCounterValue(0);

        particleCounts = new[] { 6, 0, 0, 0 };
        poolCountBuffer = Helper.CreateComputeBuffer<int>(4, ComputeBufferType.IndirectArguments);
        poolCountBuffer.SetData(particleCounts);
        activeCountBuffer = Helper.CreateComputeBuffer<int>(4, ComputeBufferType.IndirectArguments);
        activeCountBuffer.SetData(particleCounts);

        particleCompute.SetBuffer(initKernel, propParticleBuffer, particleBuffer);
        particleCompute.SetBuffer(initKernel, propDeadBuffer, poolBuffer);
        particleCompute.Dispatch(initKernel, numParticles / (int)x, 1, 1);
    }

    void UpdateParticle()
    {
        activeBuffer.SetCounterValue(0);
        particleCompute.SetFloat("_DT", Time.smoothDeltaTime);
        particleCompute.SetVector("_CamPos", Shader.GetGlobalVector("_CamPos"));
        particleCompute.SetFloat("_Damp", damping);
        particleCompute.SetBuffer(updateKernel, propParticleBuffer, particleBuffer);
        particleCompute.SetBuffer(updateKernel, propDeadBuffer, poolBuffer);
        particleCompute.SetBuffer(updateKernel, propActiveBuffer, activeBuffer);

        particleCompute.Dispatch(updateKernel, numParticles / (int)x, 1, 1);
    }

    public void EmitWithTexture(Texture posTex, Texture normTex, Texture colTex)
    {
        poolCountBuffer.SetData(particleCounts);
        ComputeBuffer.CopyCount(poolBuffer, poolCountBuffer, 4);
        poolCountBuffer.GetData(particleCounts);
        poolCount = particleCounts[1];

        particleCompute.SetInt("_PoolCount", poolCount);
        particleCompute.SetInt("_TexSize", posTex.height);
        particleCompute.SetFloat("_LifeTime", lifeTime);
        particleCompute.SetFloat("_StartVel", startVelocity);
        particleCompute.SetTexture(emitKernel, "_PosTex", posTex);
        particleCompute.SetTexture(emitKernel, "_NormTex", normTex);
        particleCompute.SetTexture(emitKernel, "_ColTex", colTex);
        particleCompute.SetFloat("_Size", particleSize);
        particleCompute.SetBuffer(emitKernel, propParticleBuffer, particleBuffer);
        particleCompute.SetBuffer(emitKernel, propPoolBuffer, poolBuffer);

        particleCompute.Dispatch(emitKernel, posTex.width / 8, posTex.height / 8, 1);
    }

    // Use this for initialization
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateParticle();
    }

    void OnRenderObject()
    {
        activeCountBuffer.SetData(particleCounts);
        ComputeBuffer.CopyCount(activeBuffer, activeCountBuffer, 4);
        activeCountBuffer.GetData(particleCounts);
        currentCount = particleCounts[1];

        visualizer.SetBuffer(propParticleBuffer, particleBuffer);
        visualizer.SetBuffer(propActiveBuffer, activeBuffer);
        visualizer.SetFloat("_LifeTime", lifeTime);
        visualizer.SetPass(0);
        Graphics.DrawProceduralIndirect(MeshTopology.Triangles, activeCountBuffer);
    }

    void OnDestroy()
    {
        new[] { particleBuffer, poolBuffer, activeBuffer, poolCountBuffer, activeCountBuffer }.ToList()
            .ForEach(buffer =>
            {
                if (buffer != null)
                    buffer.Release();
            });
    }
}