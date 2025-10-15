#pragma once

/*
 * For DXBC and DXIL shaders, use the following register order:
 *
 * For vertex shaders:
 *
 * - (t[n], space0): Sampled textures, followed by storage textures, followed
 *   by storage buffers
 * - (s[n], space0): Samplers with indices corresponding to the sampled
 *   textures
 * - (b[n], space1): Uniform buffers
 */

#define SDL_VS_UNIFORM_BUFFER(index, var) cbuffer cbuffer##index : register(b##index, space1) { var; };

/*
 * For pixel shaders:
 *
 * - (t[n], space2): Sampled textures, followed by storage textures, followed
 *   by storage buffers
 * - (s[n], space2): Samplers with indices corresponding to the sampled
 *   textures
 * - (b[n], space3): Uniform buffers
 */

#define SDL_FS_UNIFORM_BUFFER(index, var) cbuffer cbuffer##index : register(b##index, space3) { var; };
#define SDL_FS_SAMPLER(index, var) var : register(s##index, space2);
#define SDL_FS_RESOURCE(index, var) var : register(t##index, space2);

/*
 * For DXBC and DXIL shaders, use the following register order:
 *
 * - (t[n], space0): Sampled textures, followed by read-only storage textures,
 *   followed by read-only storage buffers
 * - (u[n], space1): Read-write storage textures, followed by read-write
 *   storage buffers
 * - (b[n], space2): Uniform buffers
 */

#define SDL_CS_UNIFORM_BUFFER(index, var) cbuffer cbuffer##index : register(b##index, space2) { var; };
#define SDL_CS_SAMPLER(index, var) var: register(s##index, space0);
#define SDL_CS_RESOURCE(index, var) var: register(t##index, space0);
#define SDL_CS_RW_RESOURCE(index, var) var: register(u##index, space1);

/**
 * A structure specifying the parameters of an indexed dispatch command.
 *
 * \since This struct is available since SDL 3.2.0.
 *
 * \sa SDL_DispatchGPUComputeIndirect
 */
struct SDL_GPUIndirectDispatchCommand
{
    uint groupcount_x; /**< The number of local workgroups to dispatch in the X dimension. */
    uint groupcount_y; /**< The number of local workgroups to dispatch in the Y dimension. */
    uint groupcount_z; /**< The number of local workgroups to dispatch in the Z dimension. */
};