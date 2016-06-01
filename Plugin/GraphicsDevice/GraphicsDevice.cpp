﻿#include "pch.h"
#include "gdInternal.h"

namespace gd {

int GraphicsDevice::GetTexelSize(TextureFormat format)
{
    switch (format)
    {
    case TextureFormat::RGBAu8:  return 4;
    case TextureFormat::RGu8:    return 2;
    case TextureFormat::Ru8:     return 1;

    case TextureFormat::RGBAf16:
    case TextureFormat::RGBAi16: return 8;
    case TextureFormat::RGf16:
    case TextureFormat::RGi16:   return 4;
    case TextureFormat::Rf16:
    case TextureFormat::Ri16:    return 2;

    case TextureFormat::RGBAf32:
    case TextureFormat::RGBAi32: return 16;
    case TextureFormat::RGf32:
    case TextureFormat::RGi32:   return 8;
    case TextureFormat::Rf32:
    case TextureFormat::Ri32:    return 4;
    }
    return 0;
}


static GraphicsDevice *g_gfx_device;

GraphicsDevice* CreateGraphicsDevice(DeviceType type, void *device_ptr)
{
    switch (type) {
#ifdef gdSupportD3D9
    case DeviceType::D3D9:
        g_gfx_device = CreateGraphicsDeviceD3D9(device_ptr);
        break;
#endif
#ifdef gdSupportD3D11
    case DeviceType::D3D11:
        g_gfx_device = CreateGraphicsDeviceD3D11(device_ptr);
        break;
#endif
#ifdef gdSupportD3D12
    case DeviceType::D3D12:
        g_gfx_device = CreateGraphicsDeviceD3D12(device_ptr);
        break;
#endif
#ifdef gdSupportOpenGL
    case DeviceType::OpenGL:
        g_gfx_device = CreateGraphicsDeviceOpenGL(device_ptr);
        break;
#endif
#ifdef gdSupportVulkan
    case DeviceType::Vulkan:
        g_gfx_device = CreateGraphicsDeviceVulkan(device_ptr);
        break;
#endif
    }
    return g_gfx_device;
}

GraphicsDevice* GetGraphicsDevice()
{
    return g_gfx_device;
}

void ReleaseGraphicsDevice()
{
    if (g_gfx_device) {
        delete g_gfx_device;
        g_gfx_device = nullptr;
    }
}

} // namespace gd
