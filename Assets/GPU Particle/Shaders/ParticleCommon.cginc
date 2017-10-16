#ifndef GPUPARTICLE_INCLUDED
#define GPUPARTICLE_INCLUDED

struct ParticleData{
	bool isActive;
	float3 position;
	float3 velocity;
	float4 color;
	float size;
	float duration;
};

#endif